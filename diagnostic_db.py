#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Diagnostic des chemins de base de données FNEV4
Détecte toutes les instances et propose une consolidation
"""

import os
import json
from pathlib import Path
import sqlite3
from datetime import datetime

def find_all_databases():
    """Trouve toutes les bases de données FNEV4"""
    
    project_root = Path("C:/wamp64/www/FNEV4")
    databases = []
    
    # Chercher tous les fichiers .db
    for db_file in project_root.rglob("*.db"):
        if "FNEV4" in db_file.name:
            try:
                # Obtenir info sur le fichier
                stat = db_file.stat()
                size = stat.st_size
                modified = datetime.fromtimestamp(stat.st_mtime)
                
                # Vérifier si c'est une vraie base SQLite
                is_valid = False
                table_count = 0
                try:
                    with sqlite3.connect(str(db_file)) as conn:
                        cursor = conn.cursor()
                        cursor.execute("SELECT name FROM sqlite_master WHERE type='table';")
                        tables = cursor.fetchall()
                        table_count = len(tables)
                        is_valid = table_count > 0
                except:
                    pass
                
                databases.append({
                    'path': str(db_file),
                    'relative_path': str(db_file.relative_to(project_root)),
                    'size': size,
                    'modified': modified,
                    'is_valid': is_valid,
                    'table_count': table_count
                })
            except Exception as e:
                print(f"❌ Erreur lecture {db_file}: {e}")
    
    return databases

def analyze_config_files():
    """Analyse les fichiers de configuration"""
    
    project_root = Path("C:/wamp64/www/FNEV4")
    configs = []
    
    # Chercher tous les appsettings.json
    for config_file in project_root.rglob("appsettings.json"):
        try:
            with open(config_file, 'r', encoding='utf-8') as f:
                content = json.load(f)
                
            connection_string = content.get('ConnectionStrings', {}).get('DefaultConnection', '')
            
            configs.append({
                'path': str(config_file),
                'relative_path': str(config_file.relative_to(project_root)),
                'connection_string': connection_string
            })
        except Exception as e:
            print(f"❌ Erreur lecture config {config_file}: {e}")
    
    return configs

def main():
    """Diagnostic principal"""
    
    print("🔍 DIAGNOSTIC DES BASES DE DONNÉES FNEV4")
    print("=" * 80)
    
    # 1. Trouver toutes les bases
    databases = find_all_databases()
    
    print(f"📊 {len(databases)} base(s) de données trouvée(s):")
    print()
    
    for i, db in enumerate(databases, 1):
        print(f"🔸 BASE {i}: {db['relative_path']}")
        print(f"   📁 Chemin: {db['path']}")
        print(f"   📏 Taille: {db['size']:,} bytes")
        print(f"   🕒 Modifié: {db['modified'].strftime('%Y-%m-%d %H:%M:%S')}")
        print(f"   ✅ Valide: {'Oui' if db['is_valid'] else 'Non'}")
        print(f"   📋 Tables: {db['table_count']}")
        print()
    
    # 2. Analyser les configurations
    print("=" * 60)
    print("CONFIGURATIONS")
    print("=" * 60)
    
    configs = analyze_config_files()
    
    for i, config in enumerate(configs, 1):
        print(f"🔸 CONFIG {i}: {config['relative_path']}")
        print(f"   🔗 Connection: {config['connection_string']}")
        print()
    
    # 3. Recommandations
    print("=" * 60)
    print("RECOMMANDATIONS")
    print("=" * 60)
    
    # Trouver la base principale (la plus récente avec le plus de tables)
    valid_dbs = [db for db in databases if db['is_valid'] and db['table_count'] > 0]
    
    if valid_dbs:
        # Trier par nombre de tables puis par date de modification
        main_db = max(valid_dbs, key=lambda x: (x['table_count'], x['modified']))
        
        print(f"📍 BASE PRINCIPALE RECOMMANDÉE:")
        print(f"   📁 {main_db['relative_path']}")
        print(f"   💡 Raison: {main_db['table_count']} tables, modifiée le {main_db['modified'].strftime('%Y-%m-%d %H:%M:%S')}")
        print()
        
        print("🎯 ACTIONS RECOMMANDÉES:")
        print("1. ✅ Consolider toutes les données vers:")
        print(f"      📁 C:/wamp64/www/FNEV4/data/FNEV4.db")
        print()
        print("2. ✅ Standardiser tous les appsettings.json:")
        print('      "DefaultConnection": "Data\\\\FNEV4.db"')
        print()
        print("3. ✅ Modifier PathConfigurationService pour utiliser:")
        print("      📁 Chemin absolu fixe: C:/wamp64/www/FNEV4/data/FNEV4.db")
        print()
        print("4. ✅ Nettoyer les bases dispersées")
        
        # Bases à nettoyer
        cleanup_dbs = [db for db in databases if db['path'] != main_db['path']]
        if cleanup_dbs:
            print()
            print("🧹 BASES À NETTOYER:")
            for db in cleanup_dbs:
                print(f"   🗑️  {db['relative_path']}")
    else:
        print("❌ Aucune base valide trouvée!")
    
    print()
    print("✅ Diagnostic terminé !")

if __name__ == "__main__":
    main()
