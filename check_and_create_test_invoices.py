#!/usr/bin/env python3
"""
Script pour vérifier et créer des données de test pour les factures FNE
"""
import sqlite3
import os
from datetime import datetime, timedelta
import uuid

def get_db_path():
    """Obtenir le chemin de la base de données"""
    base_paths = [
        r"D:\PROJET\FNE\FNEV4\data.db",
        r"D:\PROJET\FNE\FNEV4\src\FNEV4.Presentation\bin\Debug\net8.0-windows\data.db",
        r"D:\PROJET\FNE\data.db"
    ]
    
    for path in base_paths:
        if os.path.exists(path):
            return path
    
    # Si aucune base n'existe, créer dans le répertoire du projet
    return r"D:\PROJET\FNE\FNEV4\data.db"

def check_database_structure(cursor):
    """Vérifier si les tables existent"""
    cursor.execute("""
        SELECT name FROM sqlite_master 
        WHERE type='table' AND name IN ('FneInvoices', 'Clients', 'FneInvoiceItems')
    """)
    tables = [row[0] for row in cursor.fetchall()]
    print(f"Tables trouvées: {tables}")
    return tables

def check_existing_data(cursor):
    """Vérifier s'il y a déjà des données"""
    try:
        cursor.execute("SELECT COUNT(*) FROM FneInvoices")
        invoice_count = cursor.fetchone()[0]
        print(f"Nombre de factures existantes: {invoice_count}")
        
        cursor.execute("SELECT COUNT(*) FROM Clients")
        client_count = cursor.fetchone()[0]
        print(f"Nombre de clients existants: {client_count}")
        
        return invoice_count, client_count
    except sqlite3.OperationalError as e:
        print(f"Erreur lors de la vérification des données: {e}")
        return 0, 0

def create_test_client(cursor):
    """Créer un client de test"""
    client_id = str(uuid.uuid4())
    cursor.execute("""
        INSERT OR REPLACE INTO Clients (
            Id, CompanyName, ClientCode, ContactName, Email, Phone, Address,
            City, PostalCode, Country, TaxId, IsActive, CreatedAt, UpdatedAt
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    """, (
        client_id,
        "Client Test SARL",
        "TST001",
        "Jean Dupont",
        "test@client.com",
        "01.23.45.67.89",
        "123 Avenue des Tests",
        "Tunis",
        "1000",
        "Tunisie",
        "1234567A",
        1,  # IsActive
        datetime.now().isoformat(),
        datetime.now().isoformat()
    ))
    return client_id

def create_test_invoices(cursor, client_id, count=5):
    """Créer des factures de test"""
    invoices = []
    
    for i in range(count):
        invoice_id = str(uuid.uuid4())
        invoice_date = datetime.now() - timedelta(days=i*10)
        
        invoice_data = (
            invoice_id,
            f"FAC{2024}0{i+1:03d}",  # InvoiceNumber
            None,  # FneReference (sera rempli après certification)
            "sale",  # InvoiceType
            invoice_date.isoformat(),  # InvoiceDate
            client_id,  # ClientId
            "TST001",  # ClientCode
            "01",  # PointOfSale
            "TND",  # Currency
            1.0,  # ExchangeRate
            "draft",  # Status
            100.0 + (i * 50),  # TotalAmountHT
            18.0 + (i * 9),  # TotalVat
            118.0 + (i * 59),  # TotalAmountTTC
            None,  # FneCertificationDate
            None,  # QrCodeData
            None,  # CompanyRegistrationNumber
            "Facture de test numéro " + str(i+1),  # Notes
            datetime.now().isoformat(),  # CreatedAt
            datetime.now().isoformat()   # UpdatedAt
        )
        
        cursor.execute("""
            INSERT OR REPLACE INTO FneInvoices (
                Id, InvoiceNumber, FneReference, InvoiceType, InvoiceDate,
                ClientId, ClientCode, PointOfSale, Currency, ExchangeRate,
                Status, TotalAmountHT, TotalVat, TotalAmountTTC,
                FneCertificationDate, QrCodeData, CompanyRegistrationNumber,
                Notes, CreatedAt, UpdatedAt
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, invoice_data)
        
        invoices.append(invoice_id)
        
        # Créer quelques items pour chaque facture
        create_test_invoice_items(cursor, invoice_id, 2 + i % 3)
    
    return invoices

def create_test_invoice_items(cursor, invoice_id, item_count=2):
    """Créer des items de facture de test"""
    items = [
        ("Article A", "Produit de test A", 10.0, 50.0),
        ("Article B", "Produit de test B", 5.0, 30.0),
        ("Service C", "Service de test C", 1.0, 100.0),
        ("Article D", "Produit premium D", 2.0, 75.0),
    ]
    
    for i in range(min(item_count, len(items))):
        item_id = str(uuid.uuid4())
        name, desc, qty, price = items[i]
        
        cursor.execute("""
            INSERT OR REPLACE INTO FneInvoiceItems (
                Id, InvoiceId, ProductCode, ProductName, Description,
                Quantity, UnitPrice, TotalPrice, VatRate, VatAmount,
                CreatedAt, UpdatedAt
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, (
            item_id,
            invoice_id,
            f"PRD{i+1:03d}",
            name,
            desc,
            qty,
            price,
            qty * price,
            18.0,  # TVA 18%
            (qty * price) * 0.18,
            datetime.now().isoformat(),
            datetime.now().isoformat()
        ))

def main():
    db_path = get_db_path()
    print(f"Utilisation de la base de données: {db_path}")
    
    try:
        # Connexion à la base de données
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Vérifier la structure
        tables = check_database_structure(cursor)
        
        if not all(table in tables for table in ['FneInvoices', 'Clients']):
            print("❌ Tables manquantes. La base de données doit être initialisée d'abord.")
            return
        
        # Vérifier les données existantes
        invoice_count, client_count = check_existing_data(cursor)
        
        if invoice_count > 0:
            print(f"✅ La base de données contient déjà {invoice_count} factures.")
            
            # Afficher quelques exemples
            cursor.execute("""
                SELECT InvoiceNumber, Status, TotalAmountTTC, InvoiceDate 
                FROM FneInvoices 
                ORDER BY InvoiceDate DESC 
                LIMIT 5
            """)
            print("\nExemples de factures:")
            for row in cursor.fetchall():
                print(f"  - {row[0]}: {row[1]} - {row[2]} TND - {row[3][:10]}")
                
        else:
            print("📝 Aucune facture trouvée. Création de données de test...")
            
            # Créer un client de test s'il n'y en a pas
            if client_count == 0:
                client_id = create_test_client(cursor)
                print(f"✅ Client de test créé: {client_id}")
            else:
                # Utiliser un client existant
                cursor.execute("SELECT Id FROM Clients LIMIT 1")
                client_id = cursor.fetchone()[0]
                print(f"✅ Utilisation du client existant: {client_id}")
            
            # Créer des factures de test
            invoices = create_test_invoices(cursor, client_id, 8)
            
            # Commit des changements
            conn.commit()
            print(f"✅ {len(invoices)} factures de test créées avec succès!")
            
            # Vérification finale
            cursor.execute("SELECT COUNT(*) FROM FneInvoices")
            final_count = cursor.fetchone()[0]
            print(f"✅ Total des factures dans la base: {final_count}")
        
        conn.close()
        print(f"\n🎉 Base de données prête à l'adresse: {db_path}")
        
    except Exception as e:
        print(f"❌ Erreur: {e}")
        if 'conn' in locals():
            conn.close()

if __name__ == "__main__":
    main()