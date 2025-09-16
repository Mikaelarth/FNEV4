# 📋 SERVICE DE CERTIFICATION FNE - DOCUMENTATION TECHNIQUE

**Version :** 2.0  
**Date :** 16 septembre 2025  
**Statut :** PRODUCTION READY ✅  

---

## 🎯 APERÇU GÉNÉRAL

Le service de certification FNE de FNEV4 implémente l'intégration complète avec l'API DGI selon les spécifications officielles **FNE-procedureapi.md**. Ce service permet la certification professionnelle des factures électroniques avec tous les éléments essentiels : QR-codes, liens de téléchargement, gestion des warnings, et détails complets de certification.

### ✨ NOUVELLES FONCTIONNALITÉS (Version 2.0)

- 📱 **Génération automatique de QR-codes** à partir des tokens de vérification
- 🔗 **URLs de téléchargement et vérification** des factures certifiées  
- ⚠️ **Alertes intelligentes** sur le stock de stickers et autres warnings
- 📊 **Détails complets** de la facture certifiée selon l'API FNE
- 🔍 **Validation des tokens** de vérification avec recherche locale et distante
- 📈 **Métriques enrichies** avec balance des stickers et informations DGI

---

## 🏗️ ARCHITECTURE DES SERVICES

### Service Principal : `FneCertificationService`
```
📁 src/FNEV4.Infrastructure/Services/FneCertificationService.cs
```
- **Objectif :** Service de production avec intégration API DGI réelle
- **Authentification :** Bearer Token utilisant configuration.ApiKey  
- **Endpoints :** POST /external/invoices/sign selon documentation FNE
- **Features :** QR-codes, URLs, warnings, détails complets

### Service de Développement : `FneCertificationService.Mock.cs`
```
📁 src/FNEV4.Infrastructure/Services/FneCertificationService.Mock.cs
```
- **Objectif :** Service de test qui retourne des erreurs explicites
- **Sécurité :** Empêche l'utilisation accidentelle en production
- **Features :** QR-codes de test, URLs simulées, validation basique

### Interface Commune : `IFneCertificationService`
```
📁 src/FNEV4.Core/Interfaces/Services/IFneCertificationService.cs
```
- **Contrat :** Définit toutes les méthodes et classes de résultats
- **Modèles :** Résultats enrichis avec nouvelles propriétés FNE
- **Extensibilité :** Prêt pour futures évolutions

---

## 🔧 NOUVELLES MÉTHODES DISPONIBLES

### 📱 Génération de QR-Code
```csharp
Task<string> GenerateQrCodeAsync(string verificationToken)
```
- **Entrée :** Token de vérification FNE  
- **Sortie :** QR-Code en Base64 (format data:image/png;base64,...)
- **Usage :** Affichage direct dans l'interface utilisateur
- **Librairie :** QRCoder 1.6.0

### 🔗 URL de Vérification
```csharp
string GetVerificationUrl(string verificationToken)
```
- **Entrée :** Token de vérification FNE
- **Sortie :** URL complète pour vérification client
- **Format :** http://54.247.95.108/fr/verification/{token}
- **Usage :** Liens cliquables, partage clients

### 🔍 Validation de Token
```csharp
Task<FneTokenValidationResult> ValidateVerificationTokenAsync(string verificationToken)
```
- **Recherche locale :** Base de données FNEV4
- **Validation format :** Structure du token  
- **Informations :** Référence, date, montant, NCC
- **Résultat :** Statut de validation complet

---

## 📊 DONNÉES ENRICHIES DE CERTIFICATION

### Résultat Principal : `FneCertificationResult`

#### Nouvelles Propriétés Importantes
```csharp
// QR-Code et URLs
public string? QrCodeData { get; set; }         // Contenu du QR-Code
public string? DownloadUrl { get; set; }        // URL de téléchargement
public string? VerificationToken { get; set; }  // Token pour QR-Code

// Alertes et Warnings  
public bool HasWarning { get; set; }            // Indicateur d'alerte
public string? WarningMessage { get; set; }     // Message détaillé
public int StickerBalance { get; set; }         // Balance restante

// Détails Certification Complète
public CertifiedInvoiceInfo? CertifiedInvoiceDetails { get; set; }
```

#### Informations DGI Officielles
```csharp
public string? FneReference { get; set; }       // Numéro facture FNE
public string? NccEntreprise { get; set; }      // Identifiant contribuable  
public string? InvoiceId { get; set; }          // ID pour avoirs/annulations
```

