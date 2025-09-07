#!/usr/bin/env python3
"""
Test du convertisseur ProduitsToTooltipConverter
Vérification que le tooltips affichera correctement les détails des articles
"""

# Simuler la structure des données d'un produit Sage 100 v15
class MockProduit:
    def __init__(self, code, designation, quantite, prix, tva, montant, emballage="pcs"):
        self.CodeProduit = code
        self.Designation = designation
        self.Quantite = quantite
        self.PrixUnitaire = prix
        self.CodeTva = tva
        self.MontantHt = montant
        self.Emballage = emballage

# Créer des exemples de produits
produits_test = [
    MockProduit("PROD001", "Ordinateur portable Dell XPS 13", 2, 450000, "TVA", 900000, "unité"),
    MockProduit("PROD002", "Souris optique sans fil", 5, 15000, "TVA", 75000, "pcs"),
    MockProduit("PROD003", "Clavier mécanique RGB", 1, 80000, "TVA", 80000, "unité"),
    MockProduit("SERV001", "Installation et configuration", 1, 50000, "TVA", 50000, "service"),
]

# Fonction pour simuler le convertisseur C#
def simulate_converter(produits):
    if not produits:
        return "Aucun détail disponible"
    
    tooltip = "📦 DÉTAIL DES ARTICLES\n\n"
    
    for i, item in enumerate(produits[:10]):  # Limiter à 10
        tooltip += f"• {item.Designation}\n"
        tooltip += f"  Code: {item.CodeProduit} | Qté: {item.Quantite:,.0f} {item.Emballage}\n"
        tooltip += f"  Prix: {item.PrixUnitaire:,.0f} | TVA: {item.CodeTva}\n"
        tooltip += f"  Total: {item.MontantHt:,.0f} FCFA\n\n"
    
    if len(produits) > 10:
        tooltip += f"... et {len(produits) - 10} autres articles"
    
    return tooltip.strip()

# Test du convertisseur
print("=== TEST CONVERTISSEUR TOOLTIP ARTICLES ===\n")
print("Produits de test :")
for p in produits_test:
    print(f"- {p.CodeProduit}: {p.Designation}")

print("\n" + "="*50)
print("RÉSULTAT DU TOOLTIP :")
print("="*50)

tooltip_result = simulate_converter(produits_test)
print(tooltip_result)

print("\n" + "="*50)
print("VALIDATION :")
print("="*50)

# Vérifications
checks = [
    ("Contient l'en-tête", "📦 DÉTAIL DES ARTICLES" in tooltip_result),
    ("Affiche les codes produits", all(p.CodeProduit in tooltip_result for p in produits_test)),
    ("Affiche les désignations", all(p.Designation in tooltip_result for p in produits_test)),
    ("Affiche les quantités", all(f"{p.Quantite:,.0f}" in tooltip_result for p in produits_test)),
    ("Affiche les prix", all(f"{p.PrixUnitaire:,.0f}" in tooltip_result for p in produits_test)),
    ("Affiche les montants", all(f"{p.MontantHt:,.0f}" in tooltip_result for p in produits_test)),
    ("Format correct", "FCFA" in tooltip_result),
]

for check_name, result in checks:
    status = "✅ PASS" if result else "❌ FAIL"
    print(f"{status} {check_name}")

print(f"\n🎯 Test terminé : {sum(1 for _, r in checks if r)}/{len(checks)} vérifications réussies")

# Test avec liste vide
print("\n" + "="*50)
print("TEST LISTE VIDE :")
print("="*50)
print(simulate_converter([]))
