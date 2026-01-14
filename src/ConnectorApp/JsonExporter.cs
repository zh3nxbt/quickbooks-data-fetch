using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Xml.Linq;

namespace ConnectorApp
{
    public static class JsonExporter
    {
        public static void Export(string qbxml, string logDir, string timestamp)
        {
            if (string.IsNullOrWhiteSpace(qbxml))
            {
                return;
            }

            try
            {
                var doc = XDocument.Parse(qbxml);
                var jsonDir = Path.Combine(logDir, "json");
                Directory.CreateDirectory(jsonDir);

                WriteIfAny(jsonDir, "customers", timestamp, ParseCustomers(doc));
                WriteIfAny(jsonDir, "invoices", timestamp, ParseInvoices(doc));
                WriteIfAny(jsonDir, "invoice_lines", timestamp, ParseInvoiceLines(doc));
                WriteIfAny(jsonDir, "bills", timestamp, ParseBills(doc));
                WriteIfAny(jsonDir, "bill_lines", timestamp, ParseBillLines(doc));
                WriteIfAny(jsonDir, "estimates", timestamp, ParseEstimates(doc));
                WriteIfAny(jsonDir, "estimate_lines", timestamp, ParseEstimateLines(doc));
            }
            catch
            {
                // Best-effort export only.
            }
        }

        private static void WriteIfAny<T>(string jsonDir, string name, string timestamp, List<T> items)
        {
            if (items.Count == 0)
            {
                return;
            }

            var path = Path.Combine(jsonDir, "qbxml-" + name + "-" + timestamp + ".json");
            using (var stream = File.Create(path))
            {
                var serializer = new DataContractJsonSerializer(typeof(List<T>));
                serializer.WriteObject(stream, items);
            }
        }

        private static List<Customer> ParseCustomers(XDocument doc)
        {
            var list = new List<Customer>();
            foreach (var ret in doc.Descendants())
            {
                if (ret.Name.LocalName != "CustomerRet")
                {
                    continue;
                }

                list.Add(new Customer
                {
                    ListId = GetValue(ret, "ListID"),
                    Name = GetValue(ret, "Name"),
                    FullName = GetValue(ret, "FullName"),
                    IsActive = GetBool(ret, "IsActive"),
                    Email = GetValue(ret, "Email"),
                    Phone = GetValue(ret, "Phone"),
                    BillAddress = ParseAddress(ret.Element(ret.Name.Namespace + "BillAddress") ?? ret.Element("BillAddress")),
                    TimeModified = GetValue(ret, "TimeModified")
                });
            }

            return list;
        }

        private static List<Invoice> ParseInvoices(XDocument doc)
        {
            var list = new List<Invoice>();
            foreach (var ret in doc.Descendants())
            {
                if (ret.Name.LocalName != "InvoiceRet")
                {
                    continue;
                }

                list.Add(new Invoice
                {
                    TxnId = GetValue(ret, "TxnID"),
                    RefNumber = GetValue(ret, "RefNumber"),
                    TxnDate = GetValue(ret, "TxnDate"),
                    CustomerRef = ParseRef(ret.Element(ret.Name.Namespace + "CustomerRef") ?? ret.Element("CustomerRef")),
                    Subtotal = GetDecimal(ret, "Subtotal"),
                    SalesTaxTotal = GetDecimal(ret, "SalesTaxTotal"),
                    BalanceRemaining = GetDecimal(ret, "BalanceRemaining"),
                    IsPaid = GetBool(ret, "IsPaid"),
                    TimeModified = GetValue(ret, "TimeModified")
                });
            }

            return list;
        }

        private static List<InvoiceLine> ParseInvoiceLines(XDocument doc)
        {
            var list = new List<InvoiceLine>();
            foreach (var inv in doc.Descendants())
            {
                if (inv.Name.LocalName != "InvoiceRet")
                {
                    continue;
                }

                var invoiceTxnId = GetValue(inv, "TxnID");
                foreach (var line in inv.Elements())
                {
                    if (line.Name.LocalName != "InvoiceLineRet")
                    {
                        continue;
                    }

                    list.Add(new InvoiceLine
                    {
                        InvoiceTxnId = invoiceTxnId,
                        LineId = GetValue(line, "TxnLineID"),
                        ItemRef = ParseRef(line.Element(line.Name.Namespace + "ItemRef") ?? line.Element("ItemRef")),
                        Desc = GetValue(line, "Desc"),
                        Quantity = GetDecimal(line, "Quantity"),
                        Rate = GetDecimal(line, "Rate"),
                        Amount = GetDecimal(line, "Amount"),
                        ClassRef = ParseRef(line.Element(line.Name.Namespace + "ClassRef") ?? line.Element("ClassRef"))
                    });
                }
            }

            return list;
        }

