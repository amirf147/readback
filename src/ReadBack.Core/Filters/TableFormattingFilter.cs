using System.Text.RegularExpressions;
using ReadBack.Core.Models;

namespace ReadBack.Core.Filters;

/// <summary>
/// Converts markdown tables into clean spoken descriptions instead of pipe delimiters.
/// </summary>
public class TableFormattingFilter : INarrationFilter
{
    public string Name => "Table Formatter";
    public int Order => 50;
    public bool IsEnabled { get; set; } = true;

    private static readonly Regex TableSeparatorRegex = new(@"^\s*\|?\s*[-:]+[-| :]*\|?\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex TableRowRegex = new(@"^\s*\|(.*)\|\s*$", RegexOptions.Compiled | RegexOptions.Multiline);

    public string Process(string text, FilterContext context)
    {
        if (!IsEnabled) return text;

        // Strip table divider lines (|---|---|)
        string result = TableSeparatorRegex.Replace(text, "");

        // Convert table rows | a | b | c | -> a, b, c.
        result = TableRowRegex.Replace(result, match =>
        {
            string content = match.Groups[1].Value;
            var parts = content.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return string.Join(", ", parts) + ".";
        });

        return result;
    }
}
