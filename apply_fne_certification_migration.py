#!/usr/bin/env python3
"""
Script de migration pour ajouter les colonnes de certification FNE
"""

import sqlite3
import os
import sys
from pathlib import Path

def find_database_files():
    """Trouve tous les fichiers de base de données dans le projet"""
    db_files = []
    
    # Chemins possibles pour les bases de données
    possible_paths = [
        "data/FNEV4.db",
        "Data/FNEV4.db", 
        "src/FNEV4.Presentation/bin/Debug/net8.0-windows/data/FNEV4.db",
        "src/FNEV4.Presentation/bin/Debug/net8.0-windows/Data/FNEV4.db",
    ]
    
    for path in possible_paths:
        if os.path.exists(path):
            db_files.append(path)
            print(f"✅ Base de données trouvée: {path}")
    
    if not db_files:
        print("⚠️  Aucune base de données trouvée. Recherche dans tout le projet...")
        
        # Recherche récursive
        for root, dirs, files in os.walk("."):
            for file in files:
                if file.endswith(".db") and "FNEV4" in file:
                    db_path = os.path.join(root, file)
                    db_files.append(db_path)
                    print(f"✅ Base de données trouvée: {db_path}")
    
    return db_files

def check_column_exists(cursor, table_name, column_name):
    """Vérifie si une colonne existe dans une table"""
    try:
        cursor.execute(f"PRAGMA table_info({table_name})")
        columns = cursor.fetchall()
        column_names = [col[1] for col in columns]
        return column_name in column_names
    except Exception as e:
        print(f"Erreur lors de la vérification de colonne {column_name}: {e}")
        return False

def add_column_if_not_exists(cursor, table_name, column_name, column_type, default_value=None):
    """Ajoute une colonne si elle n'existe pas déjà"""
    if not check_column_exists(cursor, table_name, column_name):
        try:
            sql = f"ALTER TABLE {table_name} ADD COLUMN {column_name} {column_type}"
            if default_value is not None:
                sql += f" DEFAULT {default_value}"
            
            cursor.execute(sql)
            print(f"  ✅ Colonne {column_name} ajoutée")
            return True
        except Exception as e:
            print(f"  ❌ Erreur lors de l'ajout de {column_name}: {e}")
            return False
    else:
        print(f"  ℹ️  Colonne {column_name} existe déjà")
        return True

def migrate_database(db_path):
    """Applique la migration à une base de données"""
    print(f"\n🔄 Migration de: {db_path}")
    
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Vérifier que la table FneInvoices existe
        cursor.execute("SELECT name FROM sqlite_master WHERE type='table' AND name='FneInvoices'")
        if not cursor.fetchone():
            print("  ⚠️  Table FneInvoices non trouvée - création nécessaire")
            conn.close()
            return False
        
        print("  📋 Ajout des colonnes de certification FNE...")
        
        # Colonnes à ajouter
        columns_to_add = [
            ("FneCertificationNumber", "TEXT", "NULL"),
            ("FneCertificationDate", "TEXT", "NULL"),
            ("FneQrCode", "TEXT", "NULL"),
            ("FneDigitalSignature", "TEXT", "NULL"),
            ("FneValidationUrl", "TEXT", "NULL"),
            ("IsCertified", "INTEGER", "0")
        ]
        
        success_count = 0
        for col_name, col_type, default_val in columns_to_add:
            if add_column_if_not_exists(cursor, "FneInvoices", col_name, col_type, default_val):
                success_count += 1
        
        # Commit des changements
        conn.commit()
        
        # Vérification finale
        cursor.execute("PRAGMA table_info(FneInvoices)")
        columns = cursor.fetchall()
        print(f"\n  📊 Structure finale de FneInvoices:")
        for col in columns:
            col_id, name, type_name, not_null, default_val, pk = col
            print(f"    - {name} ({type_name}){' NOT NULL' if not_null else ''}{f' DEFAULT {default_val}' if default_val else ''}")
        
        conn.close()
        print(f"  ✅ Migration réussie ({success_count}/{len(columns_to_add)} colonnes)")
        return True
        
    except Exception as e:
        print(f"  ❌ Erreur de migration: {e}")
        return False

def main():
    """Point d'entrée principal"""
    print("🚀 Script de migration FNE - Colonnes de certification")
    print("=" * 60)
    
    # Changer vers le répertoire du projet
    project_root = Path(__file__).parent
    os.chdir(project_root)
    print(f"📁 Répertoire de travail: {os.getcwd()}")
    
    # Trouver les bases de données
    db_files = find_database_files()
    
    if not db_files:
        print("\n❌ Aucune base de données FNEV4 trouvée!")
        print("Assurez-vous que l'application a été exécutée au moins une fois.")
        sys.exit(1)
    
    # Appliquer la migration à chaque base de données
    success_count = 0
    for db_file in db_files:
        if migrate_database(db_file):
            success_count += 1
    
    print(f"\n🎉 Migration terminée: {success_count}/{len(db_files)} bases de données migrées")
    
    if success_count == len(db_files):
        print("✅ Toutes les migrations ont réussi!")
        print("\n💡 Vous pouvez maintenant utiliser la certification FNE dans l'application.")
    else:
        print("⚠️  Certaines migrations ont échoué. Vérifiez les erreurs ci-dessus.")

if __name__ == "__main__":
    main()