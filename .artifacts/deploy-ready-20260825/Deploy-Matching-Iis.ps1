$ErrorActionPreference = 'Stop'

$workspace = 'C:\proyecto mascotas\proyectomascotasapp'
$publishRoot = 'C:\DogPlatform\publish'
$resultPath = Join-Path $workspace '.artifacts\deploy-ready-20260825\iis-deploy-result.txt'

try {
    Import-Module WebAdministration

    function Wait-AppPoolState([string] $name, [string] $expected) {
        for ($attempt = 0; $attempt -lt 40; $attempt++) {
            if ((Get-WebAppPoolState -Name $name).Value -eq $expected) { return }
            Start-Sleep -Milliseconds 250
        }
        throw "App Pool $name did not reach state $expected"
    }

    function Wait-WebsiteState([string] $name, [string] $expected) {
        for ($attempt = 0; $attempt -lt 40; $attempt++) {
            if ((Get-WebsiteState -Name $name).Value -eq $expected) { return }
            Start-Sleep -Milliseconds 250
        }
        throw "Website $name did not reach state $expected"
    }

    function Start-AppPoolWithRetry([string] $name) {
        for ($attempt = 0; $attempt -lt 5; $attempt++) {
            try {
                if ((Get-WebAppPoolState -Name $name).Value -ne 'Started') {
                    Start-WebAppPool -Name $name
                }
                Wait-AppPoolState $name 'Started'
                return
            }
            catch {
                if ($attempt -eq 4) { throw }
                Start-Sleep -Seconds 1
            }
        }
    }

    function Start-WebsiteWithRetry([string] $name) {
        for ($attempt = 0; $attempt -lt 5; $attempt++) {
            try {
                if ((Get-WebsiteState -Name $name).Value -ne 'Started') {
                    Start-Website -Name $name
                }
                Wait-WebsiteState $name 'Started'
                return
            }
            catch {
                if ($attempt -eq 4) { throw }
                Start-Sleep -Seconds 1
            }
        }
    }

    $deployments = @(
        [pscustomobject]@{
            Name = 'Matching.API'
            Port = 5104
            Source = Join-Path $workspace '.artifacts\deploy-ready-20260825\Matching.API'
            Destination = Join-Path $publishRoot 'Matching.API'
        },
        [pscustomobject]@{
            Name = 'ApiGateway'
            Port = 5101
            Source = Join-Path $workspace '.artifacts\deploy-ready-20260825\ApiGateway'
            Destination = Join-Path $publishRoot 'ApiGateway'
        }
    )

    foreach ($deployment in $deployments) {
        $phase = "Validating $($deployment.Name)"
        $source = (Resolve-Path -LiteralPath $deployment.Source).Path
        if (-not $source.StartsWith($workspace + '\', [StringComparison]::OrdinalIgnoreCase)) {
            throw "Unsafe source: $source"
        }
        if (-not $deployment.Destination.StartsWith($publishRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
            throw "Unsafe destination: $($deployment.Destination)"
        }

        if (-not (Test-Path -LiteralPath $deployment.Destination)) {
            New-Item -ItemType Directory -Path $deployment.Destination | Out-Null
        }

        $poolPath = "IIS:\AppPools\$($deployment.Name)"
        if (-not (Test-Path -LiteralPath $poolPath)) {
            New-WebAppPool -Name $deployment.Name | Out-Null
        }
        Set-ItemProperty $poolPath -Name managedRuntimeVersion -Value ''
        Set-ItemProperty $poolPath -Name managedPipelineMode -Value 'Integrated'
        Set-ItemProperty $poolPath -Name startMode -Value 'AlwaysRunning'

        $site = Get-Website -Name $deployment.Name -ErrorAction SilentlyContinue
        $phase = "Stopping $($deployment.Name)"
        if ($site -and (Get-WebsiteState -Name $deployment.Name).Value -eq 'Started') {
            Stop-Website -Name $deployment.Name
            Wait-WebsiteState $deployment.Name 'Stopped'
        }
        if ((Get-WebAppPoolState -Name $deployment.Name).Value -eq 'Started') {
            Stop-WebAppPool -Name $deployment.Name
            Wait-AppPoolState $deployment.Name 'Stopped'
        }

        $phase = "Copying $($deployment.Name)"
        Copy-Item -Path (Join-Path $source '*') -Destination $deployment.Destination -Recurse -Force

        if (-not $site) {
            New-Website -Name $deployment.Name -Port $deployment.Port -IPAddress '*' `
                -PhysicalPath $deployment.Destination -ApplicationPool $deployment.Name | Out-Null
        }
        else {
            Set-ItemProperty "IIS:\Sites\$($deployment.Name)" -Name physicalPath -Value $deployment.Destination
            Set-ItemProperty "IIS:\Sites\$($deployment.Name)" -Name applicationPool -Value $deployment.Name
        }

        $expectedBinding = "*:$($deployment.Port):"
        $bindings = @(Get-WebBinding -Name $deployment.Name -Protocol http)
        if (-not ($bindings.bindingInformation -contains $expectedBinding)) {
            $bindings | Remove-WebBinding
            New-WebBinding -Name $deployment.Name -Protocol http -IPAddress '*' -Port $deployment.Port | Out-Null
        }

        & icacls.exe $deployment.Destination /grant '*S-1-5-32-568:(OI)(CI)(RX)' /T /C | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Could not grant IIS_IUSRS read access to $($deployment.Destination)"
        }

        $phase = "Starting $($deployment.Name)"
        Start-AppPoolWithRetry $deployment.Name
        Start-WebsiteWithRetry $deployment.Name
    }

    $lines = @('SUCCESS')
    foreach ($deployment in $deployments) {
        $site = Get-Website -Name $deployment.Name
        $pool = Get-Item "IIS:\AppPools\$($deployment.Name)"
        $lines += "$($deployment.Name)|Site=$((Get-WebsiteState -Name $deployment.Name).Value)|Pool=$((Get-WebAppPoolState -Name $deployment.Name).Value)|Identity=$($pool.processModel.identityType)|Path=$($site.PhysicalPath)|Binding=$($site.Bindings.Collection.bindingInformation -join ',')"
    }
    Set-Content -LiteralPath $resultPath -Value $lines -Encoding UTF8
    exit 0
}
catch {
    Set-Content -LiteralPath $resultPath -Value @('FAILED', "Phase=$phase", $_.Exception.ToString()) -Encoding UTF8
    exit 1
}
