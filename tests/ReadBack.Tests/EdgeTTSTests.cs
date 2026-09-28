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
using ReadBack.Core.TTS;
using Xunit;
using Xunit.Abstractions;

namespace ReadBack.Tests;

public class EdgeTTSTests
{
    private readonly ITestOutputHelper _output;

    public EdgeTTSTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task EdgeTTS_Synthesize_Test()
    {
        var client = new EdgeTTSClient();
        string tempFile = Path.Combine(Path.GetTempPath(), $"edge_test_{Guid.NewGuid():N}.mp3");
        try
        {
            bool result = await client.SynthesizeToFileAsync("Hello world, testing Edge speech.", tempFile, "en-US-ChristopherNeural", "+0%");
            _output.WriteLine($"Result: {result}, File exists: {File.Exists(tempFile)}, Length: {(File.Exists(tempFile) ? new FileInfo(tempFile).Length : 0)}");
            Assert.True(result);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Exception: {ex}");
            throw;
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
