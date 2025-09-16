#!/usr/bin/env python3
"""
Script pour créer des données de test pour la certification FNE
"""

import sqlite3
import uuid
from datetime import datetime, timedelta
import random

def create_test_certification_data():
    """Crée des données de test pour le module de certification FNE"""
    
    # Connexion à la base de données
    try:
        conn = sqlite3.connect('D:/PROJET/FNE/FNEV4/src/FNEV4.Presentation/bin/Debug/net8.0-windows/fnev4_database.db')
        cursor = conn.cursor()
        
        print("Connexion à la base de données établie")
        
        # Vérifier si la table FneInvoices existe
        cursor.execute("SELECT name FROM sqlite_master WHERE type='table' AND name='FneInvoices'")
        if not cursor.fetchone():
            print("Table FneInvoices non trouvée. Création de la table...")
            create_invoice_table(cursor)
        
        # Vérifier si la table Clients existe
        cursor.execute("SELECT name FROM sqlite_master WHERE type='table' AND name='Clients'")
        if not cursor.fetchone():
            print("Table Clients non trouvée. Création de la table...")
            create_client_table(cursor)
            
        # Créer quelques clients de test
        clients_data = create_test_clients(cursor)
        print(f"Créé {len(clients_data)} clients de test")
        
        # Créer des factures de test
        invoices_count = create_test_invoices(cursor, clients_data)
        print(f"Créé {invoices_count} factures de test")
        
        conn.commit()
        print("Données de test créées avec succès!")
        
    except Exception as e:
        print(f"Erreur lors de la création des données: {e}")
        conn.rollback()
    finally:
        conn.close()

def create_client_table(cursor):
    """Crée la table Clients si elle n'existe pas"""
    cursor.execute('''
        CREATE TABLE IF NOT EXISTS Clients (
            Id TEXT PRIMARY KEY,
            CompanyName TEXT NOT NULL,
            TaxNumber TEXT,
            Address TEXT,
            City TEXT,
            PostalCode TEXT,
            Phone TEXT,
            Email TEXT,
            IsActive INTEGER DEFAULT 1,
            CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
            UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP
        )
    ''')

def create_invoice_table(cursor):
    """Crée la table FneInvoices si elle n'existe pas"""
    cursor.execute('''
        CREATE TABLE IF NOT EXISTS FneInvoices (
            Id TEXT PRIMARY KEY,
            InvoiceNumber TEXT UNIQUE NOT NULL,
            InvoiceDate TEXT NOT NULL,
            ClientId TEXT,
            TotalAmountTTC REAL DEFAULT 0,
            Status TEXT DEFAULT 'Draft',
            CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
            UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (ClientId) REFERENCES Clients(Id)
        )
    ''')

def create_test_clients(cursor):
    """Crée des clients de test"""
    clients = [
        {
            'id': str(uuid.uuid4()),
            'name': 'Société ALPHA TECH',
            'tax': '1234567890',
            'address': '123 Avenue Habib Bourguiba',
            'city': 'Tunis',
            'postal': '1000',
            'phone': '+216 71 123 456',
            'email': 'contact@alphatech.tn'
        },
        {
            'id': str(uuid.uuid4()),
            'name': 'BETA SOLUTIONS SARL',
            'tax': '0987654321',
            'address': '456 Rue de la République',
            'city': 'Sfax',
            'postal': '3000',
            'phone': '+216 74 987 654',
            'email': 'info@betasolutions.tn'
        },
        {
            'id': str(uuid.uuid4()),
            'name': 'GAMMA CONSULTING',
            'tax': '1122334455',
            'address': '789 Boulevard des Martyrs',
            'city': 'Sousse',
            'postal': '4000',
            'phone': '+216 73 111 222',
            'email': 'contact@gamma.tn'
        },
        {
            'id': str(uuid.uuid4()),
            'name': 'DELTA SERVICES',
            'tax': '9988776655',
            'address': '321 Avenue de la Liberté',
            'city': 'Bizerte',
            'postal': '7000',
            'phone': '+216 72 999 888',
            'email': 'service@delta.tn'
        },
        {
            'id': str(uuid.uuid4()),
            'name': 'OMEGA TRADING',
            'tax': '5566778899',
            'address': '654 Rue Ibn Khaldoun',
            'city': 'Monastir',
            'postal': '5000',
            'phone': '+216 73 555 666',
            'email': 'trading@omega.tn'
        }
    ]
    
    for client in clients:
        cursor.execute('''
            INSERT OR IGNORE INTO Clients (Id, CompanyName, TaxNumber, Address, City, PostalCode, Phone, Email, IsActive, CreatedAt, UpdatedAt)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, 1, ?, ?)
        ''', (
            client['id'],
            client['name'],
            client['tax'],
            client['address'],
            client['city'],
            client['postal'],
            client['phone'],
            client['email'],
            datetime.now().isoformat(),
            datetime.now().isoformat()
        ))
    
    return clients

def create_test_invoices(cursor, clients_data):
    """Crée des factures de test avec différents statuts"""
    statuses = ['Draft', 'Validated', 'Error']
    invoice_count = 0
    
    # Générer des factures pour les 30 derniers jours
    base_date = datetime.now() - timedelta(days=30)
    
    for i in range(25):  # 25 factures de test
        invoice_date = base_date + timedelta(days=random.randint(0, 30))
        client = random.choice(clients_data)
        status = random.choice(statuses)
        
        # Différents montants selon le statut
        if status == 'Draft':
            amount = round(random.uniform(100, 2000), 2)
        elif status == 'Validated':
            amount = round(random.uniform(500, 5000), 2)
        else:  # Error
            amount = round(random.uniform(50, 500), 2)
        
        invoice_data = {
            'id': str(uuid.uuid4()),
            'number': f'FAC-{2025}{(i+1):04d}',
            'date': invoice_date.isoformat(),
            'client_id': client['id'],
            'amount': amount,
            'status': status
        }
        
        cursor.execute('''
            INSERT OR IGNORE INTO FneInvoices (Id, InvoiceNumber, InvoiceDate, ClientId, TotalAmountTTC, Status, CreatedAt, UpdatedAt)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        ''', (
            invoice_data['id'],
            invoice_data['number'],
            invoice_data['date'],
            invoice_data['client_id'],
            invoice_data['amount'],
            invoice_data['status'],
            datetime.now().isoformat(),
            datetime.now().isoformat()
        ))
        
        invoice_count += 1
        
        print(f"Facture créée: {invoice_data['number']} - {client['name'][:20]} - {amount} TND - {status}")
    
    return invoice_count

if __name__ == "__main__":
    create_test_certification_data()