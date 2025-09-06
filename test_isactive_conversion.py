#!/usr/bin/env python3
"""
Test de la logique de conversion IsActive
Analyse du code C# : client.IsActive = activeValue != 'non' && activeValue != 'false' && activeValue != '0';
"""

def test_isactive_conversion(active_value):
    """
    Simule la logique C# : activeValue != 'non' && activeValue != 'false' && activeValue != '0'
    """
    if active_value is None:
        return True  # Valeur par défaut selon Client.cs
    
    active_value_lower = str(active_value).lower().strip()
    
    # Logique C# convertie en Python
    result = active_value_lower != 'non' and active_value_lower != 'false' and active_value_lower != '0'
    
    return result

# Test avec différentes valeurs possibles dans Excel
test_values = [
    "Oui",
    "oui", 
    "OUI",
    "True",
    "true",
    "TRUE",
    "1",
    "Non",
    "non",
    "NON", 
    "False",
    "false",
    "FALSE",
    "0",
    "",
    None,
    "Actif",
    "Inactif",
    "Yes",
    "No",
    "x",
    " Oui ",  # avec espaces
    " Non ",  # avec espaces
]

print("=== Test de conversion IsActive ===")
print("Valeur Excel -> Booléen attendu")
print("-" * 40)

for value in test_values:
    result = test_isactive_conversion(value)
    print(f"{str(value):15} -> {result}")

print("\n=== Résumé ===")
print("VRAI (1) : Tout sauf 'non', 'false', '0' (insensible à la casse)")
print("FAUX (0) : Seulement 'non', 'false', '0' (insensible à la casse)")
