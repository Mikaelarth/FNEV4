#!/usr/bin/env python3
"""
Script de débogage de la structure de la base de données FNEV4
Vérifie les tables, colonnes et données réelles utilisées par l'application
"""

import sqlite3
import os
from datetime import datetime

def main():
    print("=" * 70)
    print("AUDIT COMPLET DE LA BASE DE DONNÉES FNEV4")
    print("=" * 70)
    print(f"Heure: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print()

    # Chemin de la base de données
    db_path = r"D:\PROJET\FNE\FNEV4\data\FNEV4.db"
    
    if not os.path.exists(db_path):
        print(f"❌ Base de données non trouvée: {db_path}")
        return
        
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # 1. Lister toutes les tables
        print("📋 TABLES DANS LA BASE DE DONNÉES")
        cursor.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;")
        tables = cursor.fetchall()
        
        invoice_tables = []
        for table in tables:
            table_name = table[0]
            print(f"   - {table_name}")
            if 'invoice' in table_name.lower() or 'facture' in table_name.lower():
                invoice_tables.append(table_name)
        
        print(f"\n🎯 TABLES LIÉES AUX FACTURES: {invoice_tables}")
        print()
        
        # 2. Examiner chaque table de factures en détail
        for table_name in invoice_tables:
            print(f"🔍 ANALYSE DE LA TABLE: {table_name}")
            
            # Structure de la table
            cursor.execute(f"PRAGMA table_info({table_name});")
            columns = cursor.fetchall()
            print(f"   Colonnes ({len(columns)}):")
            for col in columns:
                col_id, name, data_type, not_null, default, pk = col
                nullable = "NOT NULL" if not_null else "NULL"
                primary = " [PK]" if pk else ""
                print(f"     {name}: {data_type} {nullable}{primary}")
            
            # Compter les enregistrements
            cursor.execute(f"SELECT COUNT(*) FROM {table_name};")
            total_count = cursor.fetchone()[0]
            
            # Analyser les statuts si la colonne Status existe
            status_info = ""
            if any(col[1].lower() == 'status' for col in columns):
                cursor.execute(f"SELECT Status, COUNT(*) FROM {table_name} GROUP BY Status;")
                status_counts = cursor.fetchall()
                status_info = f" | Statuts: {dict(status_counts)}"
            
            # Vérifier les certifications
            cert_info = ""
            cert_cols = [col[1] for col in columns if 'cert' in col[1].lower()]
            if cert_cols:
                cert_info = f" | Colonnes certification: {cert_cols}"
            
            print(f"   📊 Total: {total_count} enregistrements{status_info}{cert_info}")
            
            # Afficher quelques exemples si pas trop de données
            if total_count > 0 and total_count <= 100:
                cursor.execute(f"SELECT * FROM {table_name} LIMIT 3;")
                samples = cursor.fetchall()
                print(f"   📝 Échantillon (3 premiers):")
                col_names = [col[1] for col in columns]
                for i, sample in enumerate(samples):
                    print(f"     Ligne {i+1}:")
                    for j, value in enumerate(sample):
                        if j < len(col_names):
                            print(f"       {col_names[j]}: {value}")
            print()
        
        # 3. Vérifier spécifiquement les critères utilisés par le repository
        print("🎯 VÉRIFICATION DES CRITÈRES DU REPOSITORY")
        print("   Critères actuels: Status = 'draft' AND CertifiedAt IS NULL")
        
        for table_name in invoice_tables:
            # Vérifier si les colonnes nécessaires existent
            cursor.execute(f"PRAGMA table_info({table_name});")
            columns = cursor.fetchall()
            col_names = [col[1].lower() for col in columns]
            
            has_status = 'status' in col_names
            has_certified = any('certified' in col.lower() for col in col_names)
            
            print(f"\n   Table {table_name}:")
            print(f"     - Colonne Status: {'✅' if has_status else '❌'}")
            print(f"     - Colonne CertifiedAt: {'✅' if has_certified else '❌'}")
            
            if has_status:
                # Analyser les valeurs de status
                cursor.execute(f"SELECT DISTINCT Status FROM {table_name};")
                statuses = [row[0] for row in cursor.fetchall()]
                print(f"     - Valeurs Status: {statuses}")
                
                # Compter ceux qui matchent nos critères
                if has_certified:
                    cert_col = next((col[1] for col in columns if 'certified' in col[1].lower()), 'CertifiedAt')
                    query = f"SELECT COUNT(*) FROM {table_name} WHERE Status = 'draft' AND {cert_col} IS NULL"
                else:
                    query = f"SELECT COUNT(*) FROM {table_name} WHERE Status = 'draft'"
                
                try:
                    cursor.execute(query)
                    matching_count = cursor.fetchone()[0]
                    print(f"     - Factures matchant nos critères: {matching_count}")
                except Exception as e:
                    print(f"     - Erreur requête: {e}")
        
        # 4. Vérifier la configuration FNE
        print("\n⚙️ CONFIGURATION FNE")
        try:
            cursor.execute("SELECT name FROM sqlite_master WHERE name LIKE '%fne%' OR name LIKE '%Fne%';")
            fne_tables = cursor.fetchall()
            print(f"   Tables FNE: {[t[0] for t in fne_tables]}")
            
            if any('FneConfigurations' in t[0] for t in fne_tables):
                cursor.execute("SELECT COUNT(*) FROM FneConfigurations WHERE IsActive = 1;")
                active_configs = cursor.fetchone()[0]
                print(f"   Configurations actives: {active_configs}")
            
        except Exception as e:
            print(f"   Erreur vérification config: {e}")
        
        conn.close()
        
    except Exception as e:
        print(f"❌ Erreur générale: {e}")
    
    print("\n" + "=" * 70)

if __name__ == "__main__":
    main()