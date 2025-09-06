#!/usr/bin/env python3
"""
Script d'insertion de clients de test pour valider le module Gestion Clients
"""

import sqlite3
import uuid
from datetime import datetime, timedelta
import os
from pathlib import Path

def get_database_path():
    """Obtient le chemin de la base de données"""
    # Chemin standardisé par PathConfigurationService
    db_path = Path(r"C:\wamp64\www\FNEV4\data\FNEV4.db")
    
    if not db_path.exists():
        # Si le fichier n'existe pas, créer le dossier
        db_path.parent.mkdir(parents=True, exist_ok=True)
        print(f"📁 Dossier data créé: {db_path.parent}")
    
    return str(db_path)

def create_test_clients():
    """Crée des clients de test pour valider le module"""
    
    db_path = get_database_path()
    print(f"🗄️ Connexion à la base de données: {db_path}")
    
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Vérifier si la table Clients existe
        cursor.execute("""
            SELECT name FROM sqlite_master 
            WHERE type='table' AND name='Clients'
        """)
        
        if not cursor.fetchone():
            print("❌ Table Clients introuvable dans la base de données")
            print("   Assurez-vous que l'application a créé la base de données")
            return
        
        print("✅ Table Clients trouvée")
        
        # Vérifier le nombre actuel de clients
        cursor.execute("SELECT COUNT(*) FROM Clients")
        current_count = cursor.fetchone()[0]
        print(f"📊 Nombre actuel de clients: {current_count}")
        
        # Créer des clients de test variés
        test_clients = [
            {
                'id': str(uuid.uuid4()),
                'client_code': 'CLI001',
                'client_ncc': '123456789012345',
                'name': 'SARL TechnoSoft',
                'company_name': 'TechnoSoft Solutions',
                'address': '123 Avenue de la Technologie, Tunis 1000',
                'phone': '+216 71 123 456',
                'email': 'contact@technosoft.tn',
                'client_type': 'Company',
                'default_template': 'B2B',
                'is_active': True,
                'country': 'Tunisie',
                'default_currency': 'TND',
                'seller_name': 'Ahmed Ben Ali',
                'tax_identification_number': '123456789012345',
                'notes': 'Client important - Secteur IT',
                'created_date': datetime.now() - timedelta(days=30),
                'last_modified_date': datetime.now() - timedelta(days=5)
            },
            {
                'id': str(uuid.uuid4()),
                'client_code': 'CLI002',
                'client_ncc': '987654321098765',
                'name': 'Ben Salah Mohamed',
                'company_name': None,
                'address': '45 Rue Habib Bourguiba, Sfax 3000',
                'phone': '+216 74 987 654',
                'email': 'mohamed.bensalah@email.tn',
                'client_type': 'Individual',
                'default_template': 'B2C',
                'is_active': True,
                'country': 'Tunisie',
                'default_currency': 'TND',
                'seller_name': 'Fatma Trabelsi',
                'tax_identification_number': None,
                'notes': 'Client particulier régulier',
                'created_date': datetime.now() - timedelta(days=15),
                'last_modified_date': datetime.now() - timedelta(days=2)
            },
            {
                'id': str(uuid.uuid4()),
                'client_code': 'CLI003',
                'client_ncc': '456789123456789',
                'name': 'Ministère de la Santé',
                'company_name': 'République Tunisienne',
                'address': 'Bab Saadoun, Tunis 1006',
                'phone': '+216 71 560 000',
                'email': 'contact@sanite.gov.tn',
                'client_type': 'Government',
                'default_template': 'B2G',
                'is_active': True,
                'country': 'Tunisie',
                'default_currency': 'TND',
                'seller_name': 'Service Public',
                'tax_identification_number': '456789123456789',
                'notes': 'Administration publique',
                'created_date': datetime.now() - timedelta(days=60),
                'last_modified_date': datetime.now() - timedelta(days=10)
            },
            {
                'id': str(uuid.uuid4()),
                'client_code': 'CLI004',
                'client_ncc': None,
                'name': 'Global Trading LLC',
                'company_name': 'Global Trading Limited',
                'address': '100 Business District, Dubai, UAE',
                'phone': '+971 4 123 4567',
                'email': 'info@globaltrading.ae',
                'client_type': 'International',
                'default_template': 'B2F',
                'is_active': True,
                'country': 'Émirats Arabes Unis',
                'default_currency': 'USD',
                'seller_name': 'Export Department',
                'tax_identification_number': 'TRN12345678',
                'notes': 'Client international - Devise USD',
                'created_date': datetime.now() - timedelta(days=45),
                'last_modified_date': datetime.now() - timedelta(days=1)
            },
            {
                'id': str(uuid.uuid4()),
                'client_code': 'CLI005',
                'client_ncc': '789123456789123',
                'name': 'Entreprise Inactive SARL',
                'company_name': 'Ancienne Société',
                'address': '200 Ancienne Adresse, Tunis',
                'phone': '+216 71 000 000',
                'email': 'old@inactive.tn',
                'client_type': 'Company',
                'default_template': 'B2B',
                'is_active': False,
                'country': 'Tunisie',
                'default_currency': 'TND',
                'seller_name': 'Ancien Commercial',
                'tax_identification_number': '789123456789123',
                'notes': 'Client inactif - Plus de commandes depuis 6 mois',
                'created_date': datetime.now() - timedelta(days=200),
                'last_modified_date': datetime.now() - timedelta(days=100)
            }
        ]
        
        # Insérer les clients de test
        insert_query = """
            INSERT INTO Clients (
                Id, ClientCode, ClientNcc, Name, CompanyName, Address, Phone, Email,
                ClientType, DefaultTemplate, IsActive, Country, DefaultCurrency,
                SellerName, TaxIdentificationNumber, Notes, CreatedDate, LastModifiedDate
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """
        
        for client in test_clients:
            # Vérifier si le client existe déjà
            cursor.execute("SELECT COUNT(*) FROM Clients WHERE ClientCode = ?", (client['client_code'],))
            if cursor.fetchone()[0] == 0:
                cursor.execute(insert_query, (
                    client['id'],
                    client['client_code'], 
                    client['client_ncc'],
                    client['name'],
                    client['company_name'],
                    client['address'],
                    client['phone'],
                    client['email'],
                    client['client_type'],
                    client['default_template'],
                    client['is_active'],
                    client['country'],
                    client['default_currency'],
                    client['seller_name'],
                    client['tax_identification_number'],
                    client['notes'],
                    client['created_date'].isoformat(),
                    client['last_modified_date'].isoformat() if client['last_modified_date'] else None
                ))
                print(f"✅ Client ajouté: {client['name']} ({client['client_type']})")
            else:
                print(f"⚠️  Client existe déjà: {client['name']}")
        
        conn.commit()
        
        # Afficher les statistiques finales
        cursor.execute("SELECT COUNT(*) FROM Clients")
        total_clients = cursor.fetchone()[0]
        
        cursor.execute("SELECT COUNT(*) FROM Clients WHERE IsActive = 1")
        active_clients = cursor.fetchone()[0]
        
        cursor.execute("SELECT COUNT(*) FROM Clients WHERE ClientNcc IS NOT NULL AND ClientNcc != ''")
        clients_with_ncc = cursor.fetchone()[0]
        
        cursor.execute("SELECT ClientType, COUNT(*) FROM Clients GROUP BY ClientType")
        types_stats = cursor.fetchall()
        
        print("\n📊 STATISTIQUES FINALES")
        print(f"   Total clients: {total_clients}")
        print(f"   Clients actifs: {active_clients}")
        print(f"   Clients avec NCC: {clients_with_ncc}")
        print("\n   Répartition par type:")
        for client_type, count in types_stats:
            print(f"     - {client_type}: {count}")
        
        print(f"\n🎉 Base de données mise à jour avec succès!")
        print(f"📁 Chemin: {db_path}")
        
    except sqlite3.Error as e:
        print(f"❌ Erreur SQLite: {e}")
    except Exception as e:
        print(f"❌ Erreur: {e}")
    finally:
        if conn:
            conn.close()

