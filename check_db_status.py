#!/usr/bin/env python3
"""
Script simple pour vérifier l'état de la base de données FNEV4
"""
import sqlite3
import os

DB_PATH = r"d:\PROJET\FNEV4\data\FNEV4.db"

def check_database_status():
    """Vérifie l'état actuel de la base de données"""
    if not os.path.exists(DB_PATH):
        print(f"❌ Base de données non trouvée : {DB_PATH}")
        return
    
    print("🔍 État actuel de la base de données FNEV4")
    print("=" * 50)
    
    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()
    
    # Compter les factures
    cursor.execute("SELECT COUNT(*) FROM FneInvoices")
    invoice_count = cursor.fetchone()[0]
    
    # Compter les articles de facture
    cursor.execute("SELECT COUNT(*) FROM FneInvoiceItems")
    item_count = cursor.fetchone()[0]
    
    # Quelques détails sur les factures récentes
    cursor.execute("""
        SELECT Id, InvoiceNumber, TotalAmount, CreatedAt 
        FROM FneInvoices 
        ORDER BY CreatedAt DESC 
        LIMIT 5
    """)
    recent_invoices = cursor.fetchall()
    
    print(f"📊 TOTAUX :")
    print(f"   • Factures : {invoice_count}")
    print(f"   • Articles : {item_count}")
    
    if invoice_count > 0:
        ratio = item_count / invoice_count if invoice_count > 0 else 0
        print(f"   • Ratio articles/facture : {ratio:.1f}")
    
    print(f"\n📋 Dernières factures :")
    for invoice in recent_invoices:
        id_val, number, amount, created = invoice
        print(f"   • #{number} - {amount}€ (ID: {id_val})")
    
    # Vérifier s'il y a des articles pour les dernières factures
    if recent_invoices:
        print(f"\n🔍 Articles pour les dernières factures :")
        for invoice in recent_invoices[:3]:  # Les 3 dernières
            id_val, number, amount, created = invoice
            cursor.execute("SELECT COUNT(*) FROM FneInvoiceItems WHERE FneInvoiceId = ?", (id_val,))
            item_count_for_invoice = cursor.fetchone()[0]
            print(f"   • Facture #{number} : {item_count_for_invoice} articles")
    
    conn.close()

if __name__ == "__main__":
    try:
        check_database_status()
    except Exception as e:
        print(f"❌ Erreur : {e}")
    
    print("\n" + "="*50)
    print("💡 Pour tester l'import corrigé :")
    print("1. Allez dans FNEV4 → Import & Traitement → Import de fichiers")
    print("2. Sélectionnez un fichier Excel Sage100")
    print("3. Lancez l'import")
    print("4. Réexécutez ce script pour voir les changements")