#requires -RunAsAdministrator

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$applicationPoolName = 'Pets.API'
$storagePath = 'C:\DogPlatform\uploads\pets'
$applicationPoolIdentity = "IIS APPPOOL\$applicationPoolName"

Import-Module WebAdministration

if (-not (Test-Path -LiteralPath "IIS:\AppPools\$applicationPoolName"))
{
    throw "Application Pool '$applicationPoolName' does not exist."
}

$null = New-Item -ItemType Directory -Path $storagePath -Force

$identity = [System.Security.Principal.NTAccount]::new($applicationPoolIdentity)
$null = $identity.Translate([System.Security.Principal.SecurityIdentifier])

$acl = Get-Acl -LiteralPath $storagePath
$rule = [System.Security.AccessControl.FileSystemAccessRule]::new(
    $applicationPoolIdentity,
    [System.Security.AccessControl.FileSystemRights]::Modify,
    [System.Security.AccessControl.InheritanceFlags]'ContainerInherit, ObjectInherit',
    [System.Security.AccessControl.PropagationFlags]::None,
    [System.Security.AccessControl.AccessControlType]::Allow)

$acl.SetAccessRule($rule)
Set-Acl -LiteralPath $storagePath -AclObject $acl

Write-Host "Modify permission granted to '$applicationPoolIdentity' on '$storagePath'."
Write-Host 'No IIS sites, bindings, ports, application pools, or databases were changed.'
