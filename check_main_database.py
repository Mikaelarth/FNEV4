#!/usr/bin/env python3
"""
Script pour examiner la base de données centralisée FNEV4
"""
import sqlite3
import sys
import os

def main():
    db_path = r"d:\PROJET\FNE\FNEV4\data\FNEV4.db"
    
    if not os.path.exists(db_path):
        print(f"❌ Base de données introuvable: {db_path}")
        return
    
    print(f"✅ Base de données trouvée: {db_path}")
    
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Lister les tables
        cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
        tables = cursor.fetchall()
        print(f"\n📊 Tables disponibles ({len(tables)}):")
        for table in tables:
            table_name = table[0]
            cursor.execute(f"SELECT COUNT(*) FROM [{table_name}]")
            count = cursor.fetchone()[0]
            print(f"  - {table_name}: {count} enregistrements")
        
        # Examiner spécifiquement FneInvoices pour la certification
        if any(t[0] == 'FneInvoices' for t in tables):
            print(f"\n🎯 Analyse de la table FneInvoices:")
            
            # Structure
            cursor.execute("PRAGMA table_info(FneInvoices)")
            columns = cursor.fetchall()
            print("  Colonnes:")
            for col in columns:
                print(f"    - {col[1]} ({col[2]})")
            
            # Échantillon de données
            cursor.execute("SELECT Id, ClientId, Status, Amount FROM FneInvoices LIMIT 5")
            samples = cursor.fetchall()
            print("  Échantillon de données:")
            for sample in samples:
                print(f"    - ID: {sample[0]}, ClientId: {sample[1]}, Status: {sample[2]}, Amount: {sample[3]}")
            
            # Statuts disponibles
            cursor.execute("SELECT DISTINCT Status FROM FneInvoices")
            statuses = cursor.fetchall()
            print(f"  Statuts existants: {[s[0] for s in statuses]}")
        
        # Examiner la table Clients
        if any(t[0] == 'Clients' for t in tables):
            print(f"\n👥 Analyse de la table Clients:")
            cursor.execute("SELECT COUNT(*) FROM Clients")
            client_count = cursor.fetchone()[0]
            print(f"  Nombre de clients: {client_count}")
            
            if client_count > 0:
                cursor.execute("SELECT Id, Name FROM Clients LIMIT 5")
                clients = cursor.fetchall()
                print("  Échantillon:")
                for client in clients:
                    print(f"    - ID: {client[0]}, Nom: {client[1]}")
        
        conn.close()
        print(f"\n✅ Analyse terminée avec succès")
        
    except Exception as e:
        print(f"❌ Erreur lors de l'analyse: {e}")

if __name__ == "__main__":
    main()