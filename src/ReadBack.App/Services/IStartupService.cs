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

namespace ReadBack.App.Services;

/// <summary>
/// Service interface to manage application automatic startup at Windows user login.
/// </summary>
public interface IStartupService
{
    /// <summary>
    /// Checks whether the application is registered to start automatically on Windows login.
    /// </summary>
    bool IsStartupEnabled();

    /// <summary>
    /// Enables or disables automatic startup on Windows login via the current user's registry.
    /// </summary>
    /// <param name="enable">True to launch at startup, false otherwise.</param>
    void SetStartup(bool enable);
}
