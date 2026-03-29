using Database;
using ErrorManager;
using GRF.FileFormats.LubFormat;
using GRF.IO;
using SDE.ApplicationConfiguration;
using SDE.Editor.Engines.BackupsEngine;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers.Generic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lua;
using Lua.Structure;
using Utilities;
using Utilities.Services;
using System.Linq;

namespace SDE.Editor.Generic.Parsers
{
    public sealed class DbIOClientQuests
    {
        public static void Loader(DbDebugItem<int> debug, AbstractDb<int> db)
        {
            string file = ProjectConfiguration.ClientQuest;

            if (string.IsNullOrEmpty(file))
            {
                Debug.Ignore(() => DbDebugHelper.OnUpdate(db.DbSource, null, "Client quest table will not be loaded."));
                return;
            }

            string ext = Path.GetExtension(file);

            if (ext.Equals(".txt", StringComparison.OrdinalIgnoreCase))
            {
                _loadDataQuest(db, file);   // 기존 txt 로더 그대로 사용
                return;
            }

            if (ext.Equals(".lub", StringComparison.OrdinalIgnoreCase) || ext.Equals(".lua", StringComparison.OrdinalIgnoreCase))
            {
                _loadDataQuestLub(db, file);
                return;
            }

            Debug.Ignore(() => DbDebugHelper.OnUpdate(db.DbSource, file, "Unsupported client quest file format."));
        }

        public static void Writer(DbDebugItem<int> debug, AbstractDb<int> db)
        {
            //  Block to write Quest.lub 
            /*
            try
            {
                string path = ProjectConfiguration.ClientQuest;

                if (!string.IsNullOrEmpty(path))
                {
                    string ext = Path.GetExtension(path);

                    if (ext.Equals(".lub", StringComparison.OrdinalIgnoreCase) ||
                        ext.Equals(".lua", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }

                if (db.Table.Commands.CommandIndex == -1 &&
                    !db.IsModified) return;

                db.ProjectDatabase.MetaGrf.Clear();
                // string path = ProjectConfiguration.ClientQuest;

                _dbClientQuestWrite(db.ProjectDatabase, db, path);

                try
                {
                    db.ProjectDatabase.MetaGrf.SaveAndReload();
                }
                catch (OperationCanceledException)
                {
                    ErrorHandler.HandleException("Failed to save the client files.");
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
            */
        }

        private static void _dbClientQuestWrite(SdeDatabase gdb, AbstractDb<int> db, string path)
        {
            if (path == null || gdb.MetaGrf.GetData(path) == null)
            {
                Debug.Ignore(() => DbDebugHelper.OnWriteStatusUpdate(ServerDbs.CQuests, "data\\questid2display.txt", null, "Table not saved."));
                return;
            }

            BackupEngine.Instance.BackupClient(path, gdb.MetaGrf);

            //string tmpFilename = Path.Combine(SdeAppConfiguration.TempPath, Path.GetFileName(path));
            Encoding encoder = EncodingService.DisplayEncoding;
            byte[] tmpBuffer;
            byte[] lineFeedByte = encoder.GetBytes(SdeStrings.LineFeed);
            byte[] doubleLineFeedByte = encoder.GetBytes(SdeStrings.LineFeed + SdeStrings.LineFeed);

            using (MemoryStream memStream = new MemoryStream())
            {
                IEnumerable<ReadableTuple<int>> items = gdb.GetDb<int>(ServerDbs.CQuests).Table.GetSortedItems();

                foreach (ReadableTuple<int> item in items)
                {
                    tmpBuffer = encoder.GetBytes(
                        item.GetValue<int>(ClientQuestsAttributes.Id) + "#" +
                        item.GetValue<string>(ClientQuestsAttributes.Name) + "#" +
                        item.GetValue<string>(ClientQuestsAttributes.SG) + "#" +
                        item.GetValue<string>(ClientQuestsAttributes.QUE) + "#" +
                        "\r\n" + item.GetValue<string>(ClientQuestsAttributes.FullDesc) + "#" +
                        "\r\n" + item.GetValue<string>(ClientQuestsAttributes.ShortDesc) + "#");

                    memStream.Write(tmpBuffer, 0, tmpBuffer.Length);
                    memStream.Write(doubleLineFeedByte, 0, doubleLineFeedByte.Length);
                }

                memStream.Write(lineFeedByte, 0, lineFeedByte.Length);

                tmpBuffer = new byte[memStream.Length];
                Buffer.BlockCopy(memStream.GetBuffer(), 0, tmpBuffer, 0, tmpBuffer.Length);

                //File.WriteAllBytes(tmpFilename, tmpBuffer);
            }

            string copyPath = path;

            try
            {
                gdb.MetaGrf.SetData(copyPath, tmpBuffer);
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }

            Debug.Ignore(() => DbDebugHelper.OnWriteStatusUpdate(ServerDbs.CQuests, db.ProjectDatabase.MetaGrf.FindTkPath(path), null, "Saving client table (questdb)."));
        }

