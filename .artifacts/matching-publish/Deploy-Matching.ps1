$ErrorActionPreference = 'Stop'
$source = 'C:\proyecto mascotas\proyectomascotasapp\.artifacts\matching-publish\Matching.API'
$destination = 'C:\DogPlatform\publish\Matching.API'
$result = 'C:\proyecto mascotas\proyectomascotasapp\.artifacts\matching-publish\deploy-result.txt'

try {
    Import-Module WebAdministration
    $source = (Resolve-Path -LiteralPath $source).Path
    if ($destination -ne 'C:\DogPlatform\publish\Matching.API') { throw 'Unexpected deployment destination.' }

    $settingsHash = (Get-FileHash -LiteralPath (Join-Path $destination 'appsettings.json') -Algorithm SHA256).Hash
    $developmentPath = Join-Path $destination 'appsettings.Development.json'
    $developmentHash = if (Test-Path -LiteralPath $developmentPath) {
        (Get-FileHash -LiteralPath $developmentPath -Algorithm SHA256).Hash
    } else { $null }

    if ((Get-WebsiteState -Name 'Matching.API').Value -eq 'Started') { Stop-Website -Name 'Matching.API' }
    if ((Get-WebAppPoolState -Name 'Matching.API').Value -eq 'Started') { Stop-WebAppPool -Name 'Matching.API' }
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        if ((Get-WebAppPoolState -Name 'Matching.API').Value -eq 'Stopped') { break }
        Start-Sleep -Milliseconds 500
    }
    if ((Get-WebAppPoolState -Name 'Matching.API').Value -ne 'Stopped') { throw 'Matching.API App Pool did not stop.' }
    Start-Sleep -Seconds 2

    & robocopy.exe $source $destination /E /R:10 /W:1 /XF appsettings.json appsettings.Development.json /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "Robocopy failed with exit code $LASTEXITCODE." }

    Start-WebAppPool -Name 'Matching.API'
    Start-Website -Name 'Matching.API'

    $settingsPreserved = $settingsHash -eq (Get-FileHash -LiteralPath (Join-Path $destination 'appsettings.json') -Algorithm SHA256).Hash
    $developmentPreserved = $null -eq $developmentHash -or
        $developmentHash -eq (Get-FileHash -LiteralPath $developmentPath -Algorithm SHA256).Hash
    Set-Content -LiteralPath $result -Encoding UTF8 -Value @(
        'SUCCESS',
        "Pool=$((Get-WebAppPoolState -Name 'Matching.API').Value)",
        "Site=$((Get-WebsiteState -Name 'Matching.API').Value)",
        "AppsettingsPreserved=$settingsPreserved",
        "DevelopmentSettingsPreserved=$developmentPreserved"
    )
    exit 0
}
catch {
    try {
        if ((Get-WebAppPoolState -Name 'Matching.API').Value -ne 'Started') { Start-WebAppPool -Name 'Matching.API' }
        if ((Get-WebsiteState -Name 'Matching.API').Value -ne 'Started') { Start-Website -Name 'Matching.API' }
    } catch {}
    Set-Content -LiteralPath $result -Encoding UTF8 -Value @('FAILED', $_.Exception.ToString())
    exit 1
}
