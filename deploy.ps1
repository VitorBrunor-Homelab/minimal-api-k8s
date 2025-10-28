# Deploy automatizado: compacta -> transfere para o homealb -> builda o Dockerfile -> Mnada para o Dockerhub

$remoteUser = "vitorbrunor"
$remoteHost = "192.168.1.8"
$remoteBuildPath = "/home/vitorbrunor/builds"

$dockerhubUser = "vitorbrunor"
$imageName = "minimal-api-k8s"
$imageTag = "v1.7-redis-cors" # Mudar versão em cada deply

$ErrorActionPreference = "Stop"

try {
    Write-Host "1. Limpando builds locais..."
    $projectPath = "./MinimalApiK8s/MinimalApiK8s.csproj"
    dotnet clean $projectPath

    Write-Host "2. Compactando o código-fonte (com o .dockerignore)..."
    Compress-Archive -Path ./MinimalApiK8s/* -DestinationPath source.zip -Force

    Write-Host "3. Enviando o código-fonte para o homelab via SCP..."
    scp ./source.zip "$($remoteUser)@$($remoteHost):$($remoteBuildPath)/"

    Write-Host "4. Executando o build do Docker no homelab via SSH..."
    $remoteCommand = "cd $($remoteBuildPath); sudo rm -rf ./app-src; unzip -o source.zip -d ./app-src; sudo docker build -t $($imageName) ./app-src; sudo docker tag $($imageName) $($dockerhubUser)/$($imageName):$($imageTag); sudo docker push $($dockerhubUser)/$($imageName):$($imageTag)"
    ssh "$($remoteUser)@$($remoteHost)" $remoteCommand

    Write-Host "`nDeploy concluído com sucesso!"

} catch {
    Write-Host "`nERRO: O script falhou.`n" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
} finally {
    Write-Host "`n5. Limpando os arquivos locais..."
    if (Test-Path ./source.zip) { Remove-Item ./source.zip }
}