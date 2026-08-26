$ErrorActionPreference = 'Stop'
$result = 'C:\proyecto mascotas\proyectomascotasapp\.artifacts\genealogy-500\iis-inspection.txt'
Import-Module WebAdministration

function Get-PoolVariable([string] $pool, [string] $name) {
    $filter = "system.applicationHost/applicationPools/add[@name='$pool']/environmentVariables"
    @((Get-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' -Filter $filter -Name '.')) |
        Where-Object { [string]$_.name -ceq $name } |
        Select-Object -First 1
}

$genealogyKey = Get-PoolVariable 'Genealogy.API' 'InternalServices__ApiKey'
$petsKey = Get-PoolVariable 'Pets.API' 'InternalServices__ApiKey'
$petsUrl = Get-PoolVariable 'Genealogy.API' 'PetsService__BaseUrl'
$genealogyDb = Get-PoolVariable 'Genealogy.API' 'ConnectionStrings__GenealogyDb'
$identityDb = Get-PoolVariable 'Genealogy.API' 'ConnectionStrings__IdentityDb'
$healthKey = Get-PoolVariable 'Health.API' 'InternalServices__ApiKey'
$notificationsKey = Get-PoolVariable 'Notifications.API' 'InternalServices__ApiKey'

$sameKey = $false
if ($genealogyKey -and $petsKey) {
    $sameKey = [string]$genealogyKey.value -ceq [string]$petsKey.value
}

Set-Content -LiteralPath $result -Encoding UTF8 -Value @(
    "GenealogyKeyPresent=$($null -ne $genealogyKey)",
    "GenealogyKeyLength=$(if ($genealogyKey) { ([string]$genealogyKey.value).Length } else { 0 })",
    "PetsKeyPresent=$($null -ne $petsKey)",
    "PetsKeyLength=$(if ($petsKey) { ([string]$petsKey.value).Length } else { 0 })",
    "HealthKeyPresent=$($null -ne $healthKey)",
    "HealthKeyLength=$(if ($healthKey) { ([string]$healthKey.value).Length } else { 0 })",
    "NotificationsKeyPresent=$($null -ne $notificationsKey)",
    "NotificationsKeyLength=$(if ($notificationsKey) { ([string]$notificationsKey.value).Length } else { 0 })",
    "KeysMatch=$sameKey",
    "PetsBaseUrlPresent=$($null -ne $petsUrl)",
    "PetsBaseUrl=$([string]$petsUrl.value)",
    "GenealogyDbOverridePresent=$($null -ne $genealogyDb)",
    "IdentityDbOverridePresent=$($null -ne $identityDb)",
    "GenealogySiteState=$((Get-WebsiteState -Name 'Genealogy.API').Value)",
    "GenealogyPoolState=$((Get-WebAppPoolState -Name 'Genealogy.API').Value)",
    "GenealogyPoolIdentity=$((Get-Item 'IIS:\AppPools\Genealogy.API').processModel.identityType)"
)

[xml]$appHost = Get-Content -LiteralPath `
    'C:\Windows\System32\inetsrv\config\applicationHost.config' -Raw
foreach ($poolName in @('Genealogy.API', 'Pets.API')) {
    $poolNode = $appHost.configuration.'system.applicationHost'.applicationPools.add |
        Where-Object { $_.name -eq $poolName }
    foreach ($variable in @($poolNode.environmentVariables.add)) {
        Add-Content -LiteralPath $result -Encoding UTF8 `
            -Value "XmlVariable=$poolName/$($variable.name)/Length=$(([string]$variable.value).Length)"
    }
}
