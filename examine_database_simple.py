#!/usr/bin/env python3
"""
Script simple pour examiner la base de données FNEV4
"""

import sqlite3

def main():
    db_path = r"d:\PROJET\FNE\FNEV4\data\FNEV4.db"
    
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    print("=== TABLES DE LA BASE ===")
    cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
    tables = cursor.fetchall()
    
    for table in tables:
        print(f"  - {table[0]}")
    
    print(f"\n=== FACTURES DANS LA TABLE INVOICES ===")
    cursor.execute("SELECT COUNT(*) FROM Invoices")
    count = cursor.fetchone()[0]
    print(f"Total factures: {count}")
    
    if count > 0:
        print("\nEchantillon (5 premières):")
        cursor.execute("SELECT InvoiceNumber, Status, CustomerCode, FneCertificationDate FROM Invoices LIMIT 5")
        factures = cursor.fetchall()
        for facture in factures:
            print(f"  - {facture[0]}: {facture[1]} | Client: {facture[2]} | Certifié: {facture[3]}")
    
    print(f"\n=== CONFIGURATIONS FNE ===")
    try:
        cursor.execute("SELECT COUNT(*) FROM FneConfigurations")
        config_count = cursor.fetchone()[0]
        print(f"Total configurations FNE: {config_count}")
        
        if config_count > 0:
            cursor.execute("PRAGMA table_info(FneConfigurations)")
            columns = cursor.fetchall()
            print("Colonnes FneConfigurations:")
            for col in columns:
                print(f"  - {col[1]} ({col[2]})")
    except sqlite3.Error as e:
        print(f"Erreur table FneConfigurations: {e}")
    
    conn.close()

if __name__ == "__main__":
    main()