param(
    [string]$BmxExe,
    [string]$HookDll,
    [string]$WithDll,
    [string]$OktaOrg,
    [string]$OktaUser,
    [string]$OktaPassword,
    [string]$MfaResponse,
    [string]$AwsAccount,
    [string]$AwsRole,
    [switch]$SkipBuild,
    [string]$Filter
)

$ErrorActionPreference = "Stop"

$scriptDir = $PSScriptRoot
$repoRoot = Split-Path -Parent $scriptDir

if (-not $OktaOrg)      { $OktaOrg      = $env:BMX_E2E_OKTA_ORG }
if (-not $OktaUser)     { $OktaUser     = $env:BMX_E2E_OKTA_USER }
if (-not $OktaPassword) { $OktaPassword = $env:BMX_E2E_OKTA_PASSWORD }
if (-not $AwsAccount)   { $AwsAccount   = if ($env:BMX_E2E_AWS_ACCOUNT) { $env:BMX_E2E_AWS_ACCOUNT } else { "lrn-vulcan" } }
if (-not $AwsRole)      { $AwsRole      = if ($env:BMX_E2E_AWS_ROLE) { $env:BMX_E2E_AWS_ROLE } else { "lrn-vulcan-readonly" } }

if (-not $OktaOrg)      { $OktaOrg      = Read-Host "Okta org (e.g. dev-249094.oktapreview.com)" }
if (-not $OktaUser)     { $OktaUser     = Read-Host "Okta username" }
if (-not $OktaPassword) { $OktaPassword = Read-Host "Okta password" -AsSecureString | ConvertFrom-SecureString -AsPlainText }

if (-not $MfaResponse) { $MfaResponse = $env:BMX_E2E_MFA_RESPONSE }
if (-not $MfaResponse) { $MfaResponse = Read-Host "MFA passcode for auth pre-check (or press Enter to skip)" -MaskInput }

if (-not $OktaOrg -or -not $OktaUser -or -not $OktaPassword) {
    throw "Okta org, user, and password are required."
}

Write-Host ""
Write-Host "BMX E2E Test Runner" -ForegroundColor Cyan
Write-Host "===================" -ForegroundColor Cyan
Write-Host "  Org:      $OktaOrg"
Write-Host "  User:     $OktaUser"
Write-Host "  Account:  $AwsAccount"
Write-Host "  Role:     $AwsRole"
Write-Host ""

if (-not $SkipBuild) {
    if (-not $BmxExe) {
        Write-Host "[Build] Publishing BMX (NativeAOT)..." -ForegroundColor Cyan
        $bmxPublishDir = Join-Path $repoRoot "artifacts/publish/bmx"
        dotnet publish "$repoRoot/src/D2L.Bmx/D2L.Bmx.csproj" `
            -c Release -r win-x64 -o $bmxPublishDir 2>&1 | Write-Host
        if ($LASTEXITCODE -ne 0) { throw "Failed to publish BMX" }
        $BmxExe = Join-Path $bmxPublishDir "bmx.exe"
    }

    if (-not $HookDll) {
        Write-Host "[Build] Publishing BmxTestHookNet (NativeAOT)..." -ForegroundColor Cyan
        $hookPublishDir = Join-Path $repoRoot "artifacts/publish/hook"
        dotnet publish "$repoRoot/test/BmxTestHookNet/BmxTestHookNet.csproj" `
            -c Release -r win-x64 -o $hookPublishDir 2>&1 | Write-Host
        if ($LASTEXITCODE -ne 0) { throw "Failed to publish BmxTestHookNet" }
        $HookDll = Join-Path $hookPublishDir "BmxTestHookNet.dll"
    }
}

if (-not $BmxExe -or -not (Test-Path $BmxExe)) {
    throw "bmx.exe not found at: $BmxExe. Run without -SkipBuild or pass -BmxExe."
}
if (-not $HookDll -or -not (Test-Path $HookDll)) {
    throw "BmxTestHookNet.dll not found at: $HookDll. Run without -SkipBuild or pass -HookDll."
}

Write-Host "[Paths] bmx.exe:  $BmxExe" -ForegroundColor Green
Write-Host "[Paths] hook DLL: $HookDll" -ForegroundColor Green

