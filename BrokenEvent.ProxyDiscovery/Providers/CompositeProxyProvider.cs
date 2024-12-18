﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Providers
{
  /// <summary>
  /// Provides a proxy list by getting the data with <see cref="IProxyListSource"/> implementation and transforming
  /// it to object model with <see cref="IProxyListParser"/> implementation.
  /// </summary>
  public sealed class CompositeProxyProvider: IProxyListProvider
  {
    /// <summary>
    /// Gets or sets the proxy list source.
    /// </summary>
    public IProxyListSource Source { get; set; }

    /// <summary>
    /// Gets or sets the proxy list parser.
    /// </summary>
    public IProxyListParser Parser { get; set; }

    /// <summary>
    /// Creates an instance of the proxy provider.
    /// </summary>
    /// <param name="source">Proxy list data source.</param>
    /// <param name="parser">Proxy list parser.</param>
    public CompositeProxyProvider(IProxyListSource source, IProxyListParser parser)
    {
      Source = source;
      Parser = parser;
    }

    /// <inheritdoc />
    public IEnumerable<string> Validate()
    {
      if (Source == null)
        yield return "Proxy list source cannot be null";
      else
        foreach (string s in Source.Validate())
          yield return $"[Source] {s}";

      if (Parser == null)
        yield return "Proxy list parser cannot be null";
      else
        foreach (string s in Parser.Validate())
          yield return $"[Parser] {s}";
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProxyInformation>> GetProxiesAsync(CancellationToken ct, Action<string> onError)
    {
      if (Source.HasPages)
      {
        List<ProxyInformation> results = new List<ProxyInformation>();
        for (int pageNumber = 1;; pageNumber++)
        {
          // get nth page content
          string pageContent;
          try
          {
            pageContent = await Source.GetContentAsync(ct, pageNumber, onError);
          }
          catch
          {
            // ignore the exception
            break;
          }

          // no content, so stop iteration
          if (string.IsNullOrWhiteSpace(pageContent))
            break;

          int resultsCount = results.Count;
          results.AddRange(Parser.ParseContent(pageContent, onError));

          // no proxies added, so stop iteration
          if (resultsCount == results.Count)
            break;
        }

        return results;
      }

      // get the content
      string content = await Source.GetContentAsync(ct, 0, onError);

      // do we have content?
      if (!string.IsNullOrWhiteSpace(content))
        return Parser.ParseContent(content, onError);

      onError("Proxy list source returned empty content");
      return null;
    }

    public override string ToString()
    {
      return $"Composite: {Source} + {Parser}";
    }
  }
}
