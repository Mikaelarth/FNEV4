#!/usr/bin/env python3
"""
Script pour diagnostiquer les chemins de base de données utilisés par FNEV4
"""

import os
import sqlite3
from pathlib import Path

def find_db_files():
    """Trouve tous les fichiers FNEV4.db dans le projet"""
    project_root = Path(r"C:\wamp64\www\FNEV4")
    db_files = []
    
    print("🔍 Recherche des fichiers FNEV4.db...")
    
    # Chercher dans tout le projet
    for db_file in project_root.rglob("FNEV4.db"):
        size = db_file.stat().st_size if db_file.exists() else 0
        db_files.append({
            'path': str(db_file),
            'size': size,
            'exists': db_file.exists()
        })
    
    # Chercher dans les dossiers de build
    for pattern in ["**/bin/**/FNEV4.db", "**/data/**/FNEV4.db"]:
        for db_file in project_root.rglob(pattern):
            size = db_file.stat().st_size if db_file.exists() else 0
            db_files.append({
                'path': str(db_file),
                'size': size,
                'exists': db_file.exists()
            })
    
    return db_files

def check_db_content(db_path):
    """Vérifie le contenu d'une base de données"""
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Lister les tables
        cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
        tables = [row[0] for row in cursor.fetchall()]
        
        content = {}
        for table in tables:
            if not table.startswith('sqlite_'):
                try:
                    cursor.execute(f"SELECT COUNT(*) FROM [{table}]")
                    count = cursor.fetchone()[0]
                    content[table] = count
                except:
                    content[table] = "Erreur"
        
        conn.close()
        return content
    except Exception as e:
        return f"Erreur: {e}"

def main():
    print("=" * 60)
    print("🔍 DIAGNOSTIC DES CHEMINS DE BASE DE DONNÉES FNEV4")
    print("=" * 60)
    
    db_files = find_db_files()
    
    if not db_files:
        print("❌ Aucun fichier FNEV4.db trouvé")
        return
    
    print(f"\n📁 {len(db_files)} fichier(s) de base de données trouvé(s):\n")
    
    for i, db_info in enumerate(db_files, 1):
        print(f"{i}. {db_info['path']}")
        print(f"   📏 Taille: {db_info['size']:,} bytes")
        print(f"   ✅ Existe: {db_info['exists']}")
        
        if db_info['exists'] and db_info['size'] > 0:
            content = check_db_content(db_info['path'])
            if isinstance(content, dict):
                print(f"   📊 Tables:")
                for table, count in content.items():
                    print(f"      - {table}: {count} enregistrements")
            else:
                print(f"   ❌ {content}")
        print()
    
    # Recommandations
    existing_dbs = [db for db in db_files if db['exists'] and db['size'] > 0]
    
    if len(existing_dbs) > 1:
        print("⚠️  PROBLÈME DÉTECTÉ: Plusieurs bases de données actives!")
        print("   Les différents modules utilisent probablement des bases différentes.")
        print("\n💡 SOLUTION:")
        print("   1. Identifier la base principale à conserver")
        print("   2. Unifier tous les modules sur cette base")
        print("   3. Supprimer les bases dupliquées")
    elif len(existing_dbs) == 1:
        print("✅ Base de données unique trouvée:")
        print(f"   {existing_dbs[0]['path']}")
    else:
        print("⚠️  Aucune base de données existante avec des données")

if __name__ == "__main__":
    main()
