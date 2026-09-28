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
using ReadBack.Core.Models;

namespace ReadBack.Core.Filters;

/// <summary>
/// Defines an extensible filter in the text narration pipeline.
/// </summary>
public interface INarrationFilter
{
    /// <summary>
    /// Unique display name for this filter.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Execution order in the pipeline. Lower runs earlier.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Whether this filter is currently active.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Transforms or cleans the given text prior to speech synthesis.
    /// </summary>
    string Process(string text, FilterContext context);
}
