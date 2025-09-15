#!/usr/bin/env python3
"""
Analyse des données de la facture 556295 pour identifier les incohérences
"""

import sqlite3
import os

# Chemin correct vers la base de données
db_path = "data/FNEV4.db"

try:
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    print("=== ANALYSE FACTURE 556295 ===")
    print()

    # Récupération des informations de la facture
    cursor.execute('''
    SELECT f.InvoiceNumber, f.InvoiceDate, f.TotalAmountHT, f.TotalAmountTTC,
           c.ClientCode, c.Name, c.DefaultTemplate, c.ClientNcc
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
        print(f'Montant HT: {facture[2]:.2f}')
        print(f'Montant TTC: {facture[3]:.2f}')
        print()
    else:
        print("Facture 556295 non trouvée")
        # Listons quelques factures disponibles
        cursor.execute('SELECT InvoiceNumber, InvoiceDate FROM FneInvoices LIMIT 5')
        factures = cursor.fetchall()
        print("Factures disponibles:")
        for f in factures:
            print(f"- {f[0]} ({f[1]})")
        print()

    # Récupération des articles avec tous les détails
    cursor.execute('''
    SELECT fi.ProductCode, fi.Description, fi.Quantity, fi.MeasurementUnit,
           fi.UnitPrice, fi.LineAmountHT, fi.VatCode,
           vt.Description as VatDescription, vt.Rate
    FROM FneInvoiceItems fi
    LEFT JOIN VatTypes vt ON fi.VatCode = vt.Code
    JOIN FneInvoices f ON fi.FneInvoiceId = f.Id
    WHERE f.InvoiceNumber = ?
    ORDER BY fi.Id
    ''', ('556295',))

    articles = cursor.fetchall()
    if articles:
        print('=== DÉTAIL DES ARTICLES ===')
        print('Code\t\tDescription\t\tQté\tEmball.\tPrix Unit.\tTotal HT\tTVA\tDesc. TVA\tTaux')
        print('-' * 120)

        for article in articles:
            code = article[0] or 'N/A'
            desc = (article[1] or 'MANQUANT')[:20]
            qty = article[2] or 0
            unit = article[3] or 'N/A'
            price = article[4] or 0
            total = article[5] or 0
            vat_code = article[6] or 'N/A'
            vat_desc = article[7] or 'MANQUANT'
            vat_rate = article[8] or 0
            
            print(f'{code:<12}\t{desc:<20}\t{qty}\t{unit}\t{price:.2f}\t\t{total:.2f}\t{vat_code}\t{vat_desc}\t{vat_rate}%')
    else:
        print("Aucun article trouvé pour cette facture")

    print()

    # Vérification des types TVA disponibles
    cursor.execute('SELECT Code, Description, Rate FROM VatTypes ORDER BY Code')
    vat_types = cursor.fetchall()
    print('=== TYPES TVA DISPONIBLES ===')
    for vat in vat_types:
        print(f'{vat[0]}: {vat[1]} ({vat[2]}%)')

    conn.close()
    
except Exception as e:
    print(f"Erreur: {e}")
    
    # Si erreur, vérifions les tables
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        cursor.execute('SELECT name FROM sqlite_master WHERE type="table"')
        tables = cursor.fetchall()
        print(f"\nTables disponibles: {[t[0] for t in tables]}")
        conn.close()
    except:
        print("Impossible d'accéder à la base de données")