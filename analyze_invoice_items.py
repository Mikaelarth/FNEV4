import sqlite3

# Connexion à la vraie base de données
conn = sqlite3.connect(r'D:\PROJET\FNEV4\data\FNEV4.db')
cursor = conn.cursor()

# Analyser la facture 556440 spécifiquement
cursor.execute("SELECT * FROM FneInvoices WHERE InvoiceNumber = '556440'")
facture = cursor.fetchone()

if facture:
    print("=== FACTURE 556440 DÉTAILLÉE ===")
    print(f"ID: {facture[0]}")
    print(f"Numéro: {facture[1]}")
    print(f"Date: {facture[4]}")
    print(f"Client ID: {facture[5]}")
    print(f"Code Client: {facture[6]}")
    print(f"Montant HT: {facture[11]}")
    print(f"Montant TVA: {facture[12]}")
    print(f"Montant TTC: {facture[13]}")
    print(f"Statut: {facture[15]}")
    
    # Chercher les articles de cette facture
    facture_id = facture[0]
    cursor.execute("SELECT * FROM FneInvoiceItems WHERE FneInvoiceId = ?", (facture_id,))
    items = cursor.fetchall()
    
    print(f"\n=== ARTICLES DE LA FACTURE ===")
    print(f"Nombre d'articles trouvés: {len(items)}")
    
    if items:
        for item in items:
            print(f"  - Code: {item[2]}")
            print(f"    Description: {item[3]}")
            print(f"    Prix unitaire: {item[4]}")
            print(f"    Quantité: {item[5]}")
            print(f"    Montant HT: {item[10]}")
            print()
    else:
        print("❌ AUCUN ARTICLE TROUVÉ pour cette facture !")
        print("   C'est exactement le problème que vous voyez dans l'interface.")
        
        # Vérifier s'il y a des articles pour d'autres factures
        cursor.execute("SELECT DISTINCT FneInvoiceId FROM FneInvoiceItems LIMIT 5")
        other_items = cursor.fetchall()
        if other_items:
            print(f"\nIl y a des articles pour {len(other_items)} autres factures.")
        else:
            print("\n❌ AUCUNE FACTURE N'A D'ARTICLES dans toute la base !")

# Analyser le problème d'import
print("\n=== ANALYSE DU PROBLÈME D'IMPORT ===")
cursor.execute("SELECT COUNT(*) FROM FneInvoices")
total_factures = cursor.fetchone()[0]

cursor.execute("SELECT COUNT(*) FROM FneInvoiceItems")
total_items = cursor.fetchone()[0]

print(f"📊 Total factures: {total_factures}")
print(f"📦 Total articles: {total_items}")

if total_factures > 0 and total_items == 0:
    print("🔍 DIAGNOSTIC: Les factures ont été importées mais pas les articles.")
    print("   Il faut vérifier le processus d'import Sage100 pour les lignes de facture.")

conn.close()