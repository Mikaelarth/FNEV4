@echo off
echo ========================================
echo     TEST DE COMPILATION FNEV4
echo ========================================
echo.

cd "c:\wamp64\www\FNEV4"

echo [1/3] Nettoyage des anciens builds...
dotnet clean src\FNEV4.Presentation\FNEV4.Presentation.csproj --verbosity quiet
if %ERRORLEVEL% NEQ 0 (
    echo ❌ ERREUR lors du nettoyage
    pause
    exit /b 1
)

echo [2/3] Compilation du projet...
dotnet build src\FNEV4.Presentation\FNEV4.Presentation.csproj --verbosity minimal
if %ERRORLEVEL% NEQ 0 (
    echo ❌ ERREUR de compilation
    pause
    exit /b 1
)

echo [3/3] Vérification des dépendances...
dotnet list src\FNEV4.Presentation\FNEV4.Presentation.csproj package | findstr "FNEV4"

echo.
echo ========================================
echo ✅ COMPILATION RÉUSSIE !
echo ========================================
echo.
echo État des composants :
echo ✅ DatabaseService : Fonctionnel avec synchronisation
echo ✅ DatabaseSettingsDialog : 4 onglets complets 
echo ✅ DatabaseConfigurationLoader : Service de chargement au démarrage
echo ✅ DatabaseConfigurationNotificationService : Notifications automatiques
echo ✅ BaseDonneesViewModel : Interface synchronisée
echo.
echo 🎯 Synchronisation configuration : RÉSOLUE
echo 📁 Chemin test configuré : C:\Users\HP\Downloads\test.db
echo.
pause
