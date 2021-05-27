using System.Collections.Generic;

namespace BrokenEvent.ProxyDiscovery.Interfaces
{
  /// <summary>
  /// Object capable to perform validation of its settings.
  /// </summary>
  public interface IValidatable
  {
    /// <summary>
    /// Validates the object.
    /// </summary>
    /// <returns>Enumeration of validation errors. If the validation is OK, returns empty enumeration.</returns>
    IEnumerable<string> Validate();
  }
}
