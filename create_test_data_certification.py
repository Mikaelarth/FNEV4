#!/usr/bin/env python3
"""
Script de création de données de test pour la certification FNE
==============================================================

Ce script crée des factures de test dans la base de données pour 
vérifier que l'interface de certification manuelle peut charger des données.

1. Crée la base de données si elle n'existe pas
2. Ajoute des factures de test avec le statut "draft"
3. Vérifie que les données sont bien insérées

Auteur: Assistant IA
Date: Décembre 2024
"""

import sqlite3
import os
from datetime import datetime, timedelta
import random

# Configuration
DB_PATH = r"d:\PROJET\FNE\FNEV4\Data\Database\fnev4_centralised.db"

def create_database_structure():
    """Créer la structure de base de données si elle n'existe pas"""
    print("🔧 === CRÉATION STRUCTURE BASE DE DONNÉES ===")
    
    # S'assurer que le répertoire existe
    os.makedirs(os.path.dirname(DB_PATH), exist_ok=True)
    
    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()
    
    # Créer la table FneInvoices si elle n'existe pas
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS FneInvoices (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            InvoiceNumber TEXT NOT NULL,
            InvoiceDate DATE NOT NULL,
            ClientId INTEGER,
            Amount DECIMAL(18,2),
            Status TEXT NOT NULL DEFAULT 'draft',
            CertifiedAt DATETIME NULL,
            CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
            UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
        )
    """)
    
    # Créer la table Clients si elle n'existe pas
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS Clients (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CompanyName TEXT NOT NULL,
            Email TEXT,
            Phone TEXT,
            Address TEXT,
            CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
        )
    """)
    
    # Créer la table FneConfigurations si elle n'existe pas
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS FneConfigurations (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            ApiUrl TEXT NOT NULL,
            IsActive BOOLEAN DEFAULT 0,
            CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
        )
    """)
    
    conn.commit()
    conn.close()
    print("✅ Structure de base de données créée")

def create_test_clients():
    """Créer des clients de test"""
    print("\n👥 === CRÉATION CLIENTS DE TEST ===")
    
    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()
    
    clients = [
        ("Entreprise Alpha SARL", "contact@alpha.fr", "01.23.45.67.89", "123 Rue de la Paix, 75001 Paris"),
        ("Beta Technologies", "info@beta-tech.fr", "01.98.76.54.32", "456 Avenue des Champs, 69001 Lyon"),
        ("Gamma Consulting", "hello@gamma.fr", "01.11.22.33.44", "789 Boulevard Central, 13001 Marseille"),
        ("Delta Services", "contact@delta.fr", "01.55.66.77.88", "321 Place de la République, 31000 Toulouse"),
        ("Epsilon Industries", "info@epsilon.fr", "01.99.88.77.66", "654 Rue de la Liberté, 59000 Lille")
    ]
    
    cursor.execute("DELETE FROM Clients")  # Nettoyer les anciennes données
    
    for company, email, phone, address in clients:
        cursor.execute("""
            INSERT INTO Clients (CompanyName, Email, Phone, Address)
            VALUES (?, ?, ?, ?)
        """, (company, email, phone, address))
    
    conn.commit()
    print(f"✅ {len(clients)} clients créés")
    
    # Récupérer les IDs des clients créés
    cursor.execute("SELECT Id FROM Clients")
    client_ids = [row[0] for row in cursor.fetchall()]
    conn.close()
    
    return client_ids

def create_test_invoices(client_ids):
    """Créer des factures de test"""
    print("\n📄 === CRÉATION FACTURES DE TEST ===")
    
    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()
    
    cursor.execute("DELETE FROM FneInvoices")  # Nettoyer les anciennes données
    
    # Générer des factures sur les 30 derniers jours
    invoices = []
    for i in range(1, 21):  # 20 factures de test
        invoice_number = f"FAC{datetime.now().year}{i:04d}"
        invoice_date = datetime.now() - timedelta(days=random.randint(0, 30))
        client_id = random.choice(client_ids)
        amount = round(random.uniform(100.0, 5000.0), 2)
        status = random.choice(["draft", "draft", "draft", "validated", "sent"])  # Plus de draft pour les tests
        
        invoices.append((invoice_number, invoice_date.date(), client_id, amount, status))
    
    cursor.executemany("""
        INSERT INTO FneInvoices (InvoiceNumber, InvoiceDate, ClientId, Amount, Status)
        VALUES (?, ?, ?, ?, ?)
    """, invoices)
    
    conn.commit()
    print(f"✅ {len(invoices)} factures créées")
    
    # Statistiques
    cursor.execute("SELECT Status, COUNT(*) FROM FneInvoices GROUP BY Status")
    stats = cursor.fetchall()
    print("📊 Répartition par statut:")
    for status, count in stats:
        print(f"   • {status}: {count} factures")
    
    conn.close()

def create_test_fne_config():
    """Créer une configuration FNE de test"""
    print("\n⚙️ === CRÉATION CONFIGURATION FNE ===")
    
    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()
    
    cursor.execute("DELETE FROM FneConfigurations")  # Nettoyer les anciennes données
    
    cursor.execute("""
        INSERT INTO FneConfigurations (Name, ApiUrl, IsActive)
        VALUES (?, ?, ?)
    """, ("Configuration Test", "https://api-test.fne.fr", 1))
    
    conn.commit()
    conn.close()
    print("✅ Configuration FNE active créée")

def verify_data():
    """Vérifier que les données ont été créées correctement"""
    print("\n🔍 === VÉRIFICATION DES DONNÉES ===")
    
    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()
    
    # Vérifier les clients
    cursor.execute("SELECT COUNT(*) FROM Clients")
    client_count = cursor.fetchone()[0]
    print(f"✅ Clients: {client_count}")
    
    # Vérifier les factures
    cursor.execute("SELECT COUNT(*) FROM FneInvoices")
    invoice_count = cursor.fetchone()[0]
    print(f"✅ Factures: {invoice_count}")
    
    # Vérifier les factures draft (disponibles pour certification)
    cursor.execute("SELECT COUNT(*) FROM FneInvoices WHERE Status = 'draft' AND CertifiedAt IS NULL")
    draft_count = cursor.fetchone()[0]
    print(f"✅ Factures disponibles pour certification: {draft_count}")
    
    # Vérifier la configuration FNE
    cursor.execute("SELECT COUNT(*) FROM FneConfigurations WHERE IsActive = 1")
    config_count = cursor.fetchone()[0]
    print(f"✅ Configurations FNE actives: {config_count}")
    
    # Afficher quelques exemples
    print("\n📋 Échantillon de factures draft:")
    cursor.execute("""
        SELECT f.InvoiceNumber, f.InvoiceDate, c.CompanyName, f.Amount, f.Status
        FROM FneInvoices f
        LEFT JOIN Clients c ON f.ClientId = c.Id
        WHERE f.Status = 'draft' AND f.CertifiedAt IS NULL
        LIMIT 5
    """)
    
    for invoice_number, invoice_date, company, amount, status in cursor.fetchall():
        print(f"   • {invoice_number} - {company or 'N/A'} - {amount}€ - {status}")
    
    conn.close()
    
    return draft_count > 0

def main():
    """Fonction principale"""
    print("🚀 === CRÉATION DONNÉES TEST CERTIFICATION FNE ===")
    print(f"Base de données: {DB_PATH}")
    print("=" * 60)
    
    try:
        # Créer la structure
        create_database_structure()
        
        # Créer les données de test
        client_ids = create_test_clients()
        create_test_invoices(client_ids)
        create_test_fne_config()
        
        # Vérifier
        has_data = verify_data()
        
        print("\n" + "=" * 60)
        if has_data:
            print("🎉 SUCCÈS - Données de test créées!")
            print("L'interface de certification manuelle devrait maintenant afficher des factures.")
        else:
            print("⚠️ ATTENTION - Aucune facture draft trouvée.")
        
        print(f"\n📍 Chemin base de données: {DB_PATH}")
        print("🔄 Relancez l'application FNEV4 et naviguez vers Certification FNE > Certification manuelle")
        
    except Exception as e:
        print(f"❌ ERREUR: {e}")
        import traceback
        traceback.print_exc()

if __name__ == "__main__":
    main()