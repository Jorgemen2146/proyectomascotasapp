$ErrorActionPreference = 'Stop'
$workspace = 'C:\proyecto mascotas\proyectomascotasapp'
$source = Join-Path $workspace '.artifacts\deploy-breeding-20260825\Matching.API'
$destination = 'C:\DogPlatform\publish\Matching.API'
$resultPath = Join-Path $workspace '.artifacts\deploy-breeding-20260825\iis-result.txt'

try {
    Import-Module WebAdministration
    $resolvedSource = (Resolve-Path -LiteralPath $source).Path
    if (-not $resolvedSource.StartsWith($workspace + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Unsafe source: $resolvedSource"
    }
    if ($destination -ne 'C:\DogPlatform\publish\Matching.API') {
        throw "Unsafe destination: $destination"
    }

    if ((Get-WebsiteState -Name 'Matching.API').Value -eq 'Started') {
        Stop-Website -Name 'Matching.API'
    }
    if ((Get-WebAppPoolState -Name 'Matching.API').Value -eq 'Started') {
        Stop-WebAppPool -Name 'Matching.API'
    }
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        if ((Get-WebAppPoolState -Name 'Matching.API').Value -eq 'Stopped') { break }
        Start-Sleep -Milliseconds 250
    }

    Copy-Item -Path (Join-Path $resolvedSource '*') -Destination $destination -Recurse -Force
    & icacls.exe $destination /grant '*S-1-5-32-568:(OI)(CI)(RX)' /T /C | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Could not grant IIS_IUSRS read access.' }

    for ($attempt = 0; $attempt -lt 5; $attempt++) {
        try {
            if ((Get-WebAppPoolState -Name 'Matching.API').Value -ne 'Started') {
                Start-WebAppPool -Name 'Matching.API'
            }
            if ((Get-WebsiteState -Name 'Matching.API').Value -ne 'Started') {
                Start-Website -Name 'Matching.API'
            }
            break
        }
        catch {
            if ($attempt -eq 4) { throw }
            Start-Sleep -Seconds 1
        }
    }

    $pool = Get-Item 'IIS:\AppPools\Matching.API'
    $site = Get-Website -Name 'Matching.API'
    Set-Content -LiteralPath $resultPath -Encoding UTF8 -Value @(
        'SUCCESS',
        "Site=$((Get-WebsiteState -Name 'Matching.API').Value)",
        "Pool=$((Get-WebAppPoolState -Name 'Matching.API').Value)",
        "Identity=$($pool.processModel.identityType)",
        "Path=$($site.PhysicalPath)",
        "Binding=$($site.Bindings.Collection.bindingInformation -join ',')"
    )
    exit 0
}
catch {
    Set-Content -LiteralPath $resultPath -Encoding UTF8 -Value @('FAILED', $_.Exception.ToString())
    exit 1
}
