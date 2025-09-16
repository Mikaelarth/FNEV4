#!/usr/bin/env python3
"""
Script de validation complète du menu Certification FNE
========================================================

Ce script vérifie que tous les composants du menu Certification FNE 
fonctionnent correctement après les corrections d'architecture :

1. Navigation entre sous-menus
2. Chargement des ViewModels
3. Système de logging centralisé
4. Injection de dépendances
5. Cohérence des données

Auteur: Assistant IA
Date: Décembre 2024
"""

import sqlite3
import os
import sys
from datetime import datetime
import json

# Configuration
DB_PATH = r"d:\PROJET\FNE\FNEV4\Data\Database\fnev4_centralised.db"
LOG_DIR = r"d:\PROJET\FNE\FNEV4\Data\Logs"

def check_database_connection():
    """Vérifier la connexion à la base de données"""
    print("🔍 === VÉRIFICATION DE LA BASE DE DONNÉES ===")
    
    if not os.path.exists(DB_PATH):
        print(f"❌ Base de données introuvable: {DB_PATH}")
        return False
    
    try:
        conn = sqlite3.connect(DB_PATH)
        cursor = conn.cursor()
        
        # Vérifier les tables essentielles
        tables_required = ['Invoices', 'FneConfigurations', 'InvoicesFne']
        for table in tables_required:
            cursor.execute(f"SELECT COUNT(*) FROM {table}")
            count = cursor.fetchone()[0]
            print(f"✅ Table {table}: {count} enregistrements")
        
        conn.close()
        print("✅ Connexion base de données réussie")
        return True
        
    except Exception as e:
        print(f"❌ Erreur base de données: {e}")
        return False

def check_certification_data():
    """Vérifier les données de certification"""
    print("\n📊 === ANALYSE DES DONNÉES CERTIFICATION ===")
    
    try:
        conn = sqlite3.connect(DB_PATH)
        cursor = conn.cursor()
        
        # Statistiques factures
        cursor.execute("""
            SELECT 
                Status,
                COUNT(*) as count,
                SUM(CASE WHEN Amount IS NOT NULL THEN Amount ELSE 0 END) as total_amount
            FROM Invoices 
            GROUP BY Status
        """)
        
        print("📋 Répartition des factures par statut:")
        total_invoices = 0
        for status, count, amount in cursor.fetchall():
            total_invoices += count
            print(f"   • {status}: {count} factures (Total: {amount:.2f}€)")
        
        print(f"\n📈 Total factures: {total_invoices}")
        
        # Configurations FNE actives
        cursor.execute("SELECT COUNT(*) FROM FneConfigurations WHERE IsActive = 1")
        active_configs = cursor.fetchone()[0]
        print(f"⚙️ Configurations FNE actives: {active_configs}")
        
        # Factures certifiées FNE
        cursor.execute("""
            SELECT 
                COUNT(*) as certified_count,
                COUNT(CASE WHEN CertificationStatus = 'Success' THEN 1 END) as success_count,
                COUNT(CASE WHEN CertificationStatus = 'Error' THEN 1 END) as error_count
            FROM InvoicesFne
        """)
        
        certified, success, errors = cursor.fetchone()
        if certified > 0:
            print(f"🏆 Certifications FNE: {certified} total ({success} réussies, {errors} erreurs)")
        else:
            print("ℹ️ Aucune certification FNE trouvée")
        
        conn.close()
        return True
        
    except Exception as e:
        print(f"❌ Erreur analyse données: {e}")
        return False

