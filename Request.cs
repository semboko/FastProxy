namespace fast_proxy;

public class HTTPRequestData
{
    
    public string method { get; set; }
    public string target { get; set; }
    public string version { get; set; }

    public HTTPRequestData(string httpMessage)
    {
        string requestLine = httpMessage.Split("\n")[0];
        string[] requestParts = requestLine.Split(" ");
        method = requestParts[0];
        target = requestParts[1];
        version = requestParts[2];
    }
}