### Détails de la Facture Certifiée : `CertifiedInvoiceInfo`
```csharp
public class CertifiedInvoiceInfo
{
    // Identification
    public string? Id { get; set; }              // ID unique DGI
    public string? Reference { get; set; }       // Numéro FNE officiel
    public DateTime CertificationDate { get; set; } // Date certification DGI
    
    // Informations Fiscales  
    public decimal Amount { get; set; }          // Montant HT
    public decimal VatAmount { get; set; }       // Montant TVA
    public decimal FiscalStamp { get; set; }     // Timbre fiscal
    
    // Informations Client
    public string? ClientNcc { get; set; }       // NCC du client
    public string? ClientName { get; set; }      // Nom client
    public string? ClientEmail { get; set; }     // Email client
    
    // Établissement  
    public string? PointOfSale { get; set; }     // Point de vente
    public string? Establishment { get; set; }   // Établissement
    
    // Gestion des Avoirs
    public string? ParentId { get; set; }        // Facture parent (avoirs)
    public string? ParentReference { get; set; } // Référence parent
}
```

---

## 🔄 FLUX DE CERTIFICATION COMPLET

### 1. Certification d'une Facture
```csharp
var result = await certificationService.CertifyInvoiceAsync(invoice, configuration);

if (result.IsSuccess)
{
    // ✅ Données de base disponibles
    string fneReference = result.FneReference;
    string verificationToken = result.VerificationToken;
    int stickerBalance = result.StickerBalance;
    
    // 📱 Génération automatique du QR-Code  
    string qrCodeBase64 = result.QrCodeData;
    
    // 🔗 URL de vérification pour le client
    string verificationUrl = result.DownloadUrl;
    
    // ⚠️ Vérification des alertes
    if (result.HasWarning)
    {
        ShowWarning(result.WarningMessage); // Ex: Stock faible
    }
    
    // 📊 Détails complets de certification
    if (result.CertifiedInvoiceDetails != null)
    {
        var details = result.CertifiedInvoiceDetails;
        DisplayCertificationDetails(details.Id, details.Reference, 
            details.CertificationDate, details.Amount);
    }
}
```

### 2. Génération Manuel de QR-Code
```csharp
// Si besoin de régénérer le QR-Code
string qrCode = await certificationService.GenerateQrCodeAsync(verificationToken);

// Affichage dans l'interface
imageQrCode.Source = ConvertBase64ToImage(qrCode);
```

### 3. Validation de Token Client
```csharp
// Validation d'un token fourni par un client
var validation = await certificationService.ValidateVerificationTokenAsync(token);

if (validation.IsValid)
{
    Console.WriteLine($"Facture : {validation.InvoiceReference}");
    Console.WriteLine($"Montant : {validation.InvoiceAmount}");
    Console.WriteLine($"Date : {validation.CertificationDate}");
    Console.WriteLine($"Vérifier : {validation.TokenUrl}");
}
```

---

## ⚙️ CONFIGURATION ET INSTALLATION

### Prérequis Techniques
- ✅ **.NET 8.0** ou supérieur
- ✅ **Entity Framework Core 8.0** pour la base de données  
- ✅ **QRCoder 1.6.0** pour la génération de QR-codes
- ✅ **Configuration API DGI** avec clé valide

### Configuration Base de Données
```sql
-- Configuration FNE active
UPDATE FneConfigurations 
SET IsActive = 1, 
    BaseUrl = 'http://54.247.95.108/ws',
    ApiKey = 'VotreCleAPIRéelle'
WHERE Id = 1;
```

### Injection de Dépendance
```csharp
// Dans App.xaml.cs ou Program.cs
services.AddScoped<IFneCertificationService, FneCertificationService>();
services.AddHttpClient(); // Pour les appels API
```

---

## 🛠️ UTILISATION AVANCÉE

### Gestion des Warnings de Stock
```csharp
if (result.HasWarning && result.StickerBalance < 10)
{
    // Alert critique : stock très faible
    ShowCriticalAlert($"Stock critique ! {result.StickerBalance} stickers restants");
    
    // Action recommandée : contacter DGI
    ContactDgiForStickerRefill();
}
```

### Certification en Lot avec Monitoring
```csharp
var batchResult = await certificationService.CertifyPendingInvoicesAsync(100);

foreach (var result in batchResult.Results)
{
    if (result.IsSuccess)
    {
        // Traiter chaque certification réussie
        ProcessSuccessfulCertification(result);
        
        // Générer QR-Code si nécessaire
        if (needsQrCode)
        {
            var qrCode = await certificationService.GenerateQrCodeAsync(result.VerificationToken);
            SaveQrCodeToInvoice(result.InvoiceId, qrCode);
        }
    }
}

// Statistiques globales
Console.WriteLine($"Traité : {batchResult.TotalCount}");
Console.WriteLine($"Succès : {batchResult.SuccessCount}");
Console.WriteLine($"Taux : {batchResult.SuccessRate:F1}%");
```

