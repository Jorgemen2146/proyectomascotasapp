#requires -RunAsAdministrator

[CmdletBinding()]
param(
    [ValidateNotNullOrEmpty()]
    [string]$SqlServer = 'DESKTOP-9I0JLAI'
)

$ErrorActionPreference = 'Stop'

$applicationPoolName = 'Health.API'
$applicationPoolPath = "IIS:\AppPools\$applicationPoolName"
$configurationPath = 'MACHINE/WEBROOT/APPHOST'
$variableName = 'ConnectionStrings__HealthDb'
$connectionString = "Server=$SqlServer;Database=DogPlatform_HealthDb;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;"
$environmentFilter = "system.applicationHost/applicationPools/add[@name='$applicationPoolName']/environmentVariables"

Import-Module WebAdministration

if (-not (Test-Path -LiteralPath $applicationPoolPath))
{
    throw "Application Pool '$applicationPoolName' does not exist."
}

$existingVariable = @(
    Get-WebConfigurationProperty `
        -PSPath $configurationPath `
        -Filter $environmentFilter `
        -Name '.'
) | Where-Object { [string]$_.name -ceq $variableName }

if ($existingVariable.Count -gt 0)
{
    Set-WebConfigurationProperty `
        -PSPath $configurationPath `
        -Filter "$environmentFilter/add[@name='$variableName']" `
        -Name 'value' `
        -Value $connectionString
}
else
{
    Add-WebConfigurationProperty `
        -PSPath $configurationPath `
        -Filter $environmentFilter `
        -Name '.' `
        -Value @{ name = $variableName; value = $connectionString }
}

Write-Host "Configured '$variableName' for Application Pool '$applicationPoolName'."
Write-Host "Connection: Server=$SqlServer;Database=DogPlatform_HealthDb;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;"
Write-Host "Recycle '$applicationPoolName' manually after completing the SQL steps."
