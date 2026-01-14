using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ConnectorApp
{
    public static class ResponseLogger
    {
        public static void LogRequest(string requestName, string qbxml)
        {
            var logDir = ConfigurationManager.AppSettings["LogDir"] ?? "logs";
            Directory.CreateDirectory(logDir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var fileName = "qbxml-request-" + requestName + "-" + timestamp + ".xml";
            var path = Path.Combine(logDir, fileName);
            File.WriteAllText(path, qbxml ?? string.Empty);
            File.AppendAllText(Path.Combine(logDir, "session.log"),
                string.Format("{0} Sent {1}. Saved to {2}{3}", DateTime.Now, requestName, path, Environment.NewLine));
        }

        public static void LogResponse(string qbxml, string requestName)
        {
            var logDir = ConfigurationManager.AppSettings["LogDir"] ?? "logs";
            Directory.CreateDirectory(logDir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var responseName = !string.IsNullOrWhiteSpace(requestName) ? requestName : TryDetectResponseName(qbxml);
            var fileName = string.IsNullOrWhiteSpace(responseName)
                ? "qbxml-response-" + timestamp + ".xml"
                : "qbxml-response-" + responseName + "-" + timestamp + ".xml";
            var path = Path.Combine(logDir, fileName);
            File.WriteAllText(path, qbxml ?? string.Empty);

            var counts = TryCountEntities(qbxml);
            var summary = string.Format("{0} Response saved to {1}. Type={2}. Counts: Customers={3}, Invoices={4}, Bills={5}, Estimates={6}",
                DateTime.Now, path, responseName ?? "Unknown", counts.Customers, counts.Invoices, counts.Bills, counts.Estimates);
            File.AppendAllText(Path.Combine(logDir, "session.log"), summary + Environment.NewLine);
            Console.WriteLine(summary);

            JsonExporter.Export(qbxml, logDir, timestamp);
        }

        private static string TryDetectResponseName(string qbxml)
        {
            if (string.IsNullOrWhiteSpace(qbxml))
            {
                return null;
            }

            try
            {
                var doc = XDocument.Parse(qbxml);
                var rs = doc.Descendants().FirstOrDefault(e =>
                    e.Name.LocalName == "CustomerQueryRs" ||
                    e.Name.LocalName == "InvoiceQueryRs" ||
                    e.Name.LocalName == "BillQueryRs" ||
                    e.Name.LocalName == "EstimateQueryRs");

                if (rs == null)
                {
                    return null;
                }

                switch (rs.Name.LocalName)
                {
                    case "CustomerQueryRs":
                        return "CustomerQuery";
                    case "InvoiceQueryRs":
                        return "InvoiceQuery";
                    case "BillQueryRs":
                        return "BillQuery";
                    case "EstimateQueryRs":
                        return "EstimateQuery";
                    default:
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }

        private static EntityCounts TryCountEntities(string qbxml)
        {
            if (string.IsNullOrWhiteSpace(qbxml))
            {
                return new EntityCounts();
            }

            try
            {
                var doc = XDocument.Parse(qbxml);
                return new EntityCounts
                {
                    Customers = doc.Descendants().Count(e => e.Name.LocalName == "CustomerRet"),
                    Invoices = doc.Descendants().Count(e => e.Name.LocalName == "InvoiceRet"),
                    Bills = doc.Descendants().Count(e => e.Name.LocalName == "BillRet"),
                    Estimates = doc.Descendants().Count(e => e.Name.LocalName == "EstimateRet")
                };
            }
            catch
            {
                return new EntityCounts();
            }
        }

        private struct EntityCounts
        {
            public int Customers;
            public int Invoices;
            public int Bills;
            public int Estimates;
        }
    }
}