        private static bool _isKorean(string value)
        {
            return value != EncodingService.Korean.GetString(EncodingService.Ansi.GetBytes(value));
        }

        public static void SetQuestValue(Table<int, ReadableTuple<int>> table, ReadableTuple<int> tuple, string[] elements, int id)
        {
            string value = tuple.GetValue<string>(ClientQuestsAttributes.AttributeList[id]);

            if (value == "")
            {
                if (elements[id] == "")
                    return;

                table.Commands.Set(tuple, ClientQuestsAttributes.AttributeList[id], elements[id]);
            }
            else if (elements[id] == "")
            {
                //table.Set(tuple.Key, ClientQuestsAttributes.AttributeList[id], value);
            }
            else if (_isKorean(value))
            {
                if (elements[id] == value)
                    return;

                table.Commands.Set(tuple, ClientQuestsAttributes.AttributeList[id], elements[id]);
            }
            else if (_isKorean(elements[id]))
            {
                //table.Set(tuple.Key, ClientQuestsAttributes.AttributeList[id], value);
            }
        }

        public static void SetQuestValue(Table<int, ReadableTuple<int>> table, ReadableTuple<int> tuple, string element, int id)
        {
            string value = tuple.GetValue<string>(ClientQuestsAttributes.AttributeList[id]);

            if (value == "")
            {
                table.Commands.Set(tuple, ClientQuestsAttributes.AttributeList[id], element);
            }
            else if (element == "")
            {
                //table.Set(tuple.Key, ClientQuestsAttributes.AttributeList[id], value);
            }
            else if (_isKorean(value))
            {
                table.Commands.Set(tuple, ClientQuestsAttributes.AttributeList[id], element);
            }
            else if (_isKorean(element))
            {
                //table.Set(tuple.Key, ClientQuestsAttributes.AttributeList[id], value);
            }
        }

        private static void _loadDataQuest(AbstractDb<int> db, string file)
        {
            var table = db.Table;
            TextFileHelper.LatestFile = file;

            try
            {
                foreach (string[] elements in TextFileHelper.GetElementsInt(db.ProjectDatabase.MetaGrf.GetData(file)))
                {
                    int itemId = Int32.Parse(elements[0]);

                    table.SetRaw(itemId, ClientQuestsAttributes.Name, elements[1]);
                    table.SetRaw(itemId, ClientQuestsAttributes.SG, elements[2]);
                    table.SetRaw(itemId, ClientQuestsAttributes.QUE, elements[3]);
                    table.SetRaw(itemId, ClientQuestsAttributes.FullDesc, elements[4]);
                    table.SetRaw(itemId, ClientQuestsAttributes.ShortDesc, elements[5]);
                }

                Debug.Ignore(() => DbDebugHelper.OnLoaded(db.DbSource, db.ProjectDatabase.MetaGrf.FindTkPath(file), db));
            }
            catch (Exception err)
            {
                Debug.Ignore(() => DbDebugHelper.OnExceptionThrown(db.DbSource, file, db));
                throw new Exception(TextFileHelper.GetLastError(), err);
            }
        }

