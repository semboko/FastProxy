using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace fast_proxy;

public class Program
{
    static EndPoint CreateEndPoint(string url)
    {
        string[] hp = url.Split(":");
        IPAddress ipAddr = IPAddress.Parse(hp[0]);
        IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, Int32.Parse(hp[1]));
        return ipEndPoint;
    }
    static async Task<string> ParseRequest(Socket conn)
    {
        Console.WriteLine("Parsing request...");
        byte[] bytes = new Byte[4096];
        string data = "";

        int ms = 5;
        while (ms < 1000)
        {
            if (conn.Available <= 0)
            {
                await Task.Delay(ms);
            }
            ms *= 5;
        }
        
        while (conn.Available > 0)
        {
            Console.WriteLine("Attempting to receive a chunk");
            int numByte = await conn.ReceiveAsync(bytes);
            Console.WriteLine("Received " + numByte + " bytes");
            if (numByte == 0)
            {
                Console.WriteLine("Nothing to receive");
                break;
            };
            data += Encoding.ASCII.GetString(bytes, 0, numByte);
            Console.WriteLine(data);
        }
        Console.WriteLine("All data received: ");
        return data;
    }
    static async Task SendAll(Socket conn, string data)
    {   
        Console.WriteLine("Sending data to the socket" + conn);
        byte[] bytesToSend = Encoding.UTF8.GetBytes(data);
        int bytesSent = 0;
        while (bytesSent < bytesToSend.Length)
        {
            Console.WriteLine("Sending a chunk");
            bytesSent += await conn.SendAsync(bytesToSend[bytesSent..]);
            Console.WriteLine("Chunk Sent " + bytesSent);
        }
        Console.WriteLine("Finished sending");
    }

    static ConfigBlock[] ParseConfig (string filename)
    {
        string jsonString = File.ReadAllText(filename);
        ConfigBlock[] config = JsonSerializer.Deserialize<ConfigBlock[]>(jsonString)!;
        return config;
    }
    static async Task<string> MakeRemoteRequest(string proxy, string data)
    {
        Socket remoteConnection = new(SocketType.Stream, ProtocolType.Tcp);
        await remoteConnection.ConnectAsync(CreateEndPoint(proxy));
        Console.WriteLine("Established remote connection with " + remoteConnection);

        await SendAll(remoteConnection, data);
        string remoteResponse = await ParseRequest(remoteConnection);
        return remoteResponse;
    }
    static async Task HandleRequests(Socket clientConnection, ConfigBlock config)
    {
        Console.WriteLine("Handling request...");
        string data = await ParseRequest(clientConnection);
        Console.WriteLine("Request: " + data);
        if (data == "")
        {
            byte[] response = Encoding.UTF8.GetBytes("No http header found");
            await clientConnection.SendAsync(response);
            clientConnection.Close();
            return;
        }
            
        HTTPRequestData reqData = new HTTPRequestData(data);

        string? proxy = config.getProxyPath(reqData.target);
        if (proxy == null)
        {   
            Console.WriteLine("No proxy found for the request: " + reqData.target);
            clientConnection.Close();
            return;
        }

        string remoteResponse = await MakeRemoteRequest(proxy, data);
        await SendAll(clientConnection, remoteResponse);
            
        clientConnection.Shutdown(SocketShutdown.Send);
        clientConnection.Close();
    }
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting server...");
        ConfigBlock config = ParseConfig("./test-config.json")[0];
        Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        listener.Bind(CreateEndPoint(config.server.listen));
        listener.Listen(1);
        Console.WriteLine("Ready. Listening " + config.server.listen);
        while (true)
        {
            Socket clientConnection = await listener.AcceptAsync();
            try
            {
                Console.WriteLine("==========");
                await HandleRequests(clientConnection, config);
            }
            catch (Exception e)
            {   
                clientConnection.Shutdown(SocketShutdown.Send);
                Console.WriteLine("Request handling error: " + e);
            }
        }
    }
}

