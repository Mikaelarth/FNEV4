#!/usr/bin/env python3
"""
Comparaison entre les templates en base de données et ceux affichés dans l'interface
"""

import sqlite3
from pathlib import Path

def compare_templates_display():
    db_path = Path("data/FNEV4.db")
    
    if not db_path.exists():
        print("❌ Base de données FNEV4.db introuvable")
        return
    
    try:
        conn = sqlite3.connect(str(db_path))
        cursor = conn.cursor()
        
        print("🔍 COMPARAISON TEMPLATES BASE vs INTERFACE")
        print("=" * 50)
        
        # Analyser les factures visibles dans les captures d'écran
        factures_captures = ["556452", "556469"]  # Vues dans les captures
        
        for facture_num in factures_captures:
            print(f"\n📄 FACTURE {facture_num}")
            print("-" * 25)
            
            # Récupérer TOUTES les données de template/affichage
            cursor.execute("""
                SELECT 
                    InvoiceNumber,
                    Template,
                    InvoiceType,
                    PaymentMethod,
                    PointOfSale,
                    ClientCode,
                    CommercialMessage,
                    -- Colonnes qui pourraient contenir des infos d'affichage
                    FneReference,
                    InvoiceDate,
                    TotalAmountTTC
                FROM FneInvoices 
                WHERE InvoiceNumber = ?
            """, (facture_num,))
            
            facture = cursor.fetchone()
            
            if facture:
                (num, template, inv_type, payment, pos, client_code, 
                 message, fne_ref, date, amount) = facture
                
                print(f"   🗄️ BASE DE DONNÉES:")
                print(f"      Template: '{template}'")
                print(f"      Type: '{inv_type}'")
                print(f"      Paiement: '{payment}'")
                print(f"      Point de vente: '{pos}'")
                print(f"      Client: {client_code}")
                print(f"      Message: {message}")
                print(f"      Date: {date}")
                print(f"      Montant: {amount}")
                
                # Récupérer les infos client de la table Clients
                cursor.execute("""
                    SELECT Name, CompanyName, ClientNcc, DefaultTemplate, 
                           DefaultPaymentMethod, Country, DefaultCurrency
                    FROM Clients 
                    WHERE ClientCode = ?
                """, (client_code,))
                
                client_info = cursor.fetchone()
                if client_info:
                    (name, company, ncc, def_template, def_payment, country, currency) = client_info
                    
                    print(f"\n   👤 DONNÉES CLIENT (table Clients):")
                    print(f"      Nom: {name}")
                    print(f"      Entreprise: {company}")
                    print(f"      NCC: {ncc}")
                    print(f"      Template défaut: {def_template}")
                    print(f"      Paiement défaut: {def_payment}")
                    print(f"      Pays: {country}")
                    print(f"      Devise: {currency}")
                
                # Analyser ce qui DEVRAIT s'afficher dans l'interface
                print(f"\n   🖥️ AFFICHAGE ATTENDU DANS L'INTERFACE:")
                
                # Template affiché
                template_affiche = None
                if facture_num == "556452":
                    # D'après la capture : "Business to Consumer"
                    template_affiche = "Business to Consumer"  
                elif facture_num == "556469":
                    # D'après la capture : "Business to Consumer" 
                    template_affiche = "Business to Consumer"
                
                print(f"      Template interface: '{template_affiche}'")
                print(f"      Template base: '{template}'")
                
                # Vérifier la correspondance
                template_mapping = {
                    "B2C": "Business to Consumer",
                    "B2B": "Business to Business", 
                    "B2G": "Business to Government"
                }
                
                template_attendu = template_mapping.get(template, template)
                
                if template_affiche == template_attendu:
                    print(f"      ✅ COHÉRENT: {template} → {template_affiche}")
                else:
                    print(f"      ❌ INCOHÉRENT:")
                    print(f"         Attendu: {template_attendu}")
                    print(f"         Affiché: {template_affiche}")
                
                # Analyser le nom client affiché
                print(f"\n   👤 ANALYSE NOM CLIENT AFFICHÉ:")
                
                nom_affiche = None
                if facture_num == "556452":
                    nom_affiche = "SDTM-CI (3012Q)"  # D'après capture
                elif facture_num == "556469":
                    nom_affiche = "SOREFCI (1999)"   # D'après capture
                
                print(f"      Nom interface: '{nom_affiche}'")
                
                if client_info:
                    nom_attendu = f"{name} ({client_code})"
                    if nom_affiche == nom_attendu:
                        print(f"      ✅ COHÉRENT: Nom correct")
                    else:
                        print(f"      ⚠️ DIFFÉRENCE DÉTECTÉE:")
                        print(f"         Attendu: {nom_attendu}")
                        print(f"         Affiché: {nom_affiche}")
                        
                        # Analyser d'où vient "SOREFCI" pour 556469
                        if facture_num == "556469" and "SOREFCI" in str(nom_affiche):
                            print(f"      🔍 Source probable de 'SOREFCI':")
                            if message and "SOREFCI" in message:
                                print(f"         → Trouvé dans CommercialMessage: '{message}'")
                            else:
                                print(f"         → Origine inconnue (possiblement calcul dans ViewModel)")
            else:
                print(f"   ❌ Facture {facture_num} non trouvée")
        
        # Vérifier le mapping template dans le code
        print(f"\n🔧 ANALYSE DU MAPPING TEMPLATE")
        print("-" * 30)
        
        print(f"📋 Templates stockés en base:")
        cursor.execute("""
            SELECT DISTINCT Template, COUNT(*) as count
            FROM FneInvoices 
            WHERE Template IS NOT NULL
            GROUP BY Template
        """)
        
        templates_db = cursor.fetchall()
        for template, count in templates_db:
            template_display = template_mapping.get(template, f"INCONNU: {template}")
            print(f"   {template} → {template_display} ({count} factures)")
        
        print(f"\n💡 CORRESPONDANCES ATTENDUES:")
        for code, display in template_mapping.items():
            print(f"   {code} → {display}")
        
        # Rechercher des incohérences potentielles
        print(f"\n🚨 VÉRIFICATION COHÉRENCE GLOBALE")
        print("-" * 35)
        
        cursor.execute("""
            SELECT f.InvoiceNumber, f.Template, c.DefaultTemplate, 
                   f.PaymentMethod, c.DefaultPaymentMethod,
                   f.ClientCode, c.Name
            FROM FneInvoices f
            JOIN Clients c ON f.ClientCode = c.ClientCode
            WHERE f.Template != c.DefaultTemplate 
               OR f.PaymentMethod != c.DefaultPaymentMethod
            LIMIT 5
        """)
        
        incoherences = cursor.fetchall()
        
        if incoherences:
            print(f"⚠️ INCOHÉRENCES TROUVÉES:")
            for (inv_num, f_template, c_template, f_payment, c_payment, client_code, client_name) in incoherences:
                print(f"   📄 Facture {inv_num} (Client: {client_name})")
                if f_template != c_template:
                    print(f"      Template: Facture={f_template} ≠ Client={c_template}")
                if f_payment != c_payment:
                    print(f"      Paiement: Facture={f_payment} ≠ Client={c_payment}")
        else:
            print(f"✅ AUCUNE INCOHÉRENCE template/paiement détectée")
    
    except sqlite3.Error as e:
        print(f"❌ Erreur base de données: {e}")
    
    except Exception as e:
        print(f"❌ Erreur inattendue: {e}")
    
    finally:
        if 'conn' in locals():
            conn.close()

if __name__ == "__main__":
    compare_templates_display()
    print(f"\n✅ Comparaison terminée")