using Database;
using ErrorManager;
using SDE.Editor.Engines;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Engines.Parsers.Yaml;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers.Generic;
using SDE.View;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;


namespace SDE.Editor.Generic.Parsers
{
    public sealed class DbIOCombos
    {
        public static void Loader<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db)
        {
            if (debug.FileType == FileType.Txt)
            {
                DbIOMethods.DbLoaderComma<TKey>(debug, db);
            }
            else if (debug.FileType == FileType.Yaml)
            {
                var ele = new YamlParser(debug.FilePath);
                Table<TKey, ReadableTuple<TKey>> table = debug.AbsractDb.Table;
                MetaTable<int> metaTable = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);
                DbIOUtils.ClearBuffer();
                if (ele.Output == null || ((ParserArrayBase)ele.Output).Objects.Count == 0 || (ele.Output["copy_paste"] ?? ele.Output["Body"]) == null)
                    return;
                foreach (ParserObject import in ele.Output["copy_paste"] ?? ele.Output["Body"])
                {
                    try
                    {
                        ParserObject combos = import["Combos"].To<ParserList>();
                        string script = (string)import["Script"];
                        foreach (var combo in (ParserObject)combos)
                        {
                            ParserList list = combo["Combo"].To<ParserList>();
                            string ids = "";
                            foreach (ParserArray array in list.OfType<ParserArray>())
                            {
                                try
                                {
                                    ParserString parserString = array.Objects[0] as ParserString;
                                    string aegisName = parserString.ObjectValue;
                                    int commentIndex = aegisName.IndexOf('#');
                                    if (commentIndex >= 0)
                                        aegisName = aegisName.Substring(0, commentIndex);
                                    aegisName = aegisName.TrimEnd();
                                    ids = $"{ids}{DbIOUtils.Name2IdBuffered((IEnumerable<ReadableTuple<int>>)metaTable, ServerItemAttributes.AegisName, aegisName, "item_db", true)}:";
                                }
                                catch (Exception ex)
                                {
                                    ErrorHandler.HandleException(ex);
                                }
                            }
                            TKey key = (TKey)(object)ids.TrimEnd(':');
                            table.SetRaw(key, ServerComboAttributes.Script, script, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorHandler.HandleException(ex);
                    }
                }
            }
        }

        public static void Writer(DbDebugItem<string> debug, AbstractDb<string> db)
        {
            try
            {
                var stringBuilder = new StringBuilder();
                if (debug.FileType == FileType.Txt)
                {
                    DbIOMethods.DbWriterComma(debug, db);
                }
                else if (debug.FileType == FileType.Yaml)
                {
                    MetaTable<int> metaTable = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);
                    try
                    {
                        Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
                        foreach (ReadableTuple<string> readableTuple in db.Table)
                        {
                            string key = DbIOFormatting.ScriptFormatYaml(readableTuple.GetValue<string>(ServerComboAttributes.Script), "      ");
                            if (!dictionary.ContainsKey(key))
                                dictionary[key] = new List<string>();
                            dictionary[key].Add(readableTuple.Key);
                        }
                        string[] strArray = File.ReadAllLines(DbPathLocator.GetStoredFile(debug.FilePath));
                        for (int index = 0; index < strArray.Length; ++index)
                        {
                            stringBuilder.AppendLine(strArray[index]);
                            if (strArray[index].StartsWith("Body:"))
                                break;
                        }
                        foreach (KeyValuePair<string, List<string>> keyValuePair in dictionary)
                        {
                            stringBuilder.AppendLine("  - Combos:");
                            foreach (string str1 in keyValuePair.Value)
                            {
                                stringBuilder.AppendLine("      - Combo:");
                                string str2 = str1;
                                char[] chArray = new char[1] { ':' };
                                foreach (string id in str2.Split(chArray))
                                {
                                    stringBuilder.Append("          - ");
                                    stringBuilder.AppendLine(DbIOUtils.Id2Name((Table<int, ReadableTuple<int>>)metaTable, ServerItemAttributes.AegisName, id));
                                }
                            }
                            stringBuilder.AppendLine("    Script: |");
                            stringBuilder.AppendLine(keyValuePair.Key);
                        }
                        IOHelper.WriteAllText(debug.FilePath, stringBuilder.ToString());
                    }
                    catch (Exception ex)
                    {
                        debug.ReportException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                debug.ReportException(ex);
            }
        }
       
    }
}