        private static List<Bill> ParseBills(XDocument doc)
        {
            var list = new List<Bill>();
            foreach (var ret in doc.Descendants())
            {
                if (ret.Name.LocalName != "BillRet")
                {
                    continue;
                }

                list.Add(new Bill
                {
                    TxnId = GetValue(ret, "TxnID"),
                    RefNumber = GetValue(ret, "RefNumber"),
                    TxnDate = GetValue(ret, "TxnDate"),
                    DueDate = GetValue(ret, "DueDate"),
                    VendorRef = ParseRef(ret.Element(ret.Name.Namespace + "VendorRef") ?? ret.Element("VendorRef")),
                    AmountDue = GetDecimal(ret, "AmountDue"),
                    BalanceRemaining = GetDecimal(ret, "BalanceRemaining"),
                    IsPaid = GetBool(ret, "IsPaid"),
                    TimeModified = GetValue(ret, "TimeModified")
                });
            }

            return list;
        }

        private static List<BillLine> ParseBillLines(XDocument doc)
        {
            var list = new List<BillLine>();
            foreach (var bill in doc.Descendants())
            {
                if (bill.Name.LocalName != "BillRet")
                {
                    continue;
                }

                var billTxnId = GetValue(bill, "TxnID");
                foreach (var line in bill.Elements())
                {
                    if (line.Name.LocalName == "ItemLineRet")
                    {
                        list.Add(new BillLine
                        {
                            BillTxnId = billTxnId,
                            LineId = GetValue(line, "TxnLineID"),
                            LineKind = "item",
                            ItemRef = ParseRef(line.Element(line.Name.Namespace + "ItemRef") ?? line.Element("ItemRef")),
                            Desc = GetValue(line, "Desc"),
                            Quantity = GetDecimal(line, "Quantity"),
                            RateOrCost = GetDecimal(line, "Cost"),
                            Amount = GetDecimal(line, "Amount"),
                            ClassRef = ParseRef(line.Element(line.Name.Namespace + "ClassRef") ?? line.Element("ClassRef"))
                        });
                    }
                    else if (line.Name.LocalName == "ExpenseLineRet")
                    {
                        list.Add(new BillLine
                        {
                            BillTxnId = billTxnId,
                            LineId = GetValue(line, "TxnLineID"),
                            LineKind = "expense",
                            AccountRef = ParseRef(line.Element(line.Name.Namespace + "AccountRef") ?? line.Element("AccountRef")),
                            Desc = GetValue(line, "Desc"),
                            RateOrCost = GetDecimal(line, "Amount"),
                            Amount = GetDecimal(line, "Amount"),
                            ClassRef = ParseRef(line.Element(line.Name.Namespace + "ClassRef") ?? line.Element("ClassRef"))
                        });
                    }
                }
            }

            return list;
        }

        private static List<Estimate> ParseEstimates(XDocument doc)
        {
            var list = new List<Estimate>();
            foreach (var ret in doc.Descendants())
            {
                if (ret.Name.LocalName != "EstimateRet")
                {
                    continue;
                }

                list.Add(new Estimate
                {
                    TxnId = GetValue(ret, "TxnID"),
                    RefNumber = GetValue(ret, "RefNumber"),
                    TxnDate = GetValue(ret, "TxnDate"),
                    CustomerRef = ParseRef(ret.Element(ret.Name.Namespace + "CustomerRef") ?? ret.Element("CustomerRef")),
                    TotalAmount = GetDecimal(ret, "TotalAmount"),
                    TimeModified = GetValue(ret, "TimeModified")
                });
            }

            return list;
        }

