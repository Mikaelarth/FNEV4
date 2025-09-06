#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Script pour nettoyer TOUTES les bases de données dispersées
"""

import os
import glob
from pathlib import Path

def clean_all_databases():
    """Supprime toutes les bases dispersées sauf la principale"""
    
    print("🧹 NETTOYAGE COMPLET DES BASES DE DONNÉES FNEV4")
    print("=" * 60)
    
    # Base principale à conserver
    main_db = r"C:\wamp64\www\FNEV4\data\FNEV4.db"
    
    # Rechercher tous les fichiers .db
    search_patterns = [
        r"C:\wamp64\www\FNEV4\**\*.db",
    ]
    
    all_db_files = []
    for pattern in search_patterns:
        files = glob.glob(pattern, recursive=True)
        all_db_files.extend(files)
    
    # Filtrer et catégoriser
    to_keep = []
    to_delete = []
    backups = []
    
    for db_file in all_db_files:
        db_path = Path(db_file)
        
        # Normaliser le chemin
        normalized_path = str(db_path.resolve())
        main_normalized = str(Path(main_db).resolve())
        
        if normalized_path == main_normalized:
            to_keep.append(db_file)
        elif any(folder in str(db_path).lower() for folder in ['backup', 'archive']):
            backups.append(db_file)
        else:
            to_delete.append(db_file)
    
    print(f"🔍 ANALYSE:")
    print(f"   📁 Base principale: {len(to_keep)} fichier(s)")
    print(f"   💾 Sauvegardes: {len(backups)} fichier(s)")
    print(f"   🗑️  À supprimer: {len(to_delete)} fichier(s)")
    print()
    
    # Afficher la base principale
    if to_keep:
        print("✅ BASE PRINCIPALE (À CONSERVER):")
        for db in to_keep:
            size = os.path.getsize(db)
            print(f"   📁 {db} ({size:,} bytes)")
        print()
    
    # Afficher les sauvegardes
    if backups:
        print("💾 SAUVEGARDES (À CONSERVER):")
        for db in backups:
            size = os.path.getsize(db)
            print(f"   📁 {db} ({size:,} bytes)")
        print()
    
    # Afficher et supprimer les bases dispersées
    if to_delete:
        print("🗑️  BASES DISPERSÉES (À SUPPRIMER):")
        for db in to_delete:
            try:
                size = os.path.getsize(db)
                print(f"   ❌ {db} ({size:,} bytes)")
                os.remove(db)
                print(f"      ✅ Supprimé !")
            except Exception as e:
                print(f"      ❌ Erreur: {e}")
        print()
    else:
        print("✅ Aucune base dispersée trouvée !")
    
    # Vérification finale
    print("🔍 VÉRIFICATION FINALE:")
    final_dbs = []
    for pattern in search_patterns:
        files = glob.glob(pattern, recursive=True)
        for f in files:
            if not any(folder in str(f).lower() for folder in ['backup', 'archive']):
                final_dbs.append(f)
    
    if len(final_dbs) == 1 and str(Path(final_dbs[0]).resolve()) == str(Path(main_db).resolve()):
        print("✅ PARFAIT: Une seule base principale reste !")
        print(f"   📁 {final_dbs[0]}")
    else:
        print(f"⚠️  {len(final_dbs)} base(s) actives trouvées:")
        for db in final_dbs:
            print(f"   📁 {db}")
    
    print("\n🏁 Nettoyage terminé !")

if __name__ == "__main__":
    clean_all_databases()
