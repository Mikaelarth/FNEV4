#!/usr/bin/env python3
"""
Script de test pour vérifier le chargement des données 
dans l'interface de certification manuelle FNEV4
"""
import sqlite3
import os
import sys
from datetime import datetime

def test_certification_data_loading():
    """
    Test du chargement des données pour la certification manuelle
    """
    db_path = r"d:\PROJET\FNE\FNEV4\data\FNEV4.db"
    
    if not os.path.exists(db_path):
        print(f"❌ Base de données introuvable: {db_path}")
        return False
    
    print(f"✅ Base de données trouvée: {db_path}")
    
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Test 1: Vérifier la structure de la table FneInvoices
        print("\n🔍 Test 1: Structure de la table FneInvoices")
        cursor.execute("PRAGMA table_info(FneInvoices)")
        columns = cursor.fetchall()
        essential_columns = ['Id', 'ClientId', 'Status', 'InvoiceNumber', 'TotalAmountTTC']
        
        found_columns = [col[1] for col in columns]
        for col in essential_columns:
            if col in found_columns:
                print(f"  ✅ Colonne {col} présente")
            else:
                print(f"  ❌ Colonne {col} manquante")
                return False
        
        # Test 2: Vérifier les données disponibles pour certification
        print("\n🔍 Test 2: Factures disponibles pour certification")
        cursor.execute("SELECT COUNT(*) FROM FneInvoices WHERE Status = 'draft'")
        draft_count = cursor.fetchone()[0]
        print(f"  Factures en statut 'draft': {draft_count}")
        
        if draft_count == 0:
            print("  ❌ Aucune facture en statut 'draft' pour certification")
            return False
        
        # Test 3: Vérifier le format des données
        print("\n🔍 Test 3: Format des données")
        cursor.execute("""
            SELECT Id, ClientId, Status, InvoiceNumber, TotalAmountTTC 
            FROM FneInvoices 
            WHERE Status = 'draft' 
            LIMIT 3
        """)
        samples = cursor.fetchall()
        
        for i, sample in enumerate(samples, 1):
            print(f"  Facture {i}:")
            print(f"    ID: {sample[0]} (type: {type(sample[0]).__name__})")
            print(f"    ClientId: {sample[1]} (type: {type(sample[1]).__name__})")
            print(f"    Status: {sample[2]}")
            print(f"    Numéro: {sample[3]}")
            print(f"    Montant: {sample[4]}")
            
            # Vérifier que les GUIDs sont valides
            try:
                if len(str(sample[0])) == 36 and str(sample[0]).count('-') == 4:
                    print(f"    ✅ ID GUID valide")
                else:
                    print(f"    ❌ ID GUID invalide")
                    
                if len(str(sample[1])) == 36 and str(sample[1]).count('-') == 4:
                    print(f"    ✅ ClientId GUID valide")
                else:
                    print(f"    ❌ ClientId GUID invalide")
            except Exception as e:
                print(f"    ❌ Erreur validation GUID: {e}")
        
        # Test 4: Vérifier les clients associés
        print("\n🔍 Test 4: Clients associés aux factures")
        cursor.execute("""
            SELECT COUNT(DISTINCT ClientId) 
            FROM FneInvoices 
            WHERE Status = 'draft'
        """)
        unique_clients = cursor.fetchone()[0]
        print(f"  Clients uniques avec factures draft: {unique_clients}")
        
        cursor.execute("SELECT COUNT(*) FROM Clients")
        total_clients = cursor.fetchone()[0]
        print(f"  Total clients en base: {total_clients}")
        
        # Test 5: Vérifier la jointure FneInvoices <-> Clients
        print("\n🔍 Test 5: Jointure FneInvoices <-> Clients")
        cursor.execute("""
            SELECT f.InvoiceNumber, c.Name, c.CompanyName
            FROM FneInvoices f
            LEFT JOIN Clients c ON f.ClientId = c.Id
            WHERE f.Status = 'draft'
            LIMIT 3
        """)
        joined_data = cursor.fetchall()
        
        for invoice_num, client_name, company_name in joined_data:
            client_display = company_name or client_name or "N/A"
            print(f"  Facture {invoice_num} -> Client: {client_display}")
            if client_display == "N/A":
                print(f"    ⚠️ Client non trouvé ou données manquantes")
            else:
                print(f"    ✅ Jointure réussie")
        
        conn.close()
        
        print(f"\n✅ Tests terminés avec succès")
        print(f"📊 Résumé:")
        print(f"  - {draft_count} factures disponibles pour certification")
        print(f"  - {unique_clients} clients uniques concernés")
        print(f"  - Structure de données correcte")
        
        return True
        
    except Exception as e:
        print(f"❌ Erreur lors des tests: {e}")
        return False

def test_logging_system():
    """
    Test du système de logging centralisé
    """
    print("\n🔍 Test du système de logging centralisé")
    
    logs_dir = r"d:\PROJET\FNE\FNEV4\Data\Logs"
    if not os.path.exists(logs_dir):
        print(f"❌ Répertoire de logs non trouvé: {logs_dir}")
        return False
    
    # Chercher le fichier de log le plus récent
    log_files = [f for f in os.listdir(logs_dir) if f.endswith('.log')]
    if not log_files:
        print(f"❌ Aucun fichier de log trouvé dans {logs_dir}")
        return False
    
    latest_log = max(log_files, key=lambda f: os.path.getmtime(os.path.join(logs_dir, f)))
    log_path = os.path.join(logs_dir, latest_log)
    
    print(f"  Fichier de log le plus récent: {latest_log}")
    print(f"  Taille: {os.path.getsize(log_path)} octets")
    
    # Vérifier que le fichier n'est pas vide et contient des entrées récentes
    try:
        with open(log_path, 'r', encoding='utf-8') as f:
            lines = f.readlines()
            
        if len(lines) > 0:
            print(f"  ✅ {len(lines)} entrées de log trouvées")
            print(f"  Dernière entrée: {lines[-1].strip()[:100]}...")
            return True
        else:
            print(f"  ❌ Fichier de log vide")
            return False
            
    except Exception as e:
        print(f"  ❌ Erreur lecture du log: {e}")
        return False

def main():
    """
    Fonction principale de test
    """
    print("=" * 60)
    print("FNEV4 - Test du système de certification manuelle")
    print("=" * 60)
    print(f"Date/Heure: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    
    # Test 1: Système de logging
    logging_ok = test_logging_system()
    
    # Test 2: Données de certification
    data_ok = test_certification_data_loading()
    
    print("\n" + "=" * 60)
    print("RÉSUMÉ FINAL")
    print("=" * 60)
    
    if logging_ok and data_ok:
        print("✅ TOUS LES TESTS RÉUSSIS")
        print("👉 L'interface de certification manuelle devrait fonctionner correctement")
        print("👉 Vous pouvez maintenant lancer l'application et tester l'interface")
        return True
    else:
        print("❌ CERTAINS TESTS ONT ÉCHOUÉ")
        if not logging_ok:
            print("   - Problème avec le système de logging")
        if not data_ok:
            print("   - Problème avec les données de certification")
        return False

if __name__ == "__main__":
    success = main()
    sys.exit(0 if success else 1)