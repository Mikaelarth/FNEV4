#!/usr/bin/env python3
"""
Script d'analyse - Facture FNE 556469
Analyse détaillée des données de la facture pour identifier les incohérences
"""

import sqlite3
import sys
from pathlib import Path

def analyze_invoice_556469():
    """
    Analyse détaillée de la facture FNE 556469 visible dans la capture
    """
    
    # Chemins possibles pour la base de données
    db_paths = [
        Path("data/FNEV4.db"),
        Path("FNEV4/data/FNEV4.db"),
        Path("src/FNEV4.Presentation/data/FNEV4.db"),
        Path("../data/FNEV4.db")
    ]
    
    db_path = None
    for path in db_paths:
        if path.exists():
            db_path = path
            break
    
    if not db_path:
        print("❌ Base de données FNEV4.db introuvable")
        return
    
    print(f"✅ Base de données trouvée : {db_path}")
    
    try:
        # Connexion à la base de données
        conn = sqlite3.connect(str(db_path))
        cursor = conn.cursor()
        
        # Rechercher la facture 556469
        cursor.execute("""
            SELECT * FROM FneInvoices 
            WHERE InvoiceNumber = '556469' OR InvoiceNumber LIKE '%556469%'
        """)
        
        facture = cursor.fetchone()
        
        if not facture:
            print("❌ Facture 556469 introuvable dans FneInvoices")
            print("🔍 Recherche de factures similaires...")
            
            cursor.execute("""
                SELECT InvoiceNumber, ClientDisplayName, TotalAmountTTC, InvoiceDate
                FROM FneInvoices 
                WHERE InvoiceNumber LIKE '%556469%' OR ClientDisplayName LIKE '%SOREFCI%'
                ORDER BY InvoiceNumber DESC
                LIMIT 10
            """)
            
            factures_similaires = cursor.fetchall()
            if factures_similaires:
                print("📋 Factures trouvées avec critères similaires:")
                for f in factures_similaires:
                    print(f"   • N° {f[0]} - {f[1]} - {f[2]:,.2f} FCFA - {f[3]}")
            else:
                print("❌ Aucune facture similaire trouvée")
            
            return
        
        # Obtenir les colonnes de la table
        cursor.execute("PRAGMA table_info(FneInvoices)")
        columns = cursor.fetchall()
        column_names = [col[1] for col in columns]
        
        print(f"\n🔍 ANALYSE FACTURE 556469")
        print("=" * 80)
        
        # Créer un dictionnaire des données de la facture
        facture_dict = dict(zip(column_names, facture))
        
        # Afficher les informations clés
        print(f"📋 INFORMATIONS PRINCIPALES:")
        print(f"   • ID: {facture_dict.get('Id', 'N/A')}")
        print(f"   • Numéro: {facture_dict.get('InvoiceNumber', 'N/A')}")
        print(f"   • Date: {facture_dict.get('InvoiceDate', 'N/A')}")
        print(f"   • Type: {facture_dict.get('InvoiceType', 'N/A')}")
        print(f"   • Status: {facture_dict.get('Status', 'N/A')}")
        print(f"   • Template: {facture_dict.get('Template', 'N/A')}")
        
        print(f"\n👤 INFORMATIONS CLIENT:")
        print(f"   • Code Client: {facture_dict.get('ClientCode', 'N/A')}")
        print(f"   • Nom: {facture_dict.get('ClientDisplayName', 'N/A')}")
        print(f"   • NCC: {facture_dict.get('ClientNcc', 'N/A')}")
        print(f"   • ID Client: {facture_dict.get('ClientId', 'N/A')}")
        
        print(f"\n💰 MONTANTS:")
        print(f"   • Montant HT: {facture_dict.get('TotalAmountHT', 0):,.2f} FCFA")
        print(f"   • Montant TVA: {facture_dict.get('TotalVatAmount', 0):,.2f} FCFA")
        print(f"   • Montant TTC: {facture_dict.get('TotalAmountTTC', 0):,.2f} FCFA")
        print(f"   • Remise: {facture_dict.get('GlobalDiscount', 0):,.2f}%")
        
        print(f"\n🏪 INFORMATIONS COMMERCIALES:")
        print(f"   • Point de vente: {facture_dict.get('PointOfSale', 'N/A')}")
        print(f"   • Établissement: {facture_dict.get('Establishment', 'N/A')}")
        print(f"   • Paiement: {facture_dict.get('PaymentMethod', 'N/A')}")
        print(f"   • Message: {facture_dict.get('CommercialMessage', 'N/A')}")
        
        print(f"\n🔐 CERTIFICATION:")
        print(f"   • Référence FNE: {facture_dict.get('FneReference', 'N/A')}")
        print(f"   • Certifiée: {'✅ OUI' if facture_dict.get('IsCertified') else '❌ NON'}")
        print(f"   • Date certification: {facture_dict.get('CertifiedAt', 'N/A')}")
        print(f"   • Numéro certif.: {facture_dict.get('FneCertificationNumber', 'N/A')}")
        print(f"   • Date certif.: {facture_dict.get('FneCertificationDate', 'N/A')}")
        print(f"   • QR Code: {facture_dict.get('FneQrCode', 'N/A')}")
        print(f"   • Signature: {facture_dict.get('FneDigitalSignature', 'N/A')}")
        print(f"   • URL validation: {facture_dict.get('FneValidationUrl', 'N/A')}")
        
        # Analyser les articles
        cursor.execute("""
            SELECT 
                ProductCode,
                Description,
                Quantity,
                MeasurementUnit,
                UnitPrice,
                VatCode,
                LineAmountHT,
                LineAmountTTC
            FROM FneInvoiceItems 
            WHERE InvoiceId = ?
            ORDER BY Id
        """, (facture_dict['Id'],))
        
        articles = cursor.fetchall()
        
        print(f"\n🛍️ ARTICLES ({len(articles)} trouvés):")
        if articles:
            print("   Code       | Désignation                    | Qté      | Unité | Prix U.  | TVA  | Montant HT")
            print("   " + "-" * 95)
            for article in articles:
                code, desc, qty, unit, price, vat, amount_ht, amount_ttc = article
                print(f"   {code or 'N/A':<10} | {(desc or 'N/A')[:30]:<30} | {qty:>8.2f} | {unit or 'N/A':<5} | {price:>8.2f} | {vat or 'N/A':<4} | {amount_ht:>10.2f}")
        else:
            print("   ❌ Aucun article trouvé")
        
        # Vérification des incohérences
        print(f"\n🔍 VÉRIFICATIONS D'INTÉGRITÉ:")
        issues = []
        
        # Vérifier les montants
        montant_ht = facture_dict.get('TotalAmountHT', 0) or 0
        montant_tva = facture_dict.get('TotalVatAmount', 0) or 0
        montant_ttc = facture_dict.get('TotalAmountTTC', 0) or 0
        
        if abs(montant_ht + montant_tva - montant_ttc) > 0.01:
            issues.append(f"Incohérence montants: HT({montant_ht:.2f}) + TVA({montant_tva:.2f}) ≠ TTC({montant_ttc:.2f})")
        
        # Vérifier les données client
        if not facture_dict.get('ClientCode'):
            issues.append("Code client manquant")
        if not facture_dict.get('ClientDisplayName'):
            issues.append("Nom client manquant")
        
        # Vérifier les données FNE
        if not facture_dict.get('Template'):
            issues.append("Template FNE manquant (B2B/B2C/B2G)")
        if not facture_dict.get('PaymentMethod'):
            issues.append("Méthode de paiement manquante")
        if not facture_dict.get('PointOfSale'):
            issues.append("Point de vente manquant")
        
        # Vérifier la cohérence des articles
        if articles:
            total_ht_calcule = sum(article[6] or 0 for article in articles)  # LineAmountHT
            if abs(total_ht_calcule - montant_ht) > 0.01:
                issues.append(f"Total HT articles ({total_ht_calcule:.2f}) ≠ Total HT facture ({montant_ht:.2f})")
        
        if issues:
            print("   ❌ PROBLÈMES DÉTECTÉS:")
            for issue in issues:
                print(f"      • {issue}")
        else:
            print("   ✅ Aucune incohérence majeure détectée")
        
        # Comparaison avec les données affichées dans la capture
        print(f"\n📸 COMPARAISON AVEC LA CAPTURE D'ÉCRAN:")
        capture_data = {
            'InvoiceNumber': '556469',
            'ClientCode': '1999',
            'ClientName': 'SOREFCI',
            'InvoiceDate': '01/03/2025',
            'TotalAmountHT': 272991.00,
            'TotalVatAmount': 7008.48,
            'TotalAmountTTC': 279999.48
        }
        
        comparisons = [
            ('Numéro facture', facture_dict.get('InvoiceNumber'), capture_data['InvoiceNumber']),
            ('Code client', facture_dict.get('ClientCode'), capture_data['ClientCode']),
            ('Nom client', facture_dict.get('ClientDisplayName', ''), capture_data['ClientName']),
            ('Date facture', facture_dict.get('InvoiceDate', ''), capture_data['InvoiceDate']),
            ('Montant HT', facture_dict.get('TotalAmountHT', 0), capture_data['TotalAmountHT']),
            ('Montant TVA', facture_dict.get('TotalVatAmount', 0), capture_data['TotalVatAmount']),
            ('Montant TTC', facture_dict.get('TotalAmountTTC', 0), capture_data['TotalAmountTTC'])
        ]
        
        for nom, db_value, capture_value in comparisons:
            if isinstance(db_value, (int, float)) and isinstance(capture_value, (int, float)):
                match = abs(float(db_value) - float(capture_value)) < 0.01
            else:
                match = str(db_value).strip() == str(capture_value).strip()
            
            status = "✅" if match else "❌"
            print(f"   {status} {nom}: DB='{db_value}' vs Capture='{capture_value}'")
        
    except sqlite3.Error as e:
        print(f"❌ Erreur base de données: {e}")
    
    except Exception as e:
        print(f"❌ Erreur inattendue: {e}")
    
    finally:
        if 'conn' in locals():
            conn.close()

if __name__ == "__main__":
    print("🔍 ANALYSE FACTURE FNE 556469")
    print("=" * 50)
    analyze_invoice_556469()
    print("\n✅ Analyse terminée")