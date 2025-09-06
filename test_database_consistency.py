#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Test pour vérifier que la base de données n'est plus dispersée
quelque soit la façon de lancer le programme
"""

import os
import subprocess
import time

def test_database_path_consistency():
    """Test la cohérence du chemin de base de données depuis différents répertoires"""
    
    print("🧪 TEST DE NON-DISPERSION DE LA BASE DE DONNÉES")
    print("=" * 60)
    
    # Définir les différents répertoires de lancement
    test_directories = [
        r"C:\wamp64\www\FNEV4",  # Racine du projet
        r"C:\wamp64\www\FNEV4\src\FNEV4.Presentation",  # Dossier de présentation
        r"C:\wamp64\www\FNEV4\src\FNEV4.Presentation\bin\Debug\net8.0-windows",  # Dossier de build
        r"C:\wamp64\www",  # Parent du projet
    ]
    
    expected_db_path = r"C:\wamp64\www\FNEV4\data\FNEV4.db"
    
    print(f"🎯 Chemin de base attendu: {expected_db_path}")
    print()
    
    for i, test_dir in enumerate(test_directories, 1):
        print(f"📂 TEST {i}: Lancement depuis {test_dir}")
        
        if not os.path.exists(test_dir):
            print(f"   ⚠️  Répertoire inexistant, création...")
            os.makedirs(test_dir, exist_ok=True)
        
        # Simuler la logique du PathConfigurationService
        current_dir = test_dir
        found_root = None
        
        # Remonter pour trouver FNEV4.sln
        while current_dir and current_dir != os.path.dirname(current_dir):
            sln_path = os.path.join(current_dir, "FNEV4.sln")
            if os.path.exists(sln_path):
                found_root = current_dir
                break
            current_dir = os.path.dirname(current_dir)
        
        # Fallback si pas trouvé
        if not found_root:
            fallback = r"C:\wamp64\www\FNEV4"
            if os.path.exists(fallback):
                found_root = fallback
        
        if found_root:
            computed_db_path = os.path.join(found_root, "data", "FNEV4.db")
            print(f"   📁 Racine trouvée: {found_root}")
            print(f"   🗄️  Base calculée: {computed_db_path}")
            
            if computed_db_path == expected_db_path:
                print(f"   ✅ SUCCÈS: Chemin cohérent")
            else:
                print(f"   ❌ ÉCHEC: Chemin incohérent !")
                print(f"      Attendu: {expected_db_path}")
                print(f"      Obtenu:  {computed_db_path}")
        else:
            print(f"   ❌ ÉCHEC: Impossible de trouver la racine")
        
        print()
    
    # Vérifier l'état actuel
    print("🔍 ÉTAT ACTUEL DES BASES DE DONNÉES")
    print("=" * 40)
    
    # Chercher tous les fichiers .db
    db_files = []
    for root, dirs, files in os.walk(r"C:\wamp64\www\FNEV4"):
        for file in files:
            if file.endswith('.db') and 'FNEV4' in file:
                db_path = os.path.join(root, file)
                if 'Backup' not in db_path:  # Ignorer les sauvegardes
                    db_files.append(db_path)
    
    print(f"📊 {len(db_files)} base(s) de données trouvée(s):")
    for db_file in db_files:
        size = os.path.getsize(db_file) if os.path.exists(db_file) else 0
        mtime = os.path.getmtime(db_file) if os.path.exists(db_file) else 0
        mtime_str = time.strftime('%Y-%m-%d %H:%M:%S', time.localtime(mtime))
        print(f"   📁 {db_file}")
        print(f"      📏 {size:,} bytes • 🕒 {mtime_str}")
    
    if len(db_files) == 1 and db_files[0] == expected_db_path:
        print("\n✅ PARFAIT: Une seule base centralisée !")
    elif len(db_files) == 1:
        print(f"\n⚠️  Une seule base mais mauvais emplacement:")
        print(f"   Trouvé: {db_files[0]}")
        print(f"   Attendu: {expected_db_path}")
    else:
        print(f"\n❌ PROBLÈME: {len(db_files)} bases trouvées (dispersion !)")
    
    print("\n🏁 Test terminé !")

if __name__ == "__main__":
    test_database_path_consistency()
