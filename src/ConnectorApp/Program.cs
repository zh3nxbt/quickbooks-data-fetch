using System;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace ConnectorApp
{
    internal static class Program
    {
        private static void Main()
        {
            var baseAddress = ConfigurationManager.AppSettings["BaseAddress"] ?? "http://localhost:8000/QBWC";
            var service = new WebConnectorService();

            using (var host = new ServiceHost(service, new Uri(baseAddress)))
            {
                host.AddServiceEndpoint(typeof(IWebConnectorService), new BasicHttpBinding(BasicHttpSecurityMode.None)
                {
                    MaxReceivedMessageSize = 10 * 1024 * 1024
                }, "");

                var smb = new ServiceMetadataBehavior
                {
                    HttpGetEnabled = true
                };
                host.Description.Behaviors.Add(smb);

                host.Open();
                Console.WriteLine("QBWC SOAP server listening at: {0}", baseAddress);
                Console.WriteLine("WSDL: {0}?wsdl", baseAddress);
                Console.WriteLine("Press Enter to stop.");
                Console.ReadLine();
                host.Close();
            }
        }
    }
}
