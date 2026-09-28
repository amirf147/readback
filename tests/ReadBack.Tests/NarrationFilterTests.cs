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
using ReadBack.Core.Filters;
using ReadBack.Core.Models;
using Xunit;

namespace ReadBack.Tests;

public class NarrationFilterTests
{
    private readonly NarrationPipeline _pipeline = new();
    private readonly FilterContext _context = new(new AppSettings());

    [Fact]
    public void CodeBlockFilter_ReplacesFencedCodeWithSummary()
    {
        string input = "Here is how you do it:\n```python\ndef hello():\n    print('world')\n```\nPretty simple!";
        string output = _pipeline.Process(input, _context);

        Assert.Contains("(Omitted python code snippet)", output);
        Assert.DoesNotContain("def hello()", output);
        Assert.Contains("Pretty simple!", output);
    }

    [Fact]
    public void MarkdownFilter_StripsHeadersAndBold()
    {
        string input = "## Top Features\nThis is **critically important** and *very sleek*.";
        string output = _pipeline.Process(input, _context);

        Assert.DoesNotContain("##", output);
        Assert.Contains("Top Features", output);
        Assert.Contains("critically important", output);
        Assert.DoesNotContain("**", output);
    }

    [Fact]
    public void UrlSimplifierFilter_ReplacesQueryStringsWithDomain()
    {
        string input = "Check out https://github.com/microsoft/dotnet?query=123&test=abc for docs.";
        string output = _pipeline.Process(input, _context);

        Assert.Contains("github.com link", output);
        Assert.DoesNotContain("?query=123", output);
    }

    [Fact]
    public void LatexMathFilter_ConvertsEquationsToSpokenEnglish()
    {
        string input = @"The formula is $\frac{a}{b}$ and $x \approx y$.";
        string output = _pipeline.Process(input, _context);

        Assert.Contains("a over b", output);
        Assert.Contains("approximately", output);
        Assert.DoesNotContain(@"\frac", output);
    }

    [Fact]
    public void TableFilter_FlattensTableRows()
    {
        string input = "| Name | Role |\n|---|---|\n| Alice | Engineer |\n| Bob | Designer |";
        string output = _pipeline.Process(input, _context);

        Assert.DoesNotContain("|---|---|", output);
        Assert.Contains("Alice, Engineer.", output);
        Assert.Contains("Bob, Designer.", output);
    }

    [Fact]
    public void Pipeline_SupportsCustomExtensibleFilter()
    {
        var customFilter = new GreetingPrefixFilter();
        _pipeline.RegisterFilter(customFilter);

        string input = "Hello world.";
        string output = _pipeline.Process(input, _context);

        Assert.StartsWith("[ReadBack Voice Mode]", output);
    }

    private class GreetingPrefixFilter : INarrationFilter
    {
        public string Name => "Voice Mode Prefix";
        public int Order => 1;
        public bool IsEnabled { get; set; } = true;

        public string Process(string text, FilterContext context)
        {
            return $"[ReadBack Voice Mode] {text}";
        }
    }
}
