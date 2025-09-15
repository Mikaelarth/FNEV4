#!/usr/bin/env python3
"""
Script pour tester que le service d'import corrigé crée bien les FneInvoiceItems
"""
import sqlite3
import shutil
import os
from datetime import datetime

# Chemins
DB_PATH = r"d:\PROJET\FNEV4\data\FNEV4.db"
BACKUP_PATH = r"d:\PROJET\FNEV4\data\FNEV4_backup_before_test.db"
EXCEL_FILE = r"d:\PROJET\FNEV4\src\FNEV4.Presentation\test_factures_sage100.xlsx"

def backup_database():
    """Sauvegarde la base de données avant le test"""
    if os.path.exists(DB_PATH):
        shutil.copy2(DB_PATH, BACKUP_PATH)
        print(f"✅ Base de données sauvegardée vers {BACKUP_PATH}")
    else:
        print(f"❌ Base de données non trouvée : {DB_PATH}")

def count_invoice_items():
    """Compte les éléments de facture dans la base"""
    if not os.path.exists(DB_PATH):
        print(f"❌ Base de données non trouvée : {DB_PATH}")
        return 0, 0
    
    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()
    
    # Compter les factures
    cursor.execute("SELECT COUNT(*) FROM FneInvoices")
    invoice_count = cursor.fetchone()[0]
    
    # Compter les articles de facture
    cursor.execute("SELECT COUNT(*) FROM FneInvoiceItems")
    item_count = cursor.fetchone()[0]
    
    conn.close()
    return invoice_count, item_count

def main():
    print("🔍 Test du service d'import corrigé")
    print("=" * 50)
    
    # Sauvegarde
    backup_database()
    
    # État avant
    print("\n📊 État AVANT l'import :")
    invoices_before, items_before = count_invoice_items()
    print(f"   Factures : {invoices_before}")
    print(f"   Articles : {items_before}")
    
    # Instructions pour l'utilisateur
    print("\n🎯 INSTRUCTIONS POUR TESTER :")
    print("1. Dans l'application FNEV4 qui vient de s'ouvrir")
    print("2. Allez dans le menu 'Import & Traitement'")
    print("3. Cliquez sur 'Import Sage100'")
    print(f"4. Sélectionnez le fichier : {EXCEL_FILE}")
    print("5. Lancez l'import")
    print("6. Revenez ici et appuyez sur ENTRÉE")
    
    input("\n⏸️  Appuyez sur ENTRÉE après avoir fait l'import...")
    
    # État après
    print("\n📊 État APRÈS l'import :")
    invoices_after, items_after = count_invoice_items()
    print(f"   Factures : {invoices_after}")
    print(f"   Articles : {items_after}")
    
    # Analyse
    print("\n📈 RÉSULTATS :")
    invoices_added = invoices_after - invoices_before
    items_added = items_after - items_before
    
    print(f"   Nouvelles factures : +{invoices_added}")
    print(f"   Nouveaux articles  : +{items_added}")
    
    if invoices_added > 0 and items_added > 0:
        ratio = items_added / invoices_added
        print(f"   Ratio articles/facture : {ratio:.1f}")
        
        if ratio >= 1:
            print("✅ SUCCÈS ! Le service d'import crée maintenant les articles de facture")
        else:
            print("⚠️  Problème : Trop peu d'articles par facture")
    elif invoices_added > 0 and items_added == 0:
        print("❌ ÉCHEC ! Les factures sont créées mais pas les articles")
    elif invoices_added == 0:
        print("ℹ️  Aucune nouvelle facture importée (peut-être déjà existante)")
    
    print(f"\n💾 Base sauvegardée dans : {BACKUP_PATH}")

if __name__ == "__main__":
    main()