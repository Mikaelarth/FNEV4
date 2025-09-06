#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Script pour créer un fichier Excel de test complet avec les 4 templates DGI
Inclut B2B, B2C, B2G et B2F avec exemples conformes
"""

import pandas as pd
from pathlib import Path

def create_complete_dgi_test():
    """Crée un fichier Excel avec exemples des 4 templates DGI"""
    
    # Données de test complètes avec les 4 templates
    test_data = [
        {
            'Code Client': 'CLI001',
            'Nom/Raison Sociale': 'ARTHUR LE GRAND SARL',
            'Template': 'B2B',
            'NCC': '9502363N',
            'Nom Commercial': 'Arthur Le Grand',
            'Adresse': '123 Boulevard de la Paix',
            'Ville': 'Abidjan',
            'Code Postal': '01001',
            'Pays': 'Côte d\'Ivoire',
            'Téléphone': '+225 01 02 03 04',
            'Email': 'arthur@legrand.ci',
            'Représentant': 'Jean KOUAME',
            'N° Fiscal': 'TIN123456',
            'Devise': 'XOF',
            'Actif': 'Oui',
            'Notes': 'Client VIP entreprise'
        },
        {
            'Code Client': 'CLI002',
            'Nom/Raison Sociale': 'MARIE KOUASSI',
            'Template': 'B2C',
            'NCC': '',  # B2C = pas de NCC
            'Nom Commercial': 'Marie Kouassi Boutique',
            'Adresse': '45 Rue du Commerce',
            'Ville': 'Bouaké',
            'Code Postal': '02001',
            'Pays': 'Côte d\'Ivoire',
            'Téléphone': '+225 05 06 07 08',
            'Email': 'marie.kouassi@gmail.com',
            'Représentant': 'Paul DIALLO',
            'N° Fiscal': '',
            'Devise': 'XOF',
            'Actif': 'Oui',
            'Notes': 'Client particulier'
        },
        {
            'Code Client': 'CLI003',
            'Nom/Raison Sociale': 'MINISTERE SANTE',
            'Template': 'B2G',
            'NCC': '9606123E',
            'Nom Commercial': 'Min. Santé et Hygiène',
            'Adresse': 'Plateau Tour C',
            'Ville': 'Abidjan',
            'Code Postal': '01000',
            'Pays': 'Côte d\'Ivoire',
            'Téléphone': '+225 20 21 22 23',
            'Email': 'contact@sante.gouv.ci',
            'Représentant': 'Dr. BAMBA',
            'N° Fiscal': 'GOV789',
            'Devise': 'XOF',
            'Actif': 'Oui',
            'Notes': 'Client gouvernemental'
        },
        {
            'Code Client': 'CLI004',
            'Nom/Raison Sociale': 'EXPORT TRADING CORP',
            'Template': 'B2F',
            'NCC': 'FR12345678',
            'Nom Commercial': 'Export Trading Corporation',
            'Adresse': '15 Avenue des Champs',
            'Ville': 'Paris',
            'Code Postal': '75001',
            'Pays': 'France',
            'Téléphone': '+33 1 42 00 00 00',
            'Email': 'contact@exporttrading.fr',
            'Représentant': 'Michel BERNARD',
            'N° Fiscal': 'EU987654321',
            'Devise': 'EUR',  # B2F = devise étrangère obligatoire
            'Actif': 'Oui',
            'Notes': 'Client international'
        },
        {
            'Code Client': 'CLI005',
            'Nom/Raison Sociale': 'USA IMPORTS LLC',
            'Template': 'B2F',
            'NCC': 'US98765432',
            'Nom Commercial': 'USA Imports LLC',
            'Adresse': '500 Fifth Avenue',
            'Ville': 'New York',
            'Code Postal': '10110',
            'Pays': 'États-Unis',
            'Téléphone': '+1 212 555 0123',
            'Email': 'info@usaimports.com',
            'Représentant': 'John SMITH',
            'N° Fiscal': 'US123456789',
            'Devise': 'USD',
            'Actif': 'Oui',
            'Notes': 'Client international USA'
        }
    ]
    
    # Créer DataFrame
    df = pd.DataFrame(test_data)
    
    # Sauvegarder en Excel
    output_path = Path('test_import_dgi_4templates.xlsx')
    with pd.ExcelWriter(output_path, engine='openpyxl') as writer:
        df.to_excel(writer, sheet_name='Clients_DGI', index=False)
        
        # Formater la feuille
        workbook = writer.book
        worksheet = writer.sheets['Clients_DGI']
        
        # Ajuster les largeurs de colonnes
        for column in worksheet.columns:
            max_length = 0
            column_letter = column[0].column_letter
            for cell in column:
                try:
                    if len(str(cell.value)) > max_length:
                        max_length = len(str(cell.value))
                except:
                    pass
            adjusted_width = min(max_length + 2, 50)
            worksheet.column_dimensions[column_letter].width = adjusted_width
    
    print(f"✅ Fichier créé: {output_path}")
    print(f"📊 {len(test_data)} clients de test avec templates:")
    
    template_counts = df['Template'].value_counts()
    for template, count in template_counts.items():
        print(f"   - {template}: {count} client(s)")
    
    return output_path

if __name__ == "__main__":
    create_complete_dgi_test()
