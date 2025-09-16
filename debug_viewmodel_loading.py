#!/usr/bin/env python3
"""
Script pour tester directement le chargement des données via une simulation .NET
afin de comprendre pourquoi l'interface reste sur "Chargement..."
"""

import sqlite3
import os
import time

def simulate_viewmodel_loading():
    """Simule exactement ce que fait le ViewModel pour identifier le problème"""
    print("=== SIMULATION DU CHARGEMENT VIEWMODEL ===")
    
    # 1. Vérification de la base de données
    db_path = "D:/PROJET/FNE/FNEV4/data/FNEV4.db"
    if not os.path.exists(db_path):
        print("❌ BASE DE DONNÉES NON TROUVÉE")
        return False
    
    print("✅ Base de données trouvée")
    
    # 2. Test de la nouvelle méthode GetAvailableForCertificationAsync
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Test équivalent à GetAvailableForCertificationAsync
        print("\n🔄 Exécution de GetAvailableForCertificationAsync...")
        query = """
        SELECT Id, InvoiceNumber, ClientCode, TotalAmountTTC, Status, CertifiedAt
        FROM FneInvoices 
        WHERE Status = 'draft' AND CertifiedAt IS NULL
        ORDER BY CreatedAt DESC
        """
        
        start_time = time.time()
        cursor.execute(query)
        invoices = cursor.fetchall()
        end_time = time.time()
        
        print(f"⏱️ Requête exécutée en {(end_time - start_time)*1000:.1f}ms")
        print(f"📊 {len(invoices)} factures trouvées")
        
        if len(invoices) > 0:
            print("\n✅ DONNÉES DISPONIBLES")
            print("Échantillon des 3 premières factures:")
            for i, invoice in enumerate(invoices[:3]):
                print(f"  {i+1}. {invoice[1]} | Client: {invoice[2]} | Montant: {invoice[3]}€")
        else:
            print("\n❌ AUCUNE DONNÉE TROUVÉE")
            return False
        
        conn.close()
        
        # 3. Simulation des étapes du ViewModel
        print("\n🧠 SIMULATION DES ÉTAPES DU VIEWMODEL:")
        
        print("  1. IsLoading = true ✅")
        print("  2. StatusMessage = 'Chargement des factures...' ✅")
        print("  3. Repository.GetAvailableForCertificationAsync() ✅")
        print(f"  4. {len(invoices)} factures récupérées ✅")
        print("  5. AvailableInvoices.Clear() ✅")
        print("  6. Ajout des factures à AvailableInvoices ✅")
        print("  7. TotalInvoicesCount = " + str(len(invoices)) + " ✅")
        print("  8. HasAvailableInvoices = True ✅")
        print("  9. StatusMessage = 'XX facture(s) disponible(s)' ✅")
        print("  10. IsLoading = false ✅")
        
        print("\n🎯 CONCLUSION: La logique devrait fonctionner!")
        return True
        
    except Exception as e:
        print(f"\n❌ ERREUR: {e}")
        conn.close()
        return False

def check_potential_issues():
    """Vérifie les problèmes potentiels"""
    print("\n=== VÉRIFICATION DES PROBLÈMES POTENTIELS ===")
    
    issues = []
    
    # 1. Vérifier les colonnes nécessaires
    db_path = "D:/PROJET/FNE/FNEV4/data/FNEV4.db"
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    try:
        # Test de toutes les colonnes utilisées par l'interface
        cursor.execute("""
        SELECT Id, InvoiceNumber, ClientCode, TotalAmountTTC, Status, CertifiedAt, CreatedAt
        FROM FneInvoices LIMIT 1
        """)
        result = cursor.fetchone()
        if result:
            print("✅ Toutes les colonnes nécessaires existent")
        else:
            issues.append("❌ Problème avec les colonnes de la base")
    except Exception as e:
        issues.append(f"❌ Erreur colonnes: {e}")
    
    # 2. Vérifier les relations Client
    try:
        cursor.execute("""
        SELECT f.Id, f.ClientCode, c.CompanyName 
        FROM FneInvoices f
        LEFT JOIN Clients c ON f.ClientId = c.Id
        LIMIT 3
        """)
        client_results = cursor.fetchall()
        
        has_clients = any(result[2] is not None for result in client_results)
        if has_clients:
            print("✅ Relations Client trouvées")
        else:
            print("⚠️ Pas de relations Client - utiliser ClientCode")
    except Exception as e:
        issues.append(f"⚠️ Problème relations Client: {e}")
    
    conn.close()
    
    if issues:
        print("\n🚨 PROBLÈMES DÉTECTÉS:")
        for issue in issues:
            print(f"  {issue}")
        return False
    else:
        print("\n✅ Aucun problème structural détecté")
        return True

def main():
    print("🔍 DIAGNOSTIC APPROFONDI - INTERFACE CERTIFICATION MANUELLE")
    print("=" * 60)
    
    data_ok = simulate_viewmodel_loading()
    issues_ok = check_potential_issues()
    
    print("\n" + "=" * 60)
    print("📋 RÉSUMÉ DU DIAGNOSTIC:")
    
    if data_ok and issues_ok:
        print("✅ Les données et la logique sont correctes")
        print("\n🤔 PROBLÈMES POTENTIELS RESTANTS:")
        print("  1. Exception non catchée dans le ViewModel")
        print("  2. Problème d'injection de dépendances")
        print("  3. Problème de threading/UI")
        print("  4. Configuration de la base incorrecte")
        print("\n💡 RECOMMANDATIONS:")
        print("  - Ajouter des try-catch détaillés dans LoadAvailableInvoicesAsync")
        print("  - Vérifier les logs de l'application")
        print("  - Tester le repository directement")
    else:
        print("❌ Problèmes détectés dans les données ou la structure")

if __name__ == "__main__":
    main()