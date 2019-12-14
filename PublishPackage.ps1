$ProjectDir = "PSMWUpdater"
$OutputDir = Join-Path $ProjectDir "bin/Release/netstandard2.0"
$RedistDir = Join-Path $ProjectDir "bin/Redist/PSMWUpdater"

dotnet build -c Release $ProjectDir
if ($LASTEXITCODE) {
    Exit $LASTEXITCODE
}

$RepositoryName = (Read-Host "Repository")
$ApiKey = (Read-Host "NuGet API Key")

Remove-Item $RedistDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item $RedistDir -Type:Directory -Force | Out-Null
Copy-Item (Join-Path $OutputDir *) (Join-Path $RedistDir /) -Recurse
Publish-Module -Path:$RedistDir -Repository:$RepositoryName -NuGetApiKey:$ApiKey
