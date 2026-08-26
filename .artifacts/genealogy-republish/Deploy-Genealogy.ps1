$ErrorActionPreference = 'Stop'
$source = 'C:\proyecto mascotas\proyectomascotasapp\.artifacts\genealogy-republish\Genealogy.API'
$destination = 'C:\DogPlatform\publish\Genealogy.API'
$result = 'C:\proyecto mascotas\proyectomascotasapp\.artifacts\genealogy-republish\deploy-result.txt'

try {
    Import-Module WebAdministration
    $source = (Resolve-Path -LiteralPath $source).Path
    if ($destination -ne 'C:\DogPlatform\publish\Genealogy.API') { throw 'Unexpected deployment destination.' }

    $settingsHash = (Get-FileHash -LiteralPath (Join-Path $destination 'appsettings.json') -Algorithm SHA256).Hash
    $developmentHash = (Get-FileHash -LiteralPath (Join-Path $destination 'appsettings.Development.json') -Algorithm SHA256).Hash

    if ((Get-WebsiteState -Name 'Genealogy.API').Value -eq 'Started') { Stop-Website -Name 'Genealogy.API' }
    if ((Get-WebAppPoolState -Name 'Genealogy.API').Value -eq 'Started') { Stop-WebAppPool -Name 'Genealogy.API' }
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        if ((Get-WebAppPoolState -Name 'Genealogy.API').Value -eq 'Stopped') { break }
        Start-Sleep -Milliseconds 500
    }
    if ((Get-WebAppPoolState -Name 'Genealogy.API').Value -ne 'Stopped') { throw 'Genealogy.API App Pool did not stop.' }
    Start-Sleep -Seconds 2

    & robocopy.exe $source $destination /E /R:10 /W:1 /XF appsettings.json appsettings.Development.json /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "Robocopy failed with exit code $LASTEXITCODE." }

    Start-WebAppPool -Name 'Genealogy.API'
    Start-Website -Name 'Genealogy.API'

    $settingsPreserved = $settingsHash -eq (Get-FileHash -LiteralPath (Join-Path $destination 'appsettings.json') -Algorithm SHA256).Hash
    $developmentPreserved = $developmentHash -eq (Get-FileHash -LiteralPath (Join-Path $destination 'appsettings.Development.json') -Algorithm SHA256).Hash
    Set-Content -LiteralPath $result -Encoding UTF8 -Value @(
        'SUCCESS',
        "Pool=$((Get-WebAppPoolState -Name 'Genealogy.API').Value)",
        "Site=$((Get-WebsiteState -Name 'Genealogy.API').Value)",
        "AppsettingsPreserved=$settingsPreserved",
        "DevelopmentSettingsPreserved=$developmentPreserved"
    )
    exit 0
}
catch {
    try {
        if ((Get-WebAppPoolState -Name 'Genealogy.API').Value -ne 'Started') { Start-WebAppPool -Name 'Genealogy.API' }
        if ((Get-WebsiteState -Name 'Genealogy.API').Value -ne 'Started') { Start-Website -Name 'Genealogy.API' }
    } catch {}
    Set-Content -LiteralPath $result -Encoding UTF8 -Value @('FAILED', $_.Exception.ToString())
    exit 1
}
