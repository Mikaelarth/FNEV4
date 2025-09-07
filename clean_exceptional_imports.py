#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
FNEV4 - Nettoyage Import Exceptionnel
======================================

Ce script supprime les clients d'import exceptionnel existants 
pour permettre de tester la correction du mapping template.

Date: 7 Septembre 2025
"""

import sqlite3
import os
from datetime import datetime

def clean_exceptional_imports():
    """
    Supprime les clients d'import exceptionnel existants
    """
    print("🧹 NETTOYAGE IMPORT EXCEPTIONNEL")
    print("=" * 40)
    
    # Chemin de la base de données
    db_path = os.path.join("data", "FNEV4.db")
    
    if not os.path.exists(db_path):
        print(f"❌ Base de données non trouvée: {db_path}")
        return
    
    try:
        # Connexion à la base de données
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Compter les clients à supprimer
        cursor.execute("""
            SELECT COUNT(*) 
            FROM Clients 
            WHERE Notes LIKE '%Import exceptionnel%'
        """)
        
        count_before = cursor.fetchone()[0]
        print(f"📊 Clients d'import exceptionnel trouvés: {count_before}")
        
        if count_before == 0:
            print("✅ Aucun client à supprimer")
            return
        
        # Supprimer les clients d'import exceptionnel
        cursor.execute("""
            DELETE FROM Clients 
            WHERE Notes LIKE '%Import exceptionnel%'
        """)
        
        # Confirmer la suppression
        cursor.execute("""
            SELECT COUNT(*) 
            FROM Clients 
            WHERE Notes LIKE '%Import exceptionnel%'
        """)
        
        count_after = cursor.fetchone()[0]
        deleted_count = count_before - count_after
        
        # Valider les changements
        conn.commit()
        conn.close()
        
        print(f"🗑️  Supprimés: {deleted_count} clients")
        print(f"📊 Restants: {count_after} clients d'import exceptionnel")
        
        if count_after == 0:
            print("✅ Nettoyage terminé avec succès!")
            print()
            print("🚀 Vous pouvez maintenant tester le nouvel import exceptionnel")
            print("   avec le mapping template corrigé.")
        else:
            print("⚠️  Des clients d'import exceptionnel sont encore présents")
        
    except Exception as e:
        print(f"❌ Erreur lors du nettoyage: {e}")

if __name__ == "__main__":
    # Demander confirmation
    response = input("Voulez-vous supprimer tous les clients d'import exceptionnel ? (oui/non): ")
    if response.lower() in ['oui', 'o', 'yes', 'y']:
        clean_exceptional_imports()
    else:
        print("❌ Opération annulée")
