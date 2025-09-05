# 🚨 CLARIFICATION CRITIQUE - NCC dans Excel Sage 100

## ⚠️ ATTENTION : Distinction absolument critique pour la facturation FNE

### 📋 Structure Excel selon votre exemple

| Ligne | Cellule | Contenu | Exemple réel | Type client |
|-------|---------|---------|-------------|-------------|
| **3** | A3 | **NUMERO DE FACTURE** | 702442 | Tous |
| **5** | A5 | **CODE CLIENT** | 1999 ou autre | Divers vs Normal |
| **6** | A6 | **NCC CLIENT NORMAL** | 2354552Q | Si code ≠ 1999 |
| **8** | A8 | **DATE** | 45880 → 11/08/2025 | Tous |
| **10** | A10 | **POINT DE VENTE** | Gestoci | Tous |
| **11** | A11 | **INTITULE CLIENT** | DIVERS CLIENTS CARBURANTS | Si code = 1999 |
| **13** | A13 | **NOM RÉEL CLIENT DIVERS** | ARTHUR LE GRAND | Si code = 1999 |
| **15** | A15 | **NCC CLIENT DIVERS** | 1205425Z | Si code = 1999 |
| **17** | A17 | **NUMERO FACTURE AVOIR** | - | Si avoir |

## 🎯 Explication métier corrigée

### **🔍 Logique conditionnelle sur CODE CLIENT (A5)**

#### **Si CODE CLIENT ≠ 1999 (Client Normal)**
- **A6 = NCC du client normal** (exemple: 2354552Q)
- **A13 = Vide ou non utilisé**
- **A15 = Vide ou non utilisé**

#### **Si CODE CLIENT = 1999 (Client Divers)**
- **A6 = NCC générique client divers** (exemple: 2354552Q)
- **A11 = Intitulé générique** (DIVERS CLIENTS CARBURANTS)
- **A13 = Nom réel/spécifique du client divers** (ARTHUR LE GRAND)
- **A15 = NCC spécifique du client divers** (1205425Z)

> **📝 Note importante :** Pour les clients à code 1999, la ligne 11 contient l'intitulé générique "DIVERS CLIENTS [CATÉGORIE]", mais le **vrai nom du client** se trouve à la ligne 13.

## ⚡ Impact sur l'architecture

### **🚨 ERREUR MAJEURE DANS MA COMPRÉHENSION PRÉCÉDENTE**

**J'ai confondu complètement !** A6 n'est PAS le NCC de l'entreprise émettrice, mais le NCC du CLIENT !

### **Logique de parsing correcte**

```csharp
// Lors du parsing d'une facture Excel
string codeClient = excelRow["A5"];
string nccClient;
string nomClient;

if (codeClient == "1999") // Client divers
{
    nccClient = excelRow["A15"];     // NCC spécifique du client divers
    string intitule = excelRow["A11"]; // Intitulé générique (ex: DIVERS CLIENTS CARBURANTS)
    nomClient = excelRow["A13"];     // VRAI nom du client divers (ex: ARTHUR LE GRAND)
}
else // Client normal
{
    nccClient = excelRow["A6"];      // NCC du client normal
    nomClient = "À récupérer depuis base client"; // Pas dans Excel
}

// Le NCC de NOTRE entreprise doit venir de la CONFIGURATION
string nccEntreprise = config.CompanyNcc; // À configurer dans l'app !
```

### **Configuration Entreprise (À CRÉER)**
```json
{
  "companyNcc": "9606123E",        // ← NCC de NOTRE entreprise (PAS dans Excel!)
  "companyName": "SARL FNEV4",
  "pointsOfSale": ["Gestoci", "Autre"]
}
```

## 🔧 Corrections urgentes nécessaires

### Fichiers à RE-corriger :
- ❌ `exemple_structure_excel.py` - Ligne 6 mal interprétée
- ❌ `ARCHITECTURE.md` - Tableau incorrect  
- ❌ `README.md` - Documentation erronée
- ❌ `CAHIER-DES-CHARGES-FNEV4.md` - Spécifications fausses

### **Nouvelle compréhension :**
- **A6 = NCC CLIENT (si code ≠ 1999)**
- **A15 = NCC CLIENT DIVERS (si code = 1999)**
- **NCC ENTREPRISE = À configurer dans l'application !**

## 🎯 Impact sur le développement

Cette correction **MAJEURE** confirme encore plus l'importance du module **Configuration Entreprise** car :

1. **Le NCC de notre entreprise n'est NULLE PART dans l'Excel !**
2. **Il DOIT être configuré dans l'application**
3. **Import Excel** récupère uniquement les NCC des CLIENTS
4. **API FNE** nécessitera notre NCC entreprise configuré

---

**Date de correction :** 5 Septembre 2025  
**Impact :** CRITIQUE - Erreur fondamentale de compréhension corrigée
