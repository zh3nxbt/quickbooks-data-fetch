namespace ConnectorApp
{
    public static class QbXmlBuilder
    {
        public static string BuildCustomerQuery(string fromModifiedDate)
        {
            return "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                   "<?qbxml version=\"13.0\"?>" +
                   "<QBXML>" +
                   "<QBXMLMsgsRq onError=\"stopOnError\">" +
                   "<CustomerQueryRq requestID=\"1\">" +
                   BuildModifiedDateFilter(fromModifiedDate) +
                   "<ActiveStatus>All</ActiveStatus>" +
                   "</CustomerQueryRq>" +
                   "</QBXMLMsgsRq>" +
                   "</QBXML>";
        }

        public static string BuildInvoiceQuery(string iterator, string iteratorId, int maxReturned, string fromModifiedDate)
        {
            return "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                   "<?qbxml version=\"13.0\"?>" +
                   "<QBXML>" +
                   "<QBXMLMsgsRq onError=\"stopOnError\">" +
                   "<InvoiceQueryRq requestID=\"2\"" + BuildIteratorAttrs(iterator, iteratorId) + ">" +
                   "<MaxReturned>" + maxReturned + "</MaxReturned>" +
                   BuildModifiedDateFilter(fromModifiedDate) +
                   "<IncludeLineItems>true</IncludeLineItems>" +
                   "</InvoiceQueryRq>" +
                   "</QBXMLMsgsRq>" +
                   "</QBXML>";
        }

        public static string BuildBillQuery(string iterator, string iteratorId, int maxReturned, string fromModifiedDate)
        {
            return "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                   "<?qbxml version=\"13.0\"?>" +
                   "<QBXML>" +
                   "<QBXMLMsgsRq onError=\"stopOnError\">" +
                   "<BillQueryRq requestID=\"3\"" + BuildIteratorAttrs(iterator, iteratorId) + ">" +
                   "<MaxReturned>" + maxReturned + "</MaxReturned>" +
                   BuildModifiedDateFilter(fromModifiedDate) +
                   "<IncludeLineItems>true</IncludeLineItems>" +
                   "</BillQueryRq>" +
                   "</QBXMLMsgsRq>" +
                   "</QBXML>";
        }

        public static string BuildEstimateQuery(string iterator, string iteratorId, int maxReturned, string fromModifiedDate)
        {
            return "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                   "<?qbxml version=\"13.0\"?>" +
                   "<QBXML>" +
                   "<QBXMLMsgsRq onError=\"stopOnError\">" +
                   "<EstimateQueryRq requestID=\"4\"" + BuildIteratorAttrs(iterator, iteratorId) + ">" +
                   "<MaxReturned>" + maxReturned + "</MaxReturned>" +
                   BuildModifiedDateFilter(fromModifiedDate) +
                   "<IncludeLineItems>true</IncludeLineItems>" +
                   "</EstimateQueryRq>" +
                   "</QBXMLMsgsRq>" +
                   "</QBXML>";
        }

        private static string BuildModifiedDateFilter(string fromModifiedDate)
        {
            if (string.IsNullOrWhiteSpace(fromModifiedDate))
            {
                return string.Empty;
            }

            return "<ModifiedDateRangeFilter><FromModifiedDate>" + fromModifiedDate + "</FromModifiedDate></ModifiedDateRangeFilter>";
        }

        private static string BuildIteratorAttrs(string iterator, string iteratorId)
        {
            if (string.IsNullOrWhiteSpace(iterator))
            {
                return string.Empty;
            }

            if (string.Equals(iterator, "Continue", System.StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(iteratorId))
            {
                return " iterator=\"" + iterator + "\" iteratorID=\"" + iteratorId + "\"";
            }

            return " iterator=\"" + iterator + "\"";
        }
    }
}
