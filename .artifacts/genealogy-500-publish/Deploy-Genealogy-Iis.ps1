$ErrorActionPreference = 'Stop'
$workspace = 'C:\proyecto mascotas\proyectomascotasapp'
$source = Join-Path $workspace '.artifacts\genealogy-500-publish\Genealogy.API'
$destination = 'C:\DogPlatform\publish\Genealogy.API'
$resultPath = Join-Path $workspace '.artifacts\genealogy-500-publish\iis-result.txt'

function Set-PoolVariable([string] $pool, [string] $name, [string] $value) {
    $filter = "system.applicationHost/applicationPools/add[@name='$pool']/environmentVariables"
    try {
        Set-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' `
            -Filter "$filter/add[@name='$name']" -Name value -Value $value
    }
    catch {
        Add-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' -Filter $filter `
            -Name '.' -Value @{ name = $name; value = $value }
    }
}

function Wait-Pool([string] $pool, [string] $state) {
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        if ((Get-WebAppPoolState -Name $pool).Value -eq $state) { return }
        Start-Sleep -Milliseconds 250
    }
    throw "App Pool $pool did not reach $state."
}

try {
    Import-Module WebAdministration
    $resolvedSource = (Resolve-Path -LiteralPath $source).Path
    if (-not $resolvedSource.StartsWith($workspace + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Unsafe source: $resolvedSource"
    }
    if ($destination -ne 'C:\DogPlatform\publish\Genealogy.API') {
        throw "Unsafe destination: $destination"
    }

    $settingsHashBefore = (Get-FileHash -Algorithm SHA256 `
        -LiteralPath (Join-Path $destination 'appsettings.json')).Hash
    $developmentHashBefore = (Get-FileHash -Algorithm SHA256 `
        -LiteralPath (Join-Path $destination 'appsettings.Development.json')).Hash

    $keyBytes = [byte[]]::new(48)
    $random = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $random.GetBytes($keyBytes)
    }
    finally {
        $random.Dispose()
    }
    $internalKey = [Convert]::ToBase64String($keyBytes)
    Set-PoolVariable 'Pets.API' 'InternalServices__ApiKey' $internalKey
    Set-PoolVariable 'Genealogy.API' 'InternalServices__ApiKey' $internalKey

    if ((Get-WebsiteState -Name 'Genealogy.API').Value -eq 'Started') {
        Stop-Website -Name 'Genealogy.API'
    }
    foreach ($pool in @('Genealogy.API', 'Pets.API')) {
        if ((Get-WebAppPoolState -Name $pool).Value -eq 'Started') {
            Stop-WebAppPool -Name $pool
        }
        Wait-Pool $pool 'Stopped'
    }

    Get-ChildItem -LiteralPath $resolvedSource | Where-Object {
        $_.Name -notin @('appsettings.json', 'appsettings.Development.json')
    } | Copy-Item -Destination $destination -Recurse -Force

    & icacls.exe $destination /grant '*S-1-5-32-568:(OI)(CI)(RX)' /T /C | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Could not grant IIS_IUSRS read access.' }

    foreach ($pool in @('Pets.API', 'Genealogy.API')) {
        Start-WebAppPool -Name $pool
        Wait-Pool $pool 'Started'
    }
    Start-Website -Name 'Genealogy.API'

    $internalResponse = Invoke-WebRequest -UseBasicParsing `
        -Uri 'http://localhost:5103/api/v1/internal/pets/vaccination-context' `
        -Headers @{ 'X-DogPlatform-Internal-Key' = $internalKey } -TimeoutSec 20

    $settingsPreserved = $settingsHashBefore -eq (Get-FileHash -Algorithm SHA256 `
        -LiteralPath (Join-Path $destination 'appsettings.json')).Hash
    $developmentPreserved = $developmentHashBefore -eq (Get-FileHash -Algorithm SHA256 `
        -LiteralPath (Join-Path $destination 'appsettings.Development.json')).Hash

    Set-Content -LiteralPath $resultPath -Encoding UTF8 -Value @(
        'SUCCESS',
        "GenealogySite=$((Get-WebsiteState -Name 'Genealogy.API').Value)",
        "GenealogyPool=$((Get-WebAppPoolState -Name 'Genealogy.API').Value)",
        "PetsPool=$((Get-WebAppPoolState -Name 'Pets.API').Value)",
        "KeysConfigured=True",
        "KeysMatch=True",
        "InternalPetsHttp=$([int]$internalResponse.StatusCode)",
        "AppsettingsPreserved=$settingsPreserved",
        "DevelopmentSettingsPreserved=$developmentPreserved"
    )
    exit 0
}
catch {
    Set-Content -LiteralPath $resultPath -Encoding UTF8 -Value @('FAILED', $_.Exception.ToString())
    exit 1
}
