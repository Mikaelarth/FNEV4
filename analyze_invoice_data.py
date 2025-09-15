#!/usr/bin/env python3
"""
Analyse des données de la facture 556295 pour identifier les incohérences
"""

import sqlite3
import os

# Chemin vers la base de données
db_path = "src/FNEV4.Infrastructure/Data/fnev4.db"

try:
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    print("=== TABLES DISPONIBLES ===")
    cursor.execute('SELECT name FROM sqlite_master WHERE type="table"')
    tables = cursor.fetchall()
    for table in tables:
        print(f"- {table[0]}")
    
    print("\n=== RECHERCHE DE LA FACTURE 556295 ===")
    
    # Essayons de trouver la facture dans différentes tables possibles
    for table_name in [t[0] for t in tables if 'invoice' in t[0].lower() or 'facture' in t[0].lower()]:
        try:
            print(f"\nTable: {table_name}")
            cursor.execute(f'PRAGMA table_info({table_name})')
            columns = cursor.fetchall()
            print("Colonnes:", [col[1] for col in columns])
            
            # Recherche par numéro de facture
            for col in columns:
                if 'number' in col[1].lower() or 'numero' in col[1].lower():
                    cursor.execute(f'SELECT * FROM {table_name} WHERE {col[1]} = "556295" LIMIT 1')
                    result = cursor.fetchone()
                    if result:
                        print(f"FACTURE TROUVÉE dans {table_name}!")
                        print("Données:", result)
                        break
        except Exception as e:
            print(f"Erreur avec {table_name}: {e}")
    
    # Recherche dans les tables d'articles
    print("\n=== RECHERCHE DES ARTICLES ===")
    for table_name in [t[0] for t in tables if 'item' in t[0].lower() or 'article' in t[0].lower()]:
        try:
            print(f"\nTable articles: {table_name}")
            cursor.execute(f'PRAGMA table_info({table_name})')
            columns = cursor.fetchall()
            print("Colonnes:", [col[1] for col in columns])
            
            cursor.execute(f'SELECT COUNT(*) FROM {table_name}')
            count = cursor.fetchone()[0]
            print(f"Nombre d'articles: {count}")
            
        except Exception as e:
            print(f"Erreur avec {table_name}: {e}")
    
    conn.close()
    
except Exception as e:
    print(f"Erreur de connexion à la base: {e}")