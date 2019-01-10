[CmdletBinding()]
param()

class PodHelper
{
    static [Bool]WaitForRunning($podname)
    {
        $val = 0
        $running = $false

        while($running -eq $false -and $val -le 120) {
            $running = (kubectl get pods -n default $podname -o jsonpath="{.status.phase}") -eq "Running"
            $val++
            sleep -Milliseconds 500
        }

        return $running
    }
    
    static [string]GetFullName($podname)
    {
        $val = 0
        $fullname = $null

        while($fullname -eq $null -and $val -le 120) {
            $fullname = kubectl get pods -o=name |  Select-String -Pattern "$podname*"
            $val++
            sleep -Milliseconds 500
        }

        ##Deleted pods might not have been removed yet. Return the last one
        return $fullname[-1].ToString().Remove(0,4)
    }   
}

class Port 
{
    static [void]Forward($podname, $port)
    {
        $pod = [PodHelper]::GetFullName($podname)
        If([PodHelper]::WaitForRunning($pod)) {
            Write-Host "`nPort forwarding $podname from $port to $port. Keep window open"
            start-process powershell -argument "-noexit -command kubectl port-forward $pod ${port}:$port"
        }
        Else {
            Write-Host "`nCan't port forward $podname to port $port. Pod is not running"
        }
    }

}

function refresh-path {
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") +
                ";" + 
                [System.Environment]::GetEnvironmentVariable("Path","User")
}

If(Test-Path -Path "$env:ProgramData\Chocolatey") {
   Write-Verbose "'Chocolatey' is installed."
}
Else {
    Write-Verbose "Installing 'Chocolatey'"
    Set-ExecutionPolicy Bypass -Scope Process -Force; iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
    refresh-path
}

If(((Get-ChildItem "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall") |
        Where-Object { $_.GetValue( "DisplayName" ) -like "*docker*" } ).Length -gt 0) {
   Write-Verbose "'Docker' is installed."
}
Else {
    Write-Verbose "Installing docker-for-windows"
    choco install docker-for-windows -y
    Start-Process -FilePath "C:\Program Files\Docker\Docker\Docker for Windows.exe"
}

If((choco list -lo | findstr kubernetes-kompose) -ne $null ) {
    Write-Verbose "'kubernetes-kompose' is installed"
}
Else {
    Write-Verbose "Installing kubernetes-kompose"
    choco install kubernetes-kompose -y
}

"`nOpen Docker on your computer and go to Settings -> Kubernetes. 'Enable Kubernetes' and set it as 'Default orchestrator for docker stack commands'. `n'Apply' the settings in Docker and wait for Kubernetes installation to complete."
Read-Host "Press enter to continue"

refresh-path

If((kubectl get svc --namespace=kube-system --field-selector=metadata.name==kubernetes-dashboard --ignore-not-found) -ne $null) {
    Write-Verbose "'kubernetes-dashboard' is installed"
}
Else {
    Write-Verbose "Installing 'kubernetes-dashboard'"
    kubectl create -f https://raw.githubusercontent.com/kubernetes/dashboard/v1.10.1/src/deploy/recommended/kubernetes-dashboard.yaml
}

If((kubectl get pods --selector=app=jaeger --ignore-not-found) -ne $null) {
    Write-Verbose "'jaeger' is installed"
}
Else {
    Write-Verbose "Installing 'jaeger'"
    kubectl create -f jaeger-all-in-one-template.yml
}

Write-Verbose "Deleting previous deployments"
kompose down

Write-Verbose "Deploying"
kompose up

##Proxy and port forwarding can be done in 'hidden' processes.
##For now they are opended in new ps window for clarity

"`nStarting 'kubernetes-dashboard'. Keep window open"
start-process powershell -argument '-noexit -command kubectl proxy'

[Port]::Forward("kafka-manager", 9000)
[Port]::Forward("kafdrop", 9010)
[Port]::Forward("kafka-rest-proxy", 8082)
[Port]::Forward("zookeeper", 2181)
[Port]::Forward("kafka-server", 9092)

# [Port]::Forward("jaeger-deployment", 14268) # HTTP Sender
# [Port]::Forward("jaeger-deployment", 6831) # UDP Sender (Kubernetes does not support udp port forwarding, falling back on http sender)
# [Port]::Forward("jaeger-deployment", 5778) # Sampler

"`nDONE!"
"Access Kubernetes Web UI http://127.0.0.1:8001/api/v1/namespaces/kube-system/services/https:kubernetes-dashboard:/proxy/"
"Access Kafka Manager at http://localhost:9000/"
"Access Kafdrop at http://localhost:9010"
"Access Jaeger at http://localhost:16686"