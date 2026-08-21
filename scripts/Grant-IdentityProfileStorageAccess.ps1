[CmdletBinding()]
param(
    [string]$StoragePath = 'C:\DogPlatform\uploads\profiles'
)

$ErrorActionPreference = 'Stop'
$identity = 'IIS APPPOOL\Identity.API'

New-Item -ItemType Directory -Path $StoragePath -Force | Out-Null
$acl = Get-Acl -LiteralPath $StoragePath
$rule = [System.Security.AccessControl.FileSystemAccessRule]::new(
    $identity,
    [System.Security.AccessControl.FileSystemRights]::Modify,
    [System.Security.AccessControl.InheritanceFlags]'ContainerInherit, ObjectInherit',
    [System.Security.AccessControl.PropagationFlags]::None,
    [System.Security.AccessControl.AccessControlType]::Allow)
$acl.SetAccessRule($rule)
Set-Acl -LiteralPath $StoragePath -AclObject $acl

Write-Host "Modify granted to $identity on $StoragePath"