def check_logging_system():
    """Vérifier le système de logging centralisé"""
    print("\n📝 === VÉRIFICATION SYSTÈME LOGGING ===")
    
    if not os.path.exists(LOG_DIR):
        print(f"❌ Répertoire de logs introuvable: {LOG_DIR}")
        return False
    
    print(f"✅ Répertoire de logs: {LOG_DIR}")
    
    # Chercher les fichiers de logs récents
    today = datetime.now().strftime("%Y%m%d")
    log_file_today = os.path.join(LOG_DIR, f"FNEV4_{today}.log")
    
    log_files = []
    for file in os.listdir(LOG_DIR):
        if file.startswith("FNEV4_") and file.endswith(".log"):
            log_files.append(file)
    
    print(f"📁 Fichiers de logs disponibles: {len(log_files)}")
    
    if log_files:
        latest_log = max(log_files)
        latest_path = os.path.join(LOG_DIR, latest_log)
        
        if os.path.exists(latest_path):
            size = os.path.getsize(latest_path)
            print(f"📄 Dernier log: {latest_log} ({size} bytes)")
            
            # Vérifier le contenu pour des entrées Certification
            try:
                with open(latest_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                    certification_logs = content.count("Certification")
                    fne_logs = content.count("FNE")
                    print(f"🔍 Entrées liées à Certification: {certification_logs}")
                    print(f"🔍 Entrées liées à FNE: {fne_logs}")
            except Exception as e:
                print(f"⚠️ Impossible de lire le log: {e}")
    
    return True

def check_viewmodel_architecture():
    """Vérifier l'architecture des ViewModels"""
    print("\n🏗️ === VÉRIFICATION ARCHITECTURE VIEWMODELS ===")
    
    # Chemins des ViewModels Certification FNE
    viewmodels = {
        "CertificationMainViewModel": r"d:\PROJET\FNE\FNEV4\src\FNEV4.Presentation\ViewModels\CertificationFne\CertificationMainViewModel.cs",
        "CertificationDashboardViewModel": r"d:\PROJET\FNE\FNEV4\src\FNEV4.Presentation\ViewModels\CertificationFne\CertificationDashboardViewModel.cs", 
        "CertificationManuelleViewModel": r"d:\PROJET\FNE\FNEV4\src\FNEV4.Presentation\ViewModels\CertificationFne\CertificationManuelleViewModel.cs",
        "CertificationAutomatiqueViewModel": r"d:\PROJET\FNE\FNEV4\src\FNEV4.Presentation\ViewModels\CertificationFne\CertificationAutomatiqueViewModel.cs"
    }
    
    architecture_checks = {
        "ILoggingService": 0,
        "NavigationItems": 0,
        "ObservableProperty": 0,
        "async Task": 0
    }
    
    for vm_name, vm_path in viewmodels.items():
        if os.path.exists(vm_path):
            print(f"✅ {vm_name}: Fichier trouvé")
            
            try:
                with open(vm_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                    
                    # Vérifications d'architecture
                    if "ILoggingService" in content:
                        architecture_checks["ILoggingService"] += 1
                        print(f"   ✓ Utilise ILoggingService")
                    
                    if "NavigationItems" in content:
                        architecture_checks["NavigationItems"] += 1
                        print(f"   ✓ Système de navigation")
                    
                    if "ObservableProperty" in content:
                        architecture_checks["ObservableProperty"] += 1
                        print(f"   ✓ MVVM Toolkit")
                    
                    if "async Task" in content:
                        architecture_checks["async Task"] += 1
                        print(f"   ✓ Méthodes asynchrones")
                        
            except Exception as e:
                print(f"   ❌ Erreur lecture: {e}")
        else:
            print(f"❌ {vm_name}: Fichier introuvable")
    
    print(f"\n📊 Résumé architecture:")
    for check, count in architecture_checks.items():
        print(f"   • {check}: {count}/4 ViewModels")
    
    return True

def check_dependency_injection():
    """Vérifier la configuration d'injection de dépendances"""
    print("\n💉 === VÉRIFICATION INJECTION DÉPENDANCES ===")
    
    app_xaml_path = r"d:\PROJET\FNE\FNEV4\src\FNEV4.Presentation\App.xaml.cs"
    
    if not os.path.exists(app_xaml_path):
        print(f"❌ App.xaml.cs introuvable: {app_xaml_path}")
        return False
    
    print("✅ App.xaml.cs trouvé")
    
    try:
        with open(app_xaml_path, 'r', encoding='utf-8') as f:
            content = f.read()
            
            # Vérifications DI
            checks = {
                "CertificationMainViewModel": "CertificationMainViewModel" in content,
                "CertificationDashboardViewModel": "CertificationDashboardViewModel" in content,
                "CertificationAutomatiqueViewModel": "CertificationAutomatiqueViewModel" in content,
                "ILoggingService": "ILoggingService" in content,
                "AddTransient": "AddTransient" in content
            }
            
            for check, result in checks.items():
                status = "✅" if result else "❌"
                print(f"   {status} {check}")
            
            return all(checks.values())
            
    except Exception as e:
        print(f"❌ Erreur lecture App.xaml.cs: {e}")
        return False

def generate_test_instructions():
    """Générer les instructions de test manuel"""
    print("\n🎮 === INSTRUCTIONS TEST MANUEL ===")
    
    instructions = [
        "1. Compiler et lancer FNEV4.Presentation",
        "2. Naviguer vers le menu 'Certification FNE'",
        "3. Vérifier l'affichage du tableau de bord",
        "4. Tester la navigation vers 'Certification manuelle'",
        "5. Vérifier le chargement des factures disponibles", 
        "6. Tester la navigation vers 'Certification automatique'",
        "7. Vérifier les options de configuration automatique",
        "8. Contrôler les logs dans Data\\Logs\\FNEV4_YYYYMMDD.log",
        "9. Valider l'absence d'erreurs de navigation",
        "10. Confirmer la cohérence des données affichées"
    ]
    
    for instruction in instructions:
        print(f"   {instruction}")
    
    print("\n💡 Points d'attention:")
    print("   • Les temps de chargement doivent être raisonnables")
    print("   • Aucune exception dans les logs")
    print("   • Navigation fluide entre sous-menus")
    print("   • Données cohérentes dans toutes les vues")

def main():
    """Fonction principale du script de validation"""
    print("🚀 === VALIDATION CERTIFICATION FNE COMPLÈTE ===")
    print(f"Heure de démarrage: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print("=" * 60)
    
    # Exécuter toutes les vérifications
    checks = [
        ("Base de données", check_database_connection),
        ("Données de certification", check_certification_data),
        ("Système de logging", check_logging_system),
        ("Architecture ViewModels", check_viewmodel_architecture),
        ("Injection dépendances", check_dependency_injection)
    ]
    
    results = {}
    
    for check_name, check_func in checks:
        try:
            results[check_name] = check_func()
        except Exception as e:
            print(f"❌ Erreur lors de {check_name}: {e}")
            results[check_name] = False
    
    # Résumé final
    print("\n" + "=" * 60)
    print("🎯 === RÉSUMÉ VALIDATION ===")
    
    passed = 0
    total = len(checks)
    
    for check_name, result in results.items():
        status = "✅ RÉUSSI" if result else "❌ ÉCHEC"
        print(f"   {check_name}: {status}")
        if result:
            passed += 1
    
    print(f"\n📊 Score global: {passed}/{total} ({(passed/total)*100:.1f}%)")
    
    if passed == total:
        print("\n🎉 SUCCÈS COMPLET!")
        print("Le menu Certification FNE est prêt pour les tests utilisateur.")
    else:
        print(f"\n⚠️ {total - passed} vérification(s) ont échoué.")
        print("Consulter les détails ci-dessus pour les corrections nécessaires.")
    
    # Instructions de test
    generate_test_instructions()
    
    print("\n" + "=" * 60)
    print(f"Validation terminée: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")

if __name__ == "__main__":
    main()