        private static List<EstimateLine> ParseEstimateLines(XDocument doc)
        {
            var list = new List<EstimateLine>();
            foreach (var est in doc.Descendants())
            {
                if (est.Name.LocalName != "EstimateRet")
                {
                    continue;
                }

                var estimateTxnId = GetValue(est, "TxnID");
                foreach (var line in est.Elements())
                {
                    if (line.Name.LocalName != "EstimateLineRet")
                    {
                        continue;
                    }

                    list.Add(new EstimateLine
                    {
                        EstimateTxnId = estimateTxnId,
                        LineId = GetValue(line, "TxnLineID"),
                        ItemRef = ParseRef(line.Element(line.Name.Namespace + "ItemRef") ?? line.Element("ItemRef")),
                        Desc = GetValue(line, "Desc"),
                        Quantity = GetDecimal(line, "Quantity"),
                        Rate = GetDecimal(line, "Rate"),
                        Amount = GetDecimal(line, "Amount"),
                        ClassRef = ParseRef(line.Element(line.Name.Namespace + "ClassRef") ?? line.Element("ClassRef"))
                    });
                }
            }

            return list;
        }

        private static string GetValue(XElement parent, string name)
        {
            return parent.Element(parent.Name.Namespace + name)?.Value ?? parent.Element(name)?.Value;
        }

