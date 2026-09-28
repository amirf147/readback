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
