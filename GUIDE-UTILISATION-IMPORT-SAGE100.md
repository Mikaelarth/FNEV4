# Guide d'Utilisation - Système d'Import Sage 100 v15 avec Dossiers Configurés

## Vue d'Ensemble du Système

Le système d'import FNEV4 combine maintenant **deux workflows complémentaires** pour maximiser l'efficacité et la flexibilité :

### 🔄 **Workflow 1 : Import Automatique** (Nouveau)
### 📂 **Workflow 2 : Import Manuel** (Amélioré)

---

## 🏗️ Architecture du Système

### Configuration des Dossiers (Base du Système)
Le système utilise la configuration centralisée des dossiers via `Configuration > Chemins & Dossiers` :

```
C:\wamp64\www\FNEV4\data\
├── Import\          ← Dépôt des fichiers à traiter automatiquement
├── Export\          ← Factures certifiées FNE exportées
├── Archive\         ← Fichiers traités avec succès (avec horodatage)
│   └── Erreurs\     ← Fichiers en erreur
├── Logs\            ← Journaux détaillés de toutes les opérations
└── Backup\          ← Sauvegardes automatiques
```

---

## 🔄 Workflow 1 : Import Automatique

### Étape 1 : Préparation
1. **Déposer les fichiers** : Placer les fichiers Excel Sage 100 v15 dans le dossier `Import\`
2. **Vérifier la configuration** : S'assurer que tous les dossiers sont bien configurés

### Étape 2 : Traitement Automatique
1. **Ouvrir FNEV4** → `Import > Import de fichiers`
2. **Cliquer sur "IMPORTER SAGE"** pour ouvrir la fenêtre spécialisée
3. **Section "Dossiers Configurés"** en haut de l'interface :
   - ✅ Affichage des chemins configurés (Import, Export, Archive)
   - 🔍 **Bouton "Scanner"** : Lance l'analyse automatique
   - 📂 **Bouton "Ouvrir"** : Accès direct au dossier d'import

### Étape 3 : Exécution
1. **Cliquer sur "Scanner"**
2. Le système :
   - 🔍 **Analyse** le dossier d'import
   - 📊 **Affiche** le nombre de fichiers trouvés
   - ⚠️ **Demande confirmation** avant traitement
3. **Confirmer** pour lancer le traitement automatique

### Étape 4 : Traitement en Lot
Pour chaque fichier Excel :
```
Fichier.xlsx → Validation → Import → Archivage → Log
```

Détail du processus :
- ✅ **Validation** : Structure Sage 100 v15 (1 feuille = 1 facture)
- 🔄 **Import** : Traitement des factures avec logique métier
- 📦 **Archivage** : `2024-01-15_14-30-25_3factures_MonFichier.xlsx`
- 📝 **Log** : Fichier de traçabilité détaillé

### Étape 5 : Résultat
```
✅ Traitement terminé !

