using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using BrokenEvent.ProxyDiscovery.Helpers;
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

      if (IsUploadMethod() && string.IsNullOrWhiteSpace(FormData))
        yield return $"Using upload HTTP method ({HttpMethod}), but FormData is empty";
    }

    /// <summary>
    /// Gets or sets the URL to download proxy list from. For syntax see remarks.
    /// </summary>
    /// <remarks>
    /// <para>If the source contains several pages, the substitution syntax should be used. It contains optional part in square brackets ([, ])
    /// with <c>$</c> as the page number. The page number starts from 1. See examples.</para>
    /// <para>The presence of the page number for the first page
    /// depends on <see cref="SkipNumberForFirstPage"/>.</para>
    /// <para>Whether the source is multipaged depends on presence of substitution marker.</para>
    /// </remarks>
    /// <example>
    /// <para><c>https://brokenevent.com</c> - no subsitutions.</para>
    /// <para><c>https://brokenevent.com[/$]</c> - will result <c>https://brokenevent.com</c> with no page number and <c>https://brokenevent.com/1</c> for
    /// page number 1.</para>
    /// <para><c>https://brokenevent.com?id=test[&amp;page=$]</c> - will result <c>https://brokenevent.com?id=test</c> with no page number and
    ///  <c>https://brokenevent.com?id=test&amp;page=10</c> for page number 10.</para>
    /// </example>
    public string Url { get; set; }

    /// <summary>
    /// Gets the value indicating whether to skip page number for the first page.
    /// </summary>
    /// <seealso cref="Url"/>
    public bool SkipNumberForFirstPage { get; set; } = true;

    /// <summary>
    /// Gets or sets the HTTP method to use.
    /// </summary>
    /// <remarks>
    /// <para>POST and similar methods require <see cref="FormData"/> value to be set.</para>
    /// <para>The current implementation supports only x-www-form-urlencoded.</para>
    /// </remarks>
    public string HttpMethod { get; set; } = "GET";

    /// <summary>
    /// Gets or sets the content of HTML form. Used only if <see cref="HttpMethod"/> is set to upload method (like POST).
    /// </summary>
    public string FormData { get; set; }

    /// <inheritdoc />
    public bool HasPages
    {
      get { return StringHelpers.CheckUrlForPageNumber(Url) || !string.IsNullOrEmpty(FormData) && StringHelpers.CheckUrlForPageNumber(FormData); }
    }

    private bool IsUploadMethod()
    {
      return HttpMethod == "POST" || HttpMethod == "PUT" || HttpMethod == "PATCH";
    }

    public Uri GetActualUri(int pageNumber)
    {
      return new Uri(
          StringHelpers.ProcessUrl(
            Url,
            pageNumber == 0 || (pageNumber == 1 && SkipNumberForFirstPage) ? (int?)null : pageNumber)
        );
    }

    private string GetUploadContent(int pageNumber)
    {
      return StringHelpers.ProcessUrl(
          FormData,
          pageNumber == 0 || (pageNumber == 1 && SkipNumberForFirstPage) ? (int?)null : pageNumber
        );
    }

    /// <inheritdoc />
    public Task<string> GetContentAsync(CancellationToken ct, int pageNumber, Action<string> onError)
    {
      Uri uri = GetActualUri(pageNumber);
      ServicePointManager.Expect100Continue = false;

      using (WebClient client = new WebClient())
      {
        client.Proxy = new WebProxy();

        using (ct.Register(client.CancelAsync))
        {
          if (!IsUploadMethod())
            return client.DownloadStringTaskAsync(uri);

          // upload form-data
          client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
          return client.UploadStringTaskAsync(uri, HttpMethod, GetUploadContent(pageNumber));
        }
      }
    }

    public override string ToString()
    {
      return $"Web: {Url}";
    }
  }
}
