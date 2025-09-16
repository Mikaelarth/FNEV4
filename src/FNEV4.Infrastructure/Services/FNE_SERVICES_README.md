# Services de Certification FNE - Architecture et Organisation

## 📁 Structure des Services FNE

```
Services/
├── FneCertificationService.cs        # ✅ Service de production (Réel)
├── FneCertificationService.Mock.cs   # 🧪 Service de test/développement (Fictif)
└── FNE_SERVICES_README.md            # 📖 Cette documentation
```

## 🚀 Service Principal : `FneCertificationService`

**Fichier :** `FneCertificationService.cs`  
**Classe :** `FneCertificationService`  
**Usage :** Production et environnement réel  

### Caractéristiques :
- ✅ Intégration complète avec l'API DGI selon `FNE-procedureapi.md`
- ✅ Authentification Bearer Token avec `configuration.ApiKey`
- ✅ Gestion des environnements Test/Production
- ✅ Validation complète des données avant certification
- ✅ Logging détaillé pour traçabilité
- ✅ Gestion d'erreurs robuste avec retry automatique

### Configuration requise :
```sql
-- Base de données : data/FNEV4.db
-- Table : FneConfigurations
-- Champs requis :
- ApiKey          # Token d'authentification (utilisé comme Bearer Token)
- BaseUrl         # http://54.247.95.108/ws (Test) ou URL production
- Environment     # "Test" ou "Production"
- IsActive        # 1 (true)
```

## 🧪 Service de Test : `FneCertificationServiceMock`

**Fichier :** `FneCertificationService.Mock.cs`  
**Classe :** `FneCertificationServiceMock`  
**Usage :** Tests unitaires et développement uniquement  

### Caractéristiques :
- ⚠️ **NE PAS UTILISER EN PRODUCTION**
- 🚫 Retourne des erreurs critiques pour forcer l'utilisation du service réel
- 📝 Utilisé uniquement pour les tests automatisés

## 🔧 Configuration dans `App.xaml.cs`

```csharp
// Service actuellement enregistré (PRODUCTION)
services.AddScoped<IFneCertificationService, FNEV4.Infrastructure.Services.FneCertificationService>();

// Pour basculer en mode test (DÉVELOPPEMENT UNIQUEMENT)
// services.AddScoped<IFneCertificationService, FNEV4.Infrastructure.Services.FneCertificationServiceMock>();
```

## 📋 Procédure de Certification selon DGI

### 1. Prérequis
- Inscription à la plateforme FNE : http://54.247.95.108
- Configuration de l'environnement de test
- Obtention de l'API Key depuis l'onglet "Paramétrage"

### 2. Workflow de certification
1. **Validation** : Vérification des données facture et configuration
2. **Préparation** : Construction du payload JSON selon specs DGI
3. **Authentification** : Utilisation de l'API Key comme Bearer Token
4. **Appel API** : POST vers `/external/invoices/sign`
5. **Traitement** : Analyse de la réponse et mise à jour BDD
6. **Logging** : Enregistrement dans `FneApiLogs` pour traçabilité

### 3. Format API (selon FNE-procedureapi.md)
```http
POST http://54.247.95.108/ws/external/invoices/sign
Authorization: Bearer {ApiKey}
Content-Type: application/json

{
  "invoiceType": "purchase",
  "paymentMethod": "mobile-money",
  "template": "B2B",
  "clientCompanyName": "...",
  "items": [...]
}
```

## ⚡ Points Clés de Sécurité

1. **Token d'authentification** : TOUJOURS utiliser `configuration.ApiKey` comme Bearer Token
2. **Validation stricte** : Toutes les données sont validées avant envoi
3. **Logging sécurisé** : Les tokens ne sont jamais loggés en clair
4. **Mode test** : Détection automatique des tokens d'exemple pour simulation

## 🔄 Historique des Modifications

### v1.0 - Réorganisation (2025-09-16)
- ✅ Renommage `FneCertificationServiceCorrected.cs` → `FneCertificationService.cs`
- ✅ Renommage `MockFneCertificationService.cs` → `FneCertificationService.Mock.cs`
- ✅ Suppression de `FneCertificationService.cs.disabled`
- ✅ Mise à jour de l'enregistrement dans `App.xaml.cs`
- ✅ Correction de l'utilisation de l'API Key au lieu du BearerToken
- ✅ Architecture clarifiée et documentation créée