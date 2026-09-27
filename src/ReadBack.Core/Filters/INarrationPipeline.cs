using ReadBack.Core.Models;

namespace ReadBack.Core.Filters;

/// <summary>
/// Coordinates the execution of registered narration filters.
/// </summary>
public interface INarrationPipeline
{
    IReadOnlyList<INarrationFilter> Filters { get; }
    void RegisterFilter(INarrationFilter filter);
    bool RemoveFilter(string filterName);
    string Process(string text, FilterContext context);
}
