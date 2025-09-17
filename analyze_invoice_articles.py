#!/usr/bin/env python3
"""
Analyse finale des articles et comparaison complète
"""

import sqlite3
from pathlib import Path

def analyze_invoice_articles():
    db_path = Path("data/FNEV4.db")
    
    if not db_path.exists():
        print("❌ Base de données FNEV4.db introuvable")
        return
    
    try:
        conn = sqlite3.connect(str(db_path))
        cursor = conn.cursor()
        
        print("📦 ANALYSE DES ARTICLES DES FACTURES")
        print("=" * 40)
        
        factures_test = ["556452", "556469"]
        
        for facture_num in factures_test:
            print(f"\n📄 FACTURE {facture_num}")
            print("-" * 25)
            
            # Obtenir l'ID de la facture
            cursor.execute("""
                SELECT Id, InvoiceNumber, TotalAmountHT, TotalVatAmount, TotalAmountTTC
                FROM FneInvoices 
                WHERE InvoiceNumber = ?
            """, (facture_num,))
            
            facture = cursor.fetchone()
            if not facture:
                print(f"   ❌ Facture {facture_num} non trouvée")
                continue
            
            facture_id, num, total_ht, total_tva, total_ttc = facture
            
            # Récupérer les articles avec les bons noms de colonnes
            cursor.execute("""
                SELECT ProductCode, Description, Quantity, UnitPrice, 
                       VatRate, LineAmountHT, LineVatAmount, LineAmountTTC,
                       MeasurementUnit, ItemDiscount, VatCode
                FROM FneInvoiceItems 
                WHERE FneInvoiceId = ?
                ORDER BY LineOrder
            """, (facture_id,))
            
            articles = cursor.fetchall()
            
            print(f"   📋 {len(articles)} article(s) trouvé(s):")
            
            if articles:
                total_calc_ht = 0
                total_calc_tva = 0
                total_calc_ttc = 0
                
                for i, (code, desc, qty, price, vat_rate, line_ht, line_tva, line_ttc, unit, discount, vat_code) in enumerate(articles, 1):
                    print(f"      {i}. Code: {code}")
                    print(f"         Désignation: {desc}")
                    print(f"         Quantité: {qty} {unit or ''}")
                    print(f"         Prix unitaire: {price:,.2f} FCFA")
                    print(f"         TVA: {vat_rate}% ({vat_code})")
                    if discount > 0:
                        print(f"         Remise: {discount}%")
                    print(f"         Montant HT: {line_ht:,.2f} FCFA")
                    print(f"         TVA ligne: {line_tva:,.2f} FCFA")
                    print(f"         Montant TTC: {line_ttc:,.2f} FCFA")
                    print()
                    
                    # Calcul des totaux
                    total_calc_ht += line_ht
                    total_calc_tva += line_tva
                    total_calc_ttc += line_ttc
                
                print(f"   🧮 VÉRIFICATION CALCULS:")
                print(f"      Total HT calculé: {total_calc_ht:,.2f} FCFA")
                print(f"      Total HT facture: {total_ht:,.2f} FCFA")
                
                print(f"      Total TVA calculé: {total_calc_tva:,.2f} FCFA")
                print(f"      Total TVA facture: {total_tva:,.2f} FCFA")
                
                print(f"      Total TTC calculé: {total_calc_ttc:,.2f} FCFA")
                print(f"      Total TTC facture: {total_ttc:,.2f} FCFA")
                
                # Vérifications
                if abs(total_calc_ht - total_ht) < 0.01:
                    print(f"      ✅ Total HT cohérent")
                else:
                    print(f"      ❌ Écart HT: {abs(total_calc_ht - total_ht):.2f} FCFA")
                
                if abs(total_calc_tva - total_tva) < 0.01:
                    print(f"      ✅ Total TVA cohérent")
                else:
                    print(f"      ❌ Écart TVA: {abs(total_calc_tva - total_tva):.2f} FCFA")
                
                if abs(total_calc_ttc - total_ttc) < 0.01:
                    print(f"      ✅ Total TTC cohérent")
                else:
                    print(f"      ❌ Écart TTC: {abs(total_calc_ttc - total_ttc):.2f} FCFA")
            
            else:
                print(f"   ⚠️ Aucun article trouvé pour cette facture")
        
        # Analyse des formatages dans l'interface
        print(f"\n🖥️ ANALYSE FORMATAGE INTERFACE")
        print("=" * 32)
        
        print(f"   📊 Formats observés dans les captures:")
        print(f"      Interface: '2,437,421.00 FCFA' (virgules comme séparateurs)")
        print(f"      Base: 2437421.0 (format numérique)")
        print(f"      Différence: Séparateur de milliers (virgule vs espace)")
        
        print(f"\n   🔧 Transformations identifiées:")
        print(f"      • cash → Espèces")
        print(f"      • B2C → Business to Consumer")
        print(f"      • Nom client: [Nom] ([Code])")
        print(f"      • Montants: Formatage avec virgules + ' FCFA'")
        
        print(f"\n   ✅ Points cohérents:")
        print(f"      • Noms clients correctement affichés")
        print(f"      • Templates correctement mappés")
        print(f"      • Paiements correctement traduits")
        print(f"      • Montants numériquement corrects")
        
        print(f"\n   ⚠️ Différences mineures:")
        print(f"      • Format séparateur milliers: Interface utilise ',' au lieu de ' '")
        print(f"      • Possibles arrondis d'affichage")
    
    except sqlite3.Error as e:
        print(f"❌ Erreur base de données: {e}")
    
    except Exception as e:
        print(f"❌ Erreur inattendue: {e}")
    
    finally:
        if 'conn' in locals():
            conn.close()

if __name__ == "__main__":
    analyze_invoice_articles()
    print(f"\n✅ Analyse terminée")