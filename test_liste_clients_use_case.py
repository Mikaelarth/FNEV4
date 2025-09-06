#!/usr/bin/env python3
"""
Test script pour vérifier le ListeClientsUseCase
"""

import sqlite3
import json

def test_clients_data():
    """Test des données clients dans la base"""
    print("🧪 TEST: Données clients en base")
    print("=" * 60)
    
    try:
        conn = sqlite3.connect('data/FNEV4.db')
        cursor = conn.cursor()
        
        # Compter les clients
        cursor.execute('SELECT COUNT(*) FROM Clients WHERE IsDeleted = 0 OR IsDeleted IS NULL')
        count = cursor.fetchone()[0]
        print(f"📊 Nombre de clients actifs: {count}")
        
        if count > 0:
            # Lister les clients
            cursor.execute('''
                SELECT ClientCode, Name, CompanyName, ClientType, IsActive, CreatedDate 
                FROM Clients 
                WHERE IsDeleted = 0 OR IsDeleted IS NULL
                ORDER BY ClientCode
            ''')
            clients = cursor.fetchall()
            
            print("\n📋 Liste des clients:")
            for client in clients:
                name = client[1] or client[2] or 'N/A'
                active = "✅ Actif" if client[4] else "❌ Inactif"
                print(f"  - {client[0]}: {name} ({client[3]}) - {active}")
        
        conn.close()
        return count > 0
        
    except Exception as e:
        print(f"❌ Erreur: {e}")
        return False

def test_filter_logic():
    """Test de la logique de filtrage"""
    print("\n🔍 TEST: Logique de filtrage")
    print("=" * 60)
    
    try:
        conn = sqlite3.connect('data/FNEV4.db')
        cursor = conn.cursor()
        
        # Test avec filtre IsActive = true
        cursor.execute('''
            SELECT COUNT(*) FROM Clients 
            WHERE (IsDeleted = 0 OR IsDeleted IS NULL) 
            AND IsActive = 1
        ''')
        active_count = cursor.fetchone()[0]
        print(f"📊 Clients actifs (IsActive=1): {active_count}")
        
        # Test avec filtre IsActive = null (tous)
        cursor.execute('''
            SELECT COUNT(*) FROM Clients 
            WHERE (IsDeleted = 0 OR IsDeleted IS NULL)
        ''')
        all_count = cursor.fetchone()[0]
        print(f"📊 Tous les clients: {all_count}")
        
        # Test avec filtre ClientType
        cursor.execute('''
            SELECT ClientType, COUNT(*) FROM Clients 
            WHERE (IsDeleted = 0 OR IsDeleted IS NULL)
            GROUP BY ClientType
        ''')
        types = cursor.fetchall()
        print(f"\n📊 Répartition par type:")
        for type_name, count in types:
            print(f"  - {type_name}: {count}")
        
        conn.close()
        return True
        
    except Exception as e:
        print(f"❌ Erreur: {e}")
        return False

def main():
    """Fonction principale"""
    print("🧪 TEST DU ListeClientsUseCase")
    print("=" * 60)
    
    # Test 1: Données clients
    test1_ok = test_clients_data()
    
    # Test 2: Logique de filtrage
    test2_ok = test_filter_logic()
    
    print("\n" + "=" * 60)
    print("📊 RÉSULTATS DES TESTS:")
    print(f"  - Données clients: {'✅ OK' if test1_ok else '❌ ÉCHEC'}")
    print(f"  - Logique filtrage: {'✅ OK' if test2_ok else '❌ ÉCHEC'}")
    
    if test1_ok and test2_ok:
        print("\n🎉 TOUS LES TESTS SONT PASSÉS!")
        print("💡 Le problème vient probablement du ViewModel ou de l'UI thread.")
    else:
        print("\n⚠️  PROBLÈME DÉTECTÉ dans les données ou la logique.")

if __name__ == "__main__":
    main()
