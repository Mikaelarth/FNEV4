# Exemple de fichier Excel de test pour FNE Processor
# Ce fichier montre la structure attendue pour les exports Sage 100

"""
Structure attendue pour chaque feuille Excel (une feuille = une facture) :

ENTÊTE (Colonne A):
Ligne 3:  Numéro de facture        (ex: FAC001)
Ligne 5:  Code client              (ex: 1999 pour client divers)
Ligne 6:  NCC entreprise           (ex: 9606123E)
Ligne 8:  Date                     (ex: 2025-01-15)
Ligne 10: Point de vente           (ex: 01)
Ligne 11: Intitulé client divers   (ex: Entreprise XYZ)
Ligne 13: Nom client divers        (ex: Monsieur Dupont)
Ligne 15: NCC client divers        (ex: 9502363N)
Ligne 17: Numéro facture avoir     (ex: FAC000 si c'est un avoir)

PRODUITS (à partir de ligne 20):
Colonne B: Code produit     (ex: PROD001)
Colonne C: Désignation      (ex: Ordinateur portable)
Colonne D: Prix unitaire    (ex: 500000)
Colonne E: Quantité         (ex: 2)
Colonne F: Emballage        (ex: pcs)
Colonne G: TVA              (ex: TVA)
Colonne H: Montant HT       (ex: 1000000)

EXEMPLE CONCRET:

Feuille "Facture_001":
A3: FAC001
A5: 1999
A6: 9606123E
A8: 2025-01-15
A10: 01
A11: SARL TechnoPlus
A13: Monsieur Martin
A15: 9502363N
A17: (vide si pas d'avoir)

Ligne 20:
B20: ORD001    C20: Ordinateur Dell    D20: 800000    E20: 1    F20: pcs    G20: TVA    H20: 800000
Ligne 21:
B21: SOU001    C21: Souris sans fil    D21: 25000     E21: 2    F21: pcs    G21: TVA    H21: 50000

NOTES IMPORTANTES:
- Une feuille Excel = une facture
- Les cellules vides sont autorisées sauf pour les champs obligatoires
- Les montants sont en francs CFA (sans décimales)
- Les codes TVA supportés: TVA, TVAB, TVAC, TVAD
- Code client 1999 = client divers
"""

# Pour créer un fichier de test, utilisez Excel avec cette structure
# ou utilisez le script create_test_excel.py fourni
