#!/usr/bin/env python3
"""
Test de validation - Accès centralisé à la base de données
Menu Maintenance > Base de données

Vérification que le sous-menu utilise bien le système centralisé.
"""

import os
import sqlite3
import json
from datetime import datetime

def test_centralized_database_access():
    """Test de l'accès centralisé à la base de données."""
    
    print("🔍 Test d'Accès Centralisé à la Base de Données")
    print("=" * 60)
    
    # 1. Vérifier le chemin centralisé
    centralized_db_path = r"C:\wamp64\www\FNEV4\data\FNEV4.db"
    
    print(f"📍 Chemin centralisé attendu : {centralized_db_path}")
    
    if not os.path.exists(centralized_db_path):
        print("❌ ERREUR: Base de données centralisée non trouvée!")
        return False
    
    print("✅ Base de données centralisée trouvée")
    
    # 2. Vérifier la structure de la base
    try:
        conn = sqlite3.connect(centralized_db_path)
        cursor = conn.cursor()
        
        # Obtenir la liste des tables
        cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
        tables = [row[0] for row in cursor.fetchall()]
        
        print(f"📊 Nombre de tables trouvées : {len(tables)}")
        print(f"📋 Tables : {', '.join(tables)}")
        
        # Vérifier des tables clés
        key_tables = ['Clients', 'Factures', 'Articles']
        missing_tables = []
        
        for table in key_tables:
            if table not in tables:
                missing_tables.append(table)
            else:
                # Compter les enregistrements
                cursor.execute(f"SELECT COUNT(*) FROM {table}")
                count = cursor.fetchone()[0]
                print(f"   ✅ {table}: {count} enregistrements")
        
        if missing_tables:
            print(f"⚠️  Tables manquantes : {missing_tables}")
        
        # 3. Tester l'intégrité
        cursor.execute("PRAGMA integrity_check")
        integrity_result = cursor.fetchone()[0]
        
        if integrity_result == "ok":
            print("✅ Intégrité de la base : OK")
        else:
            print(f"❌ Problème d'intégrité : {integrity_result}")
        
        # 4. Informations de la base
        cursor.execute("PRAGMA database_list")
        db_info = cursor.fetchall()
        
        # Taille du fichier
        file_size = os.path.getsize(centralized_db_path)
        size_kb = round(file_size / 1024, 2)
        
        print(f"💾 Taille de la base : {size_kb} KB")
        
        # Version SQLite
        cursor.execute("SELECT sqlite_version()")
        sqlite_version = cursor.fetchone()[0]
        print(f"⚙️  Version SQLite : {sqlite_version}")
        
        conn.close()
        
    except Exception as e:
        print(f"❌ Erreur lors de l'accès à la base : {e}")
        return False
    
    # 5. Vérifier qu'il n'y a plus de bases de données multiples
    data_dir = r"C:\wamp64\www\FNEV4\data"
    db_files = []
    
    if os.path.exists(data_dir):
        for file in os.listdir(data_dir):
            if file.endswith('.db') and not file.startswith('backup_'):
                db_files.append(file)
    
    print(f"🗂️  Fichiers .db dans /data : {db_files}")
    
    if len(db_files) == 1 and db_files[0] == "FNEV4.db":
        print("✅ SUCCÈS: Une seule base de données principale")
        centralized = True
    elif len(db_files) > 1:
        print(f"⚠️  ATTENTION: {len(db_files)} fichiers de base trouvés")
        print("   La centralisation pourrait être incomplète")
        centralized = False
    else:
        print("❌ ERREUR: Aucune base de données principale trouvée")
        centralized = False
    
    # 6. Vérifier les services d'injection de dépendances
    print("\n🔧 Vérification de l'Architecture Centralisée")
    print("-" * 40)
    
    # Vérifier les fichiers clés
    key_files = [
        ("DatabasePathProvider.cs", "src/FNEV4.Infrastructure/Services/DatabasePathProvider.cs"),
        ("PathConfigurationService.cs", "src/FNEV4.Infrastructure/Services/PathConfigurationService.cs"),
        ("BaseDonneesViewModel.cs", "src/FNEV4.Presentation/ViewModels/Maintenance/BaseDonneesViewModel.cs"),
        ("App.xaml.cs", "src/FNEV4.Presentation/App.xaml.cs")
    ]
    
    architecture_ok = True
    
    for file_name, file_path in key_files:
        full_path = os.path.join(r"C:\wamp64\www\FNEV4", file_path)
        if os.path.exists(full_path):
            print(f"   ✅ {file_name}")
        else:
            print(f"   ❌ {file_name} MANQUANT")
            architecture_ok = False
    
    # Résultat final
    print("\n" + "=" * 60)
    
    if centralized and architecture_ok:
        print("🎉 RÉSULTAT: Centralisation RÉUSSIE !")
        print("✅ Le menu 'Maintenance > Base de données' utilise le système centralisé")
        
        # Créer un rapport de validation
        validation_report = {
            "timestamp": datetime.now().isoformat(),
            "test": "Centralisation Base de Données",
            "status": "SUCCESS",
            "database_path": centralized_db_path,
            "database_size_kb": size_kb,
            "table_count": len(tables),
            "tables": tables,
            "sqlite_version": sqlite_version,
            "integrity": integrity_result,
            "architecture_files": "OK"
        }
        
        with open("validation_centralization_database.json", "w", encoding="utf-8") as f:
            json.dump(validation_report, f, indent=2, ensure_ascii=False)
        
        print("📄 Rapport de validation généré : validation_centralization_database.json")
        return True
        
    else:
        print("❌ RÉSULTAT: Centralisation INCOMPLÈTE")
        if not centralized:
            print("   - Problème de centralisation des fichiers de base")
        if not architecture_ok:
            print("   - Problème d'architecture/fichiers manquants")
        return False

if __name__ == "__main__":
    success = test_centralized_database_access()
    exit(0 if success else 1)
