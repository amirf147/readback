@REM Copyright 2026 ReadBack Contributors
@REM
@REM Licensed under the Apache License, Version 2.0 (the "License");
@REM you may not use this file except in compliance with the License.
@REM You may obtain a copy of the License at
@REM
@REM     http://www.apache.org/licenses/LICENSE-2.0
@REM
@REM Unless required by applicable law or agreed to in writing, software
@REM distributed under the License is distributed on an "AS IS" BASIS,
@REM WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
@REM See the License for the specific language governing permissions and
@REM limitations under the License.
@REM
@REM SPDX-License-Identifier: Apache-2.0
@echo off
if exist "%~dp0src\ReadBack.App\bin\Release\net10.0-windows\ReadBack.exe" (
    "%~dp0src\ReadBack.App\bin\Release\net10.0-windows\ReadBack.exe" speak
) else (
    dotnet run --project "%~dp0src\ReadBack.App\ReadBack.App.csproj" -c Release -- speak
)
