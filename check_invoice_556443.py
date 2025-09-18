#!/usr/bin/env python3
"""
Script pour vérifier spécifiquement la facture 556443
"""
import sqlite3

def check_specific_invoice():
    db_path = "data/FNEV4.db"
    
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Chercher la facture 556443
        cursor.execute("SELECT * FROM FneInvoices WHERE InvoiceNumber = ?", ("556443",))
        invoice = cursor.fetchone()
        
        if invoice:
            print("🎯 Facture 556443 trouvée dans FneInvoices:")
            
            # Obtenir les noms des colonnes
            cursor.execute("PRAGMA table_info(FneInvoices)")
            columns = cursor.fetchall()
            col_names = [desc[1] for desc in columns]
            
            # Créer un dictionnaire avec les données
            invoice_data = dict(zip(col_names, invoice))
            
            # Afficher les informations importantes pour la TVA
            print(f"   InvoiceNumber: {invoice_data['InvoiceNumber']}")
            print(f"   TotalAmountHT: {invoice_data['TotalAmountHT']} FCFA")
            print(f"   TotalVatAmount: {invoice_data['TotalVatAmount']} FCFA")
            print(f"   TotalAmountTTC: {invoice_data['TotalAmountTTC']} FCFA")
            print(f"   GlobalDiscount: {invoice_data['GlobalDiscount']}%")
            print(f"   Template: {invoice_data['Template']}")
            print(f"   ClientCode: {invoice_data['ClientCode']}")
            print(f"   PointOfSale: {invoice_data['PointOfSale']}")
            print(f"   Status: {invoice_data['Status']}")
            
            # Vérifier les articles de cette facture
            cursor.execute("SELECT * FROM FneInvoiceItems WHERE FneInvoiceId = ?", (invoice_data['Id'],))
            items = cursor.fetchall()
            
            if items:
                print(f"\n📦 Articles de la facture (Total: {len(items)}):")
                cursor.execute("PRAGMA table_info(FneInvoiceItems)")
                item_columns = cursor.fetchall()
                item_col_names = [desc[1] for desc in item_columns]
                
                total_ht_check = 0
                total_vat_check = 0
                
                for item in items:
                    item_data = dict(zip(item_col_names, item))
                    print(f"   - {item_data['Description']}")
                    print(f"     Quantité: {item_data['Quantity']} {item_data['MeasurementUnit']}")
                    print(f"     Prix unitaire: {item_data['UnitPrice']} FCFA")
                    print(f"     Montant HT: {item_data['LineAmountHT']} FCFA")
                    print(f"     TVA ({item_data['VatRate']}%): {item_data['LineVatAmount']} FCFA")
                    print(f"     Total TTC: {item_data['LineAmountTTC']} FCFA")
                    
                    total_ht_check += float(item_data['LineAmountHT'])
                    total_vat_check += float(item_data['LineVatAmount'])
                
                print(f"\n🔍 Vérification des totaux:")
                print(f"   Somme HT articles: {total_ht_check} FCFA")
                print(f"   Somme TVA articles: {total_vat_check} FCFA")
                print(f"   Total calculé TTC: {total_ht_check + total_vat_check} FCFA")
                print(f"   Total facture HT: {invoice_data['TotalAmountHT']} FCFA")
                print(f"   Total facture TVA: {invoice_data['TotalVatAmount']} FCFA")
                print(f"   Total facture TTC: {invoice_data['TotalAmountTTC']} FCFA")
                
                # Vérifier si les totaux correspondent
                if abs(total_ht_check - float(invoice_data['TotalAmountHT'])) < 0.01:
                    print("   ✅ Total HT cohérent")
                else:
                    print("   ❌ Incohérence dans le total HT")
                    
                if abs(total_vat_check - float(invoice_data['TotalVatAmount'])) < 0.01:
                    print("   ✅ Total TVA cohérent")
                else:
                    print("   ❌ Incohérence dans le total TVA")
                    
        else:
            print("❌ Facture 556443 non trouvée")
            
            # Chercher des factures similaires
            cursor.execute("SELECT InvoiceNumber FROM FneInvoices WHERE InvoiceNumber LIKE '%556443%' OR InvoiceNumber LIKE '%556%' LIMIT 10")
            similar = cursor.fetchall()
            if similar:
                print(f"🔍 Factures similaires trouvées: {[s[0] for s in similar]}")
        
        conn.close()
        
    except Exception as e:
        print(f"❌ Erreur: {e}")

if __name__ == "__main__":
    check_specific_invoice()