import sqlite3
import uuid
from datetime import datetime

# Connexion à la vraie base de données
conn = sqlite3.connect(r'D:\PROJET\FNEV4\data\FNEV4.db')
cursor = conn.cursor()

# Créer d'abord un VatType de test si nécessaire
vat_type_id = str(uuid.uuid4())
cursor.execute("""
INSERT OR IGNORE INTO VatTypes (Id, Code, Description, Rate, IsActive, CreatedAt, UpdatedAt, IsDeleted)
VALUES (?, 'TVA', 'TVA Standard 18%', 18.0, 1, ?, ?, 0)
""", (vat_type_id, datetime.utcnow().isoformat(), datetime.utcnow().isoformat()))

# Récupérer l'ID du type TVA
cursor.execute("SELECT Id FROM VatTypes WHERE Code = 'TVA' LIMIT 1")
vat_type_result = cursor.fetchone()
if vat_type_result:
    vat_type_id = vat_type_result[0]

# Récupérer la facture 556440
cursor.execute("SELECT Id FROM FneInvoices WHERE InvoiceNumber = '556440'")
facture = cursor.fetchone()

if facture:
    facture_id = facture[0]
    print(f"✅ Facture trouvée: {facture_id}")
    
    # Créer des articles de test
    articles_test = [
        {
            'code': 'PROD001',
            'description': 'Produit Test 1',
            'prix': 1000.00,
            'quantite': 5.0,
            'unite': 'pcs'
        },
        {
            'code': 'PROD002', 
            'description': 'Produit Test 2',
            'prix': 2500.50,
            'quantite': 2.0,
            'unite': 'kg'
        },
        {
            'code': 'SERV001',
            'description': 'Service Test',
            'prix': 15000.00,
            'quantite': 1.0,
            'unite': 'service'
        }
    ]
    
    for i, article in enumerate(articles_test):
        item_id = str(uuid.uuid4())
        montant_ht = article['prix'] * article['quantite']
        montant_tva = montant_ht * 0.18  # 18% TVA
        montant_ttc = montant_ht + montant_tva
        
        cursor.execute("""
        INSERT INTO FneInvoiceItems (
            Id, FneInvoiceId, ProductCode, Description, UnitPrice, Quantity, 
            MeasurementUnit, VatTypeId, VatCode, VatRate, LineAmountHT, 
            LineVatAmount, LineAmountTTC, ItemDiscount, LineOrder, 
            CreatedAt, UpdatedAt, IsDeleted
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, (
            item_id, facture_id, article['code'], article['description'],
            article['prix'], article['quantite'], article['unite'],
            vat_type_id, 'TVA', 18.0, montant_ht, montant_tva, montant_ttc,
            0.0, i + 1, datetime.utcnow().isoformat(), datetime.utcnow().isoformat(), 0
        ))
        
        print(f"➕ Article ajouté: {article['code']} - {montant_ht}€ HT")
    
    # Vérifier que les articles ont été ajoutés
    cursor.execute("SELECT COUNT(*) FROM FneInvoiceItems WHERE FneInvoiceId = ?", (facture_id,))
    nb_articles = cursor.fetchone()[0]
    
    print(f"✅ {nb_articles} articles ajoutés à la facture 556440")
    
    # Sauvegarder les changements
    conn.commit()
    print("💾 Changements sauvegardés dans la base")
    
else:
    print("❌ Facture 556440 non trouvée")

conn.close()
print("\n🎯 Maintenant testez la fenêtre de détails dans l'application !")