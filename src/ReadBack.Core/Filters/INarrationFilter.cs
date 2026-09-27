using ReadBack.Core.Models;

namespace ReadBack.Core.Filters;

/// <summary>
/// Defines an extensible filter in the text narration pipeline.
/// </summary>
public interface INarrationFilter
{
    /// <summary>
    /// Unique display name for this filter.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Execution order in the pipeline. Lower runs earlier.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Whether this filter is currently active.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Transforms or cleans the given text prior to speech synthesis.
    /// </summary>
    string Process(string text, FilterContext context);
}
