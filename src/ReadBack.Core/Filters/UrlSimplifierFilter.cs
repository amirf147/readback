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
/// Replaces complex URLs with clean domain names so speech isn't overwhelmed by query strings.
/// </summary>
public class UrlSimplifierFilter : INarrationFilter
{
    public string Name => "URL Simplifier";
    public int Order => 30;
    public bool IsEnabled { get; set; } = true;

    // Detects http/https URLs and markdown links [text](url)
    private static readonly Regex MarkdownLinkRegex = new(@"\[([^\]]+)\]\((https?://[^\)]+)\)", RegexOptions.Compiled);
    private static readonly Regex RawUrlRegex = new(@"https?://(?:www\.)?([a-zA-Z0-9\.\-]+)(?:/[^\s]*)?", RegexOptions.Compiled);

    public string Process(string text, FilterContext context)
    {
        if (!IsEnabled) return text;

        // First handle markdown links [Label](url) -> "Label"
        string result = MarkdownLinkRegex.Replace(text, "$1");

        // Then simplify raw URLs to just the domain
        result = RawUrlRegex.Replace(result, match =>
        {
            string domain = match.Groups[1].Value;
            return $"{domain} link";
        });

        return result;
    }
}
