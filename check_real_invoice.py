import sqlite3

# Connexion à la vraie base de données
conn = sqlite3.connect(r'D:\PROJET\FNEV4\data\FNEV4.db')
cursor = conn.cursor()

# Analyser la facture 556274 spécifiquement
cursor.execute("SELECT Id, InvoiceNumber, TotalAmountHT, TotalAmountTTC FROM FneInvoices WHERE InvoiceNumber = '556274'")
facture = cursor.fetchone()

if facture:
    print("=== FACTURE 556274 ANALYSÉE ===")
    print(f"ID: {facture[0]}")
    print(f"Numéro: {facture[1]}")
    print(f"Montant HT: {facture[2]}")
    print(f"Montant TTC: {facture[3]}")
    
    # Chercher les articles de cette facture
    facture_id = facture[0]
    cursor.execute("SELECT COUNT(*) FROM FneInvoiceItems WHERE FneInvoiceId = ?", (facture_id,))
    nb_articles = cursor.fetchone()[0]
    
    print(f"Nombre d'articles: {nb_articles}")
    
    if nb_articles == 0:
        print("❌ AUCUN ARTICLE pour cette facture !")
        print("Le problème persiste pour les factures existantes.")
    else:
        cursor.execute("SELECT ProductCode, Description, Quantity, UnitPrice FROM FneInvoiceItems WHERE FneInvoiceId = ?", (facture_id,))
        articles = cursor.fetchall()
        print("✅ Articles trouvés:")
        for art in articles:
            print(f"  - {art[0]}: {art[1]} (Qté: {art[2]}, Prix: {art[3]})")
else:
    print("❌ Facture 556274 non trouvée")

# Vérifier combien de factures ont des articles
cursor.execute("""
SELECT 
    COUNT(DISTINCT f.Id) as factures_avec_articles,
    (SELECT COUNT(*) FROM FneInvoices) as total_factures
FROM FneInvoices f 
INNER JOIN FneInvoiceItems i ON f.Id = i.FneInvoiceId
""")
stats = cursor.fetchone()

print(f"\n=== STATISTIQUES GLOBALES ===")
print(f"Factures avec articles: {stats[0] if stats[0] else 0}")
print(f"Total factures: {stats[1]}")
print(f"Factures SANS articles: {stats[1] - (stats[0] if stats[0] else 0)}")

conn.close()