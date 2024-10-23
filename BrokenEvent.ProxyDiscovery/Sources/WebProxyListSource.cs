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
    public List<Tuple<string, string>> Args { get; } = new List<Tuple<string, string>>();

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

    private Uri GetActualUri()
    {
      Uri uri = new Uri(Url);
      if (Args.Count == 0)
        return uri;

      // https://stackoverflow.com/a/19679135/4588884
      UriBuilder uriBuilder = new UriBuilder(uri);
      NameValueCollection query = HttpUtility.ParseQueryString(uriBuilder.Query);
      foreach (Tuple<string, string> arg in Args)
        query[arg.Item1] = arg.Item2;

      uriBuilder.Query = query.ToString();

      return uriBuilder.Uri;
    }

    /// <inheritdoc />
    public Task<string> GetContentAsync(CancellationToken ct)
    {
      Uri uri = GetActualUri();

      WebClient client = new WebClient();
      using (ct.Register(client.CancelAsync))
        return client.DownloadStringTaskAsync(uri);
    }

    public override string ToString()
    {
      return $"Web: {Url}";
    }
  }
}
