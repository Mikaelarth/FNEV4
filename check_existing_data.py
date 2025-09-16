#!/usr/bin/env python3
"""
Script pour vérifier les données existantes dans la base de données centralisée FNEV4
"""

import sqlite3
import os
import glob

def find_database_files():
    """Trouve tous les fichiers de base de données potentiels"""
    search_patterns = [
        'D:/PROJET/FNE/FNEV4/**/*.db',
        'D:/PROJET/FNE/FNEV4/**/*.sqlite',
        'D:/PROJET/FNE/FNEV4/**/fnev4_database.db',
        'D:/PROJET/FNE/FNEV4/**/FNEV4.db'
    ]
    
    db_files = []
    for pattern in search_patterns:
        db_files.extend(glob.glob(pattern, recursive=True))
    
    # Remove duplicates
    return list(set(db_files))

def check_database_content(db_path):
    """Vérifie le contenu d'une base de données"""
    try:
        print(f"\n=== Analyse de la base : {db_path} ===")
        print(f"Taille du fichier: {os.path.getsize(db_path)} bytes")
        
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Lister toutes les tables
        cursor.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name")
        tables = cursor.fetchall()
        
        if tables:
            print(f"Tables trouvées ({len(tables)}):")
            for table in tables:
                table_name = table[0]
                cursor.execute(f"SELECT COUNT(*) FROM [{table_name}]")
                count = cursor.fetchone()[0]
                print(f"  - {table_name}: {count} enregistrements")
                
                # Si c'est une table de factures, montrer quelques exemples
                if 'invoice' in table_name.lower() or 'facture' in table_name.lower():
                    try:
                        cursor.execute(f"SELECT * FROM [{table_name}] LIMIT 3")
                        rows = cursor.fetchall()
                        if rows:
                            # Récupérer les noms des colonnes
                            cursor.execute(f"PRAGMA table_info([{table_name}])")
                            columns = [col[1] for col in cursor.fetchall()]
                            print(f"    Colonnes: {', '.join(columns)}")
                            
                            print("    Exemples de données:")
                            for row in rows:
                                print(f"    {row}")
                    except Exception as e:
                        print(f"    Erreur lors de la lecture: {e}")
                        
                # Si c'est une table de clients
                if 'client' in table_name.lower():
                    try:
                        cursor.execute(f"SELECT * FROM [{table_name}] LIMIT 3")
                        rows = cursor.fetchall()
                        if rows:
                            cursor.execute(f"PRAGMA table_info([{table_name}])")
                            columns = [col[1] for col in cursor.fetchall()]
                            print(f"    Colonnes: {', '.join(columns)}")
                            
                            print("    Exemples de données:")
                            for row in rows:
                                print(f"    {row}")
                    except Exception as e:
                        print(f"    Erreur lors de la lecture: {e}")
        else:
            print("Aucune table trouvée dans cette base de données")
            
        conn.close()
        return len(tables) > 0
        
    except Exception as e:
        print(f"Erreur lors de l'analyse de {db_path}: {e}")
        return False

def main():
    print("Recherche des fichiers de base de données FNEV4...")
    
    db_files = find_database_files()
    
    if not db_files:
        print("Aucun fichier de base de données trouvé.")
        print("Recherche dans les répertoires de build...")
        
        # Recherche dans les répertoires de build
        build_dirs = [
            'D:/PROJET/FNE/FNEV4/src/FNEV4.Presentation/bin',
            'D:/PROJET/FNE/FNEV4/src/FNEV4.Infrastructure/bin'
        ]
        
        for build_dir in build_dirs:
            if os.path.exists(build_dir):
                for root, dirs, files in os.walk(build_dir):
                    for file in files:
                        if file.endswith(('.db', '.sqlite')):
                            db_files.append(os.path.join(root, file))
    
    if db_files:
        print(f"Fichiers de base de données trouvés ({len(db_files)}):")
        for db_file in db_files:
            print(f"  - {db_file}")
        
        print("\nAnalyse du contenu...")
        for db_file in db_files:
            if os.path.exists(db_file):
                has_data = check_database_content(db_file)
                if has_data:
                    print(f"✅ Base de données active: {db_file}")
                else:
                    print(f"⚠️  Base de données vide: {db_file}")
    else:
        print("Aucun fichier de base de données trouvé.")

if __name__ == "__main__":
    main()