# --- Microsoft Detours (withdll.exe) ---
$detoursDir = Join-Path $repoRoot "test/tools/Detours"
if (-not $WithDll) {
    $WithDll = Join-Path $detoursDir "bin.X64/withdll.exe"
}
if (-not (Test-Path $WithDll)) {
    $found = Get-Command withdll.exe -ErrorAction SilentlyContinue
    if ($found) { $WithDll = $found.Source }
}
if (-not (Test-Path $WithDll) -and -not $SkipBuild) {
    Write-Host "[Detours] withdll.exe not found — building Microsoft Detours..." -ForegroundColor Cyan

    # Clone if not already present
    if (-not (Test-Path (Join-Path $detoursDir "Makefile"))) {
        $toolsDir = Join-Path $repoRoot "test/tools"
        if (-not (Test-Path $toolsDir)) { New-Item -ItemType Directory -Path $toolsDir -Force | Out-Null }
        Write-Host "[Detours] Cloning microsoft/Detours..." -ForegroundColor Gray
        git clone https://github.com/microsoft/Detours.git $detoursDir 2>&1 | Write-Host
        if ($LASTEXITCODE -ne 0) { throw "Failed to clone Detours" }
    }

    # Find vcvarsall.bat from Visual Studio
    $vsWhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path $vsWhere)) {
        throw "vswhere.exe not found. Visual Studio (with C++ workload) is required to build Detours."
    }
    $vsInstallPath = & $vsWhere -latest -property installationPath -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64
    if (-not $vsInstallPath) {
        throw "Visual Studio with C++ tools not found. Install the 'Desktop development with C++' workload."
    }
    $vcvarsall = Join-Path $vsInstallPath "VC\Auxiliary\Build\vcvarsall.bat"
    if (-not (Test-Path $vcvarsall)) {
        throw "vcvarsall.bat not found at: $vcvarsall"
    }

    Write-Host "[Detours] Building with nmake (x64)..." -ForegroundColor Gray
    $buildCmd = "`"$vcvarsall`" amd64 && cd /d `"$detoursDir`" && nmake"
    cmd /c $buildCmd 2>&1 | Write-Host
    if ($LASTEXITCODE -ne 0) { throw "Failed to build Detours" }

    $WithDll = Join-Path $detoursDir "bin.X64/withdll.exe"
    if (-not (Test-Path $WithDll)) {
        throw "Detours build succeeded but withdll.exe not found at: $WithDll"
    }
    Write-Host "[Detours] Build complete." -ForegroundColor Green
}
if (-not $WithDll -or -not (Test-Path $WithDll)) {
    throw "withdll.exe not found. Expected at test/tools/Detours/bin.X64/withdll.exe. Run without -SkipBuild to auto-build."
}
Write-Host "[Paths] withdll:  $WithDll" -ForegroundColor Green

function Invoke-AuthCheck {
    param(
        [string]$Password,
        [string]$Mfa
    )

    $stdoutFile = [System.IO.Path]::GetTempFileName()
    $stderrFile = [System.IO.Path]::GetTempFileName()

    try {
        $env:BMX_TEST_STDOUT_FILE = $stdoutFile
        $env:BMX_TEST_STDERR_FILE = $stderrFile
        $env:BMX_TEST_ORG = $OktaOrg
        $env:BMX_TEST_USER = $OktaUser
        $env:BMX_TEST_PASSWORD = $Password
        $env:BMX_TEST_MFA_RESPONSE = $Mfa
        $env:BMX_TEST_HOOK_DEBUG = "1"

        $bmxDir = Join-Path $env:USERPROFILE ".bmx"
        if (Test-Path $bmxDir) {
            Remove-Item $bmxDir -Recurse -Force -ErrorAction SilentlyContinue
        }

        Write-Host "[Auth] Running: bmx configure..." -ForegroundColor Gray
        $configProc = Start-Process -FilePath $WithDll `
            -ArgumentList "/d:`"$HookDll`" `"$BmxExe`" configure --org $OktaOrg --user $OktaUser --non-interactive" `
            -NoNewWindow -Wait -PassThru
        if ($configProc.ExitCode -ne 0) {
            $stderr = if (Test-Path $stderrFile) { Get-Content $stderrFile -Raw } else { "" }
            Write-Host "[Auth] configure failed (exit $($configProc.ExitCode)): $stderr" -ForegroundColor Red
            return $false
        }

        "" | Set-Content $stdoutFile
        "" | Set-Content $stderrFile

        Write-Host "[Auth] Running: bmx login..." -ForegroundColor Gray
        $loginProc = Start-Process -FilePath $WithDll `
            -ArgumentList "/d:`"$HookDll`" `"$BmxExe`" login" `
            -NoNewWindow -Wait -PassThru

        $stdout = if (Test-Path $stdoutFile) { Get-Content $stdoutFile -Raw } else { "" }
        $stderr = if (Test-Path $stderrFile) { Get-Content $stderrFile -Raw } else { "" }
        $combined = "$stderr $stdout"

        Write-Host "[Auth] login exit code: $($loginProc.ExitCode)" -ForegroundColor Gray
        if ($stderr) { Write-Host "[Auth] stderr: $stderr" -ForegroundColor DarkGray }
        if ($stdout) { Write-Host "[Auth] stdout: $stdout" -ForegroundColor DarkGray }

        # Check for auth failure patterns
        $authFailed = ($combined -match "Unauthorized") -or
                      ($combined -match "Authentication failed") -or
                      ($combined -match "authentication for user") -or
                      ($combined -match "Check if org, user, and password is correct")

        if ($authFailed) {
            Write-Host "[Auth] Authentication FAILED." -ForegroundColor Red
            return $false
        }

        # Check sessions file was created
        $sessionsFile = Join-Path $env:USERPROFILE ".bmx/cache/sessions"
        if (Test-Path $sessionsFile) {
            Write-Host "[Auth] Session cached successfully." -ForegroundColor Green
            return $true
        }

        # No sessions file but no auth error — might be OK (DSSO?)
        Write-Host "[Auth] Login completed (exit $($loginProc.ExitCode)) but no sessions file found." -ForegroundColor Yellow
        return ($loginProc.ExitCode -eq 0)
    }
    finally {
        Remove-Item $stdoutFile -Force -ErrorAction SilentlyContinue
        Remove-Item $stderrFile -Force -ErrorAction SilentlyContinue
    }
}

