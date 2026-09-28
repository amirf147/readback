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
using ReadBack.Core.Models;

namespace ReadBack.Core.TTS;

public interface ITTSEngine
{
    string Name { get; }
    Task<bool> SynthesizeToFileAsync(string text, string outputPath, string voiceId, string rate, CancellationToken ct = default);
    Task<IReadOnlyList<VoiceInfo>> GetAvailableVoicesAsync(CancellationToken ct = default);
}
