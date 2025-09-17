#!/usr/bin/env python3
"""
Liste complète des types de factures dans la base de données
"""

import sqlite3
from pathlib import Path

def list_invoice_types():
    db_path = Path("data/FNEV4.db")
    
    if not db_path.exists():
        print("❌ Base de données FNEV4.db introuvable")
        return
    
    try:
        conn = sqlite3.connect(str(db_path))
        cursor = conn.cursor()
        
        print("📋 TYPES DE FACTURES DANS LA BASE DE DONNÉES")
        print("=" * 50)
        
        # 1. Lister tous les types de factures distincts
        print("\n🏷️ 1. TYPES DE FACTURES (InvoiceType)")
        print("-" * 38)
        
        cursor.execute("""
            SELECT 
                InvoiceType,
                COUNT(*) as nb_factures,
                MIN(InvoiceDate) as premiere_date,
                MAX(InvoiceDate) as derniere_date,
                SUM(TotalAmountTTC) as total_montant,
                AVG(TotalAmountTTC) as montant_moyen
            FROM FneInvoices 
            WHERE InvoiceType IS NOT NULL 
            GROUP BY InvoiceType 
            ORDER BY nb_factures DESC
        """)
        
        types_factures = cursor.fetchall()
        
        if types_factures:
            for type_fac, nb, premiere, derniere, total, moyenne in types_factures:
                print(f"   📄 Type: '{type_fac}'")
                print(f"      Nombre de factures: {nb}")
                print(f"      Période: {premiere} → {derniere}")
                print(f"      Total montant: {total:,.2f} FCFA")
                print(f"      Montant moyen: {moyenne:,.2f} FCFA")
                print()
        else:
            print("   ⚠️ Aucun type de facture trouvé")
        
        # 2. Vérifier les types NULL ou vides
        print("🔍 2. VÉRIFICATION DES VALEURS MANQUANTES")
        print("-" * 42)
        
        cursor.execute("""
            SELECT 
                COUNT(*) as total_factures,
                SUM(CASE WHEN InvoiceType IS NULL THEN 1 ELSE 0 END) as type_null,
                SUM(CASE WHEN InvoiceType = '' THEN 1 ELSE 0 END) as type_vide
            FROM FneInvoices
        """)
        
        stats = cursor.fetchone()
        total, null_count, empty_count = stats
        
        print(f"   📊 Total factures: {total}")
        print(f"   ❌ Types NULL: {null_count}")
        print(f"   ❌ Types vides: {empty_count}")
        print(f"   ✅ Types renseignés: {total - null_count - empty_count}")
        
        # 3. Analyser les templates associés
        print("\n📄 3. TYPES vs TEMPLATES")
        print("-" * 22)
        
        cursor.execute("""
            SELECT 
                InvoiceType,
                Template,
                COUNT(*) as nb_factures
            FROM FneInvoices 
            WHERE InvoiceType IS NOT NULL AND Template IS NOT NULL
            GROUP BY InvoiceType, Template
            ORDER BY InvoiceType, nb_factures DESC
        """)
        
        type_template = cursor.fetchall()
        
        if type_template:
            current_type = None
            for inv_type, template, nb in type_template:
                if inv_type != current_type:
                    if current_type is not None:
                        print()
                    print(f"   🏷️ Type '{inv_type}':")
                    current_type = inv_type
                print(f"      → Template '{template}': {nb} factures")
        
        # 4. Analyser les méthodes de paiement par type
        print(f"\n💳 4. TYPES vs MÉTHODES DE PAIEMENT")
        print("-" * 34)
        
        cursor.execute("""
            SELECT 
                InvoiceType,
                PaymentMethod,
                COUNT(*) as nb_factures,
                SUM(TotalAmountTTC) as total_montant
            FROM FneInvoices 
            WHERE InvoiceType IS NOT NULL AND PaymentMethod IS NOT NULL
            GROUP BY InvoiceType, PaymentMethod
            ORDER BY InvoiceType, nb_factures DESC
        """)
        
        type_payment = cursor.fetchall()
        
        if type_payment:
            current_type = None
            for inv_type, payment, nb, total in type_payment:
                if inv_type != current_type:
                    if current_type is not None:
                        print()
                    print(f"   🏷️ Type '{inv_type}':")
                    current_type = inv_type
                print(f"      → Paiement '{payment}': {nb} factures ({total:,.2f} FCFA)")
        
        # 5. Conformité FNE
        print(f"\n🎯 5. CONFORMITÉ AVEC SPÉCIFICATIONS FNE")
        print("-" * 40)
        
        # Types FNE officiels selon documentation
        types_fne_officiels = ["sale", "refund", "proforma", "delivery"]
        
        print(f"   📚 Types FNE officiels autorisés:")
        for type_fne in types_fne_officiels:
            print(f"      • {type_fne}")
        
        print(f"\n   🔍 Analyse de conformité:")
        
        for type_fac, nb, _, _, _, _ in types_factures:
            if type_fac in types_fne_officiels:
                print(f"      ✅ '{type_fac}' - CONFORME FNE ({nb} factures)")
            else:
                print(f"      ❌ '{type_fac}' - NON CONFORME FNE ({nb} factures)")
                print(f"         → Suggestions: {', '.join(types_fne_officiels)}")
        
        # 6. Exemples de factures par type
        print(f"\n📋 6. EXEMPLES DE FACTURES PAR TYPE")
        print("-" * 33)
        
        for type_fac, _, _, _, _, _ in types_factures:
            cursor.execute("""
                SELECT InvoiceNumber, ClientCode, TotalAmountTTC, InvoiceDate
                FROM FneInvoices 
                WHERE InvoiceType = ?
                ORDER BY InvoiceDate DESC
                LIMIT 3
            """, (type_fac,))
            
            exemples = cursor.fetchall()
            
            print(f"   🏷️ Type '{type_fac}' - Exemples:")
            for num, client, montant, date in exemples:
                print(f"      • Facture {num} - Client {client} - {montant:,.2f} FCFA ({date})")
            print()
        
        # 7. Statistiques par période
        print(f"📊 7. RÉPARTITION TEMPORELLE")
        print("-" * 26)
        
        cursor.execute("""
            SELECT 
                strftime('%Y-%m', InvoiceDate) as mois,
                InvoiceType,
                COUNT(*) as nb_factures,
                SUM(TotalAmountTTC) as total_montant
            FROM FneInvoices 
            WHERE InvoiceType IS NOT NULL
            GROUP BY strftime('%Y-%m', InvoiceDate), InvoiceType
            ORDER BY mois DESC, nb_factures DESC
            LIMIT 10
        """)
        
        repartition = cursor.fetchall()
        
        if repartition:
            print(f"   📅 Répartition par mois (10 derniers):")
            for mois, type_fac, nb, total in repartition:
                print(f"      {mois}: {type_fac} - {nb} factures ({total:,.2f} FCFA)")
        
    except sqlite3.Error as e:
        print(f"❌ Erreur base de données: {e}")
    
    except Exception as e:
        print(f"❌ Erreur inattendue: {e}")
    
    finally:
        if 'conn' in locals():
            conn.close()

if __name__ == "__main__":
    list_invoice_types()
    print(f"\n✅ Analyse terminée")