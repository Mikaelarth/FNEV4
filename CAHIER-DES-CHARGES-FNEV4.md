# CAHIER DES CHARGES - FNEV4
## Application Desktop d'Interfaçage FNE pour la DGI Côte d'Ivoire

**Version :** 1.0  
**Date de création :** 4 Septembre 2025  
**Projet :** FNEV4 - Facture Normalisée Électronique Desktop  
**Client :** Entreprises ivoiriennes utilisant Sage 100  
**Autorité :** Direction Générale des Impôts (DGI) Côte d'Ivoire

---

## 1. CONTEXTE ET ENJEUX

### 1.1 Cadre réglementaire
La **loi de finances 2025** de la Côte d'Ivoire impose l'obligation de délivrance de la **Facture Normalisée Électronique (FNE)** et du **Reçu Normalisé Électronique (RNE)**, reprenant les dispositions des articles 384, 385 et suivants du Code Général des Impôts (CGI).

### 1.2 Évolution du système de facturation
- **Digitalisation obligatoire** des procédures de facturation
- **Certification par sticker électronique** avec signature électronique
- **Numérotation en série ininterrompue** annuelle
- **Interfaçage API** avec les systèmes existants des entreprises

### 1.3 Problématique métier
Les entreprises ivoiriennes utilisant **Sage 100** doivent :
- Conserver leur système de facturation existant
- Se conformer aux exigences FNE de la DGI
- Automatiser le processus de certification
- Maintenir la traçabilité complète des opérations

---

## 2. OBJECTIFS DU PROJET

### 2.1 Objectif principal
Développer une **application desktop Windows WPF** permettant aux entreprises d'**interfacer automatiquement** leur système Sage 100 avec l'API FNE de la DGI pour certifier leurs factures électroniques en conformité avec la réglementation ivoirienne.

### 2.2 Objectifs spécifiques

#### 2.2.1 Objectifs fonctionnels
- **Import automatisé** des exports Excel Sage 100
- **Parsing intelligent** de la structure spécifique Sage
- **Certification API FNE** pour tous types de documents (Vente, Avoir, Bordereau)
- **Gestion complète du cycle de vie** des factures
- **Interface utilisateur moderne** et intuitive
- **Reporting et analytics** complets
- **Gestion des erreurs** et retry automatique

#### 2.2.2 Objectifs techniques
- **Architecture Clean** avec séparation des couches
- **Approche progressive** de développement (MVP → Features → Finalisation)
- **Base de données locale** SQLite pour l'autonomie
- **Logging complet** pour la traçabilité
- **Tests automatisés** pour la fiabilité
- **Performance optimisée** pour traitement de volume

