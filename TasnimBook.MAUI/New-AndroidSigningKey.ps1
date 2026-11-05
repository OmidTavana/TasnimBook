[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$solutionDirectory = $PSScriptRoot
$keystoreDirectory = Join-Path $solutionDirectory 'signing'
$secretDirectory = Join-Path $solutionDirectory '.secrets'
$keystorePath = Join-Path $keystoreDirectory 'tasnimbook-release.keystore'
$passwordPath = Join-Path $secretDirectory 'android-signing-password.txt'

if ((Test-Path -LiteralPath $keystorePath) -or (Test-Path -LiteralPath $passwordPath)) {
    throw 'Signing material already exists. Refusing to replace the release identity.'
}

New-Item -ItemType Directory -Path $keystoreDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $secretDirectory -Force | Out-Null

$randomBytes = [byte[]]::new(32)
[System.Security.Cryptography.RandomNumberGenerator]::Fill($randomBytes)
$password = [Convert]::ToBase64String($randomBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
$passwordFileContent = $password + [Environment]::NewLine + $password + [Environment]::NewLine
[IO.File]::WriteAllText($passwordPath, $passwordFileContent, [Text.UTF8Encoding]::new($false))

$env:TASNIMBOOK_SIGNING_PASSWORD = $password
try {
    & keytool `
        -genkeypair `
        -v `
        -keystore $keystorePath `
        -storetype PKCS12 `
        -alias tasnimbook `
        -keyalg RSA `
        -keysize 3072 `
        -sigalg SHA256withRSA `
        -validity 10000 `
        -dname 'CN=TasnimBook, OU=Mobile, O=TasnimBook, L=Tehran, ST=Tehran, C=IR' `
        -storepass:env TASNIMBOOK_SIGNING_PASSWORD `
        -keypass:env TASNIMBOOK_SIGNING_PASSWORD

    if ($LASTEXITCODE -ne 0) {
        throw "keytool failed with exit code $LASTEXITCODE."
    }

    & keytool `
        -list `
        -v `
        -keystore $keystorePath `
        -alias tasnimbook `
        -storepass:env TASNIMBOOK_SIGNING_PASSWORD

    if ($LASTEXITCODE -ne 0) {
        throw "Unable to verify the generated keystore (exit code $LASTEXITCODE)."
    }
}
finally {
    Remove-Item Env:TASNIMBOOK_SIGNING_PASSWORD -ErrorAction SilentlyContinue
    [Array]::Clear($randomBytes, 0, $randomBytes.Length)
    $passwordFileContent = $null
    $password = $null
}

Write-Output "Keystore: $keystorePath"
Write-Output "Password file: $passwordPath"
Write-Warning 'Back up both files securely. Losing either one prevents signing future updates.'
