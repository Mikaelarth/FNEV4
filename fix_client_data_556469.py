#!/usr/bin/env python3
"""
Script de correction - Données client facture 556469
Correction des données client pour assurer la cohérence
"""

import sqlite3
import sys
from pathlib import Path

def fix_client_data_556469():
    """
    Corrige les données client de la facture 556469
    """
    
    # Chemins possibles pour la base de données
    db_paths = [
        Path("data/FNEV4.db"),
        Path("FNEV4/data/FNEV4.db"),
        Path("src/FNEV4.Presentation/data/FNEV4.db"),
        Path("../data/FNEV4.db")
    ]
    
    db_path = None
    for path in db_paths:
        if path.exists():
            db_path = path
            break
    
    if not db_path:
        print("❌ Base de données FNEV4.db introuvable")
        return
    
    print(f"✅ Base de données trouvée : {db_path}")
    
    try:
        # Créer une sauvegarde
        import shutil
        from datetime import datetime
        
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        backup_path = db_path.parent / f"FNEV4_backup_fix_client_{timestamp}.db"
        shutil.copy2(str(db_path), str(backup_path))
        print(f"✅ Sauvegarde créée : {backup_path}")
        
        # Connexion à la base de données
        conn = sqlite3.connect(str(db_path))
        cursor = conn.cursor()
        
        # Analyser la facture 556469
        cursor.execute("""
            SELECT Id, InvoiceNumber, ClientId, ClientCode, ClientDisplayName, 
                   ClientCompanyName, CommercialMessage
            FROM FneInvoices 
            WHERE InvoiceNumber = '556469'
        """)
        
        facture = cursor.fetchone()
        
        if not facture:
            print("❌ Facture 556469 introuvable")
            return
        
        invoice_id, invoice_num, client_id, client_code, client_display, client_company, commercial_msg = facture
        
        print(f"🔍 ÉTAT ACTUEL FACTURE {invoice_num}:")
        print(f"   • ID Facture: {invoice_id}")
        print(f"   • ID Client: {client_id}")
        print(f"   • Code Client: {client_code}")
        print(f"   • Nom Affiché: {client_display}")
        print(f"   • Nom Compagnie: {client_company}")
        print(f"   • Message Commercial: {commercial_msg}")
        
        # Extraire le nom du client depuis le message commercial
        client_name_from_message = None
        if commercial_msg and commercial_msg.startswith("Client: "):
            client_name_from_message = commercial_msg[8:].strip()  # Enlever "Client: "
        
        print(f"\n🔧 CORRECTION PROPOSÉE:")
        print(f"   • Nom extrait du message: '{client_name_from_message}'")
        
        if not client_name_from_message:
            print("❌ Impossible d'extraire le nom client du message commercial")
            return
        
        # Vérifier si le client existe dans la table Clients
        cursor.execute("""
            SELECT Id, Code, CompanyName, DisplayName 
            FROM Clients 
            WHERE Id = ? OR Code = ?
        """, (client_id, client_code))
        
        client_record = cursor.fetchone()
        
        if client_record:
            client_db_id, client_db_code, client_db_company, client_db_display = client_record
            print(f"\n✅ CLIENT TROUVÉ EN BASE:")
            print(f"   • ID: {client_db_id}")
            print(f"   • Code: {client_db_code}")
            print(f"   • Nom Compagnie: {client_db_company}")
            print(f"   • Nom Affiché: {client_db_display}")
            
            # Décider quelle valeur utiliser pour la correction
            nom_a_utiliser = client_db_display or client_db_company or client_name_from_message
            
        else:
            print("⚠️ Client non trouvé dans la table Clients")
            nom_a_utiliser = client_name_from_message
        
        print(f"\n🎯 CORRECTION APPLIQUÉE:")
        print(f"   • Nouveau ClientDisplayName: '{nom_a_utiliser}'")
        print(f"   • Nouveau ClientCompanyName: '{nom_a_utiliser}'")
        
        # Demander confirmation
        confirm = input("\n❓ Appliquer cette correction ? (oui/non): ").lower().strip()
        
        if confirm not in ['oui', 'o', 'yes', 'y']:
            print("🚫 Correction annulée")
            return
        
        # Appliquer la correction
        cursor.execute("""
            UPDATE FneInvoices 
            SET 
                ClientDisplayName = ?,
                ClientCompanyName = ?,
                UpdatedAt = datetime('now')
            WHERE InvoiceNumber = ?
        """, (nom_a_utiliser, nom_a_utiliser, '556469'))
        
        affected_rows = cursor.rowcount
        conn.commit()
        
        if affected_rows > 0:
            print(f"✅ Correction appliquée avec succès ({affected_rows} ligne mise à jour)")
            
            # Vérifier le résultat
            cursor.execute("""
                SELECT ClientDisplayName, ClientCompanyName, CommercialMessage
                FROM FneInvoices 
                WHERE InvoiceNumber = '556469'
            """)
            
            result = cursor.fetchone()
            if result:
                new_display, new_company, msg = result
                print(f"\n✅ RÉSULTAT APRÈS CORRECTION:")
                print(f"   • ClientDisplayName: '{new_display}'")
                print(f"   • ClientCompanyName: '{new_company}'")
                print(f"   • CommercialMessage: '{msg}'")
        else:
            print("❌ Aucune ligne mise à jour")
        
        # Vérifier d'autres factures avec le même problème
        cursor.execute("""
            SELECT COUNT(*) 
            FROM FneInvoices 
            WHERE (ClientDisplayName IS NULL OR ClientDisplayName = '') 
            AND CommercialMessage IS NOT NULL 
            AND CommercialMessage LIKE 'Client: %'
        """)
        
        problematic_count = cursor.fetchone()[0]
        
        if problematic_count > 1:
            print(f"\n⚠️ ATTENTION: {problematic_count} autres factures ont le même problème")
            print("💡 Considérer une correction en lot si nécessaire")
        
    except sqlite3.Error as e:
        print(f"❌ Erreur base de données: {e}")
        if 'conn' in locals():
            conn.rollback()
    
    except Exception as e:
        print(f"❌ Erreur inattendue: {e}")
        if 'conn' in locals():
            conn.rollback()
    
    finally:
        if 'conn' in locals():
            conn.close()

if __name__ == "__main__":
    print("🔧 CORRECTION DONNÉES CLIENT FACTURE 556469")
    print("=" * 60)
    fix_client_data_556469()
    print("\n✅ Correction terminée")