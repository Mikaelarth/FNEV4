# STRUCTURE DÉTAILLÉE DU PROJET FNEV4

## 📁 ORGANISATION COMPLÈTE

```
FNEV4/
│
├── README.md                           # Documentation principale
├── ARCHITECTURE.md                     # Architecture technique
├── CAHIER-DES-CHARGES-FNEV4.md        # Spécifications complètes
├── FNE-procedureapi.md                 # Documentation API DGI
├── exemple_structure_excel.py          # Guide Excel Sage
├── factures.xlsx                       # Exemple de données
│
├── 📁 src/                             # CODE SOURCE
│   │
│   ├── 📁 FNEV4.Core/                  # DOMAINE (Clean Architecture)
│   │   ├── Entities/                   # Entités métier
│   │   ├── Interfaces/                 # Contrats d'abstraction
│   │   ├── Services/                   # Services domaine
│   │   ├── Enums/                      # Énumérations
│   │   └── DTOs/                       # Data Transfer Objects
│   │
│   ├── 📁 FNEV4.Application/           # APPLICATION (Use Cases)
│   │   ├── UseCases/
│   │   │   ├── Dashboard/              # Module 1: Tableau de bord
│   │   │   ├── ImportTraitement/       # Module 2: Import Excel
│   │   │   ├── GestionFactures/        # Module 3: CRUD Factures
│   │   │   ├── CertificationFne/       # Module 4: API FNE
│   │   │   ├── GestionClients/         # Module 5: Référentiel clients
│   │   │   ├── Rapports/               # Module 6: Analytics
│   │   │   ├── Configuration/          # Module 7: Paramétrage
│   │   │   └── Maintenance/            # Module 8: Outils admin
│   │   ├── Commands/                   # Commandes CQRS
│   │   ├── Queries/                    # Requêtes CQRS
│   │   └── Validators/                 # Validation FluentValidation
│   │
│   ├── 📁 FNEV4.Infrastructure/        # INFRASTRUCTURE
│   │   ├── Data/
│   │   │   ├── Configurations/         # Configuration EF Core
│   │   │   └── Migrations/             # Migrations base
│   │   ├── Repositories/               # Implémentation repositories
│   │   ├── ExternalServices/
│   │   │   └── FneApi/                 # Client API DGI
│   │   ├── ExcelProcessing/            # Parsing Excel Sage
│   │   └── Logging/                    # Configuration Serilog
│   │
│   └── 📁 FNEV4.Presentation/          # PRÉSENTATION (WPF)
│       ├── Views/
│       │   ├── Dashboard/              # Vues tableau de bord
│       │   ├── ImportTraitement/       # Vues import Excel
│       │   ├── GestionFactures/        # Vues gestion factures
│       │   ├── CertificationFne/       # Vues certification
│       │   ├── GestionClients/         # Vues clients
│       │   ├── Rapports/               # Vues rapports
│       │   ├── Configuration/          # Vues configuration
│       │   ├── Maintenance/            # Vues maintenance
│       │   └── AideSupport/            # Vues aide
│       ├── ViewModels/                 # ViewModels MVVM
│       ├── Controls/                   # Contrôles personnalisés
│       ├── Converters/                 # Convertisseurs WPF
│       └── Resources/
│           ├── Images/                 # Images et icônes
│           └── Styles/                 # Styles Material Design
│
├── 📁 tests/                           # TESTS DÉDIÉS
│   │
│   ├── 📁 FNEV4.Tests.Unit/            # TESTS UNITAIRES
│   │   ├── Core/                       # Tests couche domaine
│   │   ├── Application/                # Tests use cases
│   │   ├── Infrastructure/             # Tests infrastructure
│   │   └── Presentation/               # Tests ViewModels
│   │
│   ├── 📁 FNEV4.Tests.Integration/     # TESTS INTÉGRATION
│   │   ├── FneApi/                     # Tests API DGI
│   │   ├── Database/                   # Tests base données
│   │   └── ExcelProcessing/            # Tests parsing Excel
│   │
│   ├── 📁 FNEV4.Tests.E2E/             # TESTS BOUT EN BOUT
│   │
│   └── 📁 TestData/                    # DONNÉES DE TEST
│       ├── Excel/                      # Fichiers Excel test
│       ├── Json/                       # Réponses API mock
│       └── Mocks/                      # Objects mock
│
├── 📁 docs/                            # DOCUMENTATION
│   ├── api/                            # Docs API FNE
│   ├── technical/                      # Docs techniques
│   └── user-guide/                     # Guide utilisateur
│
├── 📁 tools/                           # OUTILS
│   ├── scripts/                        # Scripts automatisation
│   └── deployment/                     # Configuration déploiement
│
└── 📁 data/                            # DONNÉES
    ├── sample/                         # Exemples Sage 100
    └── templates/                      # Modèles Excel
```