        private static void _loadDataQuestLub(AbstractDb<int> db, string file)
        {
            var table = db.Table;
            var metaGrf = db.ProjectDatabase.MetaGrf;

            byte[] itemData = metaGrf.GetData(file);

            if (itemData == null)
            {
                Debug.Ignore(() => DbDebugHelper.OnUpdate(db.DbSource, file, "File not found."));
                return;
            }

            string outputPath = GrfPath.Combine(
                SdeAppConfiguration.TempPath,
                Path.GetFileNameWithoutExtension(file) + ".lua"
            );

            // LUB이면 Decompile
            if (Methods.ByteArrayCompare(itemData, 0, 4, new byte[] { 0x1b, 0x4c, 0x75, 0x61 }, 0))
            {
                Lub lub = new Lub(itemData);
                string text = lub.Decompile();
                itemData = EncodingService.DisplayEncoding.GetBytes(text);
                File.WriteAllBytes(outputPath, itemData);
            }
 
            DbIOMethods.DetectAndSetEncoding(itemData);

            LuaList list;
            using (LuaReader reader = new LuaReader(outputPath, DbIOMethods.DetectedEncoding))
            {
                list = reader.ReadAll();
            }

            LuaKeyValue root = list.Variables
                .OfType<LuaKeyValue>()
                .LastOrDefault(p => p.Key == "QuestInfoList" &&
                        (p.Value as LuaList) != null &&
                        ((LuaList)p.Value).Variables.Count > 0);


            LuaList items = root != null ? root.Value as LuaList : null;

            if (items == null)
            {
                Debug.Ignore(() => DbDebugHelper.OnUpdate(db.DbSource, file, "QuestInfoList not found."));
                return;
            }

            foreach (LuaKeyValue item in items.Variables)
            {
                _loadQuestLubEntry(table, item);
            }

            Debug.Ignore(() => DbDebugHelper.OnLoaded(db.DbSource, metaGrf.FindTkPath(file), db));
        }

        private static void _loadQuestLubEntry(Table<int, ReadableTuple<int>> table, LuaKeyValue item)
        {
            int itemId;
            if (!Int32.TryParse(item.Key.Trim('[', ']'), out itemId))
                return;

            LuaList itemProperties = item.Value as LuaList;
            if (itemProperties == null)
                return;

            string title = "";
            string fullDesc = "";
            string summary = "";
            string iconName = "QUE_NOIMAGE";

            foreach (LuaKeyValue itemProperty in itemProperties.Variables)
            {
                switch (itemProperty.Key)
                {
                    case "Title":
                        title = DbIOMethods.RemoveQuotes(((LuaValue)itemProperty.Value).Value);
                        break;

                    case "Summary":
                        summary = DbIOMethods.RemoveQuotes(((LuaValue)itemProperty.Value).Value);
                        break;

                    case "IconName":
                        iconName = DbIOMethods.RemoveQuotes(((LuaValue)itemProperty.Value).Value);
                        break;

                    case "Description":
                        fullDesc = _readQuestDescription(itemProperty.Value as LuaList);
                        break;
                }
            }

            table.SetRaw(itemId, ClientQuestsAttributes.Name, title);
            table.SetRaw(itemId, ClientQuestsAttributes.SG, "SG_FEEL");
            table.SetRaw(itemId, ClientQuestsAttributes.QUE, iconName);
            table.SetRaw(itemId, ClientQuestsAttributes.FullDesc, fullDesc);
            table.SetRaw(itemId, ClientQuestsAttributes.ShortDesc, summary);
        }

        private static string _readQuestDescription(LuaList descList)
        {
            if (descList == null)
                return "";

            StringBuilder builder = new StringBuilder();

            foreach (LuaValue desc in descList.Variables)
            {
                if (builder.Length > 0)
                    builder.AppendLine();

                builder.Append(DbIOMethods.RemoveQuotes(desc.Value));
            }

            return builder.ToString();
        }
    }
}