#requires -RunAsAdministrator

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ResendApiKey,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$VerificationCodeHashKey
)

$ErrorActionPreference = 'Stop'

$applicationPoolName = 'Identity.API'
$applicationPoolPath = "IIS:\AppPools\$applicationPoolName"
$configurationPath = 'MACHINE/WEBROOT/APPHOST'
$environmentFilter = "system.applicationHost/applicationPools/add[@name='$applicationPoolName']/environmentVariables"

if ($ResendApiKey.Trim().Length -lt 4 -or -not $ResendApiKey.Trim().StartsWith('re_'))
{
    throw 'ResendApiKey does not have the expected Resend API key format.'
}

if ($VerificationCodeHashKey.Trim().Length -lt 32)
{
    throw 'VerificationCodeHashKey must contain at least 32 characters.'
}

Import-Module WebAdministration

if (-not (Test-Path -LiteralPath $applicationPoolPath))
{
    throw "Application Pool '$applicationPoolName' does not exist."
}

function Set-ApplicationPoolEnvironmentVariable
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $existingVariable = @(
        Get-WebConfigurationProperty `
            -PSPath $configurationPath `
            -Filter $environmentFilter `
            -Name '.'
    ) | Where-Object { [string]$_.name -ceq $Name }

    if ($existingVariable.Count -gt 0)
    {
        Set-WebConfigurationProperty `
            -PSPath $configurationPath `
            -Filter "$environmentFilter/add[@name='$Name']" `
            -Name 'value' `
            -Value $Value
    }
    else
    {
        Add-WebConfigurationProperty `
            -PSPath $configurationPath `
            -Filter $environmentFilter `
            -Name '.' `
            -Value @{ name = $Name; value = $Value }
    }
}

$variables = [ordered]@{
    'Email__Provider'                = 'Resend'
    'Email__FromEmail'               = 'onboarding@resend.dev'
    'Email__FromName'                = 'PetLife'
    'Email__Resend__ApiKey'          = $ResendApiKey.Trim()
    'Email__VerificationCodeHashKey' = $VerificationCodeHashKey.Trim()
}

foreach ($variable in $variables.GetEnumerator())
{
    Set-ApplicationPoolEnvironmentVariable -Name $variable.Key -Value $variable.Value
}

$applicationPoolState = (Get-WebAppPoolState -Name $applicationPoolName).Value
if ($applicationPoolState -eq 'Started')
{
    Restart-WebAppPool -Name $applicationPoolName
    $poolAction = 'Recycled'
}
else
{
    Start-WebAppPool -Name $applicationPoolName
    $poolAction = 'Started'
}

$storedVariableNames = @(
    Get-WebConfigurationProperty `
        -PSPath $configurationPath `
        -Filter $environmentFilter `
        -Name '.'
) | ForEach-Object { [string]$_.name }

$variables.Keys | ForEach-Object {
    [pscustomobject]@{
        Variable        = $_
        Configured      = $storedVariableNames -ccontains $_
        ApplicationPool = $applicationPoolName
    }
}

Write-Host "Application Pool '$applicationPoolName': $poolAction."
Write-Host 'Secret values were intentionally omitted from this output.'
