#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Script de débogage : Vérification des totaux de la facture FNE 556443
Vérifie si les montants TotalAmountHT, TotalVatAmount, GlobalDiscount sont présents
"""

import sqlite3
import os
from datetime import datetime

def debug_invoice_totals():
    """Vérifie les totaux de la facture 556443"""
    
    db_path = r"D:\PROJET\FNE\FNEV4\src\FNEV4.Infrastructure\Database\fnev4.db"
    
    if not os.path.exists(db_path):
        print(f"❌ Base de données non trouvée : {db_path}")
        return
    
    try:
        with sqlite3.connect(db_path) as conn:
            cursor = conn.cursor()
            
            print("🔍 DÉBOGAGE TOTAUX FACTURE 556443")
            print("=" * 50)
            
            # 1. Vérifier la facture principale
            cursor.execute("""
                SELECT InvoiceNumber, TotalAmountHT, TotalVatAmount, TotalAmountTTC, 
                       GlobalDiscount, Status, ClientCode, InvoiceDate
                FROM FneInvoices 
                WHERE InvoiceNumber = '556443'
            """)
            facture = cursor.fetchone()
            
            if facture:
                numero, total_ht, total_tva, total_ttc, remise, status, client, date = facture
                
                print(f"📄 FACTURE TROUVÉE: {numero}")
                print(f"   • Client: {client}")
                print(f"   • Date: {date}")
                print(f"   • Statut: {status}")
                print(f"   • Total HT: {total_ht if total_ht else 'VIDE'} FCFA")
                print(f"   • Total TVA: {total_tva if total_tva else 'VIDE'} FCFA")
                print(f"   • Total TTC: {total_ttc if total_ttc else 'VIDE'} FCFA")
                print(f"   • Remise: {remise if remise else 'VIDE'}%")
                
                # 2. Vérifier les articles associés
                cursor.execute("""
                    SELECT COUNT(*), SUM(LineAmountHT), SUM(VatAmount), SUM(LineAmountTTC)
                    FROM FneInvoiceItems 
                    WHERE InvoiceId = (
                        SELECT Id FROM FneInvoices WHERE InvoiceNumber = '556443'
                    )
                """)
                articles = cursor.fetchone()
                
                if articles and articles[0] > 0:
                    nb_articles, sum_ht, sum_tva, sum_ttc = articles
                    print(f"\n📦 ARTICLES ({nb_articles} articles):")
                    print(f"   • Somme HT: {sum_ht if sum_ht else 'VIDE'} FCFA")
                    print(f"   • Somme TVA: {sum_tva if sum_tva else 'VIDE'} FCFA")
                    print(f"   • Somme TTC: {sum_ttc if sum_ttc else 'VIDE'} FCFA")
                    
                    # Comparaison avec les totaux facture
                    print(f"\n🔍 VÉRIFICATION COHÉRENCE:")
                    if total_ht and sum_ht:
                        print(f"   • HT facture vs articles: {total_ht} vs {sum_ht} ({'✅ OK' if abs(total_ht - sum_ht) < 0.01 else '❌ DIFFÉRENCE'})")
                    if total_tva and sum_tva:
                        print(f"   • TVA facture vs articles: {total_tva} vs {sum_tva} ({'✅ OK' if abs(total_tva - sum_tva) < 0.01 else '❌ DIFFÉRENCE'})")
                    if total_ttc and sum_ttc:
                        print(f"   • TTC facture vs articles: {total_ttc} vs {sum_ttc} ({'✅ OK' if abs(total_ttc - sum_ttc) < 0.01 else '❌ DIFFÉRENCE'})")
                
                else:
                    print(f"\n❌ AUCUN ARTICLE trouvé pour cette facture !")
                    
                # 3. Détail des articles
                cursor.execute("""
                    SELECT ProductCode, Description, Quantity, UnitPrice, 
                           LineAmountHT, VatRate, VatAmount, LineAmountTTC
                    FROM FneInvoiceItems 
                    WHERE InvoiceId = (
                        SELECT Id FROM FneInvoices WHERE InvoiceNumber = '556443'
                    )
                    ORDER BY LineOrder
                """)
                
                articles_detail = cursor.fetchall()
                if articles_detail:
                    print(f"\n📋 DÉTAIL ARTICLES:")
                    print("Code       | Désignation                | Qté   | Prix U. | HT      | TVA%  | TVA     | TTC")
                    print("-" * 90)
                    for article in articles_detail:
                        code, desc, qty, price, ht, tva_rate, tva_amt, ttc = article
                        print(f"{code or 'N/A':<10} | {(desc or 'N/A')[:25]:<25} | {qty:>5.2f} | {price:>7.2f} | {ht:>7.2f} | {tva_rate*100:>4.0f}% | {tva_amt:>7.2f} | {ttc:>7.2f}")
                
            else:
                print(f"❌ Facture 556443 non trouvée dans la base de données")
                
                # Rechercher toutes les factures disponibles
                cursor.execute("SELECT InvoiceNumber, ClientCode, TotalAmountTTC FROM FneInvoices ORDER BY InvoiceNumber LIMIT 5")
                factures = cursor.fetchall()
                
                if factures:
                    print(f"\n🔍 FACTURES DISPONIBLES (5 premières):")
                    for f in factures:
                        print(f"   • {f[0]} - {f[1]} - {f[2]} FCFA")
            
    except Exception as e:
        print(f"❌ Erreur lors de l'accès à la base de données : {e}")

if __name__ == "__main__":
    debug_invoice_totals()