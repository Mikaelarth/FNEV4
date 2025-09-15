import sqlite3

# Connexion à la vraie base de données
conn = sqlite3.connect(r'D:\PROJET\FNEV4\data\FNEV4.db')
cursor = conn.cursor()

print("=== ANALYSE COMPLÈTE DES FACTURES ===\n")

# 1. Statistiques générales
cursor.execute("SELECT COUNT(*) FROM FneInvoices")
total_factures = cursor.fetchone()[0]

cursor.execute("SELECT COUNT(*) FROM FneInvoiceItems")
total_articles = cursor.fetchone()[0]

cursor.execute("SELECT COUNT(DISTINCT FneInvoiceId) FROM FneInvoiceItems")
factures_avec_articles = cursor.fetchone()[0] if cursor.fetchone() else 0

print(f"📊 STATISTIQUES GÉNÉRALES")
print(f"   Total factures: {total_factures}")
print(f"   Total articles: {total_articles}")
print(f"   Factures avec articles: {factures_avec_articles}")
print(f"   Factures SANS articles: {total_factures - factures_avec_articles}\n")

# 2. Examiner quelques factures représentatives
print("🔍 ÉCHANTILLON DE FACTURES")
cursor.execute("""
SELECT InvoiceNumber, TotalAmountHT, TotalAmountTTC, Status, InvoiceDate
FROM FneInvoices 
ORDER BY InvoiceNumber 
LIMIT 10
""")
factures_sample = cursor.fetchall()

for f in factures_sample:
    # Compter les articles pour chaque facture
    cursor.execute("SELECT COUNT(*) FROM FneInvoiceItems WHERE FneInvoiceId = (SELECT Id FROM FneInvoices WHERE InvoiceNumber = ?)", (f[0],))
    nb_articles = cursor.fetchone()[0]
    
    print(f"   🧾 {f[0]} - {f[1]}€ HT - {f[2]}€ TTC - {f[3]} - {nb_articles} articles")

# 3. Identifier la facture avec articles (pour voir la structure)
print(f"\n✅ FACTURE AVEC ARTICLES (pour référence)")
cursor.execute("""
SELECT f.InvoiceNumber, i.ProductCode, i.Description, i.Quantity, i.UnitPrice, i.LineAmountHT
FROM FneInvoices f
INNER JOIN FneInvoiceItems i ON f.Id = i.FneInvoiceId
LIMIT 5
""")
articles_existants = cursor.fetchall()

if articles_existants:
    for art in articles_existants:
        print(f"   📦 Facture {art[0]}: {art[1]} - {art[2]} (Qté: {art[3]}, Prix: {art[4]}€, Total: {art[5]}€)")
else:
    print("   ❌ Aucun article trouvé dans toute la base")

# 4. Vérifier les types de TVA disponibles
print(f"\n💰 TYPES DE TVA DISPONIBLES")
cursor.execute("SELECT Code, Description, Rate FROM VatTypes")
vat_types = cursor.fetchall()

if vat_types:
    for vat in vat_types:
        print(f"   📋 {vat[0]}: {vat[1]} ({vat[2]}%)")
else:
    print("   ❌ Aucun type de TVA configuré")

# 5. Problème principal identifié
print(f"\n🚨 PROBLÈME IDENTIFIÉ")
print(f"   • Le service d'import Sage100 original ne créait PAS les FneInvoiceItems")
print(f"   • Les factures sont complètes mais sans détail des articles")
print(f"   • Il faut soit réimporter, soit créer un script de migration")

conn.close()

print(f"\n💡 SOLUTIONS POSSIBLES:")
print(f"   1. Réimporter tous les fichiers Excel Sage100 (recommandé)")
print(f"   2. Créer un script de migration pour reconstruire les articles")
print(f"   3. Supprimer les factures sans articles et réimporter")