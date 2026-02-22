using LiteNetLib;

namespace ReconEngine.NetworkingServer;

public class ReconNetCatServer
{
    public readonly int UPDATE_TIME = 50; // 20 updates per second
    public readonly int KEEP_ALIVE = 30000; // 30 second timeout

    public readonly int MaxPlayers = 6;
    public bool ServerReady { get; private set; } = false;

    private readonly EventBasedNetListener _listener;
    private readonly NetManager _server;
    public ReconNetCatServer()
    {
        _listener = new();
        _server = new(_listener)
        {
            UpdateTime = UPDATE_TIME,
            DisconnectTimeout = KEEP_ALIVE,
            AllowPeerAddressChange = true, // allow address change in case the peer loses connection and switches networks
        };
    }

    public bool Start(int port)
    {
        if (!_server.Start(port))
        {
            Console.Error.WriteLine($"Failed to start server with port '{port}'!");
            return false;
        }

        _listener.ConnectionRequestEvent += request =>
        {
            if (ServerReady && _server.GetPeersCount(ConnectionState.Connected) < MaxPlayers)
                request.AcceptIfKey("HELLOBITTER!!!");
            else
                request.Reject();
        };

        _listener.PeerConnectedEvent += peer =>
        {
            Console.WriteLine("We got connection: {0}", peer.Address);
        };

        Console.WriteLine($"Started server with port '{port}'.");

        ServerReady = true;
        return true;
    }

    public void Stop()
    {
        if (!ServerReady) return;
        _server.Stop(true); // sends disconnect messages
    }

    public void Update()
    {
        if (!ServerReady) return;
        _server.PollEvents();
    }
}