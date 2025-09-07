#!/usr/bin/env python3
"""
Test rapide pour vérifier l'import exceptionnel
"""

import os
import sys

# Ajouter le chemin du projet
sys.path.append(r'C:\wamp64\www\FNEV4')

def test_import_exceptionnel():
    """
    Test rapide de l'import exceptionnel avec clients.xlsx
    """
    print("=== TEST IMPORT EXCEPTIONNEL ===")
    
    # Vérifier les fichiers
    excel_file = r'C:\wamp64\www\FNEV4\clients.xlsx'
    if not os.path.exists(excel_file):
        print(f"❌ Fichier Excel non trouvé: {excel_file}")
        return False
    
    # Simuler l'import
    try:
        import pandas as pd
        df = pd.read_excel(excel_file)
        print(f"✅ Fichier Excel lu avec succès: {len(df)} lignes")
        print(f"Colonnes: {list(df.columns)}")
        
        # Simuler la logique d'import
        clients_valides = len(df[df['nom'].notna() & df['email'].notna()])
        print(f"✅ Clients valides détectés: {clients_valides}")
        
        return True
        
    except Exception as e:
        print(f"❌ Erreur lors du test: {e}")
        return False

if __name__ == "__main__":
    success = test_import_exceptionnel()
    sys.exit(0 if success else 1)
