# Script pour tester le démarrage de l'application avec diagnostics
$env:DOTNET_ENVIRONMENT = "Development"
$env:DOTNET_LOGGING_CONSOLE = "1"
$env:DOTNET_LOGGING_DEBUG = "1"
$env:ASPNETCORE_LOGGING_CONFIGURATION = "1"

Set-Location "D:\PROJET\FNE\FNEV4\src\FNEV4.Presentation\bin\Debug\net8.0-windows"

Write-Host "=== Test de démarrage FNEV4 ===" -ForegroundColor Green
Write-Host "Répertoire courant: $(Get-Location)" -ForegroundColor Yellow
Write-Host "Fichier exe: $(Test-Path '.\FNEV4.Presentation.exe')" -ForegroundColor Yellow
Write-Host "Configuration: $(Test-Path '.\appsettings.json')" -ForegroundColor Yellow

Write-Host "Lancement de l'application..." -ForegroundColor Cyan
try {
    & ".\FNEV4.Presentation.exe"
    Write-Host "Application fermée avec code de sortie: $LASTEXITCODE" -ForegroundColor Yellow
} catch {
    Write-Host "Erreur lors du lancement: $_" -ForegroundColor Red
}

Write-Host "=== Fin du test ===" -ForegroundColor Green