## 🎯 CORRESPONDANCE AVEC LES 9 MODULES

### 1. 📊 Dashboard (Tableau de Bord)
- **Application:** `UseCases/Dashboard/`
- **Presentation:** `Views/Dashboard/`
- **Fonctions:** KPI, stats, alertes, actions rapides

### 2. 📥 Import & Traitement
- **Application:** `UseCases/ImportTraitement/`
- **Infrastructure:** `ExcelProcessing/`
- **Presentation:** `Views/ImportTraitement/`
- **Fonctions:** Parsing Excel Sage, validation, aperçu

### 3. 📄 Gestion des Factures
- **Application:** `UseCases/GestionFactures/`
- **Presentation:** `Views/GestionFactures/`
- **Fonctions:** CRUD, recherche, édition, détails

### 4. 🔐 Certification FNE
- **Application:** `UseCases/CertificationFne/`
- **Infrastructure:** `ExternalServices/FneApi/`
- **Presentation:** `Views/CertificationFne/`
- **Fonctions:** Certification API, retry, QR codes

### 5. 👥 Gestion Clients
- **Application:** `UseCases/GestionClients/`
- **Presentation:** `Views/GestionClients/`
- **Fonctions:** Référentiel, NCC, historique

### 6. 📈 Rapports & Analyses
- **Application:** `UseCases/Rapports/`
- **Presentation:** `Views/Rapports/`
- **Fonctions:** BI, graphiques, exports

### 7. ⚙️ Configuration
- **Application:** `UseCases/Configuration/`
- **Presentation:** `Views/Configuration/`
- **Fonctions:** Paramétrage entreprise, API, chemins

### 8. 🔧 Maintenance
- **Application:** `UseCases/Maintenance/`
- **Infrastructure:** `Logging/`
- **Presentation:** `Views/Maintenance/`
- **Fonctions:** Logs, diagnostics, sauvegarde

### 9. ❓ Aide & Support
- **Presentation:** `Views/AideSupport/`
- **Fonctions:** Documentation, assistant, diagnostics

## 🧪 STRATÉGIE DE TESTS

### Tests Unitaires (80% coverage visé)
- **Core:** Tests entités et services domaine
- **Application:** Tests use cases et validators
- **Infrastructure:** Tests repositories et services
- **Presentation:** Tests ViewModels

### Tests d'Intégration
- **FneApi:** Tests réels avec API DGI
- **Database:** Tests Entity Framework
- **ExcelProcessing:** Tests parsing fichiers réels

### Tests E2E
- **Scénarios complets:** Import → Certification → Export
- **Interface utilisateur:** Tests automatisés WPF

## 📋 CONVENTIONS DE NOMMAGE

### Dossiers
- **PascalCase** pour tous les dossiers
- **Noms explicites** selon la fonction

### Fichiers
- **Entités:** `NomEntite.cs`
- **Interfaces:** `INomInterface.cs`
- **Services:** `NomService.cs`
- **ViewModels:** `NomViewModel.cs`
- **Views:** `NomView.xaml`
- **Tests:** `NomClasseTests.cs`

### Namespaces
- **Root:** `FNEV4`
- **Couches:** `FNEV4.Core`, `FNEV4.Application`, etc.
- **Modules:** `FNEV4.Application.UseCases.Dashboard`

## 🔄 WORKFLOW DE DÉVELOPPEMENT

### Phase 1 (MVP)
1. **Core:** Entités principales
2. **Infrastructure:** Base SQLite + Configuration
3. **Application:** Use cases basiques
4. **Presentation:** Interface simple

### Phase 2 (Features)
1. **Infrastructure:** API FNE + Excel
2. **Application:** Use cases complets
3. **Presentation:** Interface Material Design

### Phase 3 (Advanced)
1. **Rapports et analytics**
2. **Fonctionnalités avancées**
3. **Optimisations performance**

### Phase 4 (Production)
1. **Tests complets**
2. **Documentation**
3. **Packaging déploiement**

---

**Cette structure respecte:**
- ✅ **Clean Architecture** stricte
- ✅ **Séparation des responsabilités**
- ✅ **Tests dédiés** (comme demandé)
- ✅ **Organisation professionnelle**
- ✅ **Évolutivité** et maintenabilité
