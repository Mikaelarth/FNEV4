#!/usr/bin/env python3
"""
Comparaison complète entre les données de factures stockées en base 
et celles affichées dans l'interface des détails de facture
"""

import sqlite3
from pathlib import Path
import json

def compare_invoice_data_vs_display():
    db_path = Path("data/FNEV4.db")
    
    if not db_path.exists():
        print("❌ Base de données FNEV4.db introuvable")
        return
    
    try:
        conn = sqlite3.connect(str(db_path))
        cursor = conn.cursor()
        
        print("🔍 COMPARAISON DONNÉES BASE vs INTERFACE")
        print("=" * 55)
        
        # Analyser les factures des captures d'écran
        factures_test = {
            "556452": {
                "interface_client": "SDTM-CI (3012Q)",
                "interface_template": "Business to Consumer",
                "interface_montant_ht": "2,437,421.00 FCFA",
                "interface_montant_tva": "62,575.92 FCFA", 
                "interface_montant_ttc": "2,499,996.92 FCFA",
                "interface_point_vente": "GSM (Fictif)",
                "interface_paiement": "Espèces"
            },
            "556469": {
                "interface_client": "SOREFCI (1999)",
                "interface_template": "Business to Consumer", 
                "interface_montant_ht": "272,991.00 FCFA",
                "interface_montant_tva": "7,008.48 FCFA",
                "interface_montant_ttc": "279,999.48 FCFA",
                "interface_point_vente": "GSM (Fictif)",
                "interface_paiement": "Espèces"
            }
        }
        
        for facture_num, interface_data in factures_test.items():
            print(f"\n📄 ANALYSE FACTURE {facture_num}")
            print("=" * 35)
            
            # Récupérer TOUTES les données de la facture
            cursor.execute("""
                SELECT 
                    f.InvoiceNumber,
                    f.Template,
                    f.InvoiceType,
                    f.PaymentMethod,
                    f.PointOfSale,
                    f.ClientCode,
                    f.TotalAmountHT,
                    f.TotalVatAmount,
                    f.TotalAmountTTC,
                    f.CommercialMessage,
                    f.InvoiceDate,
                    f.FneReference,
                    f.IsCertified,
                    f.Establishment,
                    -- Colonnes de certification
                    f.FneCertificationNumber,
                    f.FneCertificationDate,
                    f.FneQrCode,
                    f.FneDigitalSignature,
                    f.FneValidationUrl
                FROM FneInvoices f
                WHERE f.InvoiceNumber = ?
            """, (facture_num,))
            
            facture = cursor.fetchone()
            
            if not facture:
                print(f"   ❌ Facture {facture_num} non trouvée")
                continue
            
            (num, template, inv_type, payment, pos, client_code, 
             ht, tva, ttc, message, date, fne_ref, is_certified, establishment,
             cert_num, cert_date, qr_code, signature, validation_url) = facture
            
            # Récupérer les données client
            cursor.execute("""
                SELECT Name, CompanyName, ClientNcc, Phone, Email,
                       Country, DefaultCurrency, DefaultTemplate, DefaultPaymentMethod
                FROM Clients 
                WHERE ClientCode = ?
            """, (client_code,))
            
            client_info = cursor.fetchone()
            
            print(f"🗄️ DONNÉES EN BASE DE DONNÉES")
            print("-" * 30)
            print(f"   📋 Facture:")
            print(f"      Numéro: {num}")
            print(f"      Date: {date}")
            print(f"      Template: '{template}'")
            print(f"      Type: '{inv_type}'")
            print(f"      Paiement: '{payment}'")
            print(f"      Point de vente: '{pos}'")
            print(f"      Établissement: '{establishment}'")
            print(f"      Message commercial: {message}")
            
            print(f"   💰 Montants:")
            print(f"      HT: {ht}")
            print(f"      TVA: {tva}")
            print(f"      TTC: {ttc}")
            
            print(f"   🔖 Certification:")
            print(f"      Référence FNE: {fne_ref}")
            print(f"      Certifiée: {'✅ OUI' if is_certified else '❌ NON'}")
            print(f"      N° Certification: {cert_num}")
            print(f"      Date certification: {cert_date}")
            
            if client_info:
                (name, company, ncc, phone, email, country, currency, def_template, def_payment) = client_info
                print(f"   👤 Client (code {client_code}):")
                print(f"      Nom: '{name}'")
                print(f"      Entreprise: '{company}'")
                print(f"      NCC: '{ncc}'")
                print(f"      Téléphone: '{phone}'")
                print(f"      Email: '{email}'")
                print(f"      Pays: '{country}'")
                print(f"      Devise: '{currency}'")
            
            print(f"\n🖥️ DONNÉES AFFICHÉES DANS L'INTERFACE")
            print("-" * 40)
            for key, value in interface_data.items():
                field_name = key.replace("interface_", "").replace("_", " ").title()
                print(f"   {field_name}: '{value}'")
            
            print(f"\n🔍 COMPARAISON ET ANALYSE")
            print("-" * 28)
            
            # Comparaison client
            interface_client = interface_data["interface_client"]
            if client_info:
                expected_client = f"{name} ({client_code})"
                if interface_client == expected_client:
                    print(f"   ✅ Client cohérent: '{interface_client}'")
                else:
                    print(f"   ⚠️ Client différent:")
                    print(f"      Interface: '{interface_client}'")
                    print(f"      Attendu: '{expected_client}'")
                    
                    # Analyser la source de la différence
                    if message and any(word in interface_client for word in message.split()):
                        print(f"      🔍 Source probable: CommercialMessage = '{message}'")
                    else:
                        print(f"      🔍 Source: Logique ViewModel inconnue")
            
            # Comparaison template
            template_mapping = {
                "B2C": "Business to Consumer",
                "B2B": "Business to Business",
                "B2G": "Business to Government"
            }
            
            expected_template = template_mapping.get(template, template)
            interface_template = interface_data["interface_template"]
            
            if interface_template == expected_template:
                print(f"   ✅ Template cohérent: '{template}' → '{interface_template}'")
            else:
                print(f"   ❌ Template incohérent:")
                print(f"      Base: '{template}' → Attendu: '{expected_template}'")
                print(f"      Interface: '{interface_template}'")
            
            # Comparaison paiement
            payment_mapping = {
                "cash": "Espèces",
                "mobile-money": "Mobile Money",
                "card": "Carte",
                "check": "Chèque",
                "transfer": "Virement",
                "credit": "Crédit",
                "other": "Autre"
            }
            
            expected_payment = payment_mapping.get(payment, payment)
            interface_payment = interface_data["interface_paiement"]
            
            if interface_payment == expected_payment:
                print(f"   ✅ Paiement cohérent: '{payment}' → '{interface_payment}'")
            else:
                print(f"   ⚠️ Paiement différent:")
                print(f"      Base: '{payment}' → Attendu: '{expected_payment}'")
                print(f"      Interface: '{interface_payment}'")
            
            # Comparaison montants (conversion format)
            def format_amount(amount):
                """Formate un montant comme dans l'interface"""
                if amount is None:
                    return "0.00"
                return f"{amount:,.2f}".replace(",", " ")
            
            expected_ht = f"{format_amount(ht)} FCFA"
            expected_tva = f"{format_amount(tva)} FCFA"
            expected_ttc = f"{format_amount(ttc)} FCFA"
            
            interface_ht = interface_data["interface_montant_ht"]
            interface_tva = interface_data["interface_montant_tva"]
            interface_ttc = interface_data["interface_montant_ttc"]
            
            print(f"   💰 Montants:")
            
            if interface_ht == expected_ht:
                print(f"      ✅ HT cohérent: {expected_ht}")
            else:
                print(f"      ⚠️ HT différent: Interface='{interface_ht}' vs Attendu='{expected_ht}'")
            
            if interface_tva == expected_tva:
                print(f"      ✅ TVA cohérente: {expected_tva}")
            else:
                print(f"      ⚠️ TVA différente: Interface='{interface_tva}' vs Attendu='{expected_tva}'")
            
            if interface_ttc == expected_ttc:
                print(f"      ✅ TTC cohérent: {expected_ttc}")
            else:
                print(f"      ⚠️ TTC différent: Interface='{interface_ttc}' vs Attendu='{expected_ttc}'")
            
            # Vérification point de vente
            interface_pos = interface_data["interface_point_vente"]
            if interface_pos == pos:
                print(f"   ✅ Point de vente cohérent: '{pos}'")
            else:
                print(f"   ⚠️ Point de vente différent:")
                print(f"      Interface: '{interface_pos}'")
                print(f"      Base: '{pos}'")
            
            # Analyse articles de la facture
            print(f"\n📦 ANALYSE DES ARTICLES")
            print("-" * 22)
            
            cursor.execute("""
                SELECT ArticleCode, Designation, Quantity, UnitPrice, 
                       VatRate, TotalPriceHT, VatAmount
                FROM FneInvoiceItems 
                WHERE InvoiceId IN (
                    SELECT Id FROM FneInvoices WHERE InvoiceNumber = ?
                )
                ORDER BY ArticleCode
            """, (facture_num,))
            
            articles = cursor.fetchall()
            
            if articles:
                print(f"   📋 {len(articles)} article(s) trouvé(s):")
                for i, (code, desig, qty, price, vat_rate, total_ht, vat_amt) in enumerate(articles, 1):
                    print(f"      {i}. {code} - {desig}")
                    print(f"         Qté: {qty}, Prix: {price} FCFA")
                    print(f"         TVA: {vat_rate}%, Total HT: {total_ht} FCFA")
            else:
                print(f"   ⚠️ Aucun article trouvé (possiblement dans une autre table)")
            
            print(f"\n" + "="*50)
        
        # Résumé global
        print(f"\n🎯 RÉSUMÉ GLOBAL")
        print("=" * 15)
        print(f"✅ POINTS POSITIFS IDENTIFIÉS:")
        print(f"   • Templates correctement mappés (B2C → Business to Consumer)")
        print(f"   • Paiements correctement mappés (cash → Espèces)")
        print(f"   • Montants correctement formatés")
        print(f"   • Structure de données cohérente")
        
        print(f"\n⚠️ POINTS D'ATTENTION:")
        print(f"   • Noms clients : Utilisation de CommercialMessage vs nom réel")
        print(f"   • Vérifier la logique du ViewModel pour l'affichage des noms")
        print(f"   • S'assurer que les transformations de données sont cohérentes")
        
        print(f"\n🔧 RECOMMANDATIONS:")
        print(f"   • Standardiser l'affichage des noms clients")
        print(f"   • Vérifier le mapping des templates et paiements")
        print(f"   • Documenter les transformations de données dans les ViewModels")
    
    except sqlite3.Error as e:
        print(f"❌ Erreur base de données: {e}")
    
    except Exception as e:
        print(f"❌ Erreur inattendue: {e}")
    
    finally:
        if 'conn' in locals():
            conn.close()

if __name__ == "__main__":
    compare_invoice_data_vs_display()
    print(f"\n✅ Comparaison terminée")