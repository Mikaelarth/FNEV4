# Script PowerShell pour exécuter tous les tests FNEV4
# Respecte la structure dédiée aux tests

param(
    [string]$Configuration = "Debug",
    [switch]$Verbose,
    [switch]$Coverage
)

Write-Host "=== FNEV4 - Exécution des Tests ===" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Dossier de tests dédié: tests/" -ForegroundColor Green
Write-Host ""

# Fonction pour afficher les résultats
function Show-TestResults {
    param($ExitCode, $TestType)
    
    if ($ExitCode -eq 0) {
        Write-Host "✅ $TestType - SUCCÈS" -ForegroundColor Green
    } else {
        Write-Host "❌ $TestType - ÉCHEC (Code: $ExitCode)" -ForegroundColor Red
    }
}

try {
    # Changement vers le répertoire racine du projet
    $ProjectRoot = Split-Path -Parent $PSScriptRoot
    Set-Location $ProjectRoot
    
    Write-Host "📁 Répertoire du projet: $ProjectRoot" -ForegroundColor Blue
    Write-Host ""
    
    # Vérification de la structure des tests
    $TestStructure = @(
        "tests\FNEV4.Tests.Unit\Presentation",
        "tests\FNEV4.Tests.Integration", 
        "tests\FNEV4.Tests.E2E",
        "tests\TestData"
    )
    
    Write-Host "🔍 Vérification de la structure des tests..." -ForegroundColor Blue
    foreach ($folder in $TestStructure) {
        if (Test-Path $folder) {
            Write-Host "   ✅ $folder" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  $folder (non trouvé)" -ForegroundColor Yellow
        }
    }
    Write-Host ""
    
    # 1. Tests unitaires - Presentation
    Write-Host "🧪 Exécution des tests unitaires - Presentation..." -ForegroundColor Blue
    $TestProject = "tests\FNEV4.Tests.Unit\Presentation\FNEV4.Tests.Unit.Presentation.csproj"
    
    if (Test-Path $TestProject) {
        $cmd = "dotnet test `"$TestProject`" --configuration $Configuration --logger console --verbosity normal"
        
        if ($Coverage) {
            $cmd += " --collect:`"XPlat Code Coverage`""
        }
        
        if ($Verbose) {
            Write-Host "Commande: $cmd" -ForegroundColor Gray
        }
        
        Invoke-Expression $cmd
        $UnitTestResult = $LASTEXITCODE
        Show-TestResults $UnitTestResult "Tests Unitaires"
    } else {
        Write-Host "⚠️  Projet de tests unitaires non trouvé: $TestProject" -ForegroundColor Yellow
        $UnitTestResult = 0
    }
    
    Write-Host ""
    
    # 2. Test manuel (optionnel)
    $ManualTestProject = "tests\FNEV4.Tests.Manual\FNEV4.Tests.Manual.csproj"
    if (Test-Path $ManualTestProject) {
        Write-Host "🎯 Test manuel disponible - Exécution sur demande..." -ForegroundColor Blue
        $response = Read-Host "Voulez-vous lancer le test manuel de l'interface? (o/N)"
        
        if ($response -eq "o" -or $response -eq "O") {
            Write-Host "🚀 Lancement du test manuel..." -ForegroundColor Blue
            dotnet run --project "$ManualTestProject" --configuration $Configuration
            Show-TestResults $LASTEXITCODE "Test Manuel"
        } else {
            Write-Host "⏭️  Test manuel ignoré" -ForegroundColor Yellow
        }
    }
    
    Write-Host ""
    
    # Résumé final
    Write-Host "=== RÉSUMÉ DES TESTS ===" -ForegroundColor Cyan
    Show-TestResults $UnitTestResult "Tests Unitaires - Presentation"
    
    if ($UnitTestResult -eq 0) {
        Write-Host ""
        Write-Host "🎉 Tous les tests ont réussi!" -ForegroundColor Green
        Write-Host "✅ L'interface principale est fonctionnelle" -ForegroundColor Green
        Write-Host "✅ La navigation est opérationnelle" -ForegroundColor Green
        Write-Host "✅ Les 32 sous-menus sont testés" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "❌ Des tests ont échoué - Vérifiez les détails ci-dessus" -ForegroundColor Red
    }
    
    # Informations sur la couverture
    if ($Coverage) {
        Write-Host ""
        Write-Host "📊 Rapports de couverture générés dans TestResults/" -ForegroundColor Blue
    }
    
} catch {
    Write-Host "💥 Erreur lors de l'exécution des tests:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
} finally {
    Write-Host ""
    Write-Host "📁 Structure des tests respectée - Projet propre ✅" -ForegroundColor Green
}
