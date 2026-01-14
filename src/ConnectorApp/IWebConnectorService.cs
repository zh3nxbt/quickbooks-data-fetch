using System.ServiceModel;

namespace ConnectorApp
{
    [ServiceContract(Namespace = "http://developer.intuit.com/")]
    [System.ServiceModel.XmlSerializerFormat]
    public interface IWebConnectorService
    {
        [OperationContract(Action = "http://developer.intuit.com/serverVersion", ReplyAction = "http://developer.intuit.com/serverVersionResponse")]
        string serverVersion();

        [OperationContract(Action = "http://developer.intuit.com/clientVersion", ReplyAction = "http://developer.intuit.com/clientVersionResponse")]
        string clientVersion(string strVersion);

        [OperationContract(Action = "http://developer.intuit.com/authenticate", ReplyAction = "http://developer.intuit.com/authenticateResponse")]
        string[] authenticate(string strUserName, string strPassword);

        [OperationContract(Action = "http://developer.intuit.com/sendRequestXML", ReplyAction = "http://developer.intuit.com/sendRequestXMLResponse")]
        string sendRequestXML(string ticket, string strHCPResponse, string strCompanyFileName, string qbXMLCountry, int qbXMLMajorVers, int qbXMLMinorVers);

        [OperationContract(Action = "http://developer.intuit.com/receiveResponseXML", ReplyAction = "http://developer.intuit.com/receiveResponseXMLResponse")]
        int receiveResponseXML(string ticket, string response, string hresult, string message);

        [OperationContract(Action = "http://developer.intuit.com/getLastError", ReplyAction = "http://developer.intuit.com/getLastErrorResponse")]
        string getLastError(string ticket);

        [OperationContract(Action = "http://developer.intuit.com/closeConnection", ReplyAction = "http://developer.intuit.com/closeConnectionResponse")]
        string closeConnection(string ticket);
    }
}
