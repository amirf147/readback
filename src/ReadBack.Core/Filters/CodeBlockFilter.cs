using System.Text.RegularExpressions;
using ReadBack.Core.Models;

namespace ReadBack.Core.Filters;

/// <summary>
/// Detects fenced programming code blocks and replaces them with clean speech summaries.
/// </summary>
public class CodeBlockFilter : INarrationFilter
{
    public string Name => "Code Block Sanitizer";
    public int Order => 10;
    public bool IsEnabled { get; set; } = true;

    private static readonly Regex FencedCodeRegex = new(
        @"```([a-zA-Z0-9_\-\.\+#]*)\s*[
]+([\s\S]*?)```",
        RegexOptions.Compiled
    );

    private static readonly Regex InlineCodeRegex = new(
        @"`([^`
]+)`",
        RegexOptions.Compiled
    );

    public string Process(string text, FilterContext context)
    {
        if (!IsEnabled || !context.Settings.SkipCodeBlocks)
            return text;

        // Replace fenced code blocks with clean audio notification
        string processed = FencedCodeRegex.Replace(text, match =>
        {
            string lang = match.Groups[1].Value.Trim();
            return string.IsNullOrWhiteSpace(lang)
                ? "(Omitted code snippet)"
                : $"(Omitted {lang} code snippet)";
        });

        // Strip backticks from inline code while preserving the text inside
        processed = InlineCodeRegex.Replace(processed, "$1");

        return processed;
    }
}
