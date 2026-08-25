#requires -RunAsAdministrator

[CmdletBinding()]
param(
    [ValidateNotNullOrEmpty()]
    [string]$SqlServer = 'DESKTOP-9I0JLAI',

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$InternalServiceApiKey
)

$ErrorActionPreference = 'Stop'
$configurationPath = 'MACHINE/WEBROOT/APPHOST'

if ($InternalServiceApiKey.Trim().Length -lt 32)
{
    throw 'InternalServiceApiKey must contain at least 32 characters.'
}

Import-Module WebAdministration

function Set-AppPoolVariable
{
    param(
        [Parameter(Mandatory = $true)][string]$ApplicationPool,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Value
    )

    $poolPath = "IIS:\AppPools\$ApplicationPool"
    if (-not (Test-Path -LiteralPath $poolPath))
    {
        throw "Application Pool '$ApplicationPool' does not exist."
    }

    $filter = "system.applicationHost/applicationPools/add[@name='$ApplicationPool']/environmentVariables"
    $existing = @(
        Get-WebConfigurationProperty -PSPath $configurationPath -Filter $filter -Name '.'
    ) | Where-Object { [string]$_.name -ceq $Name }

    if ($existing.Count -gt 0)
    {
        Set-WebConfigurationProperty -PSPath $configurationPath `
            -Filter "$filter/add[@name='$Name']" -Name 'value' -Value $Value
    }
    else
    {
        Add-WebConfigurationProperty -PSPath $configurationPath -Filter $filter `
            -Name '.' -Value @{ name = $Name; value = $Value }
    }
}

$connectionString = "Server=$SqlServer;Database=DogPlatform_NotificationsDb;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;"
Set-AppPoolVariable -ApplicationPool 'Notifications.API' `
    -Name 'ConnectionStrings__NotificationsDb' -Value $connectionString
Set-AppPoolVariable -ApplicationPool 'Notifications.API' `
    -Name 'HealthService__BaseUrl' -Value 'http://localhost:5106'
Set-AppPoolVariable -ApplicationPool 'Health.API' `
    -Name 'PetsService__BaseUrl' -Value 'http://localhost:5103'

foreach ($pool in @('Notifications.API', 'Health.API', 'Pets.API'))
{
    Set-AppPoolVariable -ApplicationPool $pool `
        -Name 'InternalServices__ApiKey' -Value $InternalServiceApiKey.Trim()
}

Write-Host "Configured NotificationsDb, Health URL and Health-to-Pets URL."
Write-Host "Configured the shared internal-service credential for Notifications.API, Health.API and Pets.API."
Write-Host 'Secret values were intentionally omitted. Recycle the three Application Pools manually.'
