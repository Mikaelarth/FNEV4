#!/usr/bin/env python3
"""
Analyse des templates et types de factures FNE
Vérifie la cohérence avec les spécifications de l'API FNE
"""

import sqlite3
import sys
from pathlib import Path

def analyze_templates_and_types():
    """
    Analyse les templates et types de factures dans la base de données
    """
    
    db_path = Path("data/FNEV4.db")
    
    if not db_path.exists():
        print("❌ Base de données FNEV4.db introuvable")
        return
    
    try:
        conn = sqlite3.connect(str(db_path))
        cursor = conn.cursor()
        
        print("🔍 ANALYSE DES TEMPLATES ET TYPES DE FACTURES FNE")
        print("=" * 60)
        
        # 1. Analyser les templates dans les factures
        print("\n📋 1. TEMPLATES UTILISÉS DANS LES FACTURES")
        print("-" * 40)
        
        cursor.execute("""
            SELECT 
                Template,
                COUNT(*) as count,
                GROUP_CONCAT(DISTINCT InvoiceNumber) as exemples
            FROM FneInvoices 
            WHERE Template IS NOT NULL 
            GROUP BY Template 
            ORDER BY count DESC
        """)
        
        templates_factures = cursor.fetchall()
        
        if templates_factures:
            for template, count, exemples in templates_factures:
                exemples_list = exemples.split(',')[:3]  # Limiter à 3 exemples
                print(f"   📄 Template: '{template}' ({count} factures)")
                print(f"      Exemples: {', '.join(exemples_list)}")
        else:
            print("   ⚠️ Aucun template trouvé dans les factures")
        
        # 2. Analyser les types de factures
        print("\n📋 2. TYPES DE FACTURES")
        print("-" * 25)
        
        cursor.execute("""
            SELECT 
                InvoiceType,
                COUNT(*) as count,
                AVG(TotalAmountTTC) as montant_moyen,
                GROUP_CONCAT(DISTINCT InvoiceNumber) as exemples
            FROM FneInvoices 
            WHERE InvoiceType IS NOT NULL 
            GROUP BY InvoiceType 
            ORDER BY count DESC
        """)
        
        types_factures = cursor.fetchall()
        
        if types_factures:
            for inv_type, count, montant_moyen, exemples in types_factures:
                exemples_list = exemples.split(',')[:3]
                print(f"   🏷️ Type: '{inv_type}' ({count} factures)")
                print(f"      Montant moyen: {montant_moyen:.2f} FCFA")
                print(f"      Exemples: {', '.join(exemples_list)}")
        else:
            print("   ⚠️ Aucun type de facture trouvé")
        
        # 3. Analyser les méthodes de paiement
        print("\n💳 3. MÉTHODES DE PAIEMENT")
        print("-" * 26)
        
        cursor.execute("""
            SELECT 
                PaymentMethod,
                COUNT(*) as count,
                SUM(TotalAmountTTC) as total_amount,
                GROUP_CONCAT(DISTINCT InvoiceNumber) as exemples
            FROM FneInvoices 
            WHERE PaymentMethod IS NOT NULL 
            GROUP BY PaymentMethod 
            ORDER BY count DESC
        """)
        
        payment_methods = cursor.fetchall()
        
        if payment_methods:
            for method, count, total_amount, exemples in payment_methods:
                exemples_list = exemples.split(',')[:3]
                print(f"   💰 Méthode: '{method}' ({count} factures)")
                print(f"      Total: {total_amount:.2f} FCFA")
                print(f"      Exemples: {', '.join(exemples_list)}")
        else:
            print("   ⚠️ Aucune méthode de paiement trouvée")
        
        # 4. Analyser les points de vente
        print("\n🏪 4. POINTS DE VENTE")
        print("-" * 18)
        
        cursor.execute("""
            SELECT 
                PointOfSale,
                COUNT(*) as count,
                GROUP_CONCAT(DISTINCT Template) as templates_utilises,
                GROUP_CONCAT(DISTINCT InvoiceNumber) as exemples
            FROM FneInvoices 
            WHERE PointOfSale IS NOT NULL 
            GROUP BY PointOfSale 
            ORDER BY count DESC
        """)
        
        points_of_sale = cursor.fetchall()
        
        if points_of_sale:
            for pos, count, templates, exemples in points_of_sale:
                exemples_list = exemples.split(',')[:3]
                templates_list = set(templates.split(',')) if templates else []
                print(f"   🏪 Point de vente: '{pos}' ({count} factures)")
                print(f"      Templates utilisés: {', '.join(templates_list)}")
                print(f"      Exemples: {', '.join(exemples_list)}")
        else:
            print("   ⚠️ Aucun point de vente trouvé")
        
        # 5. Analyser la cohérence avec les spécifications FNE
        print("\n🎯 5. VÉRIFICATION CONFORMITÉ API FNE")
        print("-" * 38)
        
        # Templates valides selon documentation FNE
        templates_fne_valides = ["B2C", "B2B", "B2G"]
        
        # Types valides selon documentation FNE  
        types_fne_valides = ["sale", "refund", "proforma", "delivery"]
        
        # Méthodes de paiement valides selon FNE
        payment_methods_fne_valides = [
            "cash", "mobile-money", "card", "check", 
            "transfer", "credit", "other"
        ]
        
        print("   📚 TEMPLATES FNE ATTENDUS:", ", ".join(templates_fne_valides))
        templates_actuels = [t[0] for t in templates_factures] if templates_factures else []
        templates_invalides = [t for t in templates_actuels if t not in templates_fne_valides]
        if templates_invalides:
            print(f"   ❌ Templates INVALIDES trouvés: {', '.join(templates_invalides)}")
        else:
            print("   ✅ Tous les templates sont conformes FNE")
        
        print("   🏷️ TYPES FNE ATTENDUS:", ", ".join(types_fne_valides))
        types_actuels = [t[0] for t in types_factures] if types_factures else []
        types_invalides = [t for t in types_actuels if t not in types_fne_valides]
        if types_invalides:
            print(f"   ❌ Types INVALIDES trouvés: {', '.join(types_invalides)}")
        else:
            print("   ✅ Tous les types sont conformes FNE")
        
        print("   💳 PAIEMENTS FNE ATTENDUS:", ", ".join(payment_methods_fne_valides))
        payments_actuels = [p[0] for p in payment_methods] if payment_methods else []
        payments_invalides = [p for p in payments_actuels if p not in payment_methods_fne_valides]
        if payments_invalides:
            print(f"   ❌ Méthodes INVALIDES trouvées: {', '.join(payments_invalides)}")
        else:
            print("   ✅ Toutes les méthodes de paiement sont conformes FNE")
        
        # 6. Analyser des factures spécifiques pour débogage
        print("\n🔬 6. ANALYSE FACTURES SPÉCIFIQUES")
        print("-" * 32)
        
        factures_test = ["556452", "556469"]
        
        for facture_num in factures_test:
            cursor.execute("""
                SELECT 
                    InvoiceNumber,
                    Template,
                    InvoiceType,
                    PaymentMethod,
                    PointOfSale,
                    EstablishmentName,
                    ClientCode,
                    TotalAmountTTC
                FROM FneInvoices 
                WHERE InvoiceNumber = ?
            """, (facture_num,))
            
            facture = cursor.fetchone()
            
            if facture:
                (num, template, inv_type, payment, pos, establishment, 
                 client_code, amount) = facture
                
                print(f"\n   📄 FACTURE {num}:")
                print(f"      Template: '{template}'")
                print(f"      Type: '{inv_type}'")
                print(f"      Paiement: '{payment}'")
                print(f"      Point de vente: '{pos}'")
                print(f"      Établissement: '{establishment}'")
                print(f"      Client: {client_code}")
                print(f"      Montant: {amount} FCFA")
                
                # Validation FNE pour cette facture
                issues = []
                if template not in templates_fne_valides:
                    issues.append(f"Template '{template}' invalide")
                if inv_type not in types_fne_valides:
                    issues.append(f"Type '{inv_type}' invalide")
                if payment not in payment_methods_fne_valides:
                    issues.append(f"Paiement '{payment}' invalide")
                
                if issues:
                    print(f"      ❌ PROBLÈMES FNE: {', '.join(issues)}")
                else:
                    print("      ✅ CONFORME FNE")
            else:
                print(f"   ❌ Facture {facture_num} non trouvée")
        
        print(f"\n🎯 RECOMMANDATIONS")
        print("-" * 15)
        
        # Recommandations basées sur l'analyse
        if not templates_factures:
            print("   🔧 URGENT: Configurer les templates dans les factures")
        
        if not payment_methods:
            print("   🔧 URGENT: Configurer les méthodes de paiement")
        
        if templates_invalides:
            print(f"   🔧 Corriger les templates invalides: {', '.join(templates_invalides)}")
            print("      → Utiliser: B2C (client), B2B (entreprise), B2G (gouvernement)")
        
        if types_invalides:
            print(f"   🔧 Corriger les types invalides: {', '.join(types_invalides)}")
            print("      → Utiliser: sale (vente), refund (avoir), proforma, delivery")
        
        if payments_invalides:
            print(f"   🔧 Corriger les paiements invalides: {', '.join(payments_invalides)}")
            print("      → Utiliser: cash, mobile-money, card, transfer, etc.")
    
    except sqlite3.Error as e:
        print(f"❌ Erreur base de données: {e}")
    
    except Exception as e:
        print(f"❌ Erreur inattendue: {e}")
    
    finally:
        if 'conn' in locals():
            conn.close()

if __name__ == "__main__":
    analyze_templates_and_types()
    print("\n✅ Analyse terminée")