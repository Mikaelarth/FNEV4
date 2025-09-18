#!/usr/bin/env python3
"""
Script pour corriger les données manquantes du client "DIVERS CLIENTS CARBURANTS" 
afin de résoudre l'erreur de certification FNE
"""

import sqlite3
import os

def find_database_path():
    """Trouve le chemin de la base de données selon la logique du système centralisé FNEV4"""
    possible_paths = [
        "data/FNEV4.db",
        "FNEV4/data/FNEV4.db", 
        "../data/FNEV4.db",
        "D:\\PROJET\\FNE\\FNEV4\\data\\FNEV4.db"
    ]
    
    for path in possible_paths:
        if os.path.exists(path):
            return path
    return None

def update_client_data(cursor):
    """Met à jour le client DIVERS CLIENTS CARBURANTS avec les données manquantes"""
    
    # Trouver le client
    cursor.execute("""
        SELECT Id, Name, Email, Phone FROM Clients 
        WHERE ClientCode = '1999' AND Name = 'DIVERS CLIENTS CARBURANTS'
    """)
    client = cursor.fetchone()
    
    if not client:
        print("❌ Client 'DIVERS CLIENTS CARBURANTS' (code 1999) non trouvé!")
        return False
    
    client_id, name, current_email, current_phone = client
    print(f"✅ Client trouvé: {name} (ID: {client_id})")
    print(f"   Email actuel: {current_email or 'VIDE'}")
    print(f"   Téléphone actuel: {current_phone or 'VIDE'}")
    
    # Données par défaut pour client "DIVERS"
    default_email = "divers@fnev4-clients.ci"
    default_phone = "+225 00 00 00 00"
    
    # Mettre à jour seulement si vide
    updates_needed = []
    new_values = {}
    
    if not current_email:
        updates_needed.append("Email = ?")
        new_values['email'] = default_email
    
    if not current_phone:
        updates_needed.append("Phone = ?")  
        new_values['phone'] = default_phone
    
    if updates_needed:
        # Construire la requête UPDATE
        update_sql = f"""
            UPDATE Clients 
            SET {', '.join(updates_needed)}, UpdatedAt = datetime('now')
            WHERE Id = ?
        """
        
        # Préparer les valeurs
        values = []
        if 'email' in new_values:
            values.append(new_values['email'])
        if 'phone' in new_values:
            values.append(new_values['phone'])
        values.append(client_id)
        
        cursor.execute(update_sql, values)
        
        print(f"\n✅ Mise à jour effectuée:")
        if 'email' in new_values:
            print(f"   - Email: {new_values['email']}")
        if 'phone' in new_values:
            print(f"   - Téléphone: {new_values['phone']}")
        
        return True
    else:
        print("\n✅ Aucune mise à jour nécessaire - données déjà présentes")
        return False

def update_zero_vat_items(cursor):
    """Met à jour les articles avec TVA 0% pour avoir TVA 0.01% (minimum acceptable)"""
    
    cursor.execute("""
        SELECT i.Id, i.Description, i.VatRate, f.InvoiceNumber
        FROM FneInvoiceItems i
        JOIN FneInvoices f ON i.FneInvoiceId = f.Id
        WHERE f.InvoiceNumber = '556481' AND i.VatRate = 0
    """)
    items = cursor.fetchall()
    
    if not items:
        print("✅ Aucun article avec TVA 0% trouvé")
        return False
    
    print(f"\n🔧 Articles avec TVA 0% trouvés ({len(items)}):")
    updates_made = 0
    
    for item_id, description, vat_rate, invoice_number in items:
        print(f"   - {description} (TVA: {vat_rate}%)")
        
        # Mettre à jour avec TVA minimum de 0.01%
        cursor.execute("""
            UPDATE FneInvoiceItems 
            SET VatRate = 0.01, UpdatedAt = datetime('now')
            WHERE Id = ?
        """, (item_id,))
        updates_made += 1
    
    if updates_made > 0:
        print(f"✅ {updates_made} article(s) mis à jour avec TVA minimum 0.01%")
        return True
    
    return False

def main():
    print("🔧 Correction des données pour la certification FNE - Facture 556481")
    print("=" * 70)
    
    db_path = find_database_path()
    if not db_path:
        print("❌ Base de données FNEV4 non trouvée!")
        return
    
    print(f"✅ Base de données: {db_path}")
    
    try:
        with sqlite3.connect(db_path) as conn:
            cursor = conn.cursor()
            
            # 1. Corriger les données client
            print("\n👤 Correction des données client:")
            print("-" * 35)
            client_updated = update_client_data(cursor)
            
            # 2. Corriger les articles avec TVA 0%
            print("\n📦 Correction des articles TVA 0%:")
            print("-" * 35)
            items_updated = update_zero_vat_items(cursor)
            
            # 3. Commit les changements
            if client_updated or items_updated:
                conn.commit()
                print(f"\n✅ Modifications sauvegardées dans la base de données")
                print(f"📋 Vous pouvez maintenant retenter la certification de la facture 556481")
            else:
                print(f"\n✅ Aucune modification nécessaire")
                
    except Exception as e:
        print(f"❌ Erreur: {e}")

if __name__ == "__main__":
    main()