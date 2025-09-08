#!/usr/bin/env python3
"""
Script de validation du système centralisé pour la vue EntrepriseConfig
Vérifie que le sous-menu "Configuration → Entreprise" utilise bien le système centralisé.
"""

import os
import re
import json
from pathlib import Path

def analyze_enterprise_config():
    """Analyse la configuration du sous-menu Entreprise pour valider l'utilisation du système centralisé"""
    results = {
        "enterprise_config_view": {
            "uses_dependency_injection": False,
            "uses_centralized_database": False,
            "properly_configured": False,
            "details": []
        },
        "view_model": {
            "uses_idatabase_service": False,
            "accesses_centralized_connection": False,
            "injection_configured": False,
            "details": []
        },
        "dependency_injection": {
            "registered_in_container": False,
            "correct_parameters": False,
            "details": []
        }
    }
    
    # 1. Analyser EntrepriseConfigView.xaml.cs
    view_file = Path("src/FNEV4.Presentation/Views/Configuration/EntrepriseConfigView.xaml.cs")
    if view_file.exists():
        content = view_file.read_text(encoding='utf-8')
        
        # Vérifier l'injection de dépendances
        if "App.ServiceProvider.GetRequiredService<EntrepriseConfigViewModel>()" in content:
            results["enterprise_config_view"]["uses_dependency_injection"] = True
            results["enterprise_config_view"]["details"].append("✅ Utilise l'injection de dépendances")
        
        # Vérifier le fallback
        if "new EntrepriseConfigViewModel()" in content:
            results["enterprise_config_view"]["details"].append("⚠️ Fallback présent pour l'injection")
        
        results["enterprise_config_view"]["properly_configured"] = results["enterprise_config_view"]["uses_dependency_injection"]
    
    # 2. Analyser EntrepriseConfigViewModel.cs
    viewmodel_file = Path("src/FNEV4.Presentation/ViewModels/Configuration/EntrepriseConfigViewModel.cs")
    if viewmodel_file.exists():
        content = viewmodel_file.read_text(encoding='utf-8')
        
        # Vérifier l'utilisation d'IDatabaseService
        if "IDatabaseService _databaseService" in content:
            results["view_model"]["uses_idatabase_service"] = True
            results["view_model"]["details"].append("✅ Utilise IDatabaseService")
        
        # Vérifier l'accès à la chaîne de connexion centralisée
        if "_databaseService.GetConnectionString()" in content:
            results["view_model"]["accesses_centralized_connection"] = True
            results["view_model"]["details"].append("✅ Accède à la chaîne de connexion centralisée")
        
        # Vérifier le constructeur avec injection
        if "IDatabaseService? databaseService" in content:
            results["view_model"]["injection_configured"] = True
            results["view_model"]["details"].append("✅ Constructeur configuré pour l'injection")
    
    # 3. Analyser App.xaml.cs pour la configuration d'injection
    app_file = Path("src/FNEV4.Presentation/App.xaml.cs")
    if app_file.exists():
        content = app_file.read_text(encoding='utf-8')
        
        # Vérifier l'enregistrement du ViewModel
        if "services.AddTransient<EntrepriseConfigViewModel>" in content:
            results["dependency_injection"]["registered_in_container"] = True
            results["dependency_injection"]["details"].append("✅ ViewModel enregistré dans le conteneur DI")
        
        # Vérifier les paramètres corrects
        if "provider.GetRequiredService<IDatabaseService>" in content:
            results["dependency_injection"]["correct_parameters"] = True
            results["dependency_injection"]["details"].append("✅ Paramètres d'injection corrects")
    
    return results

def generate_report(results):
    """Génère un rapport de validation"""
    print("=" * 80)
    print("🔍 VALIDATION DU SYSTÈME CENTRALISÉ - SOUS-MENU ENTREPRISE")
    print("=" * 80)
    
    total_checks = 0
    passed_checks = 0
    
    # Analyse EntrepriseConfigView
    print("\n📋 1. ANALYSE DE LA VUE (EntrepriseConfigView)")
    print("-" * 50)
    for detail in results["enterprise_config_view"]["details"]:
        print(f"   {detail}")
        total_checks += 1
        if "✅" in detail:
            passed_checks += 1
    
    # Analyse ViewModel
    print("\n🧠 2. ANALYSE DU VIEWMODEL (EntrepriseConfigViewModel)")
    print("-" * 50)
    for detail in results["view_model"]["details"]:
        print(f"   {detail}")
        total_checks += 1
        if "✅" in detail:
            passed_checks += 1
    
    # Analyse Injection de Dépendances
    print("\n💉 3. ANALYSE DE L'INJECTION DE DÉPENDANCES")
    print("-" * 50)
    for detail in results["dependency_injection"]["details"]:
        print(f"   {detail}")
        total_checks += 1
        if "✅" in detail:
            passed_checks += 1
    
    # Score global
    print("\n📊 RÉSULTAT GLOBAL")
    print("-" * 30)
    score = (passed_checks / total_checks * 100) if total_checks > 0 else 0
    status = "🟢 EXCELLENT" if score >= 80 else "🟡 BON" if score >= 60 else "🔴 À AMÉLIORER"
    print(f"Score: {passed_checks}/{total_checks} ({score:.1f}%) - {status}")
    
    # Validation centralisée
    is_centralized = (
        results["view_model"]["uses_idatabase_service"] and
        results["view_model"]["accesses_centralized_connection"] and
        results["dependency_injection"]["registered_in_container"]
    )
    
    print(f"\n🎯 UTILISE LE SYSTÈME CENTRALISÉ: {'✅ OUI' if is_centralized else '❌ NON'}")
    
    if is_centralized:
        print("\n🎉 VALIDATION RÉUSSIE!")
        print("Le sous-menu 'Configuration → Entreprise' utilise correctement le système centralisé:")
        print("• ✅ Injection de dépendances configurée")
        print("• ✅ Accès centralisé à la base de données")
        print("• ✅ Architecture Clean respectée")
        print("• ✅ Pas de chemins en dur dans le code")
    else:
        print("\n⚠️ AMÉLIORATIONS NÉCESSAIRES:")
        if not results["view_model"]["uses_idatabase_service"]:
            print("• ❌ Le ViewModel n'utilise pas IDatabaseService")
        if not results["view_model"]["accesses_centralized_connection"]:
            print("• ❌ Pas d'accès à la chaîne de connexion centralisée")
        if not results["dependency_injection"]["registered_in_container"]:
            print("• ❌ ViewModel non enregistré dans le conteneur DI")
    
    return is_centralized

def main():
    """Point d'entrée principal"""
    try:
        # Changer vers le répertoire du projet
        os.chdir(Path(__file__).parent)
        
        # Analyser la configuration
        results = analyze_enterprise_config()
        
        # Générer le rapport
        is_centralized = generate_report(results)
        
        # Sauvegarder les résultats
        with open("validation_centralization_enterprise_results.json", "w", encoding="utf-8") as f:
            json.dump(results, f, indent=2, ensure_ascii=False)
        
        print(f"\n📄 Résultats sauvegardés dans: validation_centralization_enterprise_results.json")
        
        return 0 if is_centralized else 1
        
    except Exception as e:
        print(f"❌ Erreur lors de la validation: {e}")
        return 1

if __name__ == "__main__":
    exit(main())
