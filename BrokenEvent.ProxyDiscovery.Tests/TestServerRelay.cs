using System.Net.Sockets;
using System.Threading.Tasks;

namespace BrokenEvent.ProxyDiscovery.Tests
{
  class TestServerRelay: TestServer
  {
    public string Target { get; }

    public TestServerRelay(string target)
    {
      Target = target;
    }

    protected override async Task HandleConnection(NetworkStream clientStream, int bufferSize)
    {
      // handle expected requests first
      await base.HandleConnection(clientStream, bufferSize);

      TcpClient serverClient = new TcpClient();
      await serverClient.ConnectAsync(Target, 443);

      NetworkStream serverStream = serverClient.GetStream();

      Relay clientToServerRelay = new Relay(clientStream, serverStream);
      Relay serverToClientRelay = new Relay(serverStream, clientStream);

      await Task.WhenAll(Task.Run(clientToServerRelay.Go), Task.Run(serverToClientRelay.Go));
    }

    private class Relay
    {
      public NetworkStream FromStream { get; }
      public NetworkStream ToStream { get; }

      public Relay(NetworkStream fromStream, NetworkStream toStream)
      {
        FromStream = fromStream;
        ToStream = toStream;
      }

      public async Task Go()
      {
        byte[] buffer = new byte[1024];

        while (true)
        {
          try
          {
            int received = await FromStream.ReadAsync(buffer, 0, buffer.Length);

            if (received == 0)
              break; // EOF

            await ToStream.WriteAsync(buffer, 0, received);
          }
          catch
          {
            // ignore
          }
        }

        ToStream.Close();
      }
    }

    public override string ToString()
    {
      return $"Exchanges: {exchanges.Count}";
    }
  }
}
