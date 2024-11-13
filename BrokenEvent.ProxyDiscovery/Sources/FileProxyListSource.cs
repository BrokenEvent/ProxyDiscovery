using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Sources
{
  /// <summary>
  /// Loads a list of proxies from a text file.
  /// </summary>
  public sealed class FileProxyListSource: IProxyListSource
  {
    /// <summary>
    /// Creates an instance of the file proxy list source.
    /// </summary>
    /// <param name="filePath">Absolute or relative path to a file to load list from.</param>
    public FileProxyListSource(string filePath)
    {
      FilePath = filePath;
    }

    /// <inheritdoc />
    public IEnumerable<string> Validate()
    {
      if (string.IsNullOrWhiteSpace(FilePath))
        yield return "FilePath is missing";
    }

    /// <summary>
    /// Gets or sets absolute or relative path to a file to load list from.
    /// </summary>
    public string FilePath { get; set; }

    /// <inheritdoc />
    public bool HasPages
    {
      get { return false; }
    }

    /// <inheritdoc />
    public Task<string> GetContentAsync(CancellationToken ct, int pageNumber, Action<string> onError)
    {
      if (!File.Exists(FilePath))
      {
        onError($"The file '{FilePath}' does not exists");
        return null;
      }

      return Task.Run(() => File.ReadAllText(FilePath), ct);
    }

    public override string ToString()
    {
      return $"File: {FilePath}";
    }
  }
}
