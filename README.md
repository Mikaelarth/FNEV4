# FNEV4 - Application Desktop d'Interfaçage FNE

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![WPF](https://img.shields.io/badge/WPF-Windows-blue.svg)](https://docs.microsoft.com/en-us/dotnet/framework/wpf/)
[![SQLite](https://img.shields.io/badge/Database-SQLite-green.svg)](https://sqlite.org/)

## 📋 Description

Application desktop Windows WPF pour interfacer automatiquement les systèmes **Sage 100** avec l'**API FNE de la DGI** ivoirienne, permettant la certification automatique des factures électroniques selon la nouvelle réglementation 2025.

## 🏗️ Architecture

Le projet suit une **Clean Architecture** avec séparation stricte des couches :

```
┌─────────────────────────────────────┐
│           PRESENTATION              │
│     (Views, ViewModels, UI)         │
├─────────────────────────────────────┤
│           APPLICATION               │
│    (Use Cases, Commands, Queries)   │
├─────────────────────────────────────┤
│              CORE                   │
│      (Entities, Services)           │
├─────────────────────────────────────┤
│          INFRASTRUCTURE             │
│  (Database, API, File System)       │
└─────────────────────────────────────┘
```

## 📁 Structure du Projet

### `/src` - Code Source
- **`FNEV4.Core/`** - Couche domaine (entités, interfaces, services métier)
- **`FNEV4.Application/`** - Couche application (use cases, commands, queries)
- **`FNEV4.Infrastructure/`** - Couche infrastructure (base de données, API, Excel)
- **`FNEV4.Presentation/`** - Couche présentation (WPF, MVVM, Material Design)

### `/tests` - Tests Dédiés
- **`FNEV4.Tests.Unit/`** - Tests unitaires par couche
- **`FNEV4.Tests.Integration/`** - Tests d'intégration (API, BDD, Excel)
- **`FNEV4.Tests.E2E/`** - Tests de bout en bout
- **`TestData/`** - Données de test (Excel, JSON, Mocks)

### `/docs` - Documentation
- **`api/`** - Documentation API FNE DGI
- **`technical/`** - Documentation technique
- **`user-guide/`** - Guide utilisateur

### `/tools` - Outils
- **`scripts/`** - Scripts d'automatisation
- **`deployment/`** - Configuration déploiement

### `/data` - Données
- **`sample/`** - Exemples de fichiers Sage 100
- **`templates/`** - Modèles Excel

## 🎯 Modules Fonctionnels

1. **📊 Dashboard** - Vue d'ensemble et KPI
2. **📥 Import & Traitement** - Parsing Excel Sage 100
3. **📄 Gestion des Factures** - CRUD complet
4. **🔐 Certification FNE** - Intégration API DGI
5. **👥 Gestion Clients** - Référentiel avec NCC
6. **📈 Rapports & Analyses** - Business Intelligence
7. **⚙️ Configuration** - Paramétrage entreprise/API
8. **🔧 Maintenance** - Logs et diagnostics
9. **❓ Aide & Support** - Documentation intégrée

## 🛠️ Stack Technique

- **Framework :** .NET 8.0 WPF (Windows Presentation Foundation)
- **Pattern UI :** MVVM avec CommunityToolkit.Mvvm
- **Base de données :** SQLite avec Entity Framework Core 8.0
- **Interface :** MaterialDesignThemes + MaterialDesignColors
- **Excel :** ClosedXML + DocumentFormat.OpenXml
- **HTTP :** HttpClient + Polly (retry policies)
- **Logging :** Serilog avec sinks multiples
- **Tests :** xUnit + FluentAssertions + Moq

## 📊 Structure Excel Sage 100

### Format d'entrée validé
- **1 Classeur Excel** = N Factures
- **1 Feuille Excel** = 1 Facture complète

### En-tête facture (Colonne A)
| Ligne | Contenu | Exemple | Obligatoire |
|-------|---------|---------|-------------|
| A3 | Numéro de facture | 702442 | ✅ |
| A5 | Code client | 1999 (divers) ou autre | ✅ |
| A6 | NCC client normal | 2354552Q (si code ≠ 1999) | ❌ |
| A8 | Date facture | 45880 → 11/08/2025 | ✅ |
| A10 | Point de vente | Gestoci | ✅ |
| A11 | Intitulé client | DIVERS CLIENTS CARBURANTS | ❌ |
| A13 | Nom réel client divers | ARTHUR LE GRAND | ❌ |
| A15 | NCC client divers | 1205425Z (si code = 1999) | ❌ |
| A17 | Facture avoir | FAC000 | ❌ |

### Lignes produits (Ligne 20+)
| Colonne | Contenu | Exemple | Obligatoire |
|---------|---------|---------|-------------|
| B | Code produit | ORD001 | ✅ |
| C | Désignation | Ordinateur Dell | ✅ |
| D | Prix unitaire | 800000 | ✅ |
| E | Quantité | 1 | ✅ |
| F | Unité | pcs | ❌ |
| G | Type TVA | TVA | ✅ |
| H | Montant HT | 800000 | ✅ |

## 🔗 API FNE DGI

### Endpoints supportés
- **POST /external/invoices/sign** - Certification facture vente
- **POST /external/invoices/{id}/refund** - Facture d'avoir
- **POST /external/invoices/sign** - Bordereau d'achat (invoiceType=purchase)

### Environnements
- **Test :** http://54.247.95.108/ws
- **Production :** URL transmise après validation DGI

## 🚀 Phases de Développement

### Phase 1 (MVP) - 2-3 semaines
- ✅ Architecture Clean implémentée
- ✅ Dashboard simple
- ✅ Import Excel basique
- ✅ Configuration entreprise

### Phase 2 (Core) - 3-4 semaines
- ✅ CRUD factures complet
- ✅ Certification API FNE
- ✅ Gestion des erreurs
- ✅ Interface Material Design

### Phase 3 (Advanced) - 2-3 semaines
- ✅ Gestion clients complète
- ✅ Rapports et analytics
- ✅ Recherche avancée
- ✅ Batch processing

### Phase 4 (Final) - 1-2 semaines
- ✅ Maintenance et diagnostics
- ✅ Documentation utilisateur
- ✅ Tests automatisés
- ✅ Packaging deployment

## 📝 Documentation de Référence

- `CAHIER-DES-CHARGES-FNEV4.md` - Spécifications complètes (32 pages)
- `ARCHITECTURE.md` - Architecture technique détaillée
- `FNE-procedureapi.md` - Documentation API officielle DGI (26 pages)
- `exemple_structure_excel.py` - Guide structure Excel Sage 100

## 🧪 Tests

Le dossier `/tests` est dédié exclusivement aux tests pour maintenir la propreté du projet :

```bash
# Tests unitaires
tests/FNEV4.Tests.Unit/

# Tests d'intégration
tests/FNEV4.Tests.Integration/

# Tests de bout en bout
tests/FNEV4.Tests.E2E/

# Données de test
tests/TestData/
```

## 🏃‍♂️ Démarrage Rapide

1. **Cloner le projet**
2. **Installer .NET 8.0 SDK**
3. **Configurer l'environnement de test DGI**
4. **Lancer les tests**
5. **Démarrer le développement Phase 1**

## 📞 Support

Pour toute assistance, contacter le support technique de la plateforme FNE :
**support.fne@dgi.gouv.ci**

---

**Version :** 1.0  
**Date :** 4 Septembre 2025  
**Statut :** Structure créée, prêt pour développement Phase 1
