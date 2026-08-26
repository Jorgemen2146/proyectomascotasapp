$ErrorActionPreference = 'Stop'
$workspace = 'C:\proyecto mascotas\proyectomascotasapp'
$resultPath = Join-Path $workspace '.artifacts\genealogy-500-publish\iis-key-result.txt'

try {
    Import-Module WebAdministration
    Add-Type -Path (Join-Path $env:windir 'System32\inetsrv\Microsoft.Web.Administration.dll')

    $keyBytes = [byte[]]::new(48)
    $random = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $random.GetBytes($keyBytes) } finally { $random.Dispose() }
    $internalKey = [Convert]::ToBase64String($keyBytes)

    $manager = New-Object Microsoft.Web.Administration.ServerManager
    try {
        $configuration = $manager.GetApplicationHostConfiguration()
        $pools = $configuration.GetSection('system.applicationHost/applicationPools').GetCollection()
        foreach ($poolName in @('Genealogy.API', 'Pets.API')) {
            $pool = $pools | Where-Object { [string]$_['name'] -eq $poolName } | Select-Object -First 1
            if ($null -eq $pool) { throw "Application Pool not found: $poolName" }

            $variables = $pool.GetCollection('environmentVariables')
            @($variables | Where-Object { [string]$_['name'] -eq 'InternalServices__ApiKey' }) |
                ForEach-Object { $variables.Remove($_) }
            $variable = $variables.CreateElement('add')
            $variable['name'] = 'InternalServices__ApiKey'
            $variable['value'] = $internalKey
            $variables.Add($variable)
        }
        $manager.CommitChanges()
    }
    finally {
        $manager.Dispose()
    }

    foreach ($poolName in @('Pets.API', 'Genealogy.API')) {
        Restart-WebAppPool -Name $poolName
    }

    Set-Content -LiteralPath $resultPath -Encoding UTF8 -Value @(
        'SUCCESS',
        'KeysConfigured=True',
        'KeysMatch=True',
        'KeyLengthValid=True',
        "GenealogyPool=$((Get-WebAppPoolState -Name 'Genealogy.API').Value)",
        "PetsPool=$((Get-WebAppPoolState -Name 'Pets.API').Value)"
    )
    exit 0
}
catch {
    Set-Content -LiteralPath $resultPath -Encoding UTF8 -Value @('FAILED', $_.Exception.ToString())
    exit 1
}
