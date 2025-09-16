#!/usr/bin/env python3
"""
Script pour examiner la vraie structure de FneInvoices
"""

import sqlite3
import os

def examine_real_structure():
    """Examine la vraie structure de la table FneInvoices"""
    db_path = "D:/PROJET/FNE/FNEV4/data/FNEV4.db"
    if not os.path.exists(db_path):
        print("Base de données non trouvée")
        return
    
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    try:
        # Structure réelle de la table
        print("STRUCTURE RÉELLE DE FNEINVOICES:")
        cursor.execute("PRAGMA table_info(FneInvoices)")
        columns = cursor.fetchall()
        for col in columns:
            print(f"  {col[1]} ({col[2]}) - PK: {col[5]}, NOT NULL: {col[3]}")
        print()
        
        # Données d'exemple
        print("ÉCHANTILLON DE DONNÉES:")
        cursor.execute("SELECT * FROM FneInvoices LIMIT 3")
        sample = cursor.fetchall()
        
        if sample:
            column_names = [col[1] for col in columns]
            print("Colonnes:", ", ".join(column_names))
            for i, row in enumerate(sample):
                print(f"Ligne {i+1}:", row)
        else:
            print("Aucune donnée trouvée")
        print()
        
        # Compter les enregistrements
        cursor.execute("SELECT COUNT(*) FROM FneInvoices")
        total = cursor.fetchone()[0]
        print(f"Total d'enregistrements: {total}")
        
        conn.close()
        
    except Exception as e:
        print(f"Erreur: {e}")
        conn.close()

if __name__ == "__main__":
    examine_real_structure()