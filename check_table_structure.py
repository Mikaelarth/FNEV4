import sqlite3

# Connexion à la vraie base de données
conn = sqlite3.connect(r'D:\PROJET\FNEV4\data\FNEV4.db')
cursor = conn.cursor()

print("=== STRUCTURE EXACTE DES TABLES ===\n")

# 1. Structure de la table FneInvoices
print("📋 TABLE FneInvoices:")
cursor.execute("PRAGMA table_info(FneInvoices)")
colonnes_factures = cursor.fetchall()
for col in colonnes_factures:
    print(f"   {col[1]} ({col[2]}) - PK: {col[5] == 1} - NOT NULL: {col[3] == 1}")

print(f"\n📦 TABLE FneInvoiceItems:")
cursor.execute("PRAGMA table_info(FneInvoiceItems)")
colonnes_items = cursor.fetchall()
for col in colonnes_items:
    print(f"   {col[1]} ({col[2]}) - PK: {col[5] == 1} - NOT NULL: {col[3] == 1}")

# 2. Vérifier les clés étrangères
print(f"\n🔗 CLÉS ÉTRANGÈRES FneInvoiceItems:")
cursor.execute("PRAGMA foreign_key_list(FneInvoiceItems)")
fk_items = cursor.fetchall()
if fk_items:
    for fk in fk_items:
        print(f"   {fk[3]} -> {fk[2]}.{fk[4]}")
else:
    print("   ❌ Aucune clé étrangère trouvée")

# 3. Vérifier avec un exemple concret
print(f"\n🔍 EXEMPLE CONCRET - Facture 556274:")
cursor.execute("SELECT Id, InvoiceNumber FROM FneInvoices WHERE InvoiceNumber = '556274'")
facture_example = cursor.fetchone()

if facture_example:
    facture_id = facture_example[0]
    print(f"   ID Facture: {facture_id}")
    
    # Chercher les articles avec cette clé
    cursor.execute("SELECT Id, ProductCode, Description FROM FneInvoiceItems WHERE FneInvoiceId = ?", (facture_id,))
    articles = cursor.fetchall()
    print(f"   Articles trouvés: {len(articles)}")
    
    for art in articles:
        print(f"     - {art[1]}: {art[2]}")
    
    # Vérifier aussi avec une requête JOIN
    cursor.execute("""
    SELECT f.InvoiceNumber, i.ProductCode, i.Description 
    FROM FneInvoices f 
    LEFT JOIN FneInvoiceItems i ON f.Id = i.FneInvoiceId 
    WHERE f.InvoiceNumber = '556274'
    """)
    join_result = cursor.fetchall()
    print(f"   Résultat JOIN: {len(join_result)} lignes")
    for jr in join_result:
        print(f"     - Facture: {jr[0]}, Article: {jr[1] or 'NULL'}")

# 4. Vérifier toutes les combinaisons possibles
print(f"\n📊 ANALYSE GLOBALE DES LIENS:")
cursor.execute("""
SELECT 
    (SELECT COUNT(*) FROM FneInvoices) as total_factures,
    (SELECT COUNT(*) FROM FneInvoiceItems) as total_articles,
    (SELECT COUNT(DISTINCT FneInvoiceId) FROM FneInvoiceItems) as factures_avec_articles
""")
stats = cursor.fetchone()
print(f"   Total factures: {stats[0]}")
print(f"   Total articles: {stats[1]}")
print(f"   Factures avec articles: {stats[2]}")

# 5. Chercher une facture qui a des articles pour voir la structure
cursor.execute("""
SELECT f.InvoiceNumber, f.Id, i.ProductCode, i.Description, i.FneInvoiceId
FROM FneInvoices f 
INNER JOIN FneInvoiceItems i ON f.Id = i.FneInvoiceId
LIMIT 3
""")
exemples_avec_articles = cursor.fetchall()

print(f"\n✅ EXEMPLES DE FACTURES AVEC ARTICLES:")
for ex in exemples_avec_articles:
    print(f"   Facture {ex[0]} (ID: {ex[1]}) -> Article: {ex[2]} (FneInvoiceId: {ex[4]})")
    
conn.close()