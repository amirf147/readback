using System.Text.RegularExpressions;
using ReadBack.Core.Models;

namespace ReadBack.Core.Filters;

public class NarrationPipeline : INarrationPipeline
{
    private readonly List<INarrationFilter> _filters = new();

    public IReadOnlyList<INarrationFilter> Filters => _filters.OrderBy(f => f.Order).ToList();

    public NarrationPipeline()
    {
        RegisterFilter(new CodeBlockFilter());
        RegisterFilter(new MarkdownFormattingFilter());
        RegisterFilter(new UrlSimplifierFilter());
        RegisterFilter(new LatexMathFilter());
        RegisterFilter(new TableFormattingFilter());
    }

    public void RegisterFilter(INarrationFilter filter)
    {
        _filters.RemoveAll(f => f.Name.Equals(filter.Name, StringComparison.OrdinalIgnoreCase));
        _filters.Add(filter);
    }

    public bool RemoveFilter(string filterName)
    {
        return _filters.RemoveAll(f => f.Name.Equals(filterName, StringComparison.OrdinalIgnoreCase)) > 0;
    }

    public string Process(string text, FilterContext context)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        string current = text;
        foreach (var filter in Filters.Where(f => f.IsEnabled))
        {
            try
            {
                current = filter.Process(current, context);
            }
            catch
            {
                // Resilient: If an experimental filter fails, continue the pipeline
            }
        }

        current = Regex.Replace(current, @"[ \t]+", " ");
        current = Regex.Replace(current, @"(\r?\n){3,}", "\n\n");
        return current.Trim();
    }
}
