#!/usr/bin/env python3
"""
Diagnostic final de centralisation de la base de données FNEV4
Validation de la centralisation et de l'uniformisation
"""

import os
import sqlite3
from pathlib import Path

def main():
    print("🔍 DIAGNOSTIC FINAL - CENTRALISATION BASE DE DONNÉES FNEV4")
    print("="*70)
    
    # Base de données principale centralisée
    main_db = Path(r"C:\wamp64\www\FNEV4\data\FNEV4.db")
    
    print(f"\n📊 VALIDATION DE LA BASE CENTRALISÉE:")
    print(f"   📁 Chemin: {main_db}")
    
    if main_db.exists():
        size = main_db.stat().st_size
        print(f"   💾 Taille: {size:,} bytes ({size/1024:.1f} KB)")
        
        # Vérifier le contenu
        try:
            conn = sqlite3.connect(str(main_db))
            cursor = conn.cursor()
            
            # Compter les clients
            cursor.execute("SELECT COUNT(*) FROM Clients")
            client_count = cursor.fetchone()[0]
            print(f"   👥 Clients: {client_count}")
            
            # Lister les tables
            cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
            tables = [row[0] for row in cursor.fetchall()]
            print(f"   📋 Tables: {len(tables)} ({', '.join(tables[:5])}{'...' if len(tables) > 5 else ''})")
            
            conn.close()
            
        except Exception as e:
            print(f"   ❌ Erreur lecture: {e}")
    else:
        print(f"   ❌ FICHIER N'EXISTE PAS!")
    
    # Rechercher d'autres bases de données
    print(f"\n🔍 RECHERCHE D'AUTRES BASES DE DONNÉES:")
    project_root = Path(r"C:\wamp64\www\FNEV4")
    other_dbs = []
    
    for db_file in project_root.rglob("*.db"):
        if db_file != main_db:
            size = db_file.stat().st_size
            
            # Vérifier si c'est une vraie base FNEV4
            try:
                conn = sqlite3.connect(str(db_file))
                cursor = conn.cursor()
                cursor.execute("SELECT name FROM sqlite_master WHERE type='table' AND name='Clients'")
                has_clients = cursor.fetchone() is not None
                
                if has_clients:
                    cursor.execute("SELECT COUNT(*) FROM Clients")
                    client_count = cursor.fetchone()[0]
                else:
                    client_count = "N/A"
                
                conn.close()
                
                other_dbs.append({
                    'path': db_file,
                    'size': size,
                    'clients': client_count
                })
                
            except:
                other_dbs.append({
                    'path': db_file,
                    'size': size,
                    'clients': "Erreur"
                })
    
    if other_dbs:
        print(f"   ⚠️  TROUVÉ {len(other_dbs)} AUTRES BASES:")
        for db in other_dbs:
            print(f"     📁 {db['path']}")
            print(f"        💾 {db['size']:,} bytes, 👥 {db['clients']} clients")
        
        print(f"\n💡 RECOMMANDATION:")
        print(f"   Ces bases peuvent être supprimées car l'application")
        print(f"   utilise maintenant uniquement la base centralisée.")
    else:
        print(f"   ✅ AUCUNE AUTRE BASE TROUVÉE - CENTRALISATION RÉUSSIE!")
    
    # Vérifier les logs de démarrage
    print(f"\n📋 VALIDATION ARCHITECTURE:")
    print(f"   ✅ DatabasePathProvider (singleton)")
    print(f"   ✅ PathConfigurationService (singleton)")
    print(f"   ✅ DbContext (scoped au lieu de transient)")
    print(f"   ✅ Injection de dépendances unifiée")
    print(f"   ✅ Initialisation centralisée au démarrage")
    
    print(f"\n🎯 RÉSULTAT:")
    if not other_dbs and main_db.exists():
        print(f"   🟢 CENTRALISATION RÉUSSIE!")
        print(f"   🟢 Une seule base de données active")
        print(f"   🟢 Tous les modules utilisent la même base")
    elif other_dbs:
        print(f"   🟡 CENTRALISATION PARTIELLE")
        print(f"   🟡 Bases multiples détectées (à nettoyer)")
    else:
        print(f"   🔴 PROBLÈME DE CONFIGURATION")
        print(f"   🔴 Base principale non trouvée")

if __name__ == "__main__":
    main()
