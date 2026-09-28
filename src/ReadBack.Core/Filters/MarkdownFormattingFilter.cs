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
