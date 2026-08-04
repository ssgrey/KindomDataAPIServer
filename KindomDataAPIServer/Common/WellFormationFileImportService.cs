using KindomDataAPIServer.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace KindomDataAPIServer.Common
{
    internal sealed class WellFormationFileRow
    {
        public string WellName { get; set; }
        public string FormationName { get; set; }
        public double Top { get; set; }
        public double Bottom { get; set; }
    }

    internal sealed class WellFormationTemporaryFile
    {
        public string FilePath { get; set; }
        public long PayloadBytes { get; set; }
    }

    internal static class WellFormationFileImportService
    {
        private const string Header = "WellName,FormationName,Top Depth,Bottom Depth";

        public static WellFormationTemporaryFile CreateTemporaryFile(IEnumerable<WellFormationFileRow> rows)
        {
            byte[] content = BuildFileContent(rows);
            var failures = new List<Exception>();

            foreach (string directory in GetCandidateDirectories())
            {
                try
                {
                    Directory.CreateDirectory(directory);
                    string filePath = Path.Combine(directory, $"formation-{Guid.NewGuid():N}.txt");
                    File.WriteAllBytes(filePath, content);
                    return new WellFormationTemporaryFile
                    {
                        FilePath = filePath,
                        PayloadBytes = content.LongLength
                    };
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                    LogManagerService.Instance.Log($"WellFormation temporary file directory unavailable. Directory:{directory}. {ExceptionLogHelper.Format(ex)}");
                }
            }

            throw new IOException("Unable to create a WellFormation import file in any configured temporary directory.", new AggregateException(failures));
        }

        public static void DeleteTemporaryFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return;
            }

            try
            {
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                LogManagerService.Instance.Log($"WellFormation temporary file cleanup failed. File:{filePath}. {ExceptionLogHelper.Format(ex)}");
            }
        }

        public static string BuildImportOptionsJson(UnitInfo meterUnit)
        {
            var unit = new JObject
            {
                ["id"] = meterUnit != null && meterUnit.Id > 0 ? meterUnit.Id : 50,
                ["abbr"] = meterUnit?.Abbr ?? "m",
                ["longName"] = meterUnit?.LongName ?? "meter"
            };
            var depthOption = new JObject
            {
                ["isDate"] = false,
                ["isUnitInfo"] = true,
                ["isEnum"] = false,
                ["isDateTime"] = false,
                ["isTime"] = false,
                ["unit"] = unit
            };
            var options = new JObject
            {
                ["NegativeZWhenMostPositive"] = false,
                ["ColumnMappings"] = new JArray
                {
                    CreateColumnMapping(1, "井名", "WELL_NAME", 0, true, 0, true, new JObject()),
                    CreateColumnMapping(2, "分层名", "Name", 0, true, 0, true, new JObject()),
                    CreateColumnMapping(3, "顶深", "Top", 4, true, 1, true, depthOption),
                    CreateColumnMapping(4, "底深", "Bottom", 4, false, 1, true, depthOption.DeepClone()),
                    CreateColumnMapping(-1, "编号", "ObserveCount", 0, false, 2, false, new JObject()),
                    CreateColumnMapping(-1, "备注", "Contact", 0, false, 2, false, new JObject())
                },
                ["SplitOptions"] = new JObject
                {
                    ["Delimeters"] = new JArray(","),
                    ["Qualifier"] = "\"",
                    ["TreatConsecutiveAsOne"] = false
                },
                ["Encoding"] = "utf-8",
                ["FirstDataRow"] = 2,
                ["SheetIndex"] = 0
            };

            return options.ToString(Formatting.None);
        }

        public static string BuildFormationMapJson(IEnumerable<WellFormationFileRow> rows)
        {
            var formationMap = new JObject();
            foreach (string formationName in (rows ?? Enumerable.Empty<WellFormationFileRow>())
                .Select(row => row.FormationName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal))
            {
                formationMap[formationName] = formationName;
            }
            return formationMap.ToString(Formatting.None);
        }

        private static JObject CreateColumnMapping(int sourceColumn, string displayName, string propertyName, int measureId, bool isRequired, int typeDescriptor, bool isMapped, JToken option)
        {
            return new JObject
            {
                ["isMapped"] = isMapped,
                ["displayName"] = displayName,
                ["propertyName"] = propertyName,
                ["measureId"] = measureId,
                ["isRequired"] = isRequired,
                ["typeDescriptor"] = typeDescriptor,
                ["ignoreRequiredInvalidData"] = true,
                ["option"] = option ?? new JObject(),
                ["SourceColumn"] = sourceColumn
            };
        }

        private static byte[] BuildFileContent(IEnumerable<WellFormationFileRow> rows)
        {
            var builder = new StringBuilder();
            builder.Append(Header).Append("\r\n");
            foreach (WellFormationFileRow row in rows ?? Enumerable.Empty<WellFormationFileRow>())
            {
                builder.Append(EscapeCsv(row.WellName)).Append(',')
                    .Append(EscapeCsv(row.FormationName)).Append(',')
                    .Append(row.Top.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                    .Append(row.Bottom.ToString("R", CultureInfo.InvariantCulture)).Append("\r\n");
            }
            return new UTF8Encoding(false).GetBytes(builder.ToString());
        }

        private static string EscapeCsv(string value)
        {
            value = value ?? string.Empty;
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
            {
                return value;
            }
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static IEnumerable<string> GetCandidateDirectories()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var candidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Temp", "WellFormationImports"),
                Path.Combine(localAppData, "KindomDataAPIServer", "Temp", "WellFormationImports"),
                Path.Combine(Path.GetTempPath(), "KindomDataAPIServer", "WellFormationImports")
            };
            return candidates.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase);
        }
    }
}
