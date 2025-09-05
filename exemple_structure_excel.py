# Exemple de fichier Excel de test pour FNE Processor
# Ce fichier montre la structure attendue pour les exports Sage 100

"""
Structure attendue pour chaque feuille Excel (une feuille = une facture) :

ENTÊTE (Colonne A):
Ligne 3:  Numéro de facture        (ex: FAC001)
Ligne 5:  Code client              (ex: 1999 pour client divers, autre pour client normal)
Ligne 6:  NCC client normal        (ex: 2354552Q - uniquement si code ≠ 1999)
Ligne 8:  Date                     (ex: 2025-01-15)
Ligne 10: Point de vente           (ex: Gestoci)
Ligne 11: Intitulé client          (ex: DIVERS CLIENTS CARBURANTS - si code = 1999)
Ligne 13: Nom réel client divers   (ex: ARTHUR LE GRAND - si code = 1999)
Ligne 15: NCC client divers        (ex: 1205425Z - si code = 1999)
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

Feuille "Facture_001" (Client divers - code 1999):
A3: 702442
A5: 1999
A6: 2354552Q (NCC générique client divers)
A8: 45880 → 11/08/2025
A10: Gestoci
A11: DIVERS CLIENTS CARBURANTS
A13: ARTHUR LE GRAND (vrai nom du client)
A15: 1205425Z (NCC spécifique du client divers)
A17: (vide si pas d'avoir)

Ligne 20:
B20: ORD001    C20: Ordinateur Dell    D20: 800000    E20: 1    F20: pcs    G20: TVA    H20: 800000
Ligne 21:
B21: SOU001    C21: Souris sans fil    D21: 25000     E21: 2    F21: pcs    G21: TVA    H21: 50000

NOTES IMPORTANTES:
- Une feuille Excel = une facture
- CODE CLIENT 1999 = client divers (nom réel à la ligne 13, NCC spécifique à la ligne 15)
- CODE CLIENT ≠ 1999 = client normal (NCC à la ligne 6, nom depuis base de données)
- Le NCC de l'entreprise émettrice DOIT être configuré dans FNEV4 (pas dans Excel!)
- Les cellules vides sont autorisées sauf pour les champs obligatoires
- Les montants sont en francs CFA (sans décimales)
- Les codes TVA supportés: TVA, TVAB, TVAC, TVAD
"""

# Pour créer un fichier de test, utilisez Excel avec cette structure
# ou utilisez le script create_test_excel.py fourni
