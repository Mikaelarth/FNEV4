#!/usr/bin/env python3
"""
Script d'analyse - Facture FNE 556469 (Corrigé)
Analyse détaillée des données avec gestion correcte des colonnes
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
        
        # D'abord, examiner la structure des tables
        print("\n🔍 STRUCTURE DES TABLES:")
        
        # Table FneInvoices
        cursor.execute("PRAGMA table_info(FneInvoices)")
        invoice_columns = cursor.fetchall()
        print(f"📋 FneInvoices - {len(invoice_columns)} colonnes:")
        for col in invoice_columns[:10]:  # Afficher les 10 premières
            print(f"   • {col[1]} ({col[2]})")
        if len(invoice_columns) > 10:
            print(f"   ... et {len(invoice_columns) - 10} autres colonnes")
        
        # Table FneInvoiceItems 
        cursor.execute("PRAGMA table_info(FneInvoiceItems)")
        items_columns = cursor.fetchall()
        if items_columns:
            print(f"🛍️ FneInvoiceItems - {len(items_columns)} colonnes:")
            for col in items_columns:
                print(f"   • {col[1]} ({col[2]})")
        else:
            print("❌ Table FneInvoiceItems n'existe pas")
        
        # Rechercher la facture 556469
        cursor.execute("""
            SELECT * FROM FneInvoices 
            WHERE InvoiceNumber = '556469'
        """)
        
        facture = cursor.fetchone()
        
        if not facture:
            print("❌ Facture 556469 introuvable")
            return
        
        # Obtenir les noms des colonnes de FneInvoices
        cursor.execute("PRAGMA table_info(FneInvoices)")
        columns_info = cursor.fetchall()
        column_names = [col[1] for col in columns_info]
        
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
        print(f"   • Nom Affiché: {facture_dict.get('ClientDisplayName', 'N/A')}")
        print(f"   • Nom Complet: {facture_dict.get('ClientCompanyName', 'N/A')}")
        print(f"   • NCC: {facture_dict.get('ClientNcc', 'N/A')}")
        print(f"   • ID Client: {facture_dict.get('ClientId', 'N/A')}")
        print(f"   • Email: {facture_dict.get('ClientEmail', 'N/A')}")
        print(f"   • Téléphone: {facture_dict.get('ClientPhone', 'N/A')}")
        
        print(f"\n💰 MONTANTS:")
        montant_ht = facture_dict.get('TotalAmountHT', 0) or 0
        montant_tva = facture_dict.get('TotalVatAmount', 0) or 0 
        montant_ttc = facture_dict.get('TotalAmountTTC', 0) or 0
        print(f"   • Montant HT: {montant_ht:,.2f} FCFA")
        print(f"   • Montant TVA: {montant_tva:,.2f} FCFA")
        print(f"   • Montant TTC: {montant_ttc:,.2f} FCFA")
        print(f"   • Remise: {facture_dict.get('GlobalDiscount', 0):,.2f}%")
        
        # Vérifier le calcul
        calcul_ttc = montant_ht + montant_tva
        if abs(calcul_ttc - montant_ttc) > 0.01:
            print(f"   ⚠️ ATTENTION: HT + TVA = {calcul_ttc:.2f} ≠ TTC affiché ({montant_ttc:.2f})")
        
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
        
        # Analyser les articles avec la bonne clé étrangère
        invoice_id = facture_dict['Id']
        
        # Essayer différentes colonnes possibles pour la liaison
        foreign_key_attempts = ['InvoiceId', 'FneInvoiceId', 'FactureId', 'ParentId']
        articles = []
        
        for fk_name in foreign_key_attempts:
            try:
                cursor.execute(f"""
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
                    WHERE {fk_name} = ?
                    ORDER BY ProductCode
                """, (invoice_id,))
                
                articles = cursor.fetchall()
                if articles:
                    print(f"✅ Articles trouvés via {fk_name}")
                    break
            except sqlite3.Error:
                continue
        
        print(f"\n🛍️ ARTICLES ({len(articles)} trouvés):")
        if articles:
            print("   Code       | Désignation                    | Qté      | Unité | Prix U.  | TVA  | Montant HT")
            print("   " + "-" * 95)
            total_ht_articles = 0
            for article in articles:
                code, desc, qty, unit, price, vat, amount_ht, amount_ttc = article
                amount_ht = amount_ht or 0
                total_ht_articles += amount_ht
                print(f"   {code or 'N/A':<10} | {(desc or 'N/A')[:30]:<30} | {qty:>8.2f} | {unit or 'N/A':<5} | {price:>8.2f} | {vat or 'N/A':<4} | {amount_ht:>10.2f}")
            
            print(f"   {'-' * 95}")
            print(f"   {'TOTAL CALCULÉ':<72} | {total_ht_articles:>10.2f}")
            
            if abs(total_ht_articles - montant_ht) > 0.01:
                print(f"   ⚠️ ATTENTION: Total articles ({total_ht_articles:.2f}) ≠ Total facture ({montant_ht:.2f})")
        else:
            print("   ❌ Aucun article trouvé")
            print("   💡 Vérifier la structure de la table FneInvoiceItems")
        
        # Comparaison avec les données affichées dans la capture
        print(f"\n📸 COMPARAISON AVEC LA CAPTURE D'ÉCRAN:")
        print("   (Données visibles dans l'interface utilisateur)")
        
        capture_data = {
            'numero': '556469',
            'client_code': '1999', 
            'client_nom': 'SOREFCI',
            'date': '01/03/2025',
            'montant_ht': 272991.00,
            'montant_tva': 7008.48,
            'montant_ttc': 279999.48,
            'template': 'Business to Consumer',
            'point_vente': 'GSM (Fictif)',
            'paiement': 'Espèces'
        }
        
        # Le client nom dans l'affichage vient probablement du message commercial
        client_affiché = facture_dict.get('ClientDisplayName') or \
                        (facture_dict.get('CommercialMessage', '').replace('Client: ', '') if 
                         facture_dict.get('CommercialMessage', '').startswith('Client: ') else 'N/A')
        
        comparisons = [
            ('Numéro facture', facture_dict.get('InvoiceNumber'), capture_data['numero']),
            ('Code client', facture_dict.get('ClientCode'), capture_data['client_code']),
            ('Nom client (affiché)', client_affiché, capture_data['client_nom']),
            ('Template', facture_dict.get('Template'), 'B2C'),
            ('Point de vente', facture_dict.get('PointOfSale'), capture_data['point_vente']),
            ('Paiement', facture_dict.get('PaymentMethod'), 'cash'),
            ('Montant HT', montant_ht, capture_data['montant_ht']),
            ('Montant TVA', montant_tva, capture_data['montant_tva']),
            ('Montant TTC', montant_ttc, capture_data['montant_ttc'])
        ]
        
        print("   Champ                | Base de données        | Interface")
        print("   " + "-" * 70)
        
        for nom, db_value, expected_value in comparisons:
            if isinstance(db_value, (int, float)) and isinstance(expected_value, (int, float)):
                match = abs(float(db_value) - float(expected_value)) < 0.01
            else:
                match = str(db_value).strip() == str(expected_value).strip()
            
            status = "✅" if match else "❌"
            print(f"   {status} {nom:<20} | {str(db_value):<20} | {str(expected_value)}")
        
        # DIAGNOSTIC DE L'AFFICHAGE CLIENT
        print(f"\n🔍 DIAGNOSTIC AFFICHAGE CLIENT:")
        print(f"   • ClientDisplayName BDD: '{facture_dict.get('ClientDisplayName')}'")
        print(f"   • ClientCompanyName BDD: '{facture_dict.get('ClientCompanyName')}'")
        print(f"   • CommercialMessage BDD: '{facture_dict.get('CommercialMessage')}'")
        print(f"   • Nom affiché interface: 'SOREFCI'")
        print(f"   💡 Le nom 'SOREFCI' vient probablement du message commercial")
        
        # Vérifier si c'est un problème de binding ou de ViewModel
        if not facture_dict.get('ClientDisplayName') and facture_dict.get('CommercialMessage'):
            print(f"\n⚠️ PROBLÈME IDENTIFIÉ:")
            print(f"   • ClientDisplayName est NULL/vide dans la BDD")
            print(f"   • Le nom 'SOREFCI' est dans CommercialMessage")
            print(f"   • Le ViewModel doit corriger ce binding")
        
    except sqlite3.Error as e:
        print(f"❌ Erreur base de données: {e}")
    
    except Exception as e:
        print(f"❌ Erreur inattendue: {e}")
        import traceback
        traceback.print_exc()
    
    finally:
        if 'conn' in locals():
            conn.close()

if __name__ == "__main__":
    print("🔍 ANALYSE DÉTAILLÉE FACTURE FNE 556469")
    print("=" * 60)
    analyze_invoice_556469()
    print("\n✅ Analyse terminée")