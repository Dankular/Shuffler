using System.Collections.ObjectModel;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Shuffler.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        SkippedItemIds = [];
        AutoSkipOnError = true;
    }

    /// <summary>
    /// Gets item IDs that are permanently skipped during shuffle (bad encodings etc).
    /// </summary>
    public Collection<string> SkippedItemIds { get; }

    /// <summary>
    /// Gets or sets a value indicating whether playback errors automatically add the item to the skip list.
    /// </summary>
    public bool AutoSkipOnError { get; set; }
}