#### 2.2.3 Objectifs business
- **Conformité réglementaire** DGI 100%
- **Réduction des erreurs** de saisie manuelle
- **Gain de temps** significatif (80% d'automatisation)
- **Traçabilité complète** des opérations
- **Scalabilité** pour croissance du volume

---

## 3. PÉRIMÈTRE FONCTIONNEL

### 3.1 Fonctionnalités principales

#### 3.1.1 Module Import & Traitement
- **Import fichiers Excel** (.xlsx/.xls) par drag & drop
- **Parsing automatique** selon structure Sage 100 documentée
- **Validation des données** avec règles métier FNE
- **Aperçu et correction** avant traitement
- **Historique complet** des imports avec logs détaillés

#### 3.1.2 Module Gestion des Factures
- **CRUD complet** des factures (Create, Read, Update, Delete)
- **Recherche avancée** multicritères avec filtres
- **Édition manuelle** pour corrections exceptionnelles
- **Gestion des factures d'avoir** avec liaison automatique
- **Statuts de traitement** avec workflow défini

#### 3.1.3 Module Certification FNE
- **Certification automatique** en lot ou unitaire
- **Intégration API DGI** complète (3 endpoints)
- **Gestion des stickers** avec alerte stock
- **Retry automatique** en cas d'échec temporaire
- **QR Code generation** pour vérification

#### 3.1.4 Module Gestion Clients
- **Référentiel clients** avec NCC et classification
- **Import/Export** formats standards
- **Recherche intelligente** par multiple critères
- **Historique des transactions** par client

#### 3.1.5 Module Rapports & Analytics
- **Tableaux de bord** temps réel avec KPI
- **Rapports standards** (CA, TVA, certifications)
- **Analyses personnalisées** avec graphiques
- **Export multi-formats** (PDF, Excel, CSV)

#### 3.1.6 Module Configuration
- **Paramétrage entreprise** (NCC, points de vente)
- **Configuration API FNE** (URLs, clés, environnements)
- **Gestion des chemins** (import, export, archivage)
- **Préférences utilisateur** (thèmes, langue)

### 3.2 Fonctionnalités avancées

#### 3.2.1 Module Maintenance
- **Logs et diagnostics** avec niveaux de détail
- **Sauvegarde automatique** de la base de données
- **Synchronisation** avec référentiels externes
- **Outils de validation** et tests de connectivité

#### 3.2.2 Module Aide & Support
- **Documentation intégrée** avec procédures
- **Assistant de configuration** pas à pas
- **Diagnostics automatiques** des problèmes
- **Export de rapports d'incident** pour support

---

## 4. SPÉCIFICATIONS TECHNIQUES

### 4.1 Architecture applicative

#### 4.1.1 Pattern architectural
- **Clean Architecture** avec séparation stricte des couches
- **MVVM (Model-View-ViewModel)** pour l'interface utilisateur
- **Dependency Injection** pour l'inversion de contrôle
- **Repository Pattern** pour l'abstraction des données

#### 4.1.2 Structure des couches
```
┌─────────────────────────────────────┐
│           PRESENTATION              │
│     (Views, ViewModels, UI)         │
├─────────────────────────────────────┤
│           APPLICATION               │
│    (Use Cases, Commands, Queries)   │
├─────────────────────────────────────┤
│              DOMAIN                 │
│      (Entities, Services)           │
├─────────────────────────────────────┤
│          INFRASTRUCTURE             │
│  (Database, API, File System)       │
└─────────────────────────────────────┘
```

### 4.2 Stack technique

#### 4.2.1 Framework et langage
- **Framework :** .NET 8.0 WPF (Windows Presentation Foundation)
- **Langage :** C# 12.0 avec nullable reference types
- **Pattern UI :** MVVM avec CommunityToolkit.Mvvm

#### 4.2.2 Base de données
- **SGBD :** SQLite (base embarquée)
- **ORM :** Entity Framework Core 8.0
- **Migrations :** Code-First avec versioning automatique

#### 4.2.3 Packages principaux
- **Interface :** MaterialDesignThemes 4.9.0 + MaterialDesignColors
- **Excel :** ClosedXML 0.102.1 + DocumentFormat.OpenXml
- **HTTP :** HttpClient natif + Polly pour retry policies
- **Logging :** Serilog avec sinks multiples
- **Validation :** FluentValidation 11.8.1
- **Mapping :** AutoMapper 12.0.1
- **JSON :** System.Text.Json 8.0

### 4.3 Spécifications de performance
- **Démarrage application :** < 3 secondes
- **Import fichier Excel :** < 10 secondes pour 1000 factures
- **Certification API :** < 5 secondes par facture
- **Recherche factures :** < 1 seconde sur 100k enregistrements
- **Génération rapports :** < 15 secondes pour 10k factures

---

## 5. STRUCTURE DES DONNÉES

### 5.1 Mapping Excel Sage 100

#### 5.1.1 Structure d'entrée validée
**Principe :** 1 Classeur Excel = N Factures, 1 Feuille = 1 Facture

#### 5.1.2 En-tête facture (Colonne A fixe)
| Ligne | Cellule | Contenu | Format | Obligatoire |
|-------|---------|---------|---------|-------------|
| 3 | A3 | Numéro de facture | String(50) | ✅ |
| 5 | A5 | Code client | String(50) | ✅ |
| 6 | A6 | NCC client normal | String(20) | ❌ (si code ≠ 1999) |
| 8 | A8 | Date facture | Date (YYYY-MM-DD) | ✅ |
| 10 | A10 | Point de vente | String(10) | ✅ |
| 11 | A11 | Intitulé client | String(200) | ❌ (si code = 1999) |
| 13 | A13 | Nom réel client divers | String(200) | ❌ (si code = 1999) |
| 15 | A15 | NCC client divers | String(20) | ❌ (si code = 1999) |
| 17 | A17 | Facture avoir | String(50) | ❌ |

#### 5.1.3 Lignes produits (Ligne 20+)
| Colonne | Contenu | Format | Obligatoire |
|---------|---------|---------|-------------|
| B | Code produit | String(100) | ✅ |
| C | Désignation | String(500) | ✅ |
| D | Prix unitaire | Decimal(15,2) | ✅ |
| E | Quantité | Decimal(10,3) | ✅ |
| F | Unité/Emballage | String(50) | ❌ |
| G | Type TVA | Enum(TVA,TVAB,TVAC,TVAD) | ✅ |
| H | Montant HT | Decimal(15,2) | ✅ |

#### 5.1.4 Logique conditionnelle clients
**IMPORTANT** : La structure Excel suit une logique conditionnelle selon le code client :

- **Si Code Client = 1999 (Client Divers) :**
  - A6 : NCC générique client divers
  - A11 : Intitulé générique (ex: "DIVERS CLIENTS CARBURANTS")
  - A13 : **Nom réel du client** (ex: "ARTHUR LE GRAND")
  - A15 : **NCC spécifique du client divers**

- **Si Code Client ≠ 1999 (Client Normal) :**
  - A6 : **NCC du client normal**
  - A11, A13, A15 : Non utilisés (nom depuis base de données)

- **NCC Entreprise Émettrice :**
  - **N'apparaît PAS dans l'Excel Sage 100**
  - **DOIT être configuré dans FNEV4**
  - **Utilisé pour la certification FNE**

### 5.2 Base de données locale

#### 5.2.1 Tables principales
- **Company** : Configuration entreprise
- **Invoices** : Factures principales avec statuts
- **InvoiceItems** : Lignes de facture avec calculs
- **Customers** : Référentiel clients
- **VatTypes** : Types de TVA avec taux
- **ImportSessions** : Historique des imports
- **Logs** : Journalisation complète

#### 5.2.2 Règles de gestion
- **Clés primaires :** GUID pour éviter conflits
- **Soft delete :** Marquage au lieu de suppression
- **Audit trail :** CreatedAt, UpdatedAt automatiques
- **Contraintes :** Foreign keys avec cascade approprié

---

## 6. INTÉGRATION API FNE

### 6.1 Spécifications API DGI

#### 6.1.1 Endpoints requis
- **POST /external/invoices/sign** : Certification facture vente
- **POST /external/invoices/{id}/refund** : Facture d'avoir
- **POST /external/invoices/sign** : Bordereau d'achat (invoiceType=purchase)

#### 6.1.2 Authentification
- **Type :** Bearer Token (JWT)
- **Header :** Authorization: Bearer {API_KEY}
- **Environnements :** Test + Production

#### 6.1.3 Formats de données
- **Request :** application/json
- **Response :** application/json
- **Encoding :** UTF-8

### 6.2 Gestion des erreurs
- **Retry automatique** avec backoff exponentiel
- **Codes erreur** : 400 (Bad Request), 401 (Unauthorized), 500 (Server Error)
- **Logging détaillé** de tous les échanges
- **Notification utilisateur** en temps réel

---

## 7. INTERFACE UTILISATEUR

### 7.1 Principles de design

#### 7.1.1 Design System
- **Style :** Material Design 3.0
- **Couleurs :** Palette DGI (bleu, blanc, orange)
- **Typography :** Roboto / Segoe UI
- **Iconographie :** Material Icons

#### 7.1.2 Navigation
- **Menu principal :** Vertical à gauche avec icônes
- **Fil d'Ariane :** Localisation dans l'arborescence
- **Actions contextuelles :** Boutons flottants
- **Raccourcis clavier :** Pour utilisateurs avancés

### 7.2 Structure de navigation

#### 7.2.1 Menu principal (9 sections)
1. **📊 Tableau de Bord** : Vue d'ensemble et KPI
2. **📥 Import & Traitement** : Gestion des fichiers Excel
3. **📄 Gestion des Factures** : CRUD et recherche factures
4. **🔐 Certification FNE** : Processus de certification
5. **👥 Gestion Clients** : Référentiel clients
6. **📈 Rapports & Analyses** : Business Intelligence
7. **⚙️ Configuration** : Paramétrage application
8. **🔧 Maintenance** : Outils techniques
9. **❓ Aide & Support** : Documentation et assistance

### 7.3 Responsive design
- **Résolutions supportées :** 1366x768 minimum, 4K optimisé
- **Scaling :** Support DPI élevé (125%, 150%, 200%)
- **Adaptation :** Redimensionnement intelligent des composants

---

## 8. QUALITÉ ET SÉCURITÉ

### 8.1 Tests et validation

#### 8.1.1 Stratégie de tests
- **Tests unitaires :** Coverage > 80% sur la logique métier
- **Tests d'intégration :** API FNE et base de données
- **Tests UI :** Scénarios utilisateur principaux
- **Tests de performance :** Charge et stress

#### 8.1.2 Outils de test
- **Framework :** xUnit + FluentAssertions
- **Mocking :** Moq pour isolation
- **Coverage :** Coverlet + ReportGenerator
- **CI/CD :** Validation automatique

### 8.2 Sécurité

#### 8.2.1 Protection des données
- **Chiffrement :** API Keys et données sensibles
- **Hachage :** Mots de passe avec BCrypt
- **Validation :** Input sanitization systématique
- **Logs :** Pas de données sensibles en clair

#### 8.2.2 Conformité
- **RGPD :** Respect des données personnelles
- **DGI :** Conformité réglementaire ivoirienne
- **Audit :** Traçabilité complète des opérations

---

## 9. DÉPLOIEMENT ET MAINTENANCE

### 9.1 Stratégie de déploiement

#### 9.1.1 Packaging
- **Format :** MSI + ClickOnce pour mise à jour automatique
- **Prérequis :** .NET 8.0 Runtime detection/installation
- **Installation :** Silent install pour déploiement masse

#### 9.1.2 Environnements
- **Développement :** Local avec SQLite et API Test
- **Test :** Validation avec données réelles anonymisées
- **Production :** Déploiement client avec API Production

### 9.2 Maintenance et support

#### 9.2.1 Monitoring
- **Logs centralisés :** Serilog avec rotation automatique
- **Métriques :** Performance et utilisation
- **Alertes :** Erreurs critiques et seuils

#### 9.2.2 Mise à jour
- **Versioning :** Semantic versioning (Major.Minor.Patch)
- **Déploiement :** Rolling update sans interruption
- **Rollback :** Capacité de retour version précédente

---

## 10. PLANNING ET PHASES

### 10.1 Approche progressive

#### 10.1.1 Phase 1 - MVP (2-3 semaines)
**Objectif :** Application fonctionnelle de base

**Livrables :**
- ✅ Architecture Clean implémentée
- ✅ Interface dashboard simple
- ✅ Import Excel basique
- ✅ Configuration entreprise
- ✅ Base de données SQLite

**Critères d'acceptation :**
- Import d'un fichier Excel Sage
- Affichage des factures importées
- Configuration API FNE de base

#### 10.1.2 Phase 2 - Core Features (3-4 semaines)
**Objectif :** Fonctionnalités métier essentielles

**Livrables :**
- ✅ CRUD factures complet
- ✅ Certification API FNE
- ✅ Gestion des erreurs
- ✅ Validation des données
- ✅ Interface Material Design

**Critères d'acceptation :**
- Certification réussie avec API DGI
- Gestion des factures d'avoir
- Interface utilisateur aboutie

#### 10.1.3 Phase 3 - Advanced Features (2-3 semaines)
**Objectif :** Fonctionnalités avancées

**Livrables :**
- ✅ Gestion clients complète
- ✅ Rapports et analytics
- ✅ Recherche avancée
- ✅ Batch processing
- ✅ Export multi-formats

**Critères d'acceptation :**
- Rapports business générés
- Performance validée (1000+ factures)
- Recherche multicritères opérationnelle

#### 10.1.4 Phase 4 - Finalisation (1-2 semaines)
**Objectif :** Production ready

**Livrables :**
- ✅ Maintenance et diagnostics
- ✅ Documentation utilisateur
- ✅ Tests automatisés
- ✅ Packaging deployment
- ✅ Formation et support

**Critères d'acceptation :**
- Tests automatisés > 80% coverage
- Documentation complète
- Déploiement automatisé

### 10.2 Jalons de validation

#### 10.2.1 Revues techniques
- **Sprint Reviews :** Toutes les 2 semaines
- **Code Reviews :** Avant chaque merge
- **Architecture Reviews :** À chaque phase

#### 10.2.2 Validation métier
- **Demo Phase 1 :** MVP fonctionnel
- **Demo Phase 2 :** Certification réussie
- **Demo Phase 3 :** Fonctionnalités complètes
- **Recette finale :** Production ready

---

## 11. CRITÈRES DE SUCCÈS

### 11.1 Critères fonctionnels
- ✅ **Conformité DGI :** 100% des factures certifiées avec succès
- ✅ **Performance :** Traitement de 1000 factures en < 10 minutes
- ✅ **Fiabilité :** Taux d'erreur < 1% sur les imports
- ✅ **Utilisabilité :** Formation utilisateur < 2 heures

### 11.2 Critères techniques
- ✅ **Architecture :** Clean Architecture respectée
- ✅ **Tests :** Coverage > 80% sur la logique métier
- ✅ **Performance :** Temps de réponse < 3 secondes
- ✅ **Sécurité :** Audit sécurité validé

### 11.3 Critères business
- ✅ **ROI :** Gain de temps > 80% vs saisie manuelle
- ✅ **Adoption :** Utilisation quotidienne par les équipes
- ✅ **Évolutivité :** Architecture prête pour nouvelles features
- ✅ **Support :** Documentation et formation complètes

---

## 12. RISQUES ET MITIGATION

### 12.1 Risques techniques

#### 12.1.1 Complexité API FNE
**Risque :** Changements dans les spécifications API DGI  
**Probabilité :** Moyenne  
**Impact :** Élevé  
**Mitigation :** Architecture découplée, tests automatisés API

#### 12.1.2 Performance Excel
**Risque :** Lenteur sur gros fichiers Excel  
**Probabilité :** Moyenne  
**Impact :** Moyen  
**Mitigation :** Streaming, traitement asynchrone, progress bars

### 12.2 Risques métier

#### 12.2.1 Changement réglementaire
**Risque :** Évolution des exigences DGI  
**Probabilité :** Élevée  
**Impact :** Élevé  
**Mitigation :** Architecture flexible, veille réglementaire

#### 12.2.2 Adoption utilisateur
**Risque :** Résistance au changement  
**Probabilité :** Moyenne  
**Impact :** Élevé  
**Mitigation :** Formation, documentation, UI intuitive

---

## 13. BUDGET ET RESSOURCES

### 13.1 Ressources humaines
- **Architecte/Lead Developer :** 1 personne
- **Développeur .NET/WPF :** 1 personne
- **Business Analyst :** 0.5 personne
- **Testeur QA :** 0.5 personne

### 13.2 Durée estimée
- **Durée totale :** 8-12 semaines
- **Phase 1 (MVP) :** 2-3 semaines
- **Phase 2 (Core) :** 3-4 semaines
- **Phase 3 (Advanced) :** 2-3 semaines
- **Phase 4 (Finalisation) :** 1-2 semaines

---

## 14. CONCLUSION

### 14.1 Valeur ajoutée
Le projet FNEV4 représente une **solution stratégique** pour la mise en conformité des entreprises ivoiriennes avec la réglementation FNE. L'approche **progressive et professionnelle** garantit un développement maîtrisé et une adoption réussie.

### 14.2 Facteurs clés de succès
- **Architecture technique solide** (Clean Architecture)
- **Approche progressive** (MVP → Features → Production)
- **Conformité réglementaire** (Spécifications DGI respectées)
- **Expérience utilisateur** (Interface moderne et intuitive)
- **Performance et fiabilité** (Tests automatisés et monitoring)

### 14.3 Perspectives d'évolution
- **Multi-ERP** : Extension à d'autres systèmes de gestion
- **Cloud** : Version SaaS pour PME
- **Mobile** : Application mobile pour contrôle nomade
- **BI Avancée** : Intelligence artificielle pour analytics

---

**Document approuvé le :** [À compléter]  
**Validation technique :** [À compléter]  
**Validation métier :** [À compléter]

---

*Ce cahier des charges constitue le référentiel complet pour la réalisation du projet FNEV4. Toute modification devra faire l'objet d'un avenant validé par toutes les parties prenantes.*
