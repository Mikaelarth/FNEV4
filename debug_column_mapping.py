#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Test de debug pour vérifier le mapping des colonnes Excel
"""

import pandas as pd

def test_column_mapping():
    """Test pour identifier le problème de mapping"""
    
    # Lire le fichier Excel de test
    df = pd.read_excel('test_import_with_payment_methods.xlsx')
    
    print("🔍 ANALYSE DES COLONNES EXCEL")
    print("=" * 50)
    
    print("📋 Colonnes dans le fichier Excel :")
    for i, col in enumerate(df.columns):
        print(f"   {i+1:2d}. '{col}' → '{col.lower().strip()}'")
    
    print(f"\n📊 Données d'exemple :")
    for i, row in df.head(3).iterrows():
        print(f"\n   Ligne {i+1} :")
        for col in df.columns:
            value = row[col]
            print(f"      {col}: {value}")
    
    # Test spécifique pour MoyenPaiement
    if 'MoyenPaiement' in df.columns:
        print(f"\n🎯 COLONNE 'MoyenPaiement' :")
        print(f"   Nom original: 'MoyenPaiement'")
        print(f"   Normalisé: '{str('MoyenPaiement').lower().strip()}'")
        
        unique_values = df['MoyenPaiement'].unique()
        print(f"   Valeurs uniques: {list(unique_values)}")
        
        print(f"\n   Répartition :")
        for value in unique_values:
            count = len(df[df['MoyenPaiement'] == value])
            print(f"      {value}: {count} occurrences")
    
    # Créer un nouveau fichier avec des en-têtes normalisés
    print(f"\n🔧 CRÉATION D'UN FICHIER AVEC EN-TÊTES NORMALISÉS")
    
    # Renommer les colonnes selon le mapping attendu
    column_mapping = {
        'Nom': 'nom',
        'Prenom': 'prenom', 
        'Email': 'email',
        'Telephone': 'telephone',
        'Adresse': 'adresse',
        'Ville': 'ville',
        'CodePostal': 'code_postal',
        'Pays': 'pays',
        'TypeClient': 'template',
        'MoyenPaiement': 'moyen_paiement',
        'IsActive': 'actif'
    }
    
    df_normalized = df.rename(columns=column_mapping)
    
    # Sauvegarder le fichier normalisé
    normalized_file = 'test_import_normalized.xlsx'
    df_normalized.to_excel(normalized_file, index=False)
    
    print(f"✅ Fichier normalisé créé: {normalized_file}")
    print(f"📋 Nouvelles colonnes :")
    for old_col, new_col in column_mapping.items():
        print(f"   '{old_col}' → '{new_col}'")
    
    return normalized_file

if __name__ == "__main__":
    test_column_mapping()
