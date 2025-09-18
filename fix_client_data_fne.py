#!/usr/bin/env python3
"""
Corrige les données client manquantes pour l'API FNE
Met à jour les clients avec email et téléphone requis
"""

import sqlite3
import os
import sys

def find_database_path():
    """Trouve le chemin vers la base de données FNEV4"""
    possible_paths = [
        r"D:\PROJET\FNE\FNEV4\fnev4_app.db",
        r"D:\PROJET\FNE\FNEV4\src\FNEV4.Presentation\bin\Debug\net8.0-windows\fnev4_app.db",
        r"D:\PROJET\FNE\FNEV4\fnev4_data.db",
        "fnev4_app.db"
    ]
    
    for path in possible_paths:
        if os.path.exists(path):
            return path
    
    raise FileNotFoundError("Base de données FNEV4 non trouvée dans les emplacements habituels")

def fix_client_data():
    """Corrige les données client manquantes"""
    try:
        db_path = find_database_path()
        print(f"📁 Base de données trouvée: {db_path}")
        
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        print("🔧 Correction des données client...")
        
        # Corriger le client DIVERS CLIENTS CARBURANTS (Code 1999)
        cursor.execute("""
            UPDATE Client 
            SET 
                Email = COALESCE(Email, 'divers@fnev4.ci'),
                Phone = COALESCE(Phone, '0000000000'),
                Address = COALESCE(Address, 'Adresse non renseignée'),
                DefaultTemplate = 'B2C',
                DefaultPaymentMethod = 'cash'
            WHERE ClientCode = '1999' AND Name LIKE '%DIVERS%'
        """)
        
        # Vérifier si des corrections ont été apportées
        if cursor.rowcount > 0:
            print(f"✅ {cursor.rowcount} client(s) 'DIVERS' corrigé(s)")
        else:
            print("ℹ️  Aucun client 'DIVERS' à corriger")
            
        # Corriger tous les autres clients sans email/téléphone
        cursor.execute("""
            UPDATE Client 
            SET 
                Email = COALESCE(Email, LOWER(REPLACE(Name, ' ', '.')) || '@client.fnev4.ci'),
                Phone = COALESCE(Phone, '0000000000')
            WHERE Email IS NULL OR Email = '' OR Phone IS NULL OR Phone = ''
        """)
        
        if cursor.rowcount > 0:
            print(f"✅ {cursor.rowcount} client(s) supplémentaire(s) corrigé(s)")
            
        # Afficher les clients corrigés
        cursor.execute("""
            SELECT ClientCode, Name, Email, Phone, DefaultPaymentMethod
            FROM Client 
            WHERE ClientCode = '1999' OR Email LIKE '%@client.fnev4.ci'
            ORDER BY ClientCode
        """)
        
        corrected_clients = cursor.fetchall()
        if corrected_clients:
            print("\n📋 Clients corrigés:")
            for client in corrected_clients:
                print(f"  {client[0]} - {client[1]} - {client[2]} - {client[3]} - {client[4]}")
        
        conn.commit()
        conn.close()
        
        print("\n✅ Corrections terminées avec succès!")
        return True
        
    except Exception as e:
        print(f"❌ Erreur lors de la correction: {e}")
        return False

if __name__ == "__main__":
    success = fix_client_data()
    sys.exit(0 if success else 1)