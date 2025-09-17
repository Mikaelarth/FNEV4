#!/usr/bin/env python3
"""
Analyse complète de la base de données FNEV4
Examine toute la structure et les données réelles
"""

import sqlite3
import sys
from pathlib import Path
import json

def analyze_complete_database():
    """
    Analyse complète de la base de données FNEV4
    """
    
    # Chemins possibles pour la base de données
    db_paths = [
        Path("data/FNEV4.db"),
        Path("FNEV4/data/FNEV4.db"),
        Path("src/FNEV4.Presentation/data/FNEV4.db"),
        Path("../data/FNEV4.db")
    ]
    
    db_path = None
    for path in db_paths:
        if path.exists():
            db_path = path
            break
    
    if not db_path:
        print("❌ Base de données FNEV4.db introuvable")
        return
    
    print(f"🔍 ANALYSE COMPLÈTE DE LA BASE DE DONNÉES")
    print(f"📍 Chemin: {db_path}")
    print("=" * 100)
    
    try:
        conn = sqlite3.connect(str(db_path))
        cursor = conn.cursor()
        
        # 1. Liste de toutes les tables
        cursor.execute("""
            SELECT name, type, sql 
            FROM sqlite_master 
            WHERE type IN ('table', 'view') 
            ORDER BY name
        """)
        
        tables = cursor.fetchall()
        
        print(f"\n📋 TABLES ET VUES DANS LA BASE ({len(tables)} total)")
        print("=" * 100)
        
        for i, (table_name, table_type, sql) in enumerate(tables, 1):
            print(f"{i:2d}. {table_type.upper()}: {table_name}")
            
            # Compter les enregistrements
            try:
                cursor.execute(f"SELECT COUNT(*) FROM [{table_name}]")
                count = cursor.fetchone()[0]
                print(f"    📊 Enregistrements: {count:,}")
            except:
                print(f"    📊 Enregistrements: N/A")
        
        print("\n" + "=" * 100)
        
        # 2. Analyse détaillée des tables principales
        main_tables = [
            'FneInvoices', 'FneInvoiceItems', 'FneConfigurations', 
            'Clients', 'Companies', 'VatTypes', 'ImportSessions',
            'FneApiLogs'
        ]
        
        for table_name in main_tables:
            if any(t[0] == table_name for t in tables):
                print(f"\n🔍 ANALYSE DÉTAILLÉE: {table_name}")
                print("-" * 80)
                
                # Structure de la table
                cursor.execute(f"PRAGMA table_info([{table_name}])")
                columns = cursor.fetchall()
                
                print(f"📋 STRUCTURE ({len(columns)} colonnes):")
                for col in columns:
                    cid, name, data_type, not_null, default, pk = col
                    pk_text = " 🔑 PK" if pk else ""
                    null_text = " NOT NULL" if not_null else ""
                    default_text = f" DEFAULT {default}" if default else ""
                    print(f"   {name} ({data_type}){null_text}{default_text}{pk_text}")
                
                # Comptage des enregistrements
                cursor.execute(f"SELECT COUNT(*) FROM [{table_name}]")
                count = cursor.fetchone()[0]
                print(f"\n📊 NOMBRE D'ENREGISTREMENTS: {count:,}")
                
                if count > 0 and count <= 10:
                    # Afficher tous les enregistrements si peu nombreux
                    print(f"\n📄 TOUS LES ENREGISTREMENTS:")
                    cursor.execute(f"SELECT * FROM [{table_name}]")
                    rows = cursor.fetchall()
                    
                    col_names = [col[1] for col in columns]
                    
                    for i, row in enumerate(rows, 1):
                        print(f"\n   📝 Enregistrement #{i}:")
                        for col_name, value in zip(col_names, row):
                            display_value = str(value)[:100] + "..." if value and len(str(value)) > 100 else value
                            print(f"      {col_name}: {display_value}")
                
                elif count > 0:
                    # Afficher quelques échantillons
                    print(f"\n📄 ÉCHANTILLONS (5 premiers):")
                    cursor.execute(f"SELECT * FROM [{table_name}] LIMIT 5")
                    rows = cursor.fetchall()
                    
                    col_names = [col[1] for col in columns]
                    
                    for i, row in enumerate(rows, 1):
                        print(f"\n   📝 Enregistrement #{i}:")
                        for col_name, value in zip(col_names, row):
                            display_value = str(value)[:100] + "..." if value and len(str(value)) > 100 else value
                            print(f"      {col_name}: {display_value}")
        
        # 3. Analyse spécifique des factures FNE
        print(f"\n" + "=" * 100)
        print("🎯 ANALYSE SPÉCIFIQUE DES FACTURES FNE")
        print("=" * 100)
        
        cursor.execute("SELECT name FROM sqlite_master WHERE name LIKE '%nvoice%' OR name LIKE '%acture%'")
        invoice_tables = cursor.fetchall()
        
        print(f"📋 Tables liées aux factures trouvées: {len(invoice_tables)}")
        for table in invoice_tables:
            print(f"   • {table[0]}")
        
        # Analyser FneInvoices en détail
        if any(t[0] == 'FneInvoices' for t in tables):
            print(f"\n🔍 ANALYSE POUSSÉE: FneInvoices")
            print("-" * 50)
            
            # Statistiques générales
            cursor.execute("""
                SELECT 
                    COUNT(*) as total,
                    COUNT(CASE WHEN ClientDisplayName IS NOT NULL THEN 1 END) as with_client_name,
                    COUNT(CASE WHEN IsCertified = 1 THEN 1 END) as certified,
                    MIN(InvoiceDate) as earliest_date,
                    MAX(InvoiceDate) as latest_date,
                    SUM(TotalAmountTTC) as total_amount
                FROM FneInvoices
            """)
            
            stats = cursor.fetchone()
            total, with_client, certified, earliest, latest, amount = stats
            
            print(f"📊 STATISTIQUES:")
            print(f"   Total factures: {total:,}")
            print(f"   Avec nom client: {with_client:,} ({with_client/total*100:.1f}%)")
            print(f"   Certifiées: {certified:,} ({certified/total*100:.1f}%)")
            print(f"   Période: {earliest} → {latest}")
            print(f"   Montant total: {amount:,.2f} FCFA")
            
            # Types de données client
            cursor.execute("""
                SELECT 
                    CASE 
                        WHEN ClientDisplayName IS NOT NULL THEN 'ClientDisplayName'
                        WHEN CommercialMessage LIKE 'Client:%' THEN 'CommercialMessage'
                        ELSE 'Aucun'
                    END as source_client,
                    COUNT(*) as count
                FROM FneInvoices
                GROUP BY 1
                ORDER BY 2 DESC
            """)
            
            client_sources = cursor.fetchall()
            print(f"\n📋 SOURCES DES DONNÉES CLIENT:")
            for source, count in client_sources:
                print(f"   {source}: {count:,} factures ({count/total*100:.1f}%)")
            
            # Exemples de données client problématiques
            cursor.execute("""
                SELECT InvoiceNumber, ClientDisplayName, CommercialMessage, ClientNcc
                FROM FneInvoices 
                WHERE ClientDisplayName IS NULL OR ClientDisplayName = ''
                ORDER BY InvoiceDate DESC
                LIMIT 5
            """)
            
            problematic = cursor.fetchall()
            if problematic:
                print(f"\n⚠️ FACTURES AVEC DONNÉES CLIENT MANQUANTES:")
                for inv_num, client_name, commercial, ncc in problematic:
                    print(f"   • {inv_num}: ClientDisplayName='{client_name}', CommercialMessage='{commercial}', NCC='{ncc}'")
        
        # 4. Relations entre tables
        print(f"\n" + "=" * 100)
        print("🔗 ANALYSE DES RELATIONS")
        print("=" * 100)
        
        # Vérifier les clés étrangères
        for table_name, _, _ in tables:
            cursor.execute(f"PRAGMA foreign_key_list([{table_name}])")
            fks = cursor.fetchall()
            if fks:
                print(f"\n🔗 Clés étrangères de {table_name}:")
                for fk in fks:
                    print(f"   {fk[3]} → {fk[2]}.{fk[4]}")
        
        # 5. Index et performances
        print(f"\n" + "=" * 100)
        print("⚡ INDICES ET PERFORMANCES")
        print("=" * 100)
        
        cursor.execute("""
            SELECT name, tbl_name, sql 
            FROM sqlite_master 
            WHERE type = 'index' AND name NOT LIKE 'sqlite_%'
            ORDER BY tbl_name, name
        """)
        
        indexes = cursor.fetchall()
        print(f"📋 INDICES PERSONNALISÉS: {len(indexes)}")
        
        current_table = None
        for idx_name, table_name, sql in indexes:
            if table_name != current_table:
                print(f"\n   📊 {table_name}:")
                current_table = table_name
            print(f"      • {idx_name}")
    
    except sqlite3.Error as e:
        print(f"❌ Erreur base de données: {e}")
    
    except Exception as e:
        print(f"❌ Erreur inattendue: {e}")
    
    finally:
        if 'conn' in locals():
            conn.close()

if __name__ == "__main__":
    print("🔍 ANALYSE COMPLÈTE DE LA BASE DE DONNÉES FNEV4")
    print("=" * 100)
    analyze_complete_database()
    print(f"\n✅ Analyse terminée")