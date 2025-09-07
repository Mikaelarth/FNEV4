#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
FNEV4 - Nettoyage Complet Base de Données
==========================================

Script pour nettoyer les bases de données dispersées ET vider la table Clients
pour permettre un redémarrage propre avec les corrections de mapping template.

Date: 7 Septembre 2025
"""

import os
import glob
import sqlite3
import shutil
from pathlib import Path
from datetime import datetime

def backup_database():
    """
    Crée une sauvegarde de la base avant nettoyage
    """
    db_path = r"C:\wamp64\www\FNEV4\data\FNEV4.db"
    if not os.path.exists(db_path):
        return None
    
    # Créer le dossier de sauvegarde s'il n'existe pas
    backup_dir = r"C:\wamp64\www\FNEV4\data\Backup"
    os.makedirs(backup_dir, exist_ok=True)
    
    # Nom de la sauvegarde avec timestamp
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    backup_path = os.path.join(backup_dir, f"FNEV4_backup_{timestamp}.db")
    
    try:
        shutil.copy2(db_path, backup_path)
        return backup_path
    except Exception as e:
        print(f"⚠️  Erreur lors de la sauvegarde: {e}")
        return None

def clean_clients_table():
    """
    Vide complètement la table Clients
    """
    print("\n🧹 NETTOYAGE COMPLET TABLE CLIENTS")
    print("=" * 45)
    
    # Chemin de la base de données principale
    db_path = r"C:\wamp64\www\FNEV4\data\FNEV4.db"
    
    if not os.path.exists(db_path):
        print(f"❌ Base de données non trouvée: {db_path}")
        return False
    
    try:
        # Sauvegarde avant nettoyage
        print("💾 Création d'une sauvegarde...")
        backup_path = backup_database()
        
        if backup_path:
            print(f"✅ Sauvegarde créée: {os.path.basename(backup_path)}")
        else:
            print("⚠️  Impossible de créer la sauvegarde")
            return False
        
        # Connexion à la base de données
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Compter les clients existants
        cursor.execute("SELECT COUNT(*) FROM Clients")
        total_clients = cursor.fetchone()[0]
        
        print(f"📊 Clients actuels dans la base: {total_clients}")
        
        if total_clients == 0:
            print("ℹ️  La table Clients est déjà vide")
            conn.close()
            return True
        
        # Analyser les types de clients
        cursor.execute("""
            SELECT 
                CASE 
                    WHEN Notes LIKE '%Import exceptionnel%' THEN 'Import Exceptionnel'
                    ELSE 'Clients Normaux'
                END as type,
                COUNT(*) as count
            FROM Clients 
            GROUP BY type
        """)
        
        breakdown = cursor.fetchall()
        print("\n📈 Répartition actuelle:")
        for client_type, count in breakdown:
            print(f"   {client_type}: {count} clients")
        
        print(f"\n🗑️  SUPPRESSION DE TOUS LES CLIENTS ({total_clients} clients)")
        
        # Supprimer tous les clients
        cursor.execute("DELETE FROM Clients")
        
        # Vérifier la suppression
        cursor.execute("SELECT COUNT(*) FROM Clients")
        remaining_clients = cursor.fetchone()[0]
        
        # Reset de l'auto-increment (optionnel)
        cursor.execute("DELETE FROM sqlite_sequence WHERE name='Clients'")
        
        # Valider les changements
        conn.commit()
        conn.close()
        
        print(f"✅ Suppression terminée!")
        print(f"   - Clients supprimés: {total_clients}")
        print(f"   - Clients restants: {remaining_clients}")
        print(f"   - Auto-increment remis à zéro")
        
        if remaining_clients == 0:
            print("\n🎉 SUCCÈS! La table Clients est maintenant vide.")
            print("🚀 Vous pouvez relancer l'application et tester les imports.")
            print("   Tous les nouveaux clients utiliseront le mapping corrigé!")
        else:
            print(f"\n⚠️  ATTENTION! {remaining_clients} clients sont encore présents.")
        
        return remaining_clients == 0
        
    except Exception as e:
        print(f"❌ Erreur lors du nettoyage: {e}")
        return False

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
    
    print("\n🏁 Nettoyage des bases dispersées terminé !")

def analyze_database_structure():
    """
    Affiche la structure de la base pour vérification
    """
    print("\n🔍 ANALYSE DE LA STRUCTURE")
    print("=" * 30)
    
    db_path = r"C:\wamp64\www\FNEV4\data\FNEV4.db"
    
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Lister les tables
        cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
        tables = cursor.fetchall()
        
        print("📋 Tables dans la base:")
        for table in tables:
            cursor.execute(f"SELECT COUNT(*) FROM {table[0]}")
            count = cursor.fetchone()[0]
            print(f"   {table[0]}: {count} enregistrements")
        
        conn.close()
        
    except Exception as e:
        print(f"❌ Erreur lors de l'analyse: {e}")

if __name__ == "__main__":
    print("🧹 FNEV4 - NETTOYAGE COMPLET")
    print("=" * 40)
    print("Options disponibles:")
    print("1. Nettoyer les bases dispersées")
    print("2. Vider la table Clients")
    print("3. Les deux")
    print()
    
    choice = input("Votre choix (1/2/3): ").strip()
    
    if choice == "1":
        clean_all_databases()
    elif choice == "2":
        print("⚠️  ATTENTION: Cette opération va supprimer TOUS les clients!")
        print("   Une sauvegarde sera créée automatiquement.")
        print()
        response = input("Voulez-vous vider complètement la table Clients ? (oui/non): ")
        
        if response.lower() in ['oui', 'o', 'yes', 'y']:
            success = clean_clients_table()
            if success:
                analyze_database_structure()
                print("\n" + "="*50)
                print("🎯 PROCHAINES ÉTAPES:")
                print("1. Relancer l'application FNEV4")
                print("2. Tester l'import exceptionnel avec le mapping corrigé")
                print("3. Vérifier les templates avec verify_template_mapping.py")
                print("="*50)
        else:
            print("❌ Opération annulée")
    elif choice == "3":
        clean_all_databases()
        print("\n" + "="*50)
        print("⚠️  ATTENTION: Voulez-vous aussi vider la table Clients?")
        print("   Une sauvegarde sera créée automatiquement.")
        print()
        response = input("Vider la table Clients ? (oui/non): ")
        
        if response.lower() in ['oui', 'o', 'yes', 'y']:
            success = clean_clients_table()
            if success:
                analyze_database_structure()
                print("\n" + "="*50)
                print("🎯 PROCHAINES ÉTAPES:")
                print("1. Relancer l'application FNEV4")
                print("2. Tester l'import exceptionnel avec le mapping corrigé")
                print("3. Vérifier les templates avec verify_template_mapping.py")
                print("="*50)
    else:
        print("❌ Choix invalide")
        analyze_database_structure()
