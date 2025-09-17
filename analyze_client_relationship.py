#!/usr/bin/env python3
"""
Script pour analyser la relation Client dans les factures FNE
"""

import sqlite3
from pathlib import Path

def analyze_client_relationship():
    """
    Analyse la relation entre FneInvoices et Clients
    """
    
    db_path = Path("data/FNEV4.db")
    if not db_path.exists():
        print("❌ Base de données introuvable")
        return
    
    print("🔍 ANALYSE RELATION CLIENT-FACTURE")
    print("=" * 50)
    
    try:
        conn = sqlite3.connect(str(db_path))
        cursor = conn.cursor()
        
        # Analyser les factures 556452 et 556469 spécifiquement
        target_invoices = ['556452', '556469']
        
        for invoice_num in target_invoices:
            print(f"\n📄 FACTURE {invoice_num}")
            print("-" * 30)
            
            # Données de la facture
            cursor.execute("""
                SELECT 
                    f.InvoiceNumber,
                    f.ClientId,
                    f.ClientCode,
                    f.CommercialMessage,
                    f.PointOfSale,
                    f.PaymentMethod,
                    f.Template,
                    f.TotalAmountTTC
                FROM FneInvoices f
                WHERE f.InvoiceNumber = ?
            """, (invoice_num,))
            
            facture_row = cursor.fetchone()
            if not facture_row:
                print(f"❌ Facture {invoice_num} introuvable")
                continue
            
            (inv_num, client_id, client_code, commercial_msg, 
             point_sale, payment, template, amount) = facture_row
            
            print(f"   InvoiceNumber: {inv_num}")
            print(f"   ClientId: {client_id}")
            print(f"   ClientCode: {client_code}")
            print(f"   CommercialMessage: {commercial_msg}")
            print(f"   PointOfSale: {point_sale}")
            print(f"   PaymentMethod: {payment}")
            print(f"   Template: {template}")
            print(f"   Montant TTC: {amount:,.2f} FCFA")
            
            # Recherche du client correspondant
            cursor.execute("""
                SELECT 
                    c.Id,
                    c.ClientCode,
                    c.Name,
                    c.CompanyName,
                    c.ClientNcc,
                    c.Phone,
                    c.Email,
                    c.Country,
                    c.DefaultCurrency,
                    c.DefaultTemplate,
                    c.DefaultPaymentMethod
                FROM Clients c
                WHERE c.ClientCode = ? OR c.Id = ?
            """, (client_code, client_id))
            
            client_row = cursor.fetchone()
            
            if client_row:
                (c_id, c_code, c_name, c_company, c_ncc, 
                 c_phone, c_email, c_country, c_currency, c_template, c_payment) = client_row
                
                print(f"\n   ✅ CLIENT TROUVÉ:")
                print(f"      Id: {c_id}")
                print(f"      Code: {c_code}")
                print(f"      Nom: {c_name}")
                print(f"      Entreprise: {c_company}")
                print(f"      NCC: {c_ncc}")
                print(f"      Téléphone: {c_phone}")
                print(f"      Email: {c_email}")
                print(f"      Pays: {c_country}")
                print(f"      Devise: {c_currency}")
                print(f"      Template: {c_template}")
                print(f"      Paiement: {c_payment}")
                
                # Proposer le nom correct à utiliser
                client_display_name = c_company if c_company else c_name
                print(f"\n   💡 NOM À UTILISER: {client_display_name}")
                
                # Extraire le nom depuis CommercialMessage si nécessaire
                if commercial_msg and "Client:" in commercial_msg:
                    extracted_name = commercial_msg.replace("Client:", "").strip()
                    print(f"   📝 Nom extrait du message: {extracted_name}")
                    
                    if extracted_name != client_display_name:
                        print(f"   ⚠️ INCOHÉRENCE détectée !")
                        print(f"      Base de données: {client_display_name}")
                        print(f"      Message commercial: {extracted_name}")
            else:
                print(f"\n   ❌ CLIENT INTROUVABLE")
                print(f"      ClientCode: {client_code}")
                print(f"      ClientId: {client_id}")
                
                # Tenter une recherche approximative
                cursor.execute("""
                    SELECT ClientCode, Name, CompanyName
                    FROM Clients
                    WHERE ClientCode LIKE ? OR Name LIKE ?
                    LIMIT 5
                """, (f"%{client_code}%", f"%{client_code}%"))
                
                similar_clients = cursor.fetchall()
                if similar_clients:
                    print(f"   🔍 CLIENTS SIMILAIRES:")
                    for sc_code, sc_name, sc_company in similar_clients:
                        display = sc_company if sc_company else sc_name
                        print(f"      {sc_code}: {display}")
            
            # Analyser les articles
            cursor.execute("""
                SELECT 
                    ProductCode,
                    Description,
                    Quantity,
                    UnitPrice,
                    LineAmountTTC,
                    VatCode,
                    VatRate
                FROM FneInvoiceItems
                WHERE FneInvoiceId = (
                    SELECT Id FROM FneInvoices WHERE InvoiceNumber = ?
                )
                ORDER BY LineOrder
            """, (invoice_num,))
            
            items = cursor.fetchall()
            print(f"\n   📦 ARTICLES ({len(items)}):")
            for item in items:
                product_code, desc, qty, price, total, vat_code, vat_rate = item
                print(f"      {product_code}: {desc}")
                print(f"         Qté: {qty}, Prix: {price:,.2f}, Total: {total:,.2f} FCFA")
                print(f"         TVA: {vat_code} ({vat_rate}%)")
        
        # Solution proposée
        print(f"\n" + "=" * 50)
        print("💡 SOLUTION POUR CORRIGER LE VIEWMODEL")
        print("=" * 50)
        
        print("""
Le problème est dans FactureFneDetailsViewModel.cs ligne 58:

❌ ACTUEL (incorrect):
public string FactureSubtitle => $"Client: {{Facture?.ClientDisplayName}} ({{Facture?.ClientCode}})";

✅ SOLUTION (utiliser relation avec table Clients):
1. Charger les données client via ClientId ou ClientCode
2. Utiliser Client?.CompanyName ou Client?.Name
3. Fallback sur CommercialMessage si besoin

Exemple de correction:
- Ajouter une propriété Client au ViewModel
- Charger le client depuis la base via navigation property
- Utiliser Client?.CompanyName ?? Client?.Name ?? "Client inconnu"
        """)
    
    except Exception as e:
        print(f"❌ Erreur: {e}")
    
    finally:
        if 'conn' in locals():
            conn.close()

if __name__ == "__main__":
    analyze_client_relationship()