import pandas as pd

# Créer des données de test avec différents moyens de paiement
test_data = [
    {
        "CodeClient": "CLI001",
        "NomRaisonSociale": "ARTHUR LE GRAND SARL",
        "Template": "B2B",
        "ClientNcc": "1234567890",
        "NomCommercial": "Arthur Le Grand",
        "Adresse": "123 Boulevard de la Paix",
        "Ville": "Abidjan",
        "CodePostal": "01001",
        "Pays": "Côte d'Ivoire",
        "Telephone": "+225 01 02 03 04",
        "Email": "arthur@legrand.ci",
        "Representant": "Jean KOUAME",
        "NumeroFiscal": "TIN123456",
        "Devise": "XOF",
        "Actif": "Oui",
        "Notes": "Client VIP",
        "Moyen_Paiement": "card"  # Test avec carte
    },
    {
        "CodeClient": "CLI002",
        "NomRaisonSociale": "MARIE KOUASSI",
        "Template": "B2C",
        "ClientNcc": "",
        "NomCommercial": "Marie Kouassi Boutique",
        "Adresse": "45 Rue du Commerce",
        "Ville": "Bouaké",
        "CodePostal": "02001",
        "Pays": "Côte d'Ivoire",
        "Telephone": "+225 05 06 07 08",
        "Email": "marie.kouassi@gmail.com",
        "Representant": "Paul DIALLO",
        "NumeroFiscal": "",
        "Devise": "XOF",
        "Actif": "Oui",
        "Notes": "Particulier",
        "Moyen_Paiement": "mobile-money"  # Test avec mobile money
    },
    {
        "CodeClient": "CLI003",
        "NomRaisonSociale": "MINISTERE SANTE",
        "Template": "B2G",
        "ClientNcc": "9876543210",
        "NomCommercial": "Min. Santé et Hygiène",
        "Adresse": "Plateau Tour C",
        "Ville": "Abidjan",
        "CodePostal": "01000",
        "Pays": "Côte d'Ivoire",
        "Telephone": "+225 20 21 22 23",
        "Email": "contact@sante.gouv.ci",
        "Representant": "Dr. BAMBA",
        "NumeroFiscal": "GOV789",
        "Devise": "XOF",
        "Actif": "Oui",
        "Notes": "Client gouvernemental",
        "Moyen_Paiement": "bank-transfer"  # Test avec virement
    },
    {
        "CodeClient": "CLI004",
        "NomRaisonSociale": "BOUTIQUE CENTRALE",
        "Template": "B2C",
        "ClientNcc": "",
        "NomCommercial": "Boutique Centrale",
        "Adresse": "Centre-ville",
        "Ville": "Yamoussoukro",
        "CodePostal": "03001",
        "Pays": "Côte d'Ivoire",
        "Telephone": "+225 30 31 32 33",
        "Email": "contact@boutiquecentrale.ci",
        "Representant": "Yves KONE",
        "NumeroFiscal": "",
        "Devise": "XOF",
        "Actif": "Oui",
        "Notes": "Boutique locale",
        "Moyen_Paiement": "check"  # Test avec chèque
    },
    {
        "CodeClient": "CLI005",
        "NomRaisonSociale": "ENTREPRISE CREDIT",
        "Template": "B2B",
        "ClientNcc": "5555666677",
        "NomCommercial": "Entreprise Crédit",
        "Adresse": "Zone industrielle",
        "Ville": "San Pedro",
        "CodePostal": "04001",
        "Pays": "Côte d'Ivoire",
        "Telephone": "+225 40 41 42 43",
        "Email": "contact@entreprisecredit.ci",
        "Representant": "Fatou TRAORE",
        "NumeroFiscal": "TIN567890",
        "Devise": "XOF",
        "Actif": "Oui",
        "Notes": "Client à crédit",
        "Moyen_Paiement": "credit"  # Test avec crédit
    }
]

# Créer le DataFrame et sauvegarder en Excel
df = pd.DataFrame(test_data)

# Sauvegarder le fichier Excel
output_file = "test_payment_method_import.xlsx"
df.to_excel(output_file, index=False, sheet_name='Clients')

print(f"Fichier Excel créé: {output_file}")
print("Colonnes:", df.columns.tolist())
print("Moyens de paiement testés:")
for i, row in df.iterrows():
    print(f"  {row['CodeClient']}: {row['Moyen_Paiement']}")