def verify_database_structure():
    """Vérifie la structure de la base de données"""
    db_path = get_database_path()
    
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Lister toutes les tables
        cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
        tables = [row[0] for row in cursor.fetchall()]
        
        print(f"📋 Tables disponibles: {', '.join(tables)}")
        
        if 'Clients' in tables:
            # Obtenir la structure de la table Clients
            cursor.execute("PRAGMA table_info(Clients)")
            columns = cursor.fetchall()
            
            print("\n🏗️  Structure de la table Clients:")
            for column in columns:
                col_id, name, data_type, not_null, default, pk = column
                print(f"   {name}: {data_type} {'(NOT NULL)' if not_null else ''} {'(PK)' if pk else ''}")
        
    except Exception as e:
        print(f"❌ Erreur lors de la vérification: {e}")
    finally:
        if conn:
            conn.close()

def main():
    print("🧪 Script de création de clients de test - Module Gestion Clients")
    print("=" * 60)
    
    # Vérifier la structure
    verify_database_structure()
    
    print("\n" + "=" * 60)
    
    # Créer les clients de test
    create_test_clients()
    
    print("\n" + "=" * 60)
    print("✅ Script terminé. Vous pouvez maintenant tester le module Gestion Clients")
    print("🔄 Redémarrez l'application si elle est ouverte pour voir les nouveaux clients")

if __name__ == "__main__":
    main()
