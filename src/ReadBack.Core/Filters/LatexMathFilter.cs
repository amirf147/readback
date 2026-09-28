// Copyright 2026 ReadBack Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// SPDX-License-Identifier: Apache-2.0
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
