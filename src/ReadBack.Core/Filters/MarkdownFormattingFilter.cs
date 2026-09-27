using System.Text.RegularExpressions;
using ReadBack.Core.Models;

namespace ReadBack.Core.Filters;

/// <summary>
/// Strips markdown syntax (headers, bold, italic, blockquotes, bullets, hr) into natural prose.
/// </summary>
public class MarkdownFormattingFilter : INarrationFilter
{
    public string Name => "Markdown Syntax Cleaner";
    public int Order => 20;
    public bool IsEnabled { get; set; } = true;

    private static readonly Regex HeaderRegex = new(@"^\s*#{1,6}\s+(.*)$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex BoldAsteriskRegex = new(@"\*\*([^*]+)\*\*", RegexOptions.Compiled);
    private static readonly Regex BoldUnderscoreRegex = new(@"__([^_]+)__", RegexOptions.Compiled);
    private static readonly Regex ItalicAsteriskRegex = new(@"(?<!\*)\*([^*]+)\*(?!\*)", RegexOptions.Compiled);
    private static readonly Regex ItalicUnderscoreRegex = new(@"(?<!_)_([^_]+)_(?!_)", RegexOptions.Compiled);
    private static readonly Regex StrikethroughRegex = new(@"~~([^~]+)~~", RegexOptions.Compiled);
    private static readonly Regex BlockquoteRegex = new(@"^\s*>\s*(.*)$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex HorizontalRuleRegex = new(@"^\s*([*\-_]){3,}\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex BulletListRegex = new(@"^\s*[\*\-\+]\s+(.*)$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex NumberedListRegex = new(@"^\s*\d+\.\s+(.*)$", RegexOptions.Compiled | RegexOptions.Multiline);

    public string Process(string text, FilterContext context)
    {
        if (!IsEnabled) return text;

        string result = text;
        result = HorizontalRuleRegex.Replace(result, "");
        result = HeaderRegex.Replace(result, "$1.");
        result = BoldAsteriskRegex.Replace(result, "$1");
        result = BoldUnderscoreRegex.Replace(result, "$1");
        result = ItalicAsteriskRegex.Replace(result, "$1");
        result = ItalicUnderscoreRegex.Replace(result, "$1");
        result = StrikethroughRegex.Replace(result, "$1");
        result = BlockquoteRegex.Replace(result, "$1");
        result = BulletListRegex.Replace(result, "$1");
        result = NumberedListRegex.Replace(result, "$1");

        return result;
    }
}
