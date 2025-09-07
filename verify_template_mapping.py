#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
FNEV4 - Vérification Template Mapping Post-Correction
======================================================

Ce script vérifie que les nouveaux clients d'import exceptionnel
utilisent bien le bon mapping template après notre correction.

Date: 7 Septembre 2025
"""

import sqlite3
import os
from datetime import datetime

def verify_template_mapping():
    """
    Vérifie la cohérence du mapping template après correction
    """
    print("🔍 VÉRIFICATION TEMPLATE MAPPING")
    print("=" * 40)
    
    # Chemin de la base de données
    db_path = os.path.join("data", "FNEV4.db")
    
    if not os.path.exists(db_path):
        print(f"❌ Base de données non trouvée: {db_path}")
        return
    
    try:
        # Connexion à la base de données
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Analyser les clients d'import exceptionnel
        cursor.execute("""
            SELECT 
                ClientType,
                DefaultTemplate,
                DefaultPaymentMethod,
                COUNT(*) as count
            FROM Clients 
            WHERE Notes LIKE '%Import exceptionnel%'
            GROUP BY ClientType, DefaultTemplate, DefaultPaymentMethod
            ORDER BY ClientType, DefaultTemplate, DefaultPaymentMethod
        """)
        
        results = cursor.fetchall()
        
        if not results:
            print("ℹ️  Aucun client d'import exceptionnel trouvé")
            print("   (Probablement normal après nettoyage)")
            conn.close()
            return
        
        print(f"📊 Analyse des nouveaux clients d'import exceptionnel:")
        print()
        
        # Mapping attendu
        expected_mapping = {
            'Company': 'B2B',
            'Individual': 'B2C', 
            'Government': 'B2G',
            'Foreign': 'B2F'
        }
        
        correct_mappings = 0
        incorrect_mappings = 0
        
        for client_type, template, payment_method, count in results:
            expected = expected_mapping.get(client_type, "UNKNOWN")
            status = "✅" if template == expected else "❌"
            
            if template == expected:
                correct_mappings += count
            else:
                incorrect_mappings += count
            
            print(f"  {status} {client_type:12} → {template:8} / {payment_method:12} ({count:3} clients) [Attendu: {expected}]")
        
        print()
        print(f"📈 RÉSUMÉ:")
        print(f"  ✅ Mappings corrects:   {correct_mappings}")
        print(f"  ❌ Mappings incorrects: {incorrect_mappings}")
        print(f"  📊 Total clients:      {correct_mappings + incorrect_mappings}")
        
        if incorrect_mappings == 0:
            print()
            print("🎉 SUCCÈS! Tous les templates sont correctement mappés!")
            print("   La correction fonctionne parfaitement.")
        else:
            print()
            print("⚠️  ATTENTION! Des mappings incorrects sont encore présents.")
            print("   La correction nécessite des ajustements.")
        
        # Analyser les templates spécifiques
        print()
        print("🔍 DÉTAIL DES TEMPLATES:")
        
        cursor.execute("""
            SELECT 
                DefaultTemplate,
                DefaultPaymentMethod,
                COUNT(*) as count
            FROM Clients 
            WHERE Notes LIKE '%Import exceptionnel%'
            GROUP BY DefaultTemplate, DefaultPaymentMethod
            ORDER BY count DESC
        """)
        
        template_results = cursor.fetchall()
        
        for template, payment_method, count in template_results:
            if template == "DGI1":
                print(f"  ❌ {template:8} / {payment_method:12} → {count:3} clients (PROBLÈME: ancien template)")
            elif template in ["B2B", "B2C", "B2G", "B2F"]:
                print(f"  ✅ {template:8} / {payment_method:12} → {count:3} clients (OK: template API DGI)")
            else:
                print(f"  ⚠️  {template:8} / {payment_method:12} → {count:3} clients (INCONNU)")
        
        conn.close()
        
    except Exception as e:
        print(f"❌ Erreur lors de la vérification: {e}")

if __name__ == "__main__":
    verify_template_mapping()