$maxRetries = 3
$authPassed = $false

for ($attempt = 1; $attempt -le $maxRetries; $attempt++) {
    Write-Host ""
    Write-Host "[Auth] Pre-check attempt $attempt/$maxRetries..." -ForegroundColor Cyan

    $authPassed = Invoke-AuthCheck -Password $OktaPassword -Mfa $MfaResponse

    if ($authPassed) {
        Write-Host "[Auth] Pre-check PASSED." -ForegroundColor Green
        break
    }

    if ($attempt -lt $maxRetries) {
        Write-Host ""
        Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
        Write-Host "║  Authentication failed. MFA token may have expired.         ║" -ForegroundColor Yellow
        Write-Host "║  Please enter new credentials to retry.                     ║" -ForegroundColor Yellow
        Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Yellow
        Write-Host ""

        $newPassword = Read-Host "New Okta password (Enter to keep current)" -MaskInput
        if ($newPassword) { $OktaPassword = $newPassword }

        $newMfa = Read-Host "New MFA passcode (Enter to skip)" -MaskInput
        if ($newMfa) { $MfaResponse = $newMfa }
    }
}

if (-not $authPassed) {
    Write-Host ""
    Write-Host "Auth pre-check failed after $maxRetries attempts. Aborting." -ForegroundColor Red
    exit 1
}

# Prompt for MFA now — right before tests run — to minimize token expiry
$env:BMX_E2E_MFA_RESPONSE = $null
$MfaResponse = Read-Host "MFA passcode (or press Enter to skip)" -MaskInput

Write-Host ""
Write-Host "[Test] Running NUnit E2E tests via dotnet test..." -ForegroundColor Cyan
Write-Host ""

$env:BMX_TEST_BMX_EXE   = $BmxExe
$env:BMX_TEST_HOOK_DLL   = $HookDll
$env:BMX_TEST_WITHDLL    = $WithDll
$env:BMX_E2E_OKTA_ORG    = $OktaOrg
$env:BMX_E2E_OKTA_USER   = $OktaUser
$env:BMX_E2E_OKTA_PASSWORD = $OktaPassword
$env:BMX_E2E_MFA_RESPONSE  = $MfaResponse
$env:BMX_E2E_AWS_ACCOUNT   = $AwsAccount
$env:BMX_E2E_AWS_ROLE      = $AwsRole

$testProject = Join-Path $repoRoot "test/D2L.Bmx.E2eTests/D2L.Bmx.E2eTests.csproj"

$dotnetTestArgs = @(
    "test"
    $testProject
    "--logger", "console;verbosity=detailed"
    "--no-restore"
)

if ($Filter) {
    $dotnetTestArgs += @("--filter", $Filter)
}

& dotnet @dotnetTestArgs
$testExitCode = $LASTEXITCODE

Write-Host ""
if ($testExitCode -eq 0) {
    Write-Host "All E2E tests PASSED." -ForegroundColor Green
} else {
    Write-Host "Some E2E tests FAILED (exit code $testExitCode)." -ForegroundColor Red
}

exit $testExitCode
