import sqlite3

# Connexion à la base de données
conn = sqlite3.connect(r'C:\wamp64\www\FNEV4\data\FNEV4.db')
cursor = conn.cursor()

# Vérifier la facture 556440
cursor.execute("SELECT Id, InvoiceNumber, TotalAmountHT, TotalAmountTTC FROM FneInvoices WHERE InvoiceNumber = '556440'")
facture = cursor.fetchone()

if facture:
    print(f"Facture trouvée: ID={facture[0]}, Numéro={facture[1]}, HT={facture[2]}, TTC={facture[3]}")
    
    # Chercher les articles de cette facture
    cursor.execute("SELECT ProductCode, Description, Quantity, UnitPrice, LineAmountHT FROM FneInvoiceItems WHERE FneInvoiceId = ?", (facture[0],))
    items = cursor.fetchall()
    
    print(f"Nombre d'articles trouvés: {len(items)}")
    for item in items:
        print(f"  - {item[0]}: {item[1]} (Qté: {item[2]}, Prix: {item[3]}, Total: {item[4]})")
else:
    print("Facture 556440 non trouvée")

# Vérifier toutes les factures et leurs articles
cursor.execute("SELECT COUNT(*) FROM FneInvoices")
total_factures = cursor.fetchone()[0]

cursor.execute("SELECT COUNT(*) FROM FneInvoiceItems")
total_items = cursor.fetchone()[0]

print(f"\nTotal factures: {total_factures}")
print(f"Total articles: {total_items}")

# Vérifier les schémas des tables
cursor.execute("PRAGMA table_info(FneInvoices)")
colonnes_factures = cursor.fetchall()
print(f"\nColonnes table FneInvoices:")
for col in colonnes_factures:
    print(f"  - {col[1]} ({col[2]})")

cursor.execute("PRAGMA table_info(FneInvoiceItems)")
colonnes_items = cursor.fetchall()
print(f"\nColonnes table FneInvoiceItems:")
for col in colonnes_items:
    print(f"  - {col[1]} ({col[2]})")

conn.close()