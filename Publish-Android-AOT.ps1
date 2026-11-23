[CmdletBinding()]
param(
    [ValidateSet('android-arm64', 'android-arm', 'android-x64')]
    [string]$RuntimeIdentifier = 'android-arm64'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$project = Join-Path $PSScriptRoot 'TasnimBook.MAUI\TasnimBook.MAUI\TasnimBook.MAUI.csproj'
$projectDirectory = Split-Path -Parent $project
$targetFramework = 'net10.0-android'
$outputDirectory = Join-Path $PSScriptRoot 'output\android\release-aot'
$publishDirectory = Join-Path $outputDirectory 'publish'
$finalApk = Join-Path $outputDirectory "TasnimBook-aot-release-$RuntimeIdentifier.apk"
$keyStore = Join-Path $PSScriptRoot 'TasnimBook.MAUI\signing\tasnimbook-release.keystore'
$passwordFile = Join-Path $PSScriptRoot 'TasnimBook.MAUI\.secrets\android-signing-password.txt'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET SDK was not found in PATH.'
}
if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
    throw "Project file was not found: $project"
}
if (-not (Test-Path -LiteralPath $keyStore -PathType Leaf)) {
    throw "Android signing keystore was not found: $keyStore"
}
if (-not (Test-Path -LiteralPath $passwordFile -PathType Leaf)) {
    throw "Android signing password file was not found: $passwordFile"
}

$visualStudioSdkCandidates = Get-PSDrive -PSProvider FileSystem | ForEach-Object {
    Join-Path $_.Root 'Program Files (x86)\Microsoft Visual Studio\Shared\Android\android-sdk'
    Join-Path $_.Root 'Program Files\Microsoft Visual Studio\Shared\Android\android-sdk'
}

$sdkCandidates = @(
    $env:ANDROID_SDK_ROOT,
    $env:ANDROID_HOME,
    (Join-Path $env:LOCALAPPDATA 'Android\Sdk'),
    'C:\Program Files (x86)\Android\android-sdk',
    'C:\Program Files\Android\android-sdk'
) + @($visualStudioSdkCandidates)
$sdkCandidates = $sdkCandidates | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

$androidSdk = $sdkCandidates |
    Where-Object { Test-Path -LiteralPath (Join-Path $_ 'platform-tools\adb.exe') -PathType Leaf } |
    Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($androidSdk)) {
    throw 'Android SDK was not found. Install it with Visual Studio Installer or set ANDROID_SDK_ROOT.'
}

New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null
$buildStarted = Get-Date

Write-Host ''
Write-Host 'Building Release APK with Full AOT...' -ForegroundColor Cyan
Write-Host "Runtime: $RuntimeIdentifier"
Write-Host "Android SDK: $androidSdk"

$publishArguments = @(
    'publish',
    $project,
    '-f', $targetFramework,
    '-c', 'Release',
    '-r', $RuntimeIdentifier,
    '--nologo',
    '-p:AndroidPackageFormats=apk',
    '-p:RunAOTCompilation=true',
    '-p:AndroidEnableProfiledAot=false',
    '-p:PublishTrimmed=true',
    '-p:AndroidUseApkSigner=true',
    "-p:AndroidSdkDirectory=$androidSdk",
    "-p:PublishDir=$publishDirectory"
)

& dotnet @publishArguments
if ($LASTEXITCODE -ne 0) {
    throw "APK build failed with exit code $LASTEXITCODE."
}

$searchRoots = @(
    $publishDirectory,
    (Join-Path $projectDirectory "bin\Release\$targetFramework")
)

$signedApk = Get-ChildItem -LiteralPath $searchRoots -Filter '*-Signed.apk' -File -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.LastWriteTime -ge $buildStarted.AddMinutes(-1) -and $_.FullName -ne $finalApk } |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

if ($null -eq $signedApk) {
    throw 'Build completed, but a signed APK was not found. Check the project keystore settings.'
}

Copy-Item -LiteralPath $signedApk.FullName -Destination $finalApk -Force
$sizeMb = [Math]::Round((Get-Item -LiteralPath $finalApk).Length / 1MB, 1)

Write-Host ''
Write-Host 'AOT APK was created successfully:' -ForegroundColor Green
Write-Host $finalApk -ForegroundColor Yellow
Write-Host "Size: $sizeMb MB"

