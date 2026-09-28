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

namespace ReadBack.Core.Sources;

/// <summary>
/// Generalized abstraction for text input providers (Clipboard, Active UI Selection,
/// Click-to-Read paragraph inspector, Document/Word extractors, Browser DOM hooks).
/// </summary>
public interface ITextSource
{
    /// <summary>
    /// Friendly name of this text provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Retrieves text content from this source asynchronously.
    /// </summary>
    Task<string?> GetTextAsync(CancellationToken ct = default);
}
