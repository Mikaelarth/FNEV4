#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
FNEV4 - Vérification Structure Table Clients
===========================================

Ce script vérifie la structure de la table Clients
et confirme la présence du champ DefaultPaymentMethod.
"""

import sqlite3
import os
import json
from datetime import datetime

def get_database_path():
    """Trouve le chemin de la base de données principale"""
    possible_paths = [
        "data/FNEV4.db",
        "src/FNEV4.Infrastructure/Data/FNEV4.db",
        "C:/wamp64/www/FNEV4/data/FNEV4.db",
        "C:/wamp64/www/FNEV4/src/FNEV4.Infrastructure/Data/FNEV4.db"
    ]
    
    for path in possible_paths:
        if os.path.exists(path) and os.path.getsize(path) > 0:
            return path
    
    return None

def analyze_clients_table():
    """Analyse la structure de la table Clients"""
    
    db_path = get_database_path()
    if not db_path:
        print("❌ Aucune base de données valide trouvée")
        return False
    
    print(f"🔍 Analyse de la base: {db_path}")
    print(f"📏 Taille: {os.path.getsize(db_path)} bytes")
    
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # 1. Vérifier que la table Clients existe
        cursor.execute("""
            SELECT name FROM sqlite_master 
            WHERE type='table' AND name='Clients'
        """)
        
        if not cursor.fetchone():
            print("❌ Table Clients non trouvée")
            return False
        
        print("✅ Table Clients trouvée")
        
        # 2. Obtenir la structure de la table
        cursor.execute("PRAGMA table_info(Clients)")
        columns = cursor.fetchall()
        
        print(f"\n📊 Structure de la table Clients ({len(columns)} colonnes):")
        print("-" * 80)
        
        payment_method_found = False
        
        for col in columns:
            cid, name, col_type, not_null, default_val, pk = col
            null_str = "NOT NULL" if not_null else "NULL"
            default_str = f"DEFAULT {default_val}" if default_val else ""
            pk_str = "PRIMARY KEY" if pk else ""
            
            print(f"  {name:<25} {col_type:<15} {null_str:<10} {default_str:<15} {pk_str}")
            
            if name == 'DefaultPaymentMethod':
                payment_method_found = True
                print(f"  ✅ TROUVÉ: DefaultPaymentMethod")
        
        # 3. Vérifier spécifiquement DefaultPaymentMethod
        if payment_method_found:
            print(f"\n✅ SUCCÈS: Le champ DefaultPaymentMethod est présent dans la table Clients")
            
            # 4. Compter les clients existants
            cursor.execute("SELECT COUNT(*) FROM Clients")
            client_count = cursor.fetchone()[0]
            print(f"📊 Nombre de clients en base: {client_count}")
            
            if client_count > 0:
                # 5. Vérifier les valeurs du champ DefaultPaymentMethod
                cursor.execute("""
                    SELECT DefaultPaymentMethod, COUNT(*) 
                    FROM Clients 
                    GROUP BY DefaultPaymentMethod
                """)
                payment_stats = cursor.fetchall()
                
                print(f"\n💳 Répartition des moyens de paiement:")
                for payment_method, count in payment_stats:
                    print(f"   - {payment_method or 'NULL'}: {count} client(s)")
            
            # 6. Vérifier les index
            cursor.execute("""
                SELECT name, sql FROM sqlite_master 
                WHERE type='index' AND tbl_name='Clients'
            """)
            indexes = cursor.fetchall()
            
            print(f"\n🔍 Index sur la table Clients ({len(indexes)}):")
            for idx_name, idx_sql in indexes:
                if idx_name and not idx_name.startswith('sqlite_'):
                    print(f"   - {idx_name}")
                    if 'DefaultPaymentMethod' in (idx_sql or ''):
                        print(f"     ✅ Index sur DefaultPaymentMethod trouvé")
            
        else:
            print(f"\n❌ ERREUR: Le champ DefaultPaymentMethod est ABSENT de la table Clients")
            print(f"🔧 Action requise: Recréer la base ou appliquer la migration")
            return False
        
        conn.close()
        return True
        
    except Exception as e:
        print(f"❌ Erreur lors de l'analyse: {e}")
        return False

def test_payment_method_values():
    """Test des valeurs acceptées pour DefaultPaymentMethod"""
    
    valid_payments = ["cash", "card", "mobile-money", "bank-transfer", "check", "credit"]
    
    print(f"\n🧪 TEST: Valeurs acceptées pour DefaultPaymentMethod")
    print("-" * 60)
    
    for payment in valid_payments:
        print(f"   ✅ {payment:<15} (API DGI conforme)")
    
    print(f"\n📋 Ces valeurs doivent être utilisées dans:")
    print(f"   - Import Excel (colonne Moyen_Paiement)")
    print(f"   - Validation ClientImportModelDgi")
    print(f"   - Stockage en base (DefaultPaymentMethod)")
    print(f"   - Envoi API DGI")

def create_verification_report():
    """Crée un rapport de vérification"""
    
    report = {
        "verification_date": datetime.now().isoformat(),
        "database_analyzed": get_database_path(),
        "verification_type": "Structure table Clients + DefaultPaymentMethod",
        "status": "ANALYSED",
        "findings": {
            "table_exists": None,
            "column_exists": None,
            "client_count": None,
            "payment_methods_found": []
        }
    }
    
    # Exécuter l'analyse
    success = analyze_clients_table()
    report["findings"]["analysis_success"] = success
    
    # Sauvegarder le rapport
    report_file = "verification_defaultpaymentmethod.json"
    with open(report_file, 'w', encoding='utf-8') as f:
        json.dump(report, f, indent=2, ensure_ascii=False)
    
    print(f"\n📝 Rapport sauvegardé: {report_file}")
    return report

if __name__ == "__main__":
    print("🔍 FNEV4 - Vérification Structure Table Clients")
    print("=" * 60)
    
    # Analyse principale
    success = analyze_clients_table()
    
    # Test des valeurs
    test_payment_method_values()
    
    # Rapport
    create_verification_report()
    
    print("\n" + "=" * 60)
    if success:
        print("✅ VÉRIFICATION RÉUSSIE - DefaultPaymentMethod configuré correctement")
    else:
        print("❌ VÉRIFICATION ÉCHOUÉE - Action requise")
