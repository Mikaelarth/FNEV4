#!/usr/bin/env python3
"""
Analyse des données de la facture 556295 dans la vraie base de données
"""

import sqlite3
import os

# Chemin vers la vraie base de données
db_path = "data/FNEV4.db"

try:
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    print("=== ANALYSE FACTURE 556295 ===")
    
    # Récupération des informations de la facture
    cursor.execute('''
    SELECT f.InvoiceNumber, f.InvoiceDate, f.TotalAmountHT, f.TotalAmountTTC,
           c.ClientCode, c.Name, c.DefaultTemplate, c.Ncc, c.PointOfSale
    FROM FneInvoices f
    JOIN Clients c ON f.ClientId = c.Id
    WHERE f.InvoiceNumber = ?
    ''', ('556295',))
    
    facture = cursor.fetchone()
    if facture:
        print(f'Facture: {facture[0]}')
        print(f'Date: {facture[1]}')
        print(f'Client: {facture[4]} - {facture[5]}')
        print(f'Template: {facture[6]}')
        print(f'NCC: {facture[7]}')
        print(f'Point de vente: {facture[8]}')
        print(f'Montant HT: {facture[2]:.2f}')
        print(f'Montant TTC: {facture[3]:.2f}')
        print()
    else:
        print("❌ Facture 556295 non trouvée!")
        return
    
    # Récupération des articles avec tous les détails
    cursor.execute('''
    SELECT fi.ProductCode, fi.Description, fi.Quantity, fi.MeasurementUnit,
           fi.UnitPriceHT, fi.LineAmountHT, fi.VatCode,
           vt.Description as VatDescription, vt.Rate
    FROM FneInvoiceItems fi
    LEFT JOIN VatTypes vt ON fi.VatCode = vt.Code
    JOIN FneInvoices f ON fi.InvoiceId = f.Id
    WHERE f.InvoiceNumber = ?
    ORDER BY fi.Id
    ''', ('556295',))
    
    articles = cursor.fetchall()
    print("=== DÉTAIL DES ARTICLES ===")
    print("Code Article\t\tDescription\t\t\tQté\tEmball.\tPrix Unit.\tTotal HT\tTVA\tDesc. TVA\t\tTaux")
    print("-" * 140)
    
    for article in articles:
        code = article[0] or 'N/A'
        desc = (article[1] or '** MANQUANT **')[:25]
        qty = article[2] or 0
        unit = article[3] or 'N/A'
        price = article[4] or 0
        total = article[5] or 0
        vat_code = article[6] or 'N/A'
        vat_desc = (article[7] or '** MANQUANT **')[:20]
        vat_rate = article[8] or 0
        
        print(f'{code:<15}\t{desc:<25}\t{qty}\t{unit}\t{price:.2f}\t\t{total:.2f}\t{vat_code}\t{vat_desc:<20}\t{vat_rate}%')
    
    print()
    print("=== PROBLÈMES IDENTIFIÉS ===")
    
    # Comptage des descriptions manquantes
    missing_desc = sum(1 for a in articles if not a[1])
    if missing_desc > 0:
        print(f"❌ {missing_desc} articles sans description")
    
    # Comptage des emballages manquants
    missing_unit = sum(1 for a in articles if not a[3])
    if missing_unit > 0:
        print(f"❌ {missing_unit} articles sans unité d'emballage")
    
    # Vérification des types TVA
    cursor.execute('SELECT Code, Description, Rate FROM VatTypes ORDER BY Code')
    vat_types = cursor.fetchall()
    print()
    print("=== TYPES TVA DISPONIBLES ===")
    for vat in vat_types:
        print(f'{vat[0]}: {vat[1]} ({vat[2]}%)')
    
    # Vérification des liaisons TVA manquantes
    missing_vat = sum(1 for a in articles if not a[7])
    if missing_vat > 0:
        print(f"\n❌ {missing_vat} articles avec des liaisons TVA manquantes")
    
    conn.close()
    
except Exception as e:
    print(f"Erreur: {e}")
    import traceback
    traceback.print_exc()