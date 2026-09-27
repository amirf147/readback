using System.Text.RegularExpressions;
using ReadBack.Core.Models;

namespace ReadBack.Core.Filters;

/// <summary>
/// Converts common LaTeX formulas and symbols into natural English spoken words.
/// </summary>
public class LatexMathFilter : INarrationFilter
{
    public string Name => "LaTeX & Math Normalizer";
    public int Order => 40;
    public bool IsEnabled { get; set; } = true;

    private static readonly Regex MathBlockRegex = new(@"\$\$([^\$]+)\$\$", RegexOptions.Compiled);
    private static readonly Regex MathInlineRegex = new(@"\$([^\$]+)\$", RegexOptions.Compiled);
    private static readonly Regex FracRegex = new(@"\\frac\{([^}]+)\}\{([^}]+)\}", RegexOptions.Compiled);

    public string Process(string text, FilterContext context)
    {
        if (!IsEnabled) return text;

        string result = text;
        result = FracRegex.Replace(result, "$1 over $2");
        result = result.Replace(@"\pm", " plus or minus ");
        result = result.Replace(@"\approx", " approximately ");
        result = result.Replace(@"\neq", " not equal to ");
        result = result.Replace(@"\times", " times ");
        result = result.Replace(@"\le", " less than or equal to ");
        result = result.Replace(@"\ge", " greater than or equal to ");
        result = result.Replace(@"\sqrt", " square root of ");
        result = result.Replace(@"\infty", " infinity ");

        // Remove surrounding math delimiters
        result = MathBlockRegex.Replace(result, "$1");
        result = MathInlineRegex.Replace(result, "$1");

        return result;
    }
}
