using Database;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Engines.Parsers.Yaml;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers.Generic;
using SDE.View;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SDE.Editor.Generic.Parsers
{
    public sealed class DbIOHomuns
    {
        private static readonly Dictionary<string, int> _classToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) {
            { "Lif", 6001 },
            { "Amistr", 6002 },
            { "Filir", 6003 },
            { "Vanilmirth", 6004 },
            { "Lif2", 6005 },
            { "Amistr2", 6006 },
            { "Filir2", 6007 },
            { "Vanilmirth2", 6008 },
            { "Lif_H", 6009 },
            { "Amistr_H", 6010 },
            { "Filir_H", 6011 },
            { "Vanilmirth_H", 6012 },
            { "Lif_H2", 6013 },
            { "Amistr_H2", 6014 },
            { "Filir_H2", 6015 },
            { "Vanilmirth_H2", 6016 },
            { "Eira", 6048 },
            { "Bayeri", 6049 },
            { "Sera", 6050 },
            { "Dieter", 6051 },
            { "Eleanor", 6052 },
        };

        public static void Loader(DbDebugItem<int> debug, AbstractDb<int> db)
        {
            if (debug.FileType == FileType.Yaml)
            {
                var ele = new YamlParser(debug.FilePath);
                var table = debug.AbsractDb.Table;

                if (ele.Output == null || ((ParserArray)ele.Output).Objects.Count == 0 || (ele.Output["copy_paste"] ?? ele.Output["Body"]) == null)
                    return;

                var itemDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);

                foreach (var homun in ele.Output["copy_paste"] ?? ele.Output["Body"])
                {
                    string className = homun["Class"] ?? homun["BaseClass"] ?? "";
                    int classId = _toClassId(className);

                    string evoClassName = homun["EvolutionClass"] ?? "";
                    string evoClassId = String.IsNullOrEmpty(evoClassName) ? "0" : _toClassId(evoClassName).ToString();

                    table.SetRaw(classId, ServerHomunAttributes.Name, homun["Name"] ?? className);
                    table.SetRaw(classId, ServerHomunAttributes.EvoClass, evoClassId);
                    table.SetRaw(classId, ServerHomunAttributes.FoodID,
                        DbIOUtils.Name2IdBuffered((IEnumerable<ReadableTuple<int>>)itemDb, ServerItemAttributes.AegisName, homun["Food"] ?? "Pet_Food", "item_db", true));
                    table.SetRaw(classId, ServerHomunAttributes.HungryDelay, homun["HungryDelay"] ?? "60000");
                    table.SetRaw(classId, ServerHomunAttributes.Race, homun["Race"] ?? "Demihuman");
                    table.SetRaw(classId, ServerHomunAttributes.Element, homun["Element"] ?? "Neutral");
                    table.SetRaw(classId, ServerHomunAttributes.BaseSize, homun["Size"] ?? "Small");
                    table.SetRaw(classId, ServerHomunAttributes.EvoSize, homun["EvolutionSize"] ?? "Medium");
                    table.SetRaw(classId, ServerHomunAttributes.BAspd, homun["AttackDelay"] ?? "700");

                    _readStatus(table, classId, homun["Status"]);
                }

                return;
            }

            DbIOMethods.DbLoaderComma(debug, db);
        }

        private static int _toClassId(string className)
        {
            if (String.IsNullOrEmpty(className))
                throw new Exception("Homunculus class name is empty.");

            if (_classToId.TryGetValue(className.Trim(), out int id))
                return id;

            throw new Exception("Unknown homunculus class: " + className);
        }

        private static void _readStatus(Table<int, ReadableTuple<int>> table, int classId, ParserObject statusNode)
        {
            if (statusNode == null)
                return;

            foreach (ParserArray status in statusNode.OfType<ParserArray>())
            {
                string type = (status["Type"] ?? "").Trim();

                switch (type.ToUpperInvariant())
                {
                    case "HP":
                        table.SetRaw(classId, ServerHomunAttributes.BHp, status["Base"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.GnHp, status["GrowthMinimum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.GxHp, status["GrowthMaximum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.EnHp, status["EvolutionMinimum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.ExHp, status["EvolutionMaximum"] ?? "0");
                        break;

                    case "SP":
                        table.SetRaw(classId, ServerHomunAttributes.BSp, status["Base"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.GnSp, status["GrowthMinimum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.GxSp, status["GrowthMaximum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.EnSp, status["EvolutionMinimum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.ExSp, status["EvolutionMaximum"] ?? "0");
                        break;

                    case "STR":
                        table.SetRaw(classId, ServerHomunAttributes.BStr, status["Base"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.GnStr, status["GrowthMinimum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.GxStr, status["GrowthMaximum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.EnStr, status["EvolutionMinimum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.ExStr, status["EvolutionMaximum"] ?? "0");
                        break;

                    case "AGI":
                        table.SetRaw(classId, ServerHomunAttributes.BAgi, status["Base"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.GnAgi, status["GrowthMinimum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.GxAgi, status["GrowthMaximum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.EnAgi, status["EvolutionMinimum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.ExAgi, status["EvolutionMaximum"] ?? "0");
                        break;

                    case "VIT":
                        table.SetRaw(classId, ServerHomunAttributes.BVit, status["Base"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.GnVit, status["GrowthMinimum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.GxVit, status["GrowthMaximum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.EnVit, status["EvolutionMinimum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.ExVit, status["EvolutionMaximum"] ?? "0");
                        break;

                    case "INT":
                        table.SetRaw(classId, ServerHomunAttributes.BInt, status["Base"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.GnInt, status["GrowthMinimum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.GxInt, status["GrowthMaximum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.EnInt, status["EvolutionMinimum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.ExInt, status["EvolutionMaximum"] ?? "0");
                        break;

                    case "DEX":
                        table.SetRaw(classId, ServerHomunAttributes.BDex, status["Base"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.GnDex, status["GrowthMinimum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.GxDex, status["GrowthMaximum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.EnDex, status["EvolutionMinimum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.ExDex, status["EvolutionMaximum"] ?? "0");
                        break;

                    case "LUK":
                        table.SetRaw(classId, ServerHomunAttributes.BLuk, status["Base"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.GnLuk, status["GrowthMinimum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.GxLuk, status["GrowthMaximum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.EnLuk, status["EvolutionMinimum"] ?? "0");
                        table.SetRaw(classId, ServerHomunAttributes.ExLuk, status["EvolutionMaximum"] ?? "0");
                        break;
                }
            }
        }

    }
}
