#!/usr/bin/env python3
"""
Recherche les tables d'établissement/entreprise dans la base de données
"""

import sqlite3
import os

def find_establishment_data():
    # Trouver la base de données
    db_path = 'data/FNEV4.db'
    if not os.path.exists(db_path):
        db_path = 'fnev4_app.db'

    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()

    # Lister toutes les tables qui pourraient contenir des infos d'établissement
    cursor.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name")
    tables = cursor.fetchall()

    print('🗂️  Tables disponibles:')
    for table in tables:
        print(f'  - {table[0]}')

    # Chercher dans les tables de configuration ou entreprise
    config_tables = ['Company', 'Enterprise', 'Establishment', 'Configuration', 'Settings', 'AppSettings']
    for table_name in config_tables:
        try:
            cursor.execute(f"SELECT * FROM {table_name} LIMIT 1")
            result = cursor.fetchone()
            if result:
                cursor.execute(f"PRAGMA table_info({table_name})")
                columns = cursor.fetchall()
                print(f'\n📋 Structure de {table_name}:')
                for col in columns:
                    print(f'  {col[1]} ({col[2]})')
                    
                # Afficher les données
                cursor.execute(f"SELECT * FROM {table_name}")
                data = cursor.fetchall()
                print(f'\n📄 Données de {table_name}:')
                for row in data[:3]:  # Limiter à 3 lignes
                    print(f'  {row}')
        except Exception as e:
            pass

    # Chercher spécifiquement les champs liés à l'établissement
    print('\n🔍 Recherche de champs "establishment" dans toutes les tables:')
    for table in tables:
        table_name = table[0]
        try:
            cursor.execute(f"PRAGMA table_info({table_name})")
            columns = cursor.fetchall()
            establishment_cols = [col for col in columns if 'establishment' in col[1].lower() or 'company' in col[1].lower() or 'enterprise' in col[1].lower()]
            if establishment_cols:
                print(f'  {table_name}: {[col[1] for col in establishment_cols]}')
        except:
            pass

    conn.close()

if __name__ == "__main__":
    find_establishment_data()