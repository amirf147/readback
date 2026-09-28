<#
Copyright 2026 ReadBack Contributors

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

SPDX-License-Identifier: Apache-2.0
#>
<#
.SYNOPSIS
    Reads whatever text is currently in the Windows clipboard aloud using ReadBack.
#>
param(
    [string]$Voice,
    [string]$Speed
)

$exePath = Join-Path $PSScriptRoot "src\ReadBack.App\bin\Release\net10.0-windows\ReadBack.exe"
if (-not (Test-Path $exePath)) {
    $exePath = Join-Path $PSScriptRoot "src\ReadBack.App\bin\Debug\net10.0-windows\ReadBack.exe"
}

if (-not (Test-Path $exePath)) {
    Write-Host "Building ReadBack..." -ForegroundColor Cyan
    dotnet build (Join-Path $PSScriptRoot "ReadBack.slnx") -c Release
    $exePath = Join-Path $PSScriptRoot "src\ReadBack.App\bin\Release\net10.0-windows\ReadBack.exe"
}

& $exePath speak | Out-Host
