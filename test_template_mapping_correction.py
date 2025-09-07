#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
FNEV4 - Test de Correction du Mapping Template
===============================================

Ce script teste la correction du mapping template dans ImportSpecialExcelUseCase
pour vérifier que les types de facturation (B2B, B2C, B2F, B2G) sont correctement
mappés comme templates et non comme "DGI1".

Date: 7 Septembre 2025
"""

import sqlite3
import os
from datetime import datetime

def analyze_template_mapping():
    """
    Analyse la cohérence du mapping template dans la base de données
    après l'import exceptionnel
    """
    print("🔍 ANALYSE DU MAPPING TEMPLATE - IMPORT EXCEPTIONNEL")
    print("=" * 60)
    
    # Chemin de la base de données
    db_path = os.path.join("data", "FNEV4.db")
    
    if not os.path.exists(db_path):
        print(f"❌ Base de données non trouvée: {db_path}")
        return
    
    try:
        # Connexion à la base de données
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        print(f"📁 Base de données: {db_path}")
        print(f"📅 Analyse effectuée le: {datetime.now().strftime('%d/%m/%Y %H:%M:%S')}")
        print()
        
        # 1. Analyse des templates utilisés
        print("1️⃣ ANALYSE DES TEMPLATES DANS LA BASE")
        print("-" * 40)
        
        cursor.execute("""
            SELECT 
                DefaultTemplate, 
                COUNT(*) as count,
                GROUP_CONCAT(DISTINCT ClientType) as client_types
            FROM Clients 
            WHERE DefaultTemplate IS NOT NULL
            GROUP BY DefaultTemplate
            ORDER BY count DESC
        """)
        
        templates = cursor.fetchall()
        
        if templates:
            print("Template           | Nb Clients | Types Clients")
            print("-" * 50)
            for template, count, types in templates:
                print(f"{template:<18} | {count:>10} | {types}")
        else:
            print("Aucun template trouvé dans la base")
        
        print()
        
        # 2. Analyse spécifique des clients avec notes "Import exceptionnel"
        print("2️⃣ CLIENTS IMPORT EXCEPTIONNEL")
        print("-" * 40)
        
        cursor.execute("""
            SELECT 
                ClientCode,
                Name,
                ClientType,
                DefaultTemplate,
                DefaultCurrency,
                Notes
            FROM Clients 
            WHERE Notes LIKE '%Import exceptionnel%'
            ORDER BY ClientCode
        """)
        
        exceptional_clients = cursor.fetchall()
        
        if exceptional_clients:
            print(f"📊 {len(exceptional_clients)} clients d'import exceptionnel trouvés")
            print()
            print("Code      | Type Client | Template | Devise | Notes")
            print("-" * 80)
            
            template_stats = {}
            type_stats = {}
            
            for code, name, client_type, template, currency, notes in exceptional_clients[:10]:
                # Statistiques
                template_stats[template] = template_stats.get(template, 0) + 1
                type_stats[client_type] = type_stats.get(client_type, 0) + 1
                
                # Troncature du nom et des notes pour l'affichage
                name_short = (name[:20] + "...") if len(name) > 20 else name
                notes_short = notes.split(" - ")[0] if " - " in notes else notes[:20]
                
                print(f"{code:<9} | {client_type:<11} | {template:<8} | {currency:<6} | {notes_short}")
            
            if len(exceptional_clients) > 10:
                print(f"... et {len(exceptional_clients) - 10} autres clients")
            
            print()
            print("📈 STATISTIQUES TEMPLATES (Import Exceptionnel):")
            for template, count in sorted(template_stats.items()):
                print(f"  • {template}: {count} clients")
            
            print()
            print("📈 STATISTIQUES TYPES CLIENTS (Import Exceptionnel):")
            for client_type, count in sorted(type_stats.items()):
                print(f"  • {client_type}: {count} clients")
                
        else:
            print("❌ Aucun client d'import exceptionnel trouvé")
        
        print()
        
        # 3. Vérification de cohérence
        print("3️⃣ VÉRIFICATION COHÉRENCE")
        print("-" * 40)
        
        # Vérifier s'il y a encore des "DGI1" (problème ancien)
        cursor.execute("""
            SELECT COUNT(*) 
            FROM Clients 
            WHERE DefaultTemplate = 'DGI1' 
            AND Notes LIKE '%Import exceptionnel%'
        """)
        
        dgi1_count = cursor.fetchone()[0]
        
        if dgi1_count > 0:
            print(f"⚠️  PROBLÈME: {dgi1_count} clients avec template 'DGI1' (ancien mapping)")
        else:
            print("✅ Aucun client avec l'ancien template 'DGI1'")
        
        # Vérifier cohérence Type <-> Template
        cursor.execute("""
            SELECT 
                ClientType,
                DefaultTemplate,
                COUNT(*) as count
            FROM Clients 
            WHERE Notes LIKE '%Import exceptionnel%'
            GROUP BY ClientType, DefaultTemplate
            ORDER BY ClientType, DefaultTemplate
        """)
        
        type_template_mapping = cursor.fetchall()
        
        print()
        print("🔗 MAPPING TYPE CLIENT → TEMPLATE:")
        expected_mapping = {
            "Company": "B2B",
            "Individual": "B2C", 
            "Government": "B2G"
        }
        
        issues = []
        for client_type, template, count in type_template_mapping:
            expected = expected_mapping.get(client_type, "Non défini")
            status = "✅" if template == expected else "⚠️"
            print(f"  {status} {client_type} → {template} ({count} clients) [Attendu: {expected}]")
            
            if template != expected:
                issues.append(f"{client_type} mappé vers {template} au lieu de {expected}")
        
        print()
        
        # 4. Conclusion
        print("4️⃣ CONCLUSION")
        print("-" * 40)
        
        if dgi1_count == 0 and len(issues) == 0:
            print("🎉 SUCCÈS: Le mapping template fonctionne correctement!")
            print("   • Aucun template 'DGI1' résiduel")
            print("   • Mapping Type → Template cohérent")
        else:
            print("❌ PROBLÈMES DÉTECTÉS:")
            if dgi1_count > 0:
                print(f"   • {dgi1_count} clients avec ancien template 'DGI1'")
            for issue in issues:
                print(f"   • {issue}")
        
        conn.close()
        
    except Exception as e:
        print(f"❌ Erreur lors de l'analyse: {e}")

if __name__ == "__main__":
    analyze_template_mapping()