✅ 5 fichier(s) traité(s) avec succès
❌ 1 fichier(s) en erreur  
📄 23 facture(s) importée(s) au total
```

---

## 📂 Workflow 2 : Import Manuel (Amélioré)

### Quand Utiliser l'Import Manuel ?
- 🔧 **Tests** et développement
- 🔍 **Analyse approfondie** d'un fichier spécifique
- ❌ **Fichiers exceptionnels** non conformes
- 🎯 **Import ponctuel** d'un seul fichier

### Processus Manuel
1. **Étape 1** : Sélection manuelle du fichier via "Parcourir"
2. **Étape 2** : Validation et prévisualisation
   - 🔍 Analyse de la structure
   - 📊 Aperçu des factures détectées
   - 🔧 Filtres et recherche dans la DataGrid
3. **Étape 3** : Import avec confirmation
   - ✅ Import en base de données
   - 📦 **Archivage automatique** (si option activée)
   - 📝 Génération des logs

### Nouvelles Fonctionnalités de l'Import Manuel
- 🔍 **Recherche avancée** dans les factures
- 🎛️ **Filtres** par statut, moyen de paiement, etc.
- 🏷️ **Tooltips détaillés** sur les articles
- 📦 **Archivage automatique** optionnel
- 📊 **Statistiques** en temps réel

---

## 🎯 Logique Métier Sage 100 v15

### Structure Attendue
```
Fichier Excel Sage 100 v15
├── Feuille 1 = Facture 1
├── Feuille 2 = Facture 2
└── Feuille N = Facture N
```

### Cellules Obligatoires par Feuille
```
A3  : Numéro de facture
A5  : Code client (1999 = client divers)
A8  : Date facture  
A13 : Nom réel (si client divers)
A15 : NCC spécifique (si client divers)
A18 : Moyen de paiement A18
Ligne 20+ : Articles de la facture
```

### Moyens de Paiement A18 Supportés
- `cash` : Espèces
- `card` : Carte bancaire
- `mobile-money` : Paiement mobile
- `bank-transfer` : Virement bancaire
- `check` : Chèque
- `credit` : Crédit

### Validation Automatique
- ✅ **Clients divers (1999)** : Validation du nom réel et NCC
- ✅ **Clients normaux** : Vérification en base de données
- ✅ **Moyens de paiement** : Conformité A18
- ✅ **Structure** : Respect du format Sage 100 v15

---

## 📊 Interface Utilisateur

### Section "Dossiers Configurés"
```
┌─────────────────────────────────────────────────────────┐
│ 🛠️ Dossiers Configurés - Import Automatique            │
│                                          [Scanner] [📂] │
├─────────────────────────────────────────────────────────┤
│ 📥 Import    📤 Export    📦 Archive                    │
│ C:\...\Import C:\...\Export C:\...\Archive              │
│                                                         │
│ ☑️ Archiver automatiquement les fichiers après import   │
└─────────────────────────────────────────────────────────┘
```

### Import Manuel
```
┌─────────────────────────────────────────────────────────┐
│ 1️⃣ Import Manuel - Sélection de fichier                │
│ Aucun fichier sélectionné          [Parcourir]        │
└─────────────────────────────────────────────────────────┘
```

### DataGrid de Prévisualisation
- 🔍 **Recherche** en temps réel
- 🎛️ **Filtres** par statut/paiement
- 🏷️ **Tooltips** détaillés sur les articles
- 📊 **Statistiques** instantanées

---

## 🗂️ Organisation des Fichiers

### Archivage Intelligent
Format : `YYYY-MM-DD_HH-mm-ss_NFfactures_NomFichier.xlsx`

Exemple : `2024-01-15_14-30-25_3factures_FacturesSage.xlsx`

### Logs Détaillés
```
Import automatique - 2024-01-15 14:30:25
Fichier source: FacturesSage.xlsx
Factures importées: 3
Factures échouées: 0
Durée: 2.3s
Archivé vers: C:\...\Archive\2024-01-15_14-30-25_3factures_FacturesSage.xlsx
```

### Gestion des Erreurs
- 📁 **Dossier spécialisé** : `Archive\Erreurs\`
- 📝 **Logs d'erreur** : `.error.log` avec détails
- 🔄 **Possibilité de retraitement** après correction

---

## 🚀 Avantages du Système

### Pour l'Utilisateur
- ⚡ **Gain de temps** : Import en lot vs fichier par fichier
- 🎯 **Flexibilité** : Choix entre automatique et manuel
- 👁️ **Visibilité** : Chemins et statuts affichés
- 📊 **Traçabilité** : Logs complets pour audit

### Pour l'Administration
- 🏗️ **Architecture cohérente** : Intégration avec "Chemins & Dossiers"
- 📦 **Organisation automatique** : Archivage avec métadonnées
- 🔍 **Audit complet** : Journalisation de toutes les opérations
- ⚙️ **Maintenance facilitée** : Centralisation des configurations

---

## 🔧 Configuration Requise

### Prérequis
1. **Configuration des dossiers** : `Configuration > Chemins & Dossiers`
2. **Droits d'accès** : Lecture/Écriture sur tous les dossiers
3. **Structure des fichiers** : Respect du format Sage 100 v15

### Première Utilisation
1. ⚙️ **Configurer les chemins** dans `Chemins & Dossiers`
2. 📂 **Créer la structure** de dossiers
3. 🔧 **Tester** avec un fichier de démonstration
4. 🚀 **Passer en production** avec import automatique

---

## 💡 Conseils d'Utilisation

### Import Automatique (Recommandé)
- 📅 **Traitement quotidien** : Déposer les fichiers, cliquer "Scanner"
- 📊 **Volume important** : Traitement en lot efficace
- 🔄 **Routine** : Workflow standardisé et fiable

### Import Manuel (Cas Spéciaux)
- 🔍 **Analyse** : Vérification approfondie avant import
- 🧪 **Tests** : Validation de nouveaux formats
- ❌ **Dépannage** : Traitement des fichiers problématiques

### Surveillance
- 📝 **Consulter les logs** régulièrement
- 📁 **Vider le dossier d'import** après traitement
- 🗂️ **Archiver périodiquement** les anciens fichiers

---

Ce système transforme FNEV4 d'un outil de traitement ponctuel en une **solution de workflow automatisé** pour l'import de factures Sage 100 v15, tout en conservant la flexibilité nécessaire pour les cas exceptionnels.
