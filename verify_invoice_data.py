#!/usr/bin/env python3
"""
Script pour vérifier les données de la facture 556443 dans la base FNEV4.db
"""
import sqlite3
import os

def check_invoice_data():
    db_path = "data/FNEV4.db"
    
    if not os.path.exists(db_path):
        print(f"❌ Base de données non trouvée: {db_path}")
        return
    
    print(f"✅ Base de données trouvée: {db_path}")
    
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Vérifier la structure des tables
        cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
        tables = cursor.fetchall()
        print(f"\n📋 Tables disponibles: {[t[0] for t in tables]}")
        
        # Rechercher la facture 556443
        tables_to_check = ['FneInvoices', 'Invoices', 'Factures', 'fne_invoices']
        invoice_found = False
        
        for table_name in tables_to_check:
            try:
                # Vérifier si la table existe et contient des colonnes pertinentes
                cursor.execute(f"PRAGMA table_info({table_name})")
                columns = cursor.fetchall()
                if columns:
                    print(f"\n📝 Structure de la table {table_name}:")
                    for col in columns:
                        print(f"   - {col[1]} ({col[2]})")
                    
                    # Chercher la facture par numéro
                    cursor.execute(f"SELECT * FROM {table_name} WHERE InvoiceNumber = ? OR Number = ? OR NumeroFacture = ?", 
                                 ("556443", "556443", "556443"))
                    invoice = cursor.fetchone()
                    
                    if invoice:
                        print(f"\n🎯 Facture 556443 trouvée dans {table_name}:")
                        col_names = [desc[1] for desc in columns]
                        for i, value in enumerate(invoice):
                            print(f"   {col_names[i]}: {value}")
                        invoice_found = True
                        break
                        
            except sqlite3.OperationalError as e:
                if "no such table" not in str(e):
                    print(f"⚠️  Erreur avec table {table_name}: {e}")
        
        if not invoice_found:
            # Chercher toutes les factures pour voir ce qui est disponible
            for table_name in [t[0] for t in tables if 'invoice' in t[0].lower() or 'facture' in t[0].lower()]:
                try:
                    cursor.execute(f"SELECT * FROM {table_name} LIMIT 5")
                    invoices = cursor.fetchall()
                    if invoices:
                        print(f"\n📊 Échantillon de {table_name} (5 premiers):")
                        cursor.execute(f"PRAGMA table_info({table_name})")
                        columns = cursor.fetchall()
                        col_names = [desc[1] for desc in columns]
                        
                        for invoice in invoices:
                            print(f"   Facture: {dict(zip(col_names, invoice))}")
                except sqlite3.OperationalError:
                    continue
        
        conn.close()
        
    except Exception as e:
        print(f"❌ Erreur lors de l'accès à la base: {e}")

if __name__ == "__main__":
    check_invoice_data()