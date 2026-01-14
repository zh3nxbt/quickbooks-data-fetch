using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ConnectorApp
{
    public static class SyncStateStore
    {
        private static readonly object SyncLock = new object();
        private static readonly Dictionary<string, string> LastModified = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static string _statePath;

        public static void Initialize()
        {
            if (_statePath != null)
            {
                return;
            }

            _statePath = ConfigurationManager.AppSettings["SyncStatePath"] ?? "logs\\sync_state.json";
            Load();
        }

        public static string GetLastModified(string entityKey)
        {
            lock (SyncLock)
            {
                return LastModified.TryGetValue(entityKey, out var value) ? value : null;
            }
        }

        public static void UpdateFromResponse(string qbxml, string requestName)
        {
            if (string.IsNullOrWhiteSpace(qbxml) || string.IsNullOrWhiteSpace(requestName))
            {
                return;
            }

            var entityKey = RequestNameToEntityKey(requestName);
            if (entityKey == null)
            {
                return;
            }

            var maxModified = TryGetMaxTimeModified(qbxml, entityKey);
            if (maxModified == null)
            {
                return;
            }

            if (!IsIteratorComplete(qbxml, requestName))
            {
                return;
            }

            lock (SyncLock)
            {
                if (LastModified.TryGetValue(entityKey, out var existing) &&
                    string.Compare(existing, maxModified, StringComparison.Ordinal) >= 0)
                {
                    return;
                }

                LastModified[entityKey] = maxModified;
                Save();
            }
        }

        private static bool IsIteratorComplete(string qbxml, string requestName)
        {
            if (requestName == "InvoiceQuery" || requestName == "BillQuery" || requestName == "EstimateQuery")
            {
                try
                {
                    var doc = XDocument.Parse(qbxml);
                    var rs = doc.Descendants().FirstOrDefault(e =>
                        e.Name.LocalName == "InvoiceQueryRs" ||
                        e.Name.LocalName == "BillQueryRs" ||
                        e.Name.LocalName == "EstimateQueryRs");

                    if (rs == null)
                    {
                        return true;
                    }

                    var remainingAttr = rs.Attribute("iteratorRemainingCount");
                    if (remainingAttr == null)
                    {
                        return true;
                    }

                    if (int.TryParse(remainingAttr.Value, out var remaining))
                    {
                        return remaining == 0;
                    }
                }
                catch
                {
                    return true;
                }
            }

            return true;
        }

        private static string TryGetMaxTimeModified(string qbxml, string entityKey)
        {
            try
            {
                var doc = XDocument.Parse(qbxml);
                var elementName = EntityKeyToRetName(entityKey);
                if (elementName == null)
                {
                    return null;
                }

                string max = null;
                foreach (var ret in doc.Descendants())
                {
                    if (ret.Name.LocalName != elementName)
                    {
                        continue;
                    }

                    var tm = ret.Element(ret.Name.Namespace + "TimeModified")?.Value ??
                             ret.Element("TimeModified")?.Value;
                    if (string.IsNullOrWhiteSpace(tm))
                    {
                        continue;
                    }

                    var normalized = NormalizeTimestamp(tm);
                    if (normalized == null)
                    {
                        continue;
                    }

                    if (max == null || string.CompareOrdinal(normalized, max) > 0)
                    {
                        max = normalized;
                    }
                }

                return max;
            }
            catch
            {
                return null;
            }
        }

        private static string NormalizeTimestamp(string qbTimestamp)
        {
            if (DateTimeOffset.TryParse(qbTimestamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
            {
                return dto.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            }

            return null;
        }

        private static string RequestNameToEntityKey(string requestName)
        {
            switch (requestName)
            {
                case "CustomerQuery":
                    return "customers";
                case "InvoiceQuery":
                    return "invoices";
                case "BillQuery":
                    return "bills";
                case "EstimateQuery":
                    return "estimates";
                default:
                    return null;
            }
        }

        private static string EntityKeyToRetName(string entityKey)
        {
            switch (entityKey)
            {
                case "customers":
                    return "CustomerRet";
                case "invoices":
                    return "InvoiceRet";
                case "bills":
                    return "BillRet";
                case "estimates":
                    return "EstimateRet";
                default:
                    return null;
            }
        }

        private static void Load()
        {
            lock (SyncLock)
            {
                LastModified.Clear();
                if (!File.Exists(_statePath))
                {
                    return;
                }

                var json = File.ReadAllText(_statePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                foreach (var line in json.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = line.Split('|');
                    if (parts.Length != 2)
                    {
                        continue;
                    }

                    LastModified[parts[0]] = parts[1];
                }
            }
        }

        private static void Save()
        {
            var dir = Path.GetDirectoryName(_statePath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var sb = new StringBuilder();
            foreach (var kvp in LastModified)
            {
                sb.Append(kvp.Key).Append('|').Append(kvp.Value).AppendLine();
            }

            File.WriteAllText(_statePath, sb.ToString());
        }
    }
}
