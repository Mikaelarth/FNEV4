@echo off
echo === Test de synchronisation FNEV4 ===
echo.

echo 1. Compilation du projet...
cd "c:\wamp64\www\FNEV4\src\FNEV4.Presentation"
dotnet build --verbosity quiet

if %ERRORLEVEL% NEQ 0 (
    echo Erreur de compilation !
    pause
    exit /b 1
)

echo 2. Configuration testée : C:\Users\HP\Downloads\test.db
echo 3. Démarrage de l'application...
echo.
echo INSTRUCTIONS POUR TEST :
echo - Allez dans le menu Maintenance > Base de données
echo - Notez le chemin affiché dans l'interface principale
echo - Ouvrez "Configuration de la base de données"
echo - Vérifiez que le chemin correspond à celui sauvegardé
echo - Si les chemins sont identiques, la synchronisation fonctionne !
echo.

start /wait dotnet run --project FNEV4.Presentation.csproj

echo.
echo Test terminé.
pause
