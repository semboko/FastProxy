using System.Text.Json.Serialization;

namespace fast_proxy;


public class Path
{
    [JsonRequired]
    public string url { get; set; }
    [JsonRequired]
    public string proxy { get; set; }
}

public class Server
{
    [JsonRequired]
    public string listen { get; set; }
    [JsonRequired]
    public Path[] paths { get; set; }
}

public class ConfigBlock
{
    [JsonRequired]
    public Server server { get; set; }

    public string? getProxyPath(string target)
    {
        foreach (var p in server.paths)
        {
            if (p.url == target)
            {
                return p.proxy;
            }
        }

        return null;
    }
}