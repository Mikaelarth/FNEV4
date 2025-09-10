#!/usr/bin/env python3
"""
Script de création d'un fichier Excel de test conforme à exemple_structure_excel.py
Pour valider le fonctionnement du validateur Sage 100
"""

import openpyxl
from datetime import datetime, timedelta
import os

def creer_fichier_test_sage100():
    """Crée un fichier Excel de test avec plusieurs factures"""
    
    # Créer un nouveau classeur
    wb = openpyxl.Workbook()
    
    # Supprimer la feuille par défaut
    wb.remove(wb.active)
    
    # === FACTURE 1: Client normal valide ===
    ws1 = wb.create_sheet("Facture_001")
    
    # Entête
    ws1['A3'] = "FAC001"  # Numéro facture
    ws1['A5'] = "CLI001"  # Code client normal
    ws1['A6'] = "1234567A"  # NCC client
    ws1['A8'] = "45508"  # Date en format Excel (01/08/2024)
    ws1['A10'] = "Abidjan"  # Point de vente
    ws1['A11'] = "ENTREPRISE TECH SARL"  # Nom client
    ws1['A18'] = "cash"  # Moyen de paiement
    
    # Produits
    ws1['B20'] = "ORD001"
    ws1['C20'] = "Ordinateur portable Dell"
    ws1['D20'] = 850000
    ws1['E20'] = 1
    ws1['F20'] = "pcs"
    ws1['G20'] = "TVA"
    ws1['H20'] = 850000
    
    ws1['B21'] = "SOU001"
    ws1['C21'] = "Souris sans fil"
    ws1['D21'] = 25000
    ws1['E21'] = 2
    ws1['F21'] = "pcs"
    ws1['G21'] = "TVA"
    ws1['H21'] = 50000
    
    # === FACTURE 2: Client divers (1999) valide ===
    ws2 = wb.create_sheet("Facture_002")
    
    # Entête
    ws2['A3'] = "FAC002"  # Numéro facture
    ws2['A5'] = "1999"  # Code client divers
    ws2['A6'] = "2354552Q"  # NCC générique client divers
    ws2['A8'] = "45509"  # Date en format Excel (02/08/2024)
    ws2['A10'] = "Gestoci"  # Point de vente
    ws2['A11'] = "DIVERS CLIENTS CARBURANTS"  # Intitulé générique
    ws2['A13'] = "KOUAME JEAN-BAPTISTE"  # Nom réel client divers
    ws2['A15'] = "9876543Z"  # NCC spécifique client divers
    ws2['A18'] = "mobile-money"  # Moyen de paiement
    
    # Produits
    ws2['B20'] = "CARB001"
    ws2['C20'] = "Essence Super 95"
    ws2['D20'] = 650
    ws2['E20'] = 50
    ws2['F20'] = "litres"
    ws2['G20'] = "TVA"
    ws2['H20'] = 32500
    
    # === FACTURE 3: Facture avec erreurs pour tester la validation ===
    ws3 = wb.create_sheet("Facture_003_ERREURS")
    
    # Entête avec erreurs
    ws3['A3'] = ""  # Numéro facture manquant
    ws3['A5'] = "1999"  # Code client divers
    ws3['A6'] = "2354552Q"  # NCC générique
    ws3['A8'] = "date_invalide"  # Date invalide
    ws3['A10'] = "Bouaké"  # Point de vente
    ws3['A11'] = "DIVERS CLIENTS"  # Intitulé générique
    # A13 manquant (nom réel client divers)
    ws3['A15'] = "123ABC"  # NCC invalide (mauvais format)
    ws3['A18'] = "bitcoin"  # Moyen de paiement invalide
    
    # Produits avec erreurs
    ws3['B20'] = "PROD001"
    ws3['C20'] = ""  # Désignation manquante
    ws3['D20'] = "prix_invalide"  # Prix non numérique
    ws3['E20'] = 0  # Quantité nulle
    ws3['F20'] = "pcs"
    ws3['G20'] = "TVX"  # Code TVA invalide
    ws3['H20'] = 100000
    
    # === FACTURE 4: Facture avoir ===
    ws4 = wb.create_sheet("Avoir_001")
    
    # Entête
    ws4['A3'] = "AVO001"  # Numéro avoir
    ws4['A5'] = "CLI002"  # Code client
    ws4['A6'] = "7654321B"  # NCC client
    ws4['A8'] = "45510"  # Date
    ws4['A10'] = "Yamoussoukro"  # Point de vente
    ws4['A11'] = "SOCIÉTÉ MODERNE CI"  # Nom client
    ws4['A17'] = "FAC150"  # Numéro facture d'origine
    ws4['A18'] = "bank-transfer"  # Moyen de paiement
    
    # Produits (montants négatifs pour avoir)
    ws4['B20'] = "TEL001"
    ws4['C20'] = "Téléphone défectueux retourné"
    ws4['D20'] = -150000
    ws4['E20'] = 1
    ws4['F20'] = "pcs"
    ws4['G20'] = "TVA"
    ws4['H20'] = -150000
    
    # === FACTURE 5: Facture complexe avec plusieurs produits ===
    ws5 = wb.create_sheet("Facture_COMPLEXE")
    
    # Entête
    ws5['A3'] = "FAC999"
    ws5['A5'] = "ENTREP001"
    ws5['A6'] = "1111111X"
    ws5['A8'] = "45511"
    ws5['A10'] = "GSM (Fictif)"
    ws5['A11'] = "GRANDE ENTREPRISE SARL"
    ws5['A18'] = "check"
    
    # Produits multiples
    produits = [
        ("SERV001", "Consultation technique", 100000, 1, "service", "TVA", 100000),
        ("MAT001", "Matériel informatique", 500000, 3, "pcs", "TVA", 1500000),
        ("FORM001", "Formation utilisateurs", 75000, 8, "heures", "TVAB", 600000),
        ("MAINT001", "Maintenance préventive", 200000, 1, "service", "TVA", 200000),
        ("LIC001", "Licence logiciel", 350000, 2, "licence", "TVAC", 700000)
    ]
    
    for i, (code, design, prix, qte, embp, tva, montant) in enumerate(produits, 20):
        ws5[f'B{i}'] = code
        ws5[f'C{i}'] = design
        ws5[f'D{i}'] = prix
        ws5[f'E{i}'] = qte
        ws5[f'F{i}'] = embp
        ws5[f'G{i}'] = tva
        ws5[f'H{i}'] = montant
    
    # Sauvegarder le fichier
    nom_fichier = "test_factures_sage100.xlsx"
    wb.save(nom_fichier)
    
    print(f"✅ Fichier de test créé: {nom_fichier}")
    print()
    print("📊 Contenu du fichier:")
    print("   • Facture_001: Client normal valide")
    print("   • Facture_002: Client divers (1999) valide")
    print("   • Facture_003_ERREURS: Facture avec erreurs de validation")
    print("   • Avoir_001: Facture avoir")
    print("   • Facture_COMPLEXE: Facture avec 5 produits différents")
    print()
    print("🔍 Pour valider ce fichier, exécutez:")
    print(f"   python validate_sage100_structure.py {nom_fichier}")
    
    return nom_fichier

if __name__ == "__main__":
    creer_fichier_test_sage100()