### Validation et Audit des Tokens
```csharp
// Audit de tous les tokens d'une période
var invoices = await certificationService.GetInvoicesForPeriodAsync(startDate, endDate);

foreach (var invoice in invoices.Where(i => !string.IsNullOrEmpty(i.VerificationToken)))
{
    var validation = await certificationService.ValidateVerificationTokenAsync(invoice.VerificationToken);
    
    if (!validation.IsValid)
    {
        // Token invalide détecté
        LogAuditIssue($"Token invalide pour facture {invoice.InvoiceNumber}");
        
        // Action corrective possible
        await RecertifyInvoiceIfNeeded(invoice);
    }
}
```

---

## 📈 MÉTRIQUES ET MONITORING

### Health Check Enrichi
```csharp
var health = await certificationService.PerformHealthCheckAsync();

Console.WriteLine($"Système : {(health.IsHealthy ? "✅ OK" : "❌ KO")}");
Console.WriteLine($"Base données : {(health.DatabaseHealthy ? "✅" : "❌")}");
Console.WriteLine($"API DGI : {(health.ApiConnectionHealthy ? "✅" : "❌")}");
Console.WriteLine($"Configuration : {(health.ConfigurationValid ? "✅" : "❌")}");

if (!health.IsHealthy)
{
    foreach (var issue in health.HealthIssues)
    {
        Console.WriteLine($"⚠️ {issue}");
    }
}
```

### Métriques de Performance
```csharp
var metrics = await certificationService.GetPerformanceMetricsAsync();

Console.WriteLine($"🎯 Taux de succès : {metrics.SuccessRate:F1}%");
Console.WriteLine($"⏱️ Temps moyen : {metrics.AverageTime.TotalSeconds:F2}s");
Console.WriteLine($"📊 Certifications today : {metrics.TotalCertificationsToday}");
Console.WriteLine($"💰 Montant certifié : {metrics.TotalAmountCertifiedToday:C}");
Console.WriteLine($"⚡ Dernière heure : {metrics.CertificationsLastHour}");
```

---

## 🔧 DÉPANNAGE ET FAQ

### ❓ Problèmes Courants

**Q: Le QR-Code n'est pas généré**
```
A: Vérifiez que le package QRCoder est installé :
   dotnet add package QRCoder
   
   Et que le token de vérification n'est pas vide.
```

**Q: L'URL de vérification est incorrecte**
```
A: Vérifiez la configuration BaseUrl dans la base :
   - Test: http://54.247.95.108/ws
   - Prod: URL fournie par la DGI après validation
```

**Q: Warning "Stock de stickers faible"**
```
A: Contactez la DGI via support.fne@dgi.gouv.ci 
   pour renouveler votre stock de stickers.
```

### 🔍 Logs de Debugging
```csharp
// Activation des logs détaillés
logger.LogInformation("Certification : {Reference}, QR: {HasQR}, URL: {Url}, Warning: {Warning}", 
    result.FneReference, 
    !string.IsNullOrEmpty(result.QrCodeData),
    result.DownloadUrl,
    result.HasWarning);
```

---

## 🌟 BONNES PRATIQUES

### ✅ Recommandations
1. **QR-Codes :** Générez-les automatiquement après chaque certification
2. **URLs :** Partagez les liens de vérification avec vos clients  
3. **Warnings :** Surveillez le stock de stickers proactivement
4. **Validation :** Auditez régulièrement vos tokens de vérification
5. **Performance :** Utilisez la certification en lot pour de gros volumes

### 🔐 Sécurité
- Les tokens de vérification sont publics (clients peuvent les vérifier)
- Les QR-codes peuvent être scannés par les clients
- Gardez votre ApiKey confidentielle
- Surveillez les tentatives de certification suspectes

---

## 🆕 ÉVOLUTIONS FUTURES

### 🚀 Roadmap Version 2.1
- [ ] **Support avoirs/annulations** selon API #2 FNE
- [ ] **Bordereau d'achat agricole** selon API #3 FNE  
- [ ] **Intégration TERNE** pour les points de vente
- [ ] **Mode hors ligne** avec synchronisation différée
- [ ] **Export PDF** avec QR-codes intégrés

### 📞 Support et Contact
- **Documentation FNE :** FNE-procedureapi.md  
- **Support DGI :** support.fne@dgi.gouv.ci
- **Issues Techniques :** GitHub Issues ou équipe de développement

---

**🎉 Le service de certification FNE version 2.0 est maintenant prêt pour un usage professionnel complet avec toutes les fonctionnalités avancées de la documentation officielle DGI !**