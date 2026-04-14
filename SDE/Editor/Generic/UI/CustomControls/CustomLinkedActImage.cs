using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ErrorManager;
using GRF.FileFormats.ActFormat;
using GRF.IO;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.TabsMakerCore;
using TokeiLibrary;
using Utilities.Services;
using Imaging = ActImaging.Imaging;

namespace SDE.Editor.Generic.UI.CustomControls
{
    public class CustomLinkedActImage<TKey, TValue> : ICustomControl<TKey, TValue> where TValue : Database.Tuple
    {
        private readonly string _grfPath;
        private readonly Image _image;
        private readonly TextBox _textBox;
        private readonly ScrollViewer _viewer;
        private readonly DispatcherTimer _timer;

        private Act _act;
        private int _actionIndex = 0;
        private int _frameIndex;
        private int _redirect;
        private GDbTabWrapper<TKey, TValue> _tab;

        public CustomLinkedActImage(TextBox textBox, string grfPath, int row, int col, int rSpan, int cSpan)
        {
            _image = new Image
            {
                Stretch = Stretch.None,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _image.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.NearestNeighbor);

            _viewer = new ScrollViewer();
            WpfUtilities.SetGridPosition(_viewer, row, rSpan, col, cSpan);

            _viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            _viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            _viewer.Content = _image;

            _textBox = textBox;
            _grfPath = grfPath.Trim('\\');

            _timer = new DispatcherTimer(DispatcherPriority.Background);
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += _timer_Tick;

            _textBox.TextChanged += _textBox_TextChanged;
            _viewer.SizeChanged += delegate { _viewer.MaxHeight = _viewer.ActualHeight; };
            _viewer.Unloaded += delegate { _timer.Stop(); };
        }

        #region ICustomControl<TKey,TValue> Members
        public void Init(GDbTabWrapper<TKey, TValue> tab, DisplayableProperty<TKey, TValue> dp)
        {
            _tab = tab;
            _tab.PropertiesGrid.Children.Add(_viewer);
        }
        #endregion

        public void Update(ReadableTuple<int> tuple, int redirect)
        {
            _redirect = redirect;
            _textBox_TextChanged(tuple, null);
        }

        private void _textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _threadStart(sender, e);
        }

        private void _timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_act == null)
                {
                    _timer.Stop();
                    return;
                }

                if (_actionIndex < 0 || _actionIndex >= _act.NumberOfActions)
                {
                    _cancelAnimation();
                    return;
                }

                if (_act[_actionIndex].NumberOfFrames <= 0)
                {
                    _cancelAnimation();
                    return;
                }

                _frameIndex++;
                if (_frameIndex >= _act[_actionIndex].NumberOfFrames)
                {
                    _frameIndex = 0;
                }

                _renderFrame(_frameIndex);
            }
            catch (Exception err)
            {
                _cancelAnimation();
                ErrorHandler.HandleException(err);
            }
        }

        private void _threadStart(object sender, TextChangedEventArgs e)
        {
            try
            {
                string text = _textBox.Text;
                var tuple = e == null ? sender as ReadableTuple<int> : _tab._listView.SelectedItem as ReadableTuple<int>;

                if (tuple != null)
                {
                    if (e != null)
                    {
                        _redirect = tuple.GetIntNoThrow(ServerMobAttributes.Sprite);
                    }

                    if (_redirect > 0)
                    {
                        var db = _tab.GetMetaTable<int>(ServerDbs.Mobs);
                        tuple = db.TryGetTuple(_redirect);
                        _redirect = 0;

                        if (tuple != null)
                        {
                            text = tuple.GetValue<string>(ServerMobAttributes.ClientSprite);
                        }
                    }
                }

                if (String.IsNullOrEmpty(text))
                {
                    _cancelAnimation();
                    return;
                }

                string actPath = EncodingService.FromAnyToDisplayEncoding(GrfPath.Combine(_grfPath, text) + ".act");
                string sprPath = EncodingService.FromAnyToDisplayEncoding(GrfPath.Combine(_grfPath, text) + ".spr");

                byte[] actData = _tab.ProjectDatabase.MetaGrf.GetData(actPath);
                byte[] sprData = _tab.ProjectDatabase.MetaGrf.GetData(sprPath);

                if (actData == null || sprData == null)
                {
                    _cancelAnimation();
                    return;
                }

                _timer.Stop();

                _act = new Act(actData, sprData);
                _actionIndex = 0;
                _frameIndex = 0;

                if (_act.NumberOfActions <= 0)
                {
                    _cancelAnimation();
                    return;
                }

                if (_act[_actionIndex].NumberOfFrames <= 0)
                {
                    _cancelAnimation();
                    return;
                }

                _image.Tag = text;
                _renderFrame(0);

                if (_isAnimatedAction(_act, _actionIndex))
                {
                    _timer.Interval = TimeSpan.FromMilliseconds(_getAnimationDelay(_act, _actionIndex));
                    _timer.Start();
                }
            }
            catch (Exception err)
            {
                _cancelAnimation();
                ErrorHandler.HandleException(err);
            }
        }

        private void _renderFrame(int frameIndex)
        {
            try
            {
                if (_act == null)
                {
                    _image.Source = null;
                    return;
                }

                if (_actionIndex < 0 || _actionIndex >= _act.NumberOfActions)
                {
                    _image.Source = null;
                    return;
                }

                if (_act[_actionIndex].NumberOfFrames <= 0)
                {
                    _image.Source = null;
                    return;
                }

                int currentFrame = frameIndex % _act[_actionIndex].NumberOfFrames;

                if (_act[_actionIndex].Frames[currentFrame].NumberOfLayers <= 0)
                {
                    _image.Source = null;
                    return;
                }

                ImageSource source = Imaging.GenerateImage(_act, _actionIndex, currentFrame, BitmapScalingMode.NearestNeighbor);
                _image.Source = source;
            }
            catch
            {
                _image.Source = null;
            }
        }

        private bool _isAnimatedAction(Act act, int actionIndex)
        {
            try
            {
                if (act == null)
                    return false;

                if (actionIndex < 0 || actionIndex >= act.NumberOfActions)
                    return false;

                if (act[actionIndex].NumberOfFrames <= 1)
                    return false;

                float speed = act[actionIndex].AnimationSpeed;

                if (float.IsNaN(speed))
                    return false;

                return (int)(speed * 24) > 0;
            }
            catch
            {
                return false;
            }
        }

        private int _getAnimationDelay(Act act, int actionIndex)
        {
            try
            {
                if (act == null)
                    return 100;

                if (actionIndex < 0 || actionIndex >= act.NumberOfActions)
                    return 100;

                float speed = act[actionIndex].AnimationSpeed;

                if (float.IsNaN(speed))
                    return 100;

                int delay = (int)(speed * 20);

                if (delay <= 0)
                    return 100;

                return delay;
            }
            catch
            {
                return 100;
            }
        }

        private void _cancelAnimation()
        {
            _timer.Stop();
            _act = null;
            _frameIndex = 0;

            _image.BeginDispatch(delegate {
                _image.Source = null;
            });
        }
    }
}
