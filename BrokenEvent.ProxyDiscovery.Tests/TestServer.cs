using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

namespace BrokenEvent.ProxyDiscovery.Tests
{
  class TestServer
  {
    private TcpListener listener;
    private TcpClient client;
    protected List<TestServerExchange> exchanges = new List<TestServerExchange>();

    public string Error { get; private set; }

    public TestServer AddExchange(string request, string response)
    {
      exchanges.Add(new TestServerExchange(request, response));
      return this;
    }

    public async Task Start(int port, int bufferSize = 1024)
    {
      listener = new TcpListener(IPAddress.Any, port);
      listener.Start();

      client = await listener.AcceptTcpClientAsync();
      try
      {
        await HandleConnection(client.GetStream(), bufferSize);
      }
      catch (Exception e)
      {
        Error = e.Message;
      }
      finally
      {
        client.Dispose();
        client = null;
        listener.Stop();
      }
    }

    public void Stop()
    {
      client?.Dispose();
    }

    protected virtual async Task HandleConnection(NetworkStream stream, int bufferSize)
    {
      byte[] buffer = new byte[bufferSize];

      foreach (TestServerExchange exchange in exchanges)
      {
        int received = await stream.ReadAsync(buffer, 0, bufferSize);

        if (received == 0)
          Assert.Fail("Premature end of connection");

        string request = Encoding.ASCII.GetString(buffer, 0, received);

        if (exchange.Request != null)
          Assert.AreEqual(exchange.Request, request);

        byte[] response = Encoding.ASCII.GetBytes(exchange.Response);

        await stream.WriteAsync(response, 0, response.Length);
      }
    }

    protected class TestServerExchange
    {
      public TestServerExchange(string request, string response)
      {
        Request = request;
        Response = response;
      }

      public string Request { get; }
      public string Response { get; }
    }

    public override string ToString()
    {
      return $"Exchanges: {exchanges.Count}";
    }
  }
}
