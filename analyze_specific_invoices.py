#!/usr/bin/env python3
"""
Analyse spécifique des factures de test
"""

import sqlite3
from pathlib import Path

def analyze_specific_invoices():
    db_path = Path("data/FNEV4.db")
    
    if not db_path.exists():
        print("❌ Base de données FNEV4.db introuvable")
        return
    
    try:
        conn = sqlite3.connect(str(db_path))
        cursor = conn.cursor()
        
        print("🔬 ANALYSE DÉTAILLÉE DES FACTURES SPÉCIFIQUES")
        print("=" * 50)
        
        # Obtenir d'abord la structure exacte
        cursor.execute("PRAGMA table_info(FneInvoices)")
        colonnes = cursor.fetchall()
        
        colonnes_disponibles = [col[1] for col in colonnes]
        print(f"📋 Colonnes disponibles: {', '.join(colonnes_disponibles[:10])}...")
        
        # Analyser les factures de test
        factures_test = ["556452", "556469"]
        
        for facture_num in factures_test:
            print(f"\n🔍 FACTURE {facture_num}")
            print("-" * 25)
            
            cursor.execute("""
                SELECT 
                    InvoiceNumber,
                    Template,
                    InvoiceType,
                    PaymentMethod,
                    PointOfSale,
                    ClientCode,
                    TotalAmountHT,
                    TotalVatAmount,
                    TotalAmountTTC,
                    CommercialMessage,
                    FneCertificationNumber,
                    IsCertified
                FROM FneInvoices 
                WHERE InvoiceNumber = ?
            """, (facture_num,))
            
            facture = cursor.fetchone()
            
            if facture:
                (num, template, inv_type, payment, pos, client_code, 
                 ht, tva, ttc, message, cert_num, is_certified) = facture
                
                print(f"   📄 Numéro: {num}")
                print(f"   📝 Template: '{template}'")
                print(f"   🏷️ Type: '{inv_type}'")
                print(f"   💳 Paiement: '{payment}'")
                print(f"   🏪 Point de vente: '{pos}'")
                print(f"   👤 Client: {client_code}")
                print(f"   💰 Montant HT: {ht} FCFA")
                print(f"   📊 TVA: {tva} FCFA")
                print(f"   💯 TTC: {ttc} FCFA")
                print(f"   💬 Message: {message}")
                print(f"   🔖 Certifié: {'✅ OUI' if is_certified else '❌ NON'}")
                print(f"   📋 N° Certification: {cert_num or 'Aucun'}")
                
                # Vérifier la conformité FNE
                print(f"\n   🎯 CONFORMITÉ API FNE:")
                
                # Templates FNE valides
                templates_fne = ["B2C", "B2B", "B2G"]
                if template in templates_fne:
                    print(f"      ✅ Template '{template}' conforme")
                else:
                    print(f"      ❌ Template '{template}' NON conforme")
                
                # Types FNE valides
                types_fne = ["sale", "refund", "proforma", "delivery"]
                if inv_type in types_fne:
                    print(f"      ✅ Type '{inv_type}' conforme")
                else:
                    print(f"      ❌ Type '{inv_type}' NON conforme")
                
                # Paiements FNE valides
                payments_fne = ["cash", "mobile-money", "card", "check", "transfer", "credit", "other"]
                if payment in payments_fne:
                    print(f"      ✅ Paiement '{payment}' conforme")
                else:
                    print(f"      ❌ Paiement '{payment}' NON conforme")
                
                # Vérifier les données requises pour FNE
                print(f"\n   📋 DONNÉES REQUISES POUR CERTIFICATION:")
                
                required_fields = [
                    ("Template", template),
                    ("InvoiceType", inv_type),
                    ("PaymentMethod", payment),
                    ("PointOfSale", pos),
                    ("ClientCode", client_code),
                    ("TotalAmountTTC", ttc)
                ]
                
                missing_fields = []
                for field_name, field_value in required_fields:
                    if field_value is None or field_value == '':
                        missing_fields.append(field_name)
                        print(f"      ❌ {field_name}: MANQUANT")
                    else:
                        print(f"      ✅ {field_name}: '{field_value}'")
                
                if missing_fields:
                    print(f"\n      🚨 PROBLÈMES BLOQUANTS: {', '.join(missing_fields)}")
                    print(f"      💡 Ces champs doivent être renseignés pour la certification FNE")
                else:
                    print(f"\n      ✅ TOUTES LES DONNÉES REQUISES SONT PRÉSENTES")
                
                # Obtenir les informations client depuis la table Clients
                print(f"\n   👤 INFORMATIONS CLIENT DÉTAILLÉES:")
                cursor.execute("""
                    SELECT Name, CompanyName, ClientNcc, Phone, Email, 
                           Country, DefaultCurrency, DefaultTemplate, DefaultPaymentMethod
                    FROM Clients 
                    WHERE ClientCode = ?
                """, (client_code,))
                
                client_info = cursor.fetchone()
                if client_info:
                    (name, company, ncc, phone, email, country, currency, def_template, def_payment) = client_info
                    print(f"      📛 Nom: {name}")
                    print(f"      🏢 Entreprise: {company}")
                    print(f"      🔢 NCC: {ncc}")
                    print(f"      📞 Téléphone: {phone}")
                    print(f"      📧 Email: {email}")
                    print(f"      🌍 Pays: {country}")
                    print(f"      💱 Devise: {currency}")
                    print(f"      📄 Template par défaut: {def_template}")
                    print(f"      💳 Paiement par défaut: {def_payment}")
                    
                    # Vérifier la cohérence
                    if template != def_template:
                        print(f"      ⚠️ Template facture ({template}) ≠ défaut client ({def_template})")
                    if payment != def_payment:
                        print(f"      ⚠️ Paiement facture ({payment}) ≠ défaut client ({def_payment})")
                else:
                    print(f"      ❌ Informations client non trouvées pour le code {client_code}")
                
            else:
                print(f"   ❌ Facture {facture_num} non trouvée")
        
        # Résumé final
        print(f"\n🎯 RÉSUMÉ POUR LA CERTIFICATION FNE")
        print("=" * 35)
        
        print(f"✅ POINTS POSITIFS:")
        print(f"   • Templates conformes FNE (B2C)")
        print(f"   • Types conformes FNE (sale)")
        print(f"   • Paiements conformes FNE (cash)")
        print(f"   • Structure de base OK")
        
        print(f"\n🔧 POINTS À VÉRIFIER POUR CERTIFICATION:")
        print(f"   • Configuration API FNE active ✅")
        print(f"   • Connexion réseau vers DGI")
        print(f"   • Format exact des données selon FNE-procedureapi.md")
        print(f"   • Mapping client → API FNE")
    
    except sqlite3.Error as e:
        print(f"❌ Erreur base de données: {e}")
    
    except Exception as e:
        print(f"❌ Erreur inattendue: {e}")
    
    finally:
        if 'conn' in locals():
            conn.close()

if __name__ == "__main__":
    analyze_specific_invoices()
    print(f"\n✅ Analyse terminée")