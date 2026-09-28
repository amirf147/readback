// Copyright 2026 Amir Farhadi
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
