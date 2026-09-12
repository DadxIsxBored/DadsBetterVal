param([switch]$Package)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$project = Join-Path $root 'DadsBetterVal.csproj'
$packageRoot = Join-Path $root 'package'
$dll = Join-Path $root 'bin\Release\net48\DadsBetterVal.dll'
$manifest = Get-Content -LiteralPath (Join-Path $packageRoot 'manifest.json') -Raw | ConvertFrom-Json
dotnet build $project -c Release
if ($LASTEXITCODE -ne 0) { throw "Source build exited with code $LASTEXITCODE" }
if (-not $Package) { return }
$entries = [ordered]@{
  'DadsBetterVal.dll'=$dll; 'manifest.json'=(Join-Path $packageRoot 'manifest.json'); 'README.md'=(Join-Path $packageRoot 'README.md');
  'icon.png'=(Join-Path $packageRoot 'icon.png'); 'CHANGELOG.md'=(Join-Path $root 'CHANGELOG.md'); 'LICENSE'=(Join-Path $root 'LICENSE');
  'THIRD_PARTY.md'=(Join-Path $root 'THIRD_PARTY.md'); 'Recycle_N_Reclaim-LICENSE.txt'=(Join-Path $root 'THIRD_PARTY_LICENSES\Recycle_N_Reclaim-LICENSE.txt')
}
foreach($path in $entries.Values) { if(-not (Test-Path -LiteralPath $path -PathType Leaf)){ throw "Required file missing: $path" } }
Add-Type -AssemblyName System.Drawing
$image=[System.Drawing.Image]::FromFile($entries['icon.png']); try { if($image.Width -ne 256 -or $image.Height -ne 256){throw 'icon.png must be 256x256'} } finally {$image.Dispose()}
$dist=Join-Path $root 'dist'; $archive=Join-Path $root 'Archive\package-builds'; New-Item -ItemType Directory -Force $dist,$archive|Out-Null
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss-fff'
foreach($artifact in Get-ChildItem -LiteralPath $dist -Force){ $name=if($artifact.PSIsContainer){"$($artifact.Name)-$stamp"}else{"$($artifact.BaseName)-$stamp$($artifact.Extension)"}; Move-Item -LiteralPath $artifact.FullName -Destination (Join-Path $archive $name) }
$folder=Join-Path $dist "DadsBetterVal-$($manifest.version_number)"; New-Item -ItemType Directory $folder|Out-Null
foreach($entry in $entries.GetEnumerator()){Copy-Item -LiteralPath $entry.Value -Destination (Join-Path $folder $entry.Key)}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=Join-Path $dist "DadsBetterVal-$($manifest.version_number).zip"; [System.IO.Compression.ZipFile]::CreateFromDirectory($folder,$zip,[System.IO.Compression.CompressionLevel]::Optimal,$false)
Write-Host "Thunderstore package: $zip"; Write-Host "SHA256: $((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash)"