        private static bool? GetBool(XElement parent, string name)
        {
            var value = GetValue(parent, name);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (bool.TryParse(value, out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private static decimal? GetDecimal(XElement parent, string name)
        {
            var value = GetValue(parent, name);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private static Address ParseAddress(XElement address)
        {
            if (address == null)
            {
                return null;
            }

            return new Address
            {
                Addr1 = GetValue(address, "Addr1"),
                Addr2 = GetValue(address, "Addr2"),
                Addr3 = GetValue(address, "Addr3"),
                Addr4 = GetValue(address, "Addr4"),
                Addr5 = GetValue(address, "Addr5"),
                City = GetValue(address, "City"),
                State = GetValue(address, "State"),
                PostalCode = GetValue(address, "PostalCode"),
                Country = GetValue(address, "Country")
            };
        }

        private static Ref ParseRef(XElement refEl)
        {
            if (refEl == null)
            {
                return null;
            }

            return new Ref
            {
                ListId = GetValue(refEl, "ListID"),
                FullName = GetValue(refEl, "FullName")
            };
        }

        [DataContract]
        private sealed class Customer
        {
            [DataMember(Name = "list_id")] public string ListId { get; set; }
            [DataMember(Name = "name")] public string Name { get; set; }
            [DataMember(Name = "full_name")] public string FullName { get; set; }
            [DataMember(Name = "is_active")] public bool? IsActive { get; set; }
            [DataMember(Name = "email")] public string Email { get; set; }
            [DataMember(Name = "phone")] public string Phone { get; set; }
            [DataMember(Name = "bill_address")] public Address BillAddress { get; set; }
            [DataMember(Name = "time_modified")] public string TimeModified { get; set; }
        }

        [DataContract]
        private sealed class Invoice
        {
            [DataMember(Name = "txn_id")] public string TxnId { get; set; }
            [DataMember(Name = "ref_number")] public string RefNumber { get; set; }
            [DataMember(Name = "txn_date")] public string TxnDate { get; set; }
            [DataMember(Name = "customer_ref")] public Ref CustomerRef { get; set; }
            [DataMember(Name = "subtotal")] public decimal? Subtotal { get; set; }
            [DataMember(Name = "sales_tax_total")] public decimal? SalesTaxTotal { get; set; }
            [DataMember(Name = "balance_remaining")] public decimal? BalanceRemaining { get; set; }
            [DataMember(Name = "is_paid")] public bool? IsPaid { get; set; }
            [DataMember(Name = "time_modified")] public string TimeModified { get; set; }
        }

        [DataContract]
        private sealed class InvoiceLine
        {
            [DataMember(Name = "invoice_txn_id")] public string InvoiceTxnId { get; set; }
            [DataMember(Name = "line_id")] public string LineId { get; set; }
            [DataMember(Name = "item_ref")] public Ref ItemRef { get; set; }
            [DataMember(Name = "desc")] public string Desc { get; set; }
            [DataMember(Name = "quantity")] public decimal? Quantity { get; set; }
            [DataMember(Name = "rate")] public decimal? Rate { get; set; }
            [DataMember(Name = "amount")] public decimal? Amount { get; set; }
            [DataMember(Name = "class_ref")] public Ref ClassRef { get; set; }
        }

        [DataContract]
        private sealed class Bill
        {
            [DataMember(Name = "txn_id")] public string TxnId { get; set; }
            [DataMember(Name = "ref_number")] public string RefNumber { get; set; }
            [DataMember(Name = "txn_date")] public string TxnDate { get; set; }
            [DataMember(Name = "due_date")] public string DueDate { get; set; }
            [DataMember(Name = "vendor_ref")] public Ref VendorRef { get; set; }
            [DataMember(Name = "amount_due")] public decimal? AmountDue { get; set; }
            [DataMember(Name = "balance_remaining")] public decimal? BalanceRemaining { get; set; }
            [DataMember(Name = "is_paid")] public bool? IsPaid { get; set; }
            [DataMember(Name = "time_modified")] public string TimeModified { get; set; }
        }

        [DataContract]
        private sealed class BillLine
        {
            [DataMember(Name = "bill_txn_id")] public string BillTxnId { get; set; }
            [DataMember(Name = "line_id")] public string LineId { get; set; }
            [DataMember(Name = "line_kind")] public string LineKind { get; set; }
            [DataMember(Name = "item_ref")] public Ref ItemRef { get; set; }
            [DataMember(Name = "account_ref")] public Ref AccountRef { get; set; }
            [DataMember(Name = "desc")] public string Desc { get; set; }
            [DataMember(Name = "quantity")] public decimal? Quantity { get; set; }
            [DataMember(Name = "rate_or_cost")] public decimal? RateOrCost { get; set; }
            [DataMember(Name = "amount")] public decimal? Amount { get; set; }
            [DataMember(Name = "class_ref")] public Ref ClassRef { get; set; }
        }

        [DataContract]
        private sealed class Estimate
        {
            [DataMember(Name = "txn_id")] public string TxnId { get; set; }
            [DataMember(Name = "ref_number")] public string RefNumber { get; set; }
            [DataMember(Name = "txn_date")] public string TxnDate { get; set; }
            [DataMember(Name = "customer_ref")] public Ref CustomerRef { get; set; }
            [DataMember(Name = "total_amount")] public decimal? TotalAmount { get; set; }
            [DataMember(Name = "time_modified")] public string TimeModified { get; set; }
        }

        [DataContract]
        private sealed class EstimateLine
        {
            [DataMember(Name = "estimate_txn_id")] public string EstimateTxnId { get; set; }
            [DataMember(Name = "line_id")] public string LineId { get; set; }
            [DataMember(Name = "item_ref")] public Ref ItemRef { get; set; }
            [DataMember(Name = "desc")] public string Desc { get; set; }
            [DataMember(Name = "quantity")] public decimal? Quantity { get; set; }
            [DataMember(Name = "rate")] public decimal? Rate { get; set; }
            [DataMember(Name = "amount")] public decimal? Amount { get; set; }
            [DataMember(Name = "class_ref")] public Ref ClassRef { get; set; }
        }

        [DataContract]
        private sealed class Address
        {
            [DataMember(Name = "addr1")] public string Addr1 { get; set; }
            [DataMember(Name = "addr2")] public string Addr2 { get; set; }
            [DataMember(Name = "addr3")] public string Addr3 { get; set; }
            [DataMember(Name = "addr4")] public string Addr4 { get; set; }
            [DataMember(Name = "addr5")] public string Addr5 { get; set; }
            [DataMember(Name = "city")] public string City { get; set; }
            [DataMember(Name = "state")] public string State { get; set; }
            [DataMember(Name = "postal_code")] public string PostalCode { get; set; }
            [DataMember(Name = "country")] public string Country { get; set; }
        }

        [DataContract]
        private sealed class Ref
        {
            [DataMember(Name = "list_id")] public string ListId { get; set; }
            [DataMember(Name = "full_name")] public string FullName { get; set; }
        }
    }
}
