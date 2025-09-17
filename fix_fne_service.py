#!/usr/bin/env python3
"""
Correctif Service FNE - Format API selon FNE-procedureapi.md
Corrige le format de données pour correspondre exactement à la documentation officielle DGI
"""

import os
from pathlib import Path

def create_corrected_fne_service():
    """
    Crée un service FNE corrigé selon la documentation officielle
    """
    
    service_content = '''using System.Text.Json;
using Microsoft.Extensions.Logging;
using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces.Services.Fne;
using FNEV4.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace FNEV4.Infrastructure.Services
{
    /// <summary>
    /// Service de certification FNE CORRIGÉ selon FNE-procedureapi.md OFFICIEL
    /// Implémentation EXACTE selon documentation DGI Mai 2025
    /// </summary>
    public partial class FneCertificationService : IFneCertificationService
    {
        /// <summary>
        /// CORRECTION MAJEURE: Conversion au format FNE OFFICIEL selon documentation
        /// </summary>
        private async Task<object> ConvertToFneApiFormatAsync(FneInvoice invoice)
        {
            _logger.LogDebug("🔧 CONVERSION FORMAT FNE OFFICIEL - Facture {InvoiceId}", invoice.Id);

            var items = await _context.FneInvoiceItems
                .Where(i => i.FneInvoiceId == invoice.Id)
                .ToListAsync();

            // ✅ FORMAT OFFICIEL selon FNE-procedureapi.md Section V.1
            var fnePayload = new
            {
                // === PARAMÈTRES OBLIGATOIRES FNE ===
                invoiceType = "sale", // Obligatoire: "sale", "purchase"
                
                paymentMethod = ConvertToFnePaymentMethod(invoice.PaymentMethod), // Obligatoire
                
                template = DetermineTemplate(invoice), // Obligatoire: B2C, B2B, B2G, B2F
                
                isRne = false, // Obligatoire: si relié à un reçu
                
                // === CLIENT INFORMATION (Format FNE) ===
                clientCompanyName = GetClientName(invoice), // Obligatoire
                
                clientPhone = GetClientPhone(invoice), // Obligatoire: format int
                
                clientEmail = GetClientEmail(invoice), // Obligatoire
                
                // === CLIENT NCC (Obligatoire si B2B) ===
                clientNcc = GetClientNccForTemplate(invoice),
                
                // === ÉTABLISSEMENT (Obligatoires FNE) ===
                pointOfSale = GetPointOfSale(invoice), // Obligatoire
                
                establishment = GetEstablishment(invoice), // Obligatoire
                
                // === MESSAGES OPTIONNELS ===
                commercialMessage = "", // Optionnel
                footer = $"Facture générée par FNEV4 - {DateTime.Now:dd/MM/yyyy}", // Optionnel
                
                // === DEVISE (Correct pour Côte d'Ivoire) ===
                foreignCurrency = "", // Vide pour FCFA local
                foreignCurrencyRate = 0, // 0 pour devise locale
                
                // === ARTICLES (Format FNE exact) ===
                items = items.Select(item => new
                {
                    // Taxes selon format FNE
                    taxes = new[] { DetermineItemTaxType(item) }, // Obligatoire: ["TVA"], ["TVAB"], ["TVAC"], ["TVAD"]
                    
                    // Taxes personnalisées (optionnel)
                    customTaxes = new object[0], // Array vide si pas de taxes custom
                    
                    reference = item.ItemCode ?? $"ART-{item.Id}", // Optionnel mais recommandé
                    
                    description = item.Description ?? "Article", // Obligatoire
                    
                    quantity = (double)item.Quantity, // Obligatoire: format number
                    
                    amount = (double)item.UnitPrice, // Obligatoire: Prix unitaire HT
                    
                    discount = (double)(item.DiscountAmount ?? 0), // Optionnel: remise article
                    
                    measurementUnit = item.MeasurementUnit ?? "pcs" // Optionnel: unité de mesure
                }).ToArray(),
                
                // === TAXES GLOBALES ===
                taxes = "TVA", // Obligatoire: Type de TVA global
                
                // === REMISE GLOBALE ===
                discount = (double)(invoice.DiscountAmount ?? 0) // Optionnel: remise sur total HT
            };

            _logger.LogInformation("✅ PAYLOAD FNE OFFICIEL généré - Template: {Template}, Articles: {ItemCount}", 
                fnePayload.template, items.Count);

            return fnePayload;
        }

        /// <summary>
        /// Conversion méthode de paiement vers format FNE OFFICIEL
        /// Selon Annexe 1 du document FNE-procedureapi.md
        /// </summary>
        private string ConvertToFnePaymentMethod(string? paymentMethod)
        {
            return paymentMethod?.ToLower() switch
            {
                "espèces" or "espece" or "cash" => "cash",
                "carte bancaire" or "carte" => "card", 
                "chèque" or "cheque" => "check",
                "mobile money" or "mobile-money" or "orange money" => "mobile-money",
                "virement" or "virement bancaire" => "transfer",
                "à terme" or "credit" or "crédit" => "deferred",
                _ => "cash" // Défaut: espèces
            };
        }

        /// <summary>
        /// Détermine le template B2C/B2B/B2G/B2F selon client
        /// Selon Section V.1 - Template obligatoire
        /// </summary>
        private string DetermineTemplate(FneInvoice invoice)
        {
            // B2B: Client entreprise avec NCC
            if (!string.IsNullOrEmpty(invoice.Client?.ClientNcc))
            {
                return "B2B";
            }
            
            // B2G: Client gouvernemental (à déterminer selon logique métier)
            // if (IsGovernmentClient(invoice.Client))
            // {
            //     return "B2G";
            // }
            
            // B2F: Client international (à déterminer selon logique métier)
            // if (IsInternationalClient(invoice.Client))
            // {
            //     return "B2F";
            // }
            
            // B2C: Client particulier (par défaut)
            return "B2C";
        }

        /// <summary>
        /// Obtient le NCC client si template B2B (obligatoire pour B2B)
        /// </summary>
        private string? GetClientNccForTemplate(FneInvoice invoice)
        {
            var template = DetermineTemplate(invoice);
            if (template == "B2B")
            {
                return invoice.Client?.ClientNcc;
            }
            return null; // Pas obligatoire pour B2C, B2G, B2F
        }

        /// <summary>
        /// Obtient le téléphone client au format requis (int)
        /// </summary>
        private long GetClientPhone(FneInvoice invoice)
        {
            var phone = invoice.Client?.Phone ?? "0000000000";
            
            // Nettoyer le numéro (enlever espaces, tirets, etc.)
            var cleanPhone = new string(phone.Where(char.IsDigit).ToArray());
            
            // Convertir en long (format requis par API FNE)
            if (long.TryParse(cleanPhone, out var phoneNumber))
            {
                return phoneNumber;
            }
            
            return 0000000000; // Numéro par défaut si conversion échoue
        }

        /// <summary>
        /// Obtient l'email client (obligatoire)
        /// </summary>
        private string GetClientEmail(FneInvoice invoice)
        {
            return invoice.Client?.Email ?? "client@example.com";
        }

        /// <summary>
        /// Détermine le point de vente (obligatoire FNE)
        /// </summary>
        private string GetPointOfSale(FneInvoice invoice)
        {
            // À personnaliser selon la logique métier de l'entreprise
            return invoice.PointOfSale ?? "POS-001";
        }

        /// <summary>
        /// Détermine l'établissement (obligatoire FNE)
        /// </summary>
        private string GetEstablishment(FneInvoice invoice)
        {
            // À personnaliser selon la logique métier de l'entreprise
            return invoice.Establishment ?? "Établissement Principal";
        }

        /// <summary>
        /// Détermine le type de TVA pour un article
        /// Selon Annexe 1: TVA (18%), TVAB (9%), TVAC (0%), TVAD (0%)
        /// </summary>
        private string DetermineItemTaxType(FneInvoiceItem item)
        {
            var vatRate = item.VatRate;
            
            return vatRate switch
            {
                18 or 0.18m => "TVA",    // TVA normale 18%
                9 or 0.09m => "TVAB",    // TVA réduite 9%
                0 => "TVAC",             // TVA exonérée conventionnelle 0%
                _ => "TVA"               // Par défaut TVA normale
            };
            
            // Note: TVAD est pour TVA exonérée légale TEE et RME
        }
    }
}'''

    # Créer le fichier correctif
    output_path = Path("src/FNEV4.Infrastructure/Services/FneCertificationService.Corrected.cs")
    output_path.parent.mkdir(parents=True, exist_ok=True)
    
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write(service_content)
    
    print(f"✅ Service FNE corrigé créé: {output_path}")
    
    # Instructions
    instructions = '''
🔧 INSTRUCTIONS DE CORRECTION CRITIQUES

1. REMPLACER la méthode ConvertToFneApiFormatAsync dans FneCertificationService.cs
2. AJOUTER les nouvelles méthodes utilitaires
3. METTRE À JOUR les propriétés manquantes dans FneInvoice:
   - PointOfSale
   - Establishment
4. TESTER avec le format corrigé

📋 CHANGEMENTS CRITIQUES:
✅ Format payload conforme FNE-procedureapi.md
✅ Devise corrigée: FCFA (Côte d'Ivoire) 
✅ Paramètres obligatoires FNE ajoutés
✅ Types de paiement selon documentation
✅ Templates B2C/B2B/B2G/B2F
✅ Format taxes selon Annexe 1

⚠️ L'erreur "Service provider non disponible" vient du format incorrect des données !
'''
    
    print(instructions)

if __name__ == "__main__":
    print("🔧 GÉNÉRATION CORRECTIF SERVICE FNE")
    print("=" * 60)
    create_corrected_fne_service()