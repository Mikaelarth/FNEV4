#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Script pour créer un modèle Excel d'import des clients FNEV4
Génère un fichier Excel avec les colonnes requises et des exemples de données
"""

import pandas as pd
from datetime import datetime
import os

def create_client_import_template():
    """
    Crée un modèle Excel pour l'import des clients avec:
    - Les colonnes requises selon le modèle DGI
    - Des exemples de données
    - Une feuille d'instructions
    """
    
    print("🔄 Création du modèle Excel d'import clients...")
    
    # Colonnes requises pour l'import des clients
    columns = [
        'Code',           # Code unique du client
        'Type',           # Individual, Company, Government, International
        'Nom',            # Nom complet ou raison sociale
        'NCC',            # Numéro Carte Contribuable (optionnel)
        'Email',          # Adresse email
        'Telephone',      # Numéro de téléphone
        'Adresse',        # Adresse complète
        'Ville',          # Ville
        'CodePostal',     # Code postal
        'Actif',          # True/False pour le statut
        'Notes'           # Notes complémentaires (optionnel)
    ]
    
    # Données d'exemple
    examples = [
        {
            'Code': 'CLI001',
            'Type': 'Individual',
            'Nom': 'DUPONT Jean-Marie',
            'NCC': '1234567890123',
            'Email': 'jean.dupont@email.com',
            'Telephone': '+33 1 23 45 67 89',
            'Adresse': '123 Rue de la République',
            'Ville': 'Paris',
            'CodePostal': '75001',
            'Actif': True,
            'Notes': 'Client VIP - Facturation mensuelle'
        },
        {
            'Code': 'CLI002',
            'Type': 'Company',
            'Nom': 'SARL TECH INNOVATIONS',
            'NCC': '9876543210987',
            'Email': 'contact@tech-innovations.fr',
            'Telephone': '+33 4 56 78 90 12',
            'Adresse': '456 Avenue des Entreprises',
            'Ville': 'Lyon',
            'CodePostal': '69000',
            'Actif': True,
            'Notes': 'Société de services informatiques'
        },
        {
            'Code': 'CLI003',
            'Type': 'Government',
            'Nom': 'MAIRIE DE BORDEAUX',
            'NCC': '5555666677778',
            'Email': 'services@mairie-bordeaux.fr',
            'Telephone': '+33 5 11 22 33 44',
            'Adresse': 'Place Pey Berland',
            'Ville': 'Bordeaux',
            'CodePostal': '33000',
            'Actif': True,
            'Notes': 'Organisme public - Paiement par mandat'
        },
        {
            'Code': 'CLI004',
            'Type': 'International',
            'Nom': 'SWISS TRADING SA',
            'NCC': '',  # Pas de NCC pour les clients internationaux
            'Email': 'info@swisstrading.ch',
            'Telephone': '+41 22 123 45 67',
            'Adresse': 'Rue du Commerce 12',
            'Ville': 'Genève',
            'CodePostal': '1201',
            'Actif': True,
            'Notes': 'Client international - Devise EUR'
        },
        {
            'Code': 'CLI005',
            'Type': 'Individual',
            'Nom': 'MARTIN Sophie',
            'NCC': '1111222233334',
            'Email': 'sophie.martin@gmail.com',
            'Telephone': '+33 6 98 76 54 32',
            'Adresse': '789 Chemin des Vignes',
            'Ville': 'Nice',
            'CodePostal': '06000',
            'Actif': False,
            'Notes': 'Client inactif depuis juin 2024'
        }
    ]
    
    # Instructions pour l'utilisation
    instructions = [
        {
            'Colonne': 'Code',
            'Description': 'Code unique du client (obligatoire)',
            'Format': 'Texte, max 20 caractères',
            'Exemple': 'CLI001, COMP_001, etc.'
        },
        {
            'Colonne': 'Type',
            'Description': 'Type de client (obligatoire)',
            'Format': 'Individual, Company, Government, International',
            'Exemple': 'Company'
        },
        {
            'Colonne': 'Nom',
            'Description': 'Nom complet ou raison sociale (obligatoire)',
            'Format': 'Texte, max 200 caractères',
            'Exemple': 'DUPONT Jean ou SARL TECH SOLUTIONS'
        },
        {
            'Colonne': 'NCC',
            'Description': 'Numéro Carte Contribuable DGI (optionnel)',
            'Format': 'Numérique, 13 chiffres',
            'Exemple': '1234567890123'
        },
        {
            'Colonne': 'Email',
            'Description': 'Adresse email principale (optionnel)',
            'Format': 'Email valide',
            'Exemple': 'contact@entreprise.fr'
        },
        {
            'Colonne': 'Telephone',
            'Description': 'Numéro de téléphone (optionnel)',
            'Format': 'Texte libre',
            'Exemple': '+33 1 23 45 67 89'
        },
        {
            'Colonne': 'Adresse',
            'Description': 'Adresse complète (optionnel)',
            'Format': 'Texte libre',
            'Exemple': '123 Rue de la Paix'
        },
        {
            'Colonne': 'Ville',
            'Description': 'Ville (optionnel)',
            'Format': 'Texte libre',
            'Exemple': 'Paris'
        },
        {
            'Colonne': 'CodePostal',
            'Description': 'Code postal (optionnel)',
            'Format': 'Texte libre',
            'Exemple': '75001'
        },
        {
            'Colonne': 'Actif',
            'Description': 'Statut du client (optionnel, défaut: True)',
            'Format': 'True/False ou 1/0',
            'Exemple': 'True'
        },
        {
            'Colonne': 'Notes',
            'Description': 'Notes complémentaires (optionnel)',
            'Format': 'Texte libre',
            'Exemple': 'Client VIP - Facturation mensuelle'
        }
    ]
    
    # Création du fichier Excel avec plusieurs feuilles
    file_path = os.path.join('data', 'templates', 'modele_import_clients_dgi.xlsx')
    
    # Créer le dossier s'il n'existe pas
    os.makedirs(os.path.dirname(file_path), exist_ok=True)
    
    with pd.ExcelWriter(file_path, engine='openpyxl') as writer:
        
        # Feuille 1: Template vide
        template_df = pd.DataFrame(columns=columns)
        template_df.to_excel(writer, sheet_name='Template', index=False)
        
        # Feuille 2: Exemples de données
        examples_df = pd.DataFrame(examples)
        examples_df.to_excel(writer, sheet_name='Exemples', index=False)
        
        # Feuille 3: Instructions
        instructions_df = pd.DataFrame(instructions)
        instructions_df.to_excel(writer, sheet_name='Instructions', index=False)
        
        # Feuille 4: Informations importantes
        info_data = [
            {
                'Information': 'Version du template',
                'Valeur': 'v1.0.0'
            },
            {
                'Information': 'Date de création',
                'Valeur': datetime.now().strftime('%d/%m/%Y %H:%M')
            },
            {
                'Information': 'Colonnes obligatoires',
                'Valeur': 'Code, Type, Nom'
            },
            {
                'Information': 'Types de clients supportés',
                'Valeur': 'Individual, Company, Government, International'
            },
            {
                'Information': 'Format NCC',
                'Valeur': '13 chiffres (obligatoire pour types Individual, Company, Government)'
            },
            {
                'Information': 'Encodage recommandé',
                'Valeur': 'UTF-8'
            },
            {
                'Information': 'Nombre max de lignes',
                'Valeur': '10,000 par import'
            }
        ]
        
        info_df = pd.DataFrame(info_data)
        info_df.to_excel(writer, sheet_name='Infos', index=False)
    
    print(f"✅ Modèle Excel créé: {file_path}")
    print(f"📊 Feuilles créées:")
    print(f"   - Template: Colonnes vides prêtes à remplir")
    print(f"   - Exemples: 5 exemples de clients")
    print(f"   - Instructions: Guide détaillé des colonnes")
    print(f"   - Infos: Informations techniques")
    
    return file_path

def validate_existing_template():
    """
    Vérifie si le template existant est correct
    """
    template_path = os.path.join('data', 'templates', 'modele_import_clients_dgi.xlsx')
    
    if os.path.exists(template_path):
        print(f"📄 Template existant trouvé: {template_path}")
        try:
            df = pd.read_excel(template_path, sheet_name='Template')
            print(f"✅ Colonnes trouvées: {list(df.columns)}")
            return True
        except Exception as e:
            print(f"❌ Erreur lors de la lecture du template: {e}")
            return False
    else:
        print(f"❌ Aucun template trouvé à: {template_path}")
        return False

if __name__ == "__main__":
    print("=== GÉNÉRATEUR DE TEMPLATE EXCEL CLIENTS FNEV4 ===")
    print()
    
    # Vérifier le template existant
    if not validate_existing_template():
        # Créer un nouveau template
        template_path = create_client_import_template()
        print()
        print("🎯 Template prêt à utiliser!")
        print(f"📁 Emplacement: {template_path}")
    else:
        print("✅ Template existant valide!")
    
    print()
    print("📖 GUIDE D'UTILISATION:")
    print("1. Ouvrir le fichier Excel généré")
    print("2. Utiliser la feuille 'Template' pour saisir vos données")
    print("3. Se référer aux 'Exemples' et 'Instructions' au besoin")
    print("4. Importer via FNEV4 > Gestion Clients > Import Excel")
