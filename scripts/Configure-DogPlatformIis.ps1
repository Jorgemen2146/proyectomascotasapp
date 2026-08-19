#requires -RunAsAdministrator

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

Import-Module WebAdministration

$publishRoot = 'C:\DogPlatform\publish'
$sites = @(
    [pscustomobject]@{ Name = 'ApiGateway';       Port = 5101 },
    [pscustomobject]@{ Name = 'Identity.API';     Port = 5102 },
    [pscustomobject]@{ Name = 'Pets.API';         Port = 5103 },
    [pscustomobject]@{ Name = 'Matching.API';     Port = 5104 },
    [pscustomobject]@{ Name = 'Walks.API';        Port = 5105 },
    [pscustomobject]@{ Name = 'Health.API';       Port = 5106 },
    [pscustomobject]@{ Name = 'Veterinarian.API'; Port = 5107 },
    [pscustomobject]@{ Name = 'Genealogy.API';    Port = 5108 },
    [pscustomobject]@{ Name = 'Notification.API'; Port = 5109 }
)

foreach ($site in $sites) {
    $physicalPath = Join-Path $publishRoot $site.Name
    if (-not (Test-Path -LiteralPath $physicalPath)) {
        throw "Publish directory not found: $physicalPath"
    }

    $poolPath = "IIS:\AppPools\$($site.Name)"
    if (-not (Test-Path $poolPath)) {
        New-WebAppPool -Name $site.Name | Out-Null
    }

    Set-ItemProperty $poolPath -Name managedRuntimeVersion -Value ''
    Set-ItemProperty $poolPath -Name managedPipelineMode -Value 'Integrated'
    Set-ItemProperty $poolPath -Name startMode -Value 'AlwaysRunning'

    $environmentFilter =
        "system.applicationHost/applicationPools/add[@name='$($site.Name)']/environmentVariables"
    $environmentVariables = @(
        Get-WebConfigurationProperty `
            -PSPath 'MACHINE/WEBROOT/APPHOST' `
            -Filter $environmentFilter `
            -Name '.'
    )
    $aspNetEnvironment = $environmentVariables |
        Where-Object { $_.name -eq 'ASPNETCORE_ENVIRONMENT' }

    if ($aspNetEnvironment) {
        Set-WebConfigurationProperty `
            -PSPath 'MACHINE/WEBROOT/APPHOST' `
            -Filter "$environmentFilter/add[@name='ASPNETCORE_ENVIRONMENT']" `
            -Name value `
            -Value 'Development'
    }
    else {
        Add-WebConfigurationProperty `
            -PSPath 'MACHINE/WEBROOT/APPHOST' `
            -Filter $environmentFilter `
            -Name '.' `
            -Value @{ name = 'ASPNETCORE_ENVIRONMENT'; value = 'Development' }
    }

    $existingSite = Get-Website -Name $site.Name -ErrorAction SilentlyContinue
    if ($null -eq $existingSite) {
        New-Website `
            -Name $site.Name `
            -Port $site.Port `
            -IPAddress '*' `
            -PhysicalPath $physicalPath `
            -ApplicationPool $site.Name | Out-Null
    }
    else {
        Set-ItemProperty "IIS:\Sites\$($site.Name)" -Name physicalPath -Value $physicalPath
        Set-ItemProperty "IIS:\Sites\$($site.Name)" -Name applicationPool -Value $site.Name

        Get-WebBinding -Name $site.Name -Protocol http | Remove-WebBinding
        New-WebBinding `
            -Name $site.Name `
            -Protocol http `
            -IPAddress '*' `
            -Port $site.Port | Out-Null
    }

    # Read/execute only; this does not change the Application Pool identity.
    & icacls.exe $physicalPath /grant '*S-1-5-32-568:(OI)(CI)(RX)' /T /C | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Could not grant IIS_IUSRS read access to $physicalPath"
    }

    $poolState = (Get-WebAppPoolState -Name $site.Name).Value
    if ($poolState -eq 'Started') {
        Restart-WebAppPool -Name $site.Name
    }
    else {
        Start-WebAppPool -Name $site.Name
    }

    if ((Get-WebsiteState -Name $site.Name).Value -ne 'Started') {
        Start-Website -Name $site.Name
    }
}

Get-Website |
    Where-Object Name -in $sites.Name |
    Select-Object Name, State, PhysicalPath, ApplicationPool, Bindings

$sites | ForEach-Object {
    $pool = Get-Item "IIS:\AppPools\$($_.Name)"
    [pscustomobject]@{
        AppPool     = $_.Name
        State       = (Get-WebAppPoolState -Name $_.Name).Value
        Identity    = $pool.processModel.identityType
        Runtime     = if ($pool.managedRuntimeVersion) { $pool.managedRuntimeVersion } else { 'No Managed Code' }
        Pipeline    = $pool.managedPipelineMode
        StartMode   = $pool.startMode
        Environment = 'Development'
    }
}
