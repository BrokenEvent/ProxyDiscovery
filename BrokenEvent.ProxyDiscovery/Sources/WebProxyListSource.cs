using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Sources
{
  /// <summary>
  /// Loads a proxy list from a web server.
  /// </summary>
  public sealed class WebProxyListSource: IProxyListSource
  {
    /// <summary>
    /// Gets the optional URL arguments to be added to the original URL.
    /// </summary>
    public List<WebProxyListSourceArg> Args { get; } = new List<WebProxyListSourceArg>();

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

      foreach (WebProxyListSourceArg arg in Args)
      {
        if (string.IsNullOrWhiteSpace(arg.Name))
          yield return "URL argument name cannot be empty";
        if (string.IsNullOrWhiteSpace(arg.Value))
          yield return "URL argument value cannot be empty";
      }
    }

    /// <summary>
    /// Gets or sets the URL to download proxy list from.
    /// </summary>
    public string Url { get; set; }

    private Uri GetActualUri()
    {
      Uri uri = new Uri(Url);
      if (Args.Count == 0)
        return uri;

      // https://stackoverflow.com/a/19679135/4588884
      UriBuilder uriBuilder = new UriBuilder(uri);
      NameValueCollection query = HttpUtility.ParseQueryString(uriBuilder.Query);
      foreach (WebProxyListSourceArg arg in Args)
        query[arg.Name] = arg.Value;

      uriBuilder.Query = query.ToString();

      return uriBuilder.Uri;
    }

    /// <inheritdoc />
    public Task<string> GetContentAsync(CancellationToken ct, Action<string> onError)
    {
      Uri uri = GetActualUri();

      using (WebClient client = new WebClient())
      {
        client.Proxy = new WebProxy();

        using (ct.Register(client.CancelAsync))
          return client.DownloadStringTaskAsync(uri);
      }
    }

    public override string ToString()
    {
      return $"Web: {Url}";
    }
  }

  public class WebProxyListSourceArg
  {
    public string Name { get; set; }
    public string Value { get; set; }
  }
}
