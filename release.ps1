<#
.SYNOPSIS
  Builds SuperKRWKMod and publishes it as a new GitHub release.

.DESCRIPTION
  1. Bumps the version (patch by default) in SuperKRWKMod.cs and AssemblyInfo.cs
  2. Builds Release without debug symbols (no local paths in the DLL)
  3. Checks every API the DLL uses exists in the game's stripped assemblies (tools/RefCheck)
  4. Commits, tags vX.Y.Z, pushes, and creates the release with SuperKRWKMod.zip attached

  Requires: Build Tools for Visual Studio (.NET desktop), .NET SDK 8+, gh (logged in), Bongo Cat installed.

.EXAMPLE
  pwsh ./release.ps1              # 0.3.0 -> 0.3.1
  pwsh ./release.ps1 -Bump minor  # 0.3.1 -> 0.4.0
  pwsh ./release.ps1 -Version 1.0.0
  pwsh ./release.ps1 -DryRun      # build and check only, change nothing
#>
param(
	[ValidateSet('patch', 'minor', 'major')]
	[string]$Bump = 'patch',
	[string]$Version,
	[switch]$DryRun
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$Repo = 'kaikomziu/SuperKRWKMod'
$ModFile = 'SuperKRWKMod/SuperKRWKMod.cs'
$AssemblyInfo = 'SuperKRWKMod/Properties/AssemblyInfo.cs'
$Project = 'SuperKRWKMod/SuperKRWKMod.csproj'
$Zip = 'SuperKRWKMod/bin/SuperKRWKMod.zip'
$Dll = 'SuperKRWKMod/bin/Release/BongoCat/SuperKRWKMod/SuperKRWKMod.dll'

function Step($message) { Write-Host "==> $message" -ForegroundColor Cyan }
function Fail($message) { Write-Host "ERROR: $message" -ForegroundColor Red; exit 1 }
function Run($exe) {
	& $exe @args
	if ($LASTEXITCODE -ne 0) { Fail "$exe $args failed (exit $LASTEXITCODE)" }
}

# --- Preconditions --------------------------------------------------------------------------
if (-not $DryRun) {
	if ((git branch --show-current) -ne 'main') { Fail 'Releases are made from the main branch.' }
	if (git status --porcelain) { Fail 'Commit or stash your changes first.' }
	Run gh auth status | Out-Null
	Run git fetch -q origin main
	if ((git rev-list --count 'HEAD..origin/main') -ne '0') { Fail 'origin/main has commits you do not have; pull first.' }
}

# --- Version --------------------------------------------------------------------------------
$modSource = Get-Content $ModFile -Raw
if ($modSource -notmatch 'Version = "(\d+)\.(\d+)\.(\d+)"') { Fail "Version not found in $ModFile" }
$current = "$($Matches[1]).$($Matches[2]).$($Matches[3])"

if (-not $Version) {
	$major = [int]$Matches[1]; $minor = [int]$Matches[2]; $patch = [int]$Matches[3]
	switch ($Bump) {
		'major' { $major++; $minor = 0; $patch = 0 }
		'minor' { $minor++; $patch = 0 }
		'patch' { $patch++ }
	}
	$Version = "$major.$minor.$patch"
}
if ($Version -notmatch '^\d+\.\d+\.\d+$') { Fail "Version must look like 1.2.3 (got '$Version')." }
$tag = "v$Version"
if (-not $DryRun -and (git tag --list $tag)) { Fail "Tag $tag already exists." }
Step "Version $current -> $Version$(if ($DryRun) { ' (dry run)' })"

$originalMod = $modSource
$originalInfo = Get-Content $AssemblyInfo -Raw
Set-Content $ModFile ($modSource -replace 'Version = "\d+\.\d+\.\d+"', "Version = `"$Version`"") -NoNewline
Set-Content $AssemblyInfo ($originalInfo -replace 'Version\("\d+\.\d+\.\d+\.\d+"\)', "Version(`"$Version.0`")") -NoNewline

try {
	# --- Build ------------------------------------------------------------------------------
	Step 'Building Release'
	$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
	$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/MSBuild.exe' | Select-Object -First 1
	if (-not $msbuild) { Fail 'MSBuild not found. Install Build Tools for Visual Studio (.NET desktop build tools).' }
	Remove-Item -Recurse -Force SuperKRWKMod/bin, SuperKRWKMod/obj -ErrorAction SilentlyContinue
	# Quoted: PowerShell would split -p:Name=Value at the colon when forwarding arguments.
	Run $msbuild $Project '/p:Configuration=Release' '/p:Platform=AnyCPU' '/p:DebugType=none' '/p:DebugSymbols=false' '/t:Rebuild' '/v:minimal' '/nologo'
	if (-not (Test-Path $Zip)) { Fail "$Zip was not produced." }

	# --- API check against the game's stripped assemblies ----------------------------------
	Step 'Checking APIs against the game'
	$steam = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -ErrorAction SilentlyContinue).SteamPath
	$managed = Join-Path $steam 'steamapps/common/BongoCat/BongoCat_Data/Managed'
	if (-not (Test-Path $managed)) { Fail "Bongo Cat not found at $managed" }
	Run dotnet run --project tools/RefCheck -c Release -v q '--' $Dll $managed

	if ($DryRun) {
		Step "Dry run OK. Built $Zip; nothing was committed or published."
		return
	}

	# --- Commit, tag, push ------------------------------------------------------------------
	$previousTag = git describe --tags --abbrev=0 2>$null
	$range = if ($previousTag) { "$previousTag..HEAD" } else { 'HEAD' }
	$changes = git log $range --no-merges --format='- %s' | Where-Object { $_ -notmatch '^- Release v' }

	Step "Committing and tagging $tag"
	Run git add $ModFile $AssemblyInfo
	Run git commit -q -m "Release $tag"
	Run git tag -a $tag -m "SuperKRWKMod $tag"
	Run git push -q origin main $tag

	# --- GitHub release ---------------------------------------------------------------------
	Step 'Publishing GitHub release'
	$notes = @"
## Changes
$(if ($changes) { $changes -join "`n" } else { '- Maintenance release' })

## Install
1. Download ``SuperKRWKMod.zip`` below and extract it.
2. Double-click ``install.bat`` (Bongo Cat is closed automatically if running).
3. Launch Bongo Cat through Steam.

Uninstall with ``uninstall.bat``. Visual-only: never changes taps, stats, items or drops.
Based on [Bongo Cat Catnip Mod](https://github.com/Shilo/bongo-cat-catnip-mod) by Shilo (MIT).
"@
	$notesFile = New-TemporaryFile
	Set-Content $notesFile $notes -Encoding utf8
	Run gh release create $tag $Zip --repo $Repo --title "SuperKRWKMod $tag" --notes-file $notesFile
	Remove-Item $notesFile
	Step "Released https://github.com/$Repo/releases/tag/$tag"
}
finally {
	if ($DryRun) {
		# Leave the working tree exactly as it was.
		Set-Content $ModFile $originalMod -NoNewline
		Set-Content $AssemblyInfo $originalInfo -NoNewline
	}
}
