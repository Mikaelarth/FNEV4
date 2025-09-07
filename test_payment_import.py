#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Test de création d'un fichier Excel avec moyens de paiement
pour tester l'interface d'import normal
"""

import pandas as pd
import random

def create_test_excel_with_payment_methods():
    """Créer un fichier Excel de test avec les moyens de paiement"""
    
    # Données de test avec moyens de paiement variés
    clients_data = [
        {
            'Nom': 'Client Test 1',
            'Prenom': 'Jean',
            'Email': 'jean.test1@email.com',
            'Telephone': '22690101010',
            'Adresse': 'Adresse Test 1',
            'Ville': 'Lomé',
            'CodePostal': '01000',
            'Pays': 'Togo',
            'TypeClient': 'B2C',
            'MoyenPaiement': 'cash',
            'IsActive': 'True'
        },
        {
            'Nom': 'Client Test 2',
            'Prenom': 'Marie',
            'Email': 'marie.test2@email.com',
            'Telephone': '22690202020',
            'Adresse': 'Adresse Test 2',
            'Ville': 'Kara',
            'CodePostal': '02000',
            'Pays': 'Togo',
            'TypeClient': 'B2B',
            'MoyenPaiement': 'card',
            'IsActive': 'True'
        },
        {
            'Nom': 'Client Test 3',
            'Prenom': 'Paul',
            'Email': 'paul.test3@email.com',
            'Telephone': '22690303030',
            'Adresse': 'Adresse Test 3',
            'Ville': 'Sokodé',
            'CodePostal': '03000',
            'Pays': 'Togo',
            'TypeClient': 'B2G',
            'MoyenPaiement': 'mobile-money',
            'IsActive': 'True'
        },
        {
            'Nom': 'Client Test 4',
            'Prenom': 'Sophie',
            'Email': 'sophie.test4@email.com',
            'Telephone': '22690404040',
            'Adresse': 'Adresse Test 4',
            'Ville': 'Atakpamé',
            'CodePostal': '04000',
            'Pays': 'Togo',
            'TypeClient': 'B2F',
            'MoyenPaiement': 'bank-transfer',
            'IsActive': 'True'
        },
        {
            'Nom': 'Client Test 5',
            'Prenom': 'Ahmed',
            'Email': 'ahmed.test5@email.com',
            'Telephone': '22690505050',
            'Adresse': 'Adresse Test 5',
            'Ville': 'Dapaong',
            'CodePostal': '05000',
            'Pays': 'Togo',
            'TypeClient': 'B2C',
            'MoyenPaiement': 'check',
            'IsActive': 'True'
        },
        {
            'Nom': 'Client Test 6',
            'Prenom': 'Fatou',
            'Email': 'fatou.test6@email.com',
            'Telephone': '22690606060',
            'Adresse': 'Adresse Test 6',
            'Ville': 'Lomé',
            'CodePostal': '01000',
            'Pays': 'Togo',
            'TypeClient': 'B2B',
            'MoyenPaiement': 'credit',
            'IsActive': 'False'
        }
    ]
    
    # Créer DataFrame
    df = pd.DataFrame(clients_data)
    
    # Sauvegarder dans un fichier Excel
    excel_file = 'test_import_with_payment_methods.xlsx'
    df.to_excel(excel_file, index=False, sheet_name='Clients')
    
    print(f"✅ Fichier Excel créé : {excel_file}")
    print(f"📊 {len(clients_data)} clients créés avec différents moyens de paiement")
    print("\n📋 Moyens de paiement inclus :")
    payment_methods = df['MoyenPaiement'].unique()
    for method in payment_methods:
        count = len(df[df['MoyenPaiement'] == method])
        print(f"   - {method}: {count} client(s)")
    
    return excel_file

if __name__ == "__main__":
    create_test_excel_with_payment_methods()
