using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Xml.Linq;

namespace ConnectorApp
{
    [System.ServiceModel.ServiceBehavior(InstanceContextMode = System.ServiceModel.InstanceContextMode.Single)]
    public sealed class WebConnectorService : IWebConnectorService
    {
        private readonly Dictionary<string, SessionState> _sessions = new Dictionary<string, SessionState>();
        private readonly string _expectedUser;
        private readonly string _expectedPassword;
        private readonly string _companyFile;
        private readonly int _pageSize;
        private readonly string _syncMode;

        public WebConnectorService()
        {
            _expectedUser = ConfigurationManager.AppSettings["Username"] ?? "qbwc";
            _expectedPassword = ConfigurationManager.AppSettings["Password"] ?? "qbwc";
            _companyFile = ConfigurationManager.AppSettings["CompanyFile"] ?? string.Empty;
            _pageSize = ReadPageSize();
            _syncMode = (ConfigurationManager.AppSettings["SyncMode"] ?? "incremental").Trim();
            SyncStateStore.Initialize();
        }

        public string serverVersion()
        {
            return string.Empty;
        }

        public string clientVersion(string strVersion)
        {
            return string.Empty;
        }

        public string[] authenticate(string strUserName, string strPassword)
        {
            if (!string.Equals(strUserName, _expectedUser, StringComparison.Ordinal) ||
                !string.Equals(strPassword, _expectedPassword, StringComparison.Ordinal))
            {
                return new[] { string.Empty, "nvu" };
            }

            var ticket = Guid.NewGuid().ToString("N");
            _sessions[ticket] = new SessionState(_pageSize, _syncMode);
            return new[] { ticket, _companyFile };
        }

        public string sendRequestXML(string ticket, string strHCPResponse, string strCompanyFileName, string qbXMLCountry, int qbXMLMajorVers, int qbXMLMinorVers)
        {
            if (!_sessions.TryGetValue(ticket, out var session))
            {
                return string.Empty;
            }

            if (session.PendingRequests.Count == 0)
            {
                return string.Empty;
            }

            var next = session.PendingRequests.Dequeue();
            session.LastRequestName = next.RequestName;
            ResponseLogger.LogRequest(next.RequestName, next.QbXml);
            return next.QbXml;
        }

        public int receiveResponseXML(string ticket, string response, string hresult, string message)
        {
            if (!_sessions.TryGetValue(ticket, out var session))
            {
                return 100;
            }

            session.LastError = string.IsNullOrWhiteSpace(message) ? null : message;
            ResponseLogger.LogResponse(response, session.LastRequestName);
            EnqueueIteratorContinuation(session, response);
            SyncStateStore.UpdateFromResponse(response, session.LastRequestName);
            if (session.PendingRequests.Count > 0)
            {
                return 50;
            }

            return 100;
        }

        public string getLastError(string ticket)
        {
            if (_sessions.TryGetValue(ticket, out var session))
            {
                return session.LastError ?? string.Empty;
            }

            return string.Empty;
        }

        public string closeConnection(string ticket)
        {
            _sessions.Remove(ticket);
            return "OK";
        }

        private static int ReadPageSize()
        {
            if (int.TryParse(ConfigurationManager.AppSettings["PageSize"], out var size) && size > 0)
            {
                return size;
            }

            return 100;
        }

        private static void EnqueueIteratorContinuation(SessionState session, string qbxml)
        {
            if (string.IsNullOrWhiteSpace(qbxml))
            {
                return;
            }

            try
            {
                var doc = XDocument.Parse(qbxml);
                var rs = doc.Descendants().FirstOrDefault(e =>
                    e.Name.LocalName == "InvoiceQueryRs" ||
                    e.Name.LocalName == "BillQueryRs" ||
                    e.Name.LocalName == "EstimateQueryRs");

                if (rs == null)
                {
                    return;
                }

                var remainingAttr = rs.Attribute("iteratorRemainingCount");
                var iteratorIdAttr = rs.Attribute("iteratorID");
                if (remainingAttr == null || iteratorIdAttr == null)
                {
                    return;
                }

                if (!int.TryParse(remainingAttr.Value, out var remaining) || remaining <= 0)
                {
                    return;
                }

                var iteratorId = iteratorIdAttr.Value;
                if (string.IsNullOrWhiteSpace(iteratorId))
                {
                    return;
                }

                switch (rs.Name.LocalName)
                {
                    case "InvoiceQueryRs":
                        session.PendingRequests.Enqueue(new WorkItem("InvoiceQuery",
                            QbXmlBuilder.BuildInvoiceQuery("Continue", iteratorId, session.PageSize, null)));
                        break;
                    case "BillQueryRs":
                        session.PendingRequests.Enqueue(new WorkItem("BillQuery",
                            QbXmlBuilder.BuildBillQuery("Continue", iteratorId, session.PageSize, null)));
                        break;
                    case "EstimateQueryRs":
                        session.PendingRequests.Enqueue(new WorkItem("EstimateQuery",
                            QbXmlBuilder.BuildEstimateQuery("Continue", iteratorId, session.PageSize, null)));
                        break;
                }
            }
            catch
            {
                // Ignore parse errors; QBWC will still finish the session.
            }
        }

        private sealed class SessionState
        {
            public SessionState(int pageSize, string syncMode)
            {
                PageSize = pageSize;
                SyncMode = syncMode;
                var isIncremental = string.Equals(syncMode, "incremental", StringComparison.OrdinalIgnoreCase);
                var customerFrom = isIncremental ? SyncStateStore.GetLastModified("customers") : null;
                var invoiceFrom = isIncremental ? SyncStateStore.GetLastModified("invoices") : null;
                var billFrom = isIncremental ? SyncStateStore.GetLastModified("bills") : null;
                var estimateFrom = isIncremental ? SyncStateStore.GetLastModified("estimates") : null;

                PendingRequests = new Queue<WorkItem>(new[]
                {
                    new WorkItem("CustomerQuery", QbXmlBuilder.BuildCustomerQuery(customerFrom)),
                    new WorkItem("InvoiceQuery", QbXmlBuilder.BuildInvoiceQuery("Start", null, pageSize, invoiceFrom)),
                    new WorkItem("BillQuery", QbXmlBuilder.BuildBillQuery("Start", null, pageSize, billFrom)),
                    new WorkItem("EstimateQuery", QbXmlBuilder.BuildEstimateQuery("Start", null, pageSize, estimateFrom))
                });
            }

            public int PageSize { get; }
            public string SyncMode { get; }
            public string LastError { get; set; }
            public string LastRequestName { get; set; }
            public Queue<WorkItem> PendingRequests { get; }
        }

        private sealed class WorkItem
        {
            public WorkItem(string requestName, string qbXml)
            {
                RequestName = requestName;
                QbXml = qbXml;
            }

            public string RequestName { get; }
            public string QbXml { get; }
        }
    }
}
