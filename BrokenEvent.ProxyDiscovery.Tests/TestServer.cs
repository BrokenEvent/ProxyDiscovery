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

    public TestServer AddExchange(string request, string headers, string content)
    {
      exchanges.Add(new TestServerExchange(request, $"{headers}\r\nContent-Length:{content.Length}\r\n\r\n{content}"));
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

    private static async Task<byte[]> Receive(NetworkStream stream, string expected, int bufferSize)
    {
      int minimumExpected = expected == null ? 1 : expected.Length;

      byte[] buffer = new byte[bufferSize];
      int totalReceived = 0;
      while (totalReceived < minimumExpected)
      {
        int received = await stream.ReadAsync(buffer, totalReceived, bufferSize - totalReceived);

        if (received == 0)
          Assert.Fail("Premature end of connection");

        totalReceived += received;
      }

      byte[] result = new byte[totalReceived];
      Array.Copy(buffer, result, totalReceived);

      return result;
    }

    protected virtual async Task HandleConnection(NetworkStream stream, int bufferSize)
    {
      foreach (TestServerExchange exchange in exchanges)
      {
        byte[] buffer = await Receive(stream, exchange.Request, bufferSize);

        string request = Encoding.ASCII.GetString(buffer, 0, buffer.Length);

        string responseString = exchange.Response;

        if (exchange.Request != null && exchange.Request != request)
          responseString = "HTTP/1.0 400 Bad Request\r\n\r\n";

        if (responseString == null)
          continue;

        byte[] response = Encoding.ASCII.GetBytes(responseString);

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
