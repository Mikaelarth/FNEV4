#  ARCHITECTURE FNEV4 - APPLICATION FNE DESKTOP

**Version :** 4.0  
**Date :** 4 Septembre 2025  
**Projet :** Application d'interfaçage FNE pour la DGI Côte d'Ivoire

---

##  VUE D'ENSEMBLE

### Objectif principal
Créer une **application desktop Windows WPF** qui permet aux entreprises ivoiriennes d'**interfacer leur système de facturation Sage 100** avec l'API FNE de la DGI pour certifier leurs factures électroniques.

### Fonctionnalités clés
-  **Import fichiers Excel Sage** (parsing structure spécifique)
-  **Certification API FNE** (Vente, Avoir, Bordereau d'achat)
-  **Gestion complète des factures** (CRUD)
-  **Interface utilisateur moderne** (Material Design)
-  **Base de données locale** (SQLite)
-  **Rapports et analytics** (Business Intelligence)

---

##  ARCHITECTURE DES MENUS

### Navigation principale (Menu vertical gauche)

#### 1.  **TABLEAU DE BORD (Dashboard)**
- Vue d'ensemble (stats, graphiques, alertes)
- Statut du système (API FNE, stickers, sync)
- Actions rapides (import, certification, erreurs)

#### 2.  **IMPORT & TRAITEMENT**
-  Import de fichiers (Excel .xlsx/.xls, drag&drop)
-  Parsing & Validation (aperçu, validation, correction)
-  Historique des imports (sessions, détails, logs)

#### 3.  **GESTION DES FACTURES**
-  Liste des factures (filtres, tri, recherche)
-  Édition de factures (client, produits, calculs)
-  Détails de facture (vue complète, historique, QR Code)
-  Factures d'avoir (création, retours, liaison)

#### 4.  **CERTIFICATION FNE**
-  Certification manuelle (sélection, aperçu, suivi)
-  Certification automatique (règles, planification)
-  Suivi des certifications (dashboard, taux, temps)
-  Retry & Reprises (échecs, retry auto, analyse)

#### 5.  **GESTION CLIENTS**
-  Liste des clients (répertoire, contact, historique)
-  Ajout/Modification (formulaire, NCC, classification)
-  Recherche avancée (multicritères, filtres, export)

#### 6.  **RAPPORTS & ANALYSES**
-  Rapports standards (CA, TVA, top clients/produits)
-  Rapports FNE (certifications, stickers, erreurs)
-  Analyses personnalisées (constructeur, graphiques)

#### 7.  **CONFIGURATION**
-  Entreprise (NCC, points de vente, logo)
-  API FNE (URLs, clés, connexion, test)
-  Chemins & Dossiers (import, export, archivage)
-  Interface utilisateur (thèmes, langue, préférences)
-  Performances (timeouts, retry, optimisations)

#### 8.  **MAINTENANCE**
-  Logs & Diagnostics (consultation, filtrage, export)
-  Base de données (sauvegarde, nettoyage, stats)
-  Synchronisation (Sage, configs, référentiels)
-  Outils techniques (tests, validation, rapports)

#### 9.  **AIDE & SUPPORT**
-  Documentation (guide, procédures, FAQ)
-  Support (contact, rapports d'incident, tickets)
- ℹ À propos (versions, changelog, licences)

---

##  STRUCTURE EXCEL SAGE 100

### Architecture du fichier Excel
**Principe fondamental :** 
- **1 Classeur Excel** = Plusieurs factures
- **1 Feuille Excel** = **1 Facture complète**

### Structure de chaque feuille (facture)

#### EN-TÊTE DE FACTURE (Colonne A fixe)
| Ligne | Cellule | Contenu | Exemple | Obligatoire |
|-------|---------|---------|---------|-------------|
| **3** | A3 | Numéro de facture | 702442 |  |
| **5** | A5 | Code client | 1999 (divers) ou autre (normal) |  |
| **6** | A6 | NCC client normal | 2354552Q (si code ≠ 1999) |  |
| **8** | A8 | Date facture | 45880 → 11/08/2025 |  |
| **10** | A10 | Point de vente | Gestoci |  |
| **11** | A11 | Intitulé client | DIVERS CLIENTS CARBURANTS |  |
| **13** | A13 | Nom réel client divers | ARTHUR LE GRAND (si code = 1999) |  |
| **15** | A15 | NCC client divers | 1205425Z (si code = 1999) |  |
| **17** | A17 | Numéro facture avoir | FAC000 (si avoir) |  |

#### LIGNES DE PRODUITS (à partir de la ligne 20)
| Colonne | Contenu | Exemple | Obligatoire |
|---------|---------|---------|-------------|
| **B** | Code produit | ORD001 |  |
| **C** | Désignation | Ordinateur Dell |  |
| **D** | Prix unitaire | 800000 |  |
| **E** | Quantité | 1 |  |
| **F** | Emballage/Unité | pcs |  |
| **G** | Type TVA | TVA/TVAB/TVAC/TVAD |  |
| **H** | Montant HT | 800000 |  |

---

##  STRUCTURE DE BASE DE DONNÉES (SQLite)

### Principales tables

`sql
-- Configuration entreprise
CREATE TABLE Company (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Ncc VARCHAR(20) NOT NULL UNIQUE,
    Name VARCHAR(200) NOT NULL,
    FneApiKey VARCHAR(500),
    FneEnvironment VARCHAR(10) DEFAULT 'test',
    DefaultPointOfSale VARCHAR(10),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Factures principales
CREATE TABLE Invoices (
    Id VARCHAR(36) PRIMARY KEY,
    CompanyId INTEGER NOT NULL,
    InvoiceNumber VARCHAR(50) NOT NULL,
    CustomerCode VARCHAR(50) NOT NULL,
    InvoiceDate DATE NOT NULL,
    PointOfSaleCode VARCHAR(10) NOT NULL,
    CustomerCompanyTitle VARCHAR(200),
    CustomerName VARCHAR(200),
    CustomerNcc VARCHAR(20),
    TotalAmountHT DECIMAL(15,2) DEFAULT 0,
    TotalVatAmount DECIMAL(15,2) DEFAULT 0,
    TotalAmountTTC DECIMAL(15,2) DEFAULT 0,
    Status VARCHAR(20) DEFAULT 'Imported',
    FneReference VARCHAR(100),
    FneToken VARCHAR(500),
    SourceFileName VARCHAR(500),
    SourceSheetName VARCHAR(200),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CompanyId) REFERENCES Company(Id)
);

-- Lignes de produits
CREATE TABLE InvoiceItems (
    Id VARCHAR(36) PRIMARY KEY,
    InvoiceId VARCHAR(36) NOT NULL,
    LineNumber INTEGER NOT NULL,
    ProductCode VARCHAR(100) NOT NULL,
    Description VARCHAR(500) NOT NULL,
    UnitPrice DECIMAL(15,2) NOT NULL,
    Quantity DECIMAL(10,3) NOT NULL,
    Unit VARCHAR(50),
    VatType VARCHAR(10) NOT NULL,
    AmountHT DECIMAL(15,2) NOT NULL,
    VatRate DECIMAL(5,4),
    VatAmount DECIMAL(15,2),
    AmountTTC DECIMAL(15,2),
    FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id) ON DELETE CASCADE
);

-- Types de TVA
CREATE TABLE VatTypes (
    Code VARCHAR(10) PRIMARY KEY,
    Name VARCHAR(50) NOT NULL,
    Rate DECIMAL(5,4) NOT NULL,
    Description VARCHAR(200)
);

INSERT INTO VatTypes VALUES
('TVA', 'TVA normale', 0.1800, 'TVA normale de 18%'),
('TVAB', 'TVA réduite', 0.0900, 'TVA réduite de 9%'),
('TVAC', 'TVA exonérée convention', 0.0000, 'TVA exonérée convention 0%'),
('TVAD', 'TVA exonérée légale', 0.0000, 'TVA exonérée légale 0%');
`

---

##  TECHNOLOGIES

### Stack technique recommandé
- **Frontend:** WPF (.NET 8.0) + Material Design Themes
- **Pattern:** MVVM avec CommunityToolkit.Mvvm
- **Backend:** Clean Architecture (Core/Infrastructure/Presentation)
- **Base de données:** SQLite avec Entity Framework Core 8.0
- **Logging:** Serilog
- **HTTP Client:** HttpClient avec Polly (retry policies)
- **Excel:** ClosedXML
- **JSON:** System.Text.Json
- **Validation:** FluentValidation

---

##  PHASES DE DÉVELOPPEMENT

### Phase 1 (MVP) - Fondations (2-3 semaines)
1. Dashboard (interface de base)
2. Import & Traitement (parsing Excel)
3. Configuration de base (paramètres entreprise)

### Phase 2 - Fonctionnalités core (3-4 semaines)
4. Gestion des factures (CRUD complet)
5. Certification FNE (intégration API)

### Phase 3 - Fonctionnalités avancées (2-3 semaines)
6. Gestion clients (référentiel)
7. Rapports de base (analytics)

### Phase 4 - Finalisation (1-2 semaines)
8. Maintenance (outils admin)
9. Aide & Support (documentation)

---

##  ORGANISATION VISUELLE

`

 [Logo] FNE Desktop v4.0           [User] [Settings] 

 Menu                                               
 Nav             CONTENU PRINCIPAL                   
 ical                                               
                                                    

 Status:  Connecté FNE |  1,245 stickers restants 

`

### Navigation
- **Menu vertical gauche:** Icônes + textes
- **Fil d'Ariane:** Localisation dans l'arborescence
- **Zones de contenu adaptatif:** Responsive selon la résolution
- **Barre de statut:** Informations temps réel

---

**Dernière mise à jour:** 4 Septembre 2025  
**Version:** 1.0  
**Statut:** Architecture validée, prêt pour développement
