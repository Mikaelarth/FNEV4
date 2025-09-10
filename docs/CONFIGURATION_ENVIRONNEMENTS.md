# Configuration des Environnements FNEV4

## Fichiers de Configuration

### `appsettings.json` (Configuration par défaut)
Configuration de base utilisée dans tous les environnements.

### `appsettings.Development.json` 
Configuration pour le développement - utilise le répertoire du projet.

### `appsettings.Production.json`
Configuration pour la production - utilise %LocalAppData%\FNEV4\.

### `appsettings.Custom.json`
Exemple de configuration personnalisée avec chemins spécifiques.

### `appsettings.Portable.json`
Configuration portable - tous les chemins relatifs au répertoire de l'application.

## Options de Configuration

### Environment.Type
- `Development` : Mode développement
- `Production` : Mode production
- `Custom` : Mode personnalisé
- `Portable` : Mode portable

### Environment.DatabaseMode
- `Auto` : Détection automatique (Project en dev, AppData en prod)
- `Project` : Utilise le répertoire du projet (D:\PROJET\FNEV4\data\)
- `AppData` : Utilise %LocalAppData%\FNEV4\
- `Custom` : Utilise PathSettings.DatabasePath

### Environment.PathMode
- `Auto` : Détection automatique des chemins selon l'environnement
- `Project` : Tous les dossiers dans le répertoire du projet
- `AppData` : Tous les dossiers dans %LocalAppData%\FNEV4\
- `Custom` : Utilise PathSettings.CustomPaths

### PathSettings
- `DataRootPath` : Répertoire racine des données (si vide, déduit automatiquement)
- `DatabasePath` : Chemin personnalisé vers la base de données
- `ImportFolder`, `ExportFolder`, etc. : Noms des sous-dossiers (relatifs à DataRoot)
- `CustomPaths` : Chemins absolus personnalisés pour chaque dossier

## Variables d'Environnement Supportées

### Dans les chemins de configuration :
- `%LocalAppData%` : Dossier AppData\Local de l'utilisateur
- `{UserProfile}` : Profil utilisateur
- `{AppData}` : Dossier AppData\Roaming
- `{LocalAppData}` : Dossier AppData\Local
- `{Documents}` : Dossier Documents

### Variables globales :
- `FNEV4_DATABASE_PATH` : Chemin forcé de la base de données
- `DOTNET_ENVIRONMENT` : Environnement d'exécution

## Ordre de Priorité

### Pour la base de données :
1. **Variable d'environnement** : `FNEV4_DATABASE_PATH`
2. **Configuration appsettings** : `PathSettings.DatabasePath`
3. **Mode automatique** : Basé sur `Environment.DatabaseMode`

### Pour les dossiers :
1. **Chemins personnalisés** : `PathSettings.CustomPaths.*`
2. **Chemins relatifs** : `PathSettings.*Folder` (relatifs à DataRoot)
3. **Chemins par défaut** : Import, Export, Archive, Logs, Backup

## Utilisation

### Développement
```bash
# Utilise automatiquement appsettings.Development.json
dotnet run
```

### Production
```bash
# Définir l'environnement de production
set DOTNET_ENVIRONMENT=Production
dotnet run
```

### Mode portable
```bash
# Utiliser le mode portable
set DOTNET_ENVIRONMENT=Portable
dotnet run
```

### Configuration personnalisée
```bash
# Utiliser un fichier de configuration spécifique
set DOTNET_ENVIRONMENT=Custom
dotnet run
```

### Variable d'environnement directe
```bash
# Forcer un chemin spécifique pour la base de données
set FNEV4_DATABASE_PATH=C:\MonCheminPersonnalise\FNEV4.db
dotnet run
```

## Exemples de Configuration

### Mode Development
```json
{
  "Environment": {
    "Type": "Development",
    "PathMode": "Project"
  }
}
```
**Résultat** : Tous les dossiers dans `D:\PROJET\FNEV4\data\`

### Mode Production
```json
{
  "Environment": {
    "Type": "Production",
    "PathMode": "AppData"
  }
}
```
**Résultat** : Tous les dossiers dans `%LocalAppData%\FNEV4\`

### Mode Custom
```json
{
  "PathSettings": {
    "CustomPaths": {
      "ImportFolder": "D:\MonImport",
      "ExportFolder": "D:\MonExport",
      "LogsFolder": "%LocalAppData%\FNEV4\Logs"
    }
  }
}
```
**Résultat** : Chaque dossier dans son chemin spécifique

### Mode Portable
```json
{
  "PathSettings": {
    "DataRootPath": ".\\PortableData"
  }
}
```
**Résultat** : Tous les dossiers relatifs au répertoire de l'application

## Exemples de Chemins Résultants

| Configuration | Base de Données | Import | Export | Logs |
|---------------|-----------------|---------|---------|------|
| Development | `D:\PROJET\FNEV4\data\FNEV4.db` | `D:\PROJET\FNEV4\data\Import` | `D:\PROJET\FNEV4\data\Export` | `D:\PROJET\FNEV4\data\Logs` |
| Production | `%LocalAppData%\FNEV4\FNEV4.db` | `%LocalAppData%\FNEV4\Import` | `%LocalAppData%\FNEV4\Export` | `%LocalAppData%\FNEV4\Logs` |
| Custom | `D:\MonProjet\FNEV4.db` | `D:\MonImport` | `D:\MonExport` | `%LocalAppData%\FNEV4\Logs` |
| Portable | `.\PortableData\FNEV4.db` | `.\PortableData\Import` | `.\PortableData\Export` | `.\PortableData\Logs` |
