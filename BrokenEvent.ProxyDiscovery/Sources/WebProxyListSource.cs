using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Sources
{
  /// <summary>
  /// Loads a proxy list from a web server.
  /// </summary>
  public sealed class WebProxyListSource: IProxyListSource
  {
    /// <summary>
    /// Creates an instance of the Web proxy list source.
    /// </summary>
    /// <param name="url">URL to download proxy list from.</param>
    public WebProxyListSource(string url)
    {
      Url = url;
    }

    /// <inheritdoc />
    public IEnumerable<string> Validate()
    {
      if (string.IsNullOrWhiteSpace(Url))
        yield return "Source URL is missing";
    }

    /// <summary>
    /// Gets or sets the URL to download proxy list from.
    /// </summary>
    public string Url { get; set; }

    /// <inheritdoc />
    public string GetContent()
    {
      return new WebClient().DownloadString(Url);
    }

    /// <inheritdoc />
    public Task<string> GetContentAsync(CancellationToken ct)
    {
      WebClient client = new WebClient();
      using (ct.Register(client.CancelAsync))
        return client.DownloadStringTaskAsync(Url);
    }

    public override string ToString()
    {
      return $"Web: {Url}";
    }
  }
}
