// EXEMPLE D'IMPLÉMENTATION COMPLÈTE pour ConvertToFneInvoiceAsync
// À ajouter dans Sage100ImportService.cs

/// <summary>
/// Convertit les données Sage 100 en entité FneInvoice pour sauvegarde en base
/// IMPLÉMENTATION COMPLÈTE avec les bonnes entités
/// </summary>
private async Task<FneInvoice?> ConvertToFneInvoiceAsync(Sage100FactureData factureData, string sourceSheet)
{
    try
    {
        // 1. Créer une session d'import pour traçabilité
        var importSession = new ImportSession
        {
            NomFichier = sourceSheet,
            DateImport = DateTime.Now,
            TypeImport = "Sage100",
            NbLignesTraitees = 1,
            NbLignesValides = 1,
            NbLignesErreur = 0,
            UtilisateurImport = Environment.UserName,
            Statut = "Termine"
        };

        await _context.ImportSessions.AddAsync(importSession);
        await _context.SaveChangesAsync(); // Pour obtenir l'ID

        // 2. Récupérer ou créer le client
        var clientId = await GetOrCreateClientAsync(factureData.CodeClient, factureData.IntituleClient);

        // 3. Calculer les montants de TVA
        var totalHT = factureData.Produits.Sum(p => p.MontantHt);
        var totalTVA = factureData.Produits.Sum(p => 
        {
            var vatRate = GetVatRateFromCode(p.CodeTva);
            return p.MontantHt * (vatRate / 100);
        });
        var totalTTC = totalHT + totalTVA;

        // 4. Créer l'entité FneInvoice
        var fneInvoice = new FneInvoice
        {
            // === INFORMATIONS OBLIGATOIRES ===
            InvoiceNumber = factureData.NumeroFacture,
            InvoiceDate = factureData.DateFacture,
            InvoiceType = "sale", // Type par défaut
            
            // === CLIENT ===
            ClientId = clientId,
            ClientCode = factureData.CodeClient,
            
            // === POINT DE VENTE ===
            PointOfSale = string.IsNullOrEmpty(factureData.PointDeVente) ? "01" : factureData.PointDeVente,
            
            // === MONTANTS ===
            TotalAmountHT = totalHT,
            TotalVatAmount = totalTVA,
            TotalAmountTTC = totalTTC,
            
            // === PAIEMENT ===
            PaymentMethod = MapPaymentMethod(factureData.MoyenPaiement),
            
            // === TEMPLATE BASÉ SUR LE TYPE DE CLIENT ===
            Template = factureData.EstClientDivers ? "B2C" : "B2B",
            
            // === STATUT ===
            Status = "Draft", // Brouillon, sera certifiée plus tard
            
            // === MÉTADONNÉES ===
            ImportSessionId = importSession.Id,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // 5. Ajouter les lignes de facture
        foreach (var produit in factureData.Produits)
        {
            var vatRate = GetVatRateFromCode(produit.CodeTva);
            var vatAmount = produit.MontantHt * (vatRate / 100);
            
            var item = new FneInvoiceItem
            {
                // === NUMÉROTATION ===
                ItemNumber = produit.NumeroLigne,
                
                // === PRODUIT ===
                ProductCode = produit.CodeProduit,
                Description = produit.Designation,
                
                // === QUANTITÉS ET PRIX ===
                Quantity = produit.Quantite,
                UnitPrice = produit.PrixUnitaire,
                
                // === MONTANTS ===
                TotalAmountHT = produit.MontantHt,
                VatRate = vatRate,
                VatAmount = vatAmount,
                TotalAmountTTC = produit.MontantHt + vatAmount,
                
                // === MÉTADONNÉES ===
                CreatedAt = DateTime.Now
            };
            
            fneInvoice.Items.Add(item);
        }

        return fneInvoice;
    }
    catch (Exception ex)
    {
        await _loggingService.LogErrorAsync(
            $"Erreur conversion facture {factureData.NumeroFacture}: {ex.Message}", 
            "Sage100Import", 
            ex);
        return null;
    }
}

/// <summary>
/// Récupère ou crée un client basé sur le code client Sage
/// </summary>
private async Task<Guid> GetOrCreateClientAsync(string codeClient, string nomClient)
{
    try
    {
        // 1. Chercher le client existant par NCC
        var existingClient = await _context.Clients
            .FirstOrDefaultAsync(c => c.Ncc == codeClient || c.CompanyName == nomClient);
        
        if (existingClient != null)
        {
            return existingClient.Id;
        }

        // 2. Si code client = "1999", gérer le client divers
        if (codeClient == "1999")
        {
            var clientDivers = await _context.Clients
                .FirstOrDefaultAsync(c => c.Ncc == "1999" || c.CompanyName.Contains("Divers"));
            
            if (clientDivers != null)
            {
                return clientDivers.Id;
            }
            
            // Créer le client divers s'il n'existe pas
            var newClientDivers = new Client
            {
                Ncc = "1999",
                CompanyName = "Clients Divers",
                ClientType = "Divers",
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _context.Clients.AddAsync(newClientDivers);
            await _context.SaveChangesAsync();
            
            return newClientDivers.Id;
        }

        // 3. Créer un nouveau client standard
        var newClient = new Client
        {
            Ncc = codeClient,
            CompanyName = nomClient,
            ClientType = "Entreprise",
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        await _context.Clients.AddAsync(newClient);
        await _context.SaveChangesAsync();
        
        return newClient.Id;
    }
    catch (Exception ex)
    {
        await _loggingService.LogErrorAsync(
            $"Erreur création client {codeClient}: {ex.Message}", 
            "Sage100Import", 
            ex);
        
        // En cas d'erreur, créer un client temporaire
        // En production, il faudrait avoir un client par défaut fixe
        var tempClient = new Client
        {
            Ncc = $"TEMP_{DateTime.Now:yyyyMMddHHmmss}",
            CompanyName = $"Client Temporaire - {nomClient}",
            ClientType = "Temporaire",
            IsActive = false,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        
        await _context.Clients.AddAsync(tempClient);
        await _context.SaveChangesAsync();
        
        return tempClient.Id;
    }
}

/// <summary>
/// Mappe les moyens de paiement Sage vers les codes DGI
/// </summary>
private string MapPaymentMethod(string moyenPaiementSage)
{
    if (string.IsNullOrEmpty(moyenPaiementSage))
        return "cash"; // Défaut espèces
        
    return moyenPaiementSage.ToUpper() switch
    {
        "ESPECES" or "CASH" or "LIQUIDE" => "cash",
        "CHEQUE" or "CHQ" => "check", 
        "VIREMENT" or "VIR" => "transfer",
        "CARTE" or "CB" or "CARD" => "card",
        "CREDIT" => "credit",
        "MOBILE" => "mobile-money",
        _ => "cash" // Espèces par défaut
    };
}

/// <summary>
/// Convertit le code TVA Sage en taux numérique
/// </summary>
private decimal GetVatRateFromCode(string codeTva)
{
    if (string.IsNullOrEmpty(codeTva))
        return 18.0m; // TVA normale par défaut
        
    return codeTva.ToUpper() switch
    {
        "TVA" or "18" or "NORMALE" => 18.0m,   // TVA normale 18%
        "TVAB" or "9" or "REDUITE" => 9.0m,    // TVA réduite 9%
        "TVAC" or "0" or "EXONEREE" => 0.0m,   // Exonération conventionnelle
        "TVAD" or "LEGALE" => 0.0m,            // Exonération légale
        _ => 18.0m                             // TVA par défaut
    };
}

// NOTES D'IMPLÉMENTATION:
// 1. Vérifier que les types Guid/int sont cohérents avec Client.Id
// 2. Adapter les noms de propriétés selon les vraies entités
// 3. Ajouter la gestion des erreurs et rollback
// 4. Implémenter la validation des données avant sauvegarde
// 5. Ajouter des logs détaillés pour le debugging
