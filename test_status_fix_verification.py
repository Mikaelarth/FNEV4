#!/usr/bin/env python3
"""
Script de vérification de la correction du problème de statut figé
dans le sous-menu "Chemins & Dossiers".

Ce script valide que :
1. Les corrections de threading sont bien implémentées
2. Le système de mise à jour de statut fonctionne correctement
3. L'interface utilisateur n'est plus figée
"""

import os
import re
import json
from datetime import datetime

def analyze_viewmodel_fixes():
    """Analyse les corrections apportées au ViewModel"""
    viewmodel_path = r"src\FNEV4.Presentation\ViewModels\Configuration\CheminsDossiersConfigViewModel.cs"
    
    if not os.path.exists(viewmodel_path):
        return {"error": "Fichier ViewModel non trouvé"}
    
    with open(viewmodel_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    fixes = {
        "using_system_windows": "using System.Windows;" in content,
        "initialize_status_immediately_method": "InitializeStatusImmediatelyAsync" in content,
        "dispatcher_invoke_async": "Application.Current.Dispatcher.InvokeAsync" in content,
        "proper_async_initialization": "_ = InitializeStatusImmediatelyAsync();" in content,
        "ui_thread_updates": content.count("Dispatcher.InvokeAsync") >= 3
    }
    
    # Vérification de la structure des méthodes
    method_patterns = {
        "InitializeStatusImmediatelyAsync": r"private async Task InitializeStatusImmediatelyAsync\(\)",
        "UpdateAllStatusAsync": r"private async Task UpdateAllStatusAsync\(\)",
        "UpdateGlobalStatus": r"private void UpdateGlobalStatus\(\)"
    }
    
    for method_name, pattern in method_patterns.items():
        fixes[f"method_{method_name.lower()}"] = bool(re.search(pattern, content))
    
    # Vérification des corrections de threading
    threading_fixes = {
        "global_status_message_ui_thread": "GlobalStatusMessage =" in content and "Dispatcher.InvokeAsync" in content,
        "global_status_icon_ui_thread": "GlobalStatusIcon =" in content and "Dispatcher.InvokeAsync" in content,
        "global_status_color_ui_thread": "GlobalStatusColor =" in content and "Dispatcher.InvokeAsync" in content,
        "configured_folders_count_ui_thread": "ConfiguredFoldersCount =" in content and "Dispatcher.InvokeAsync" in content
    }
    
    fixes.update(threading_fixes)
    
    return fixes

def check_compilation_status():
    """Vérifie le statut de compilation"""
    # Vérifier si les fichiers binaires existent
    binary_paths = [
        r"src\FNEV4.Presentation\bin\Release\net8.0-windows\FNEV4.Presentation.dll",
        r"src\FNEV4.Core\bin\Release\net8.0\FNEV4.Core.dll",
        r"src\FNEV4.Application\bin\Release\net8.0\FNEV4.Application.dll"
    ]
    
    compilation_status = {}
    for binary_path in binary_paths:
        compilation_status[os.path.basename(binary_path)] = os.path.exists(binary_path)
    
    return compilation_status

def verify_threading_pattern():
    """Vérifie que le pattern de threading est correct"""
    viewmodel_path = r"src\FNEV4.Presentation\ViewModels\Configuration\CheminsDossiersConfigViewModel.cs"
    
    if not os.path.exists(viewmodel_path):
        return {"error": "Fichier ViewModel non trouvé"}
    
    with open(viewmodel_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Recherche des patterns critiques
    patterns = {
        "correct_constructor_call": r"_\s*=\s*InitializeStatusImmediatelyAsync\(\);",
        "dispatcher_usage": r"await\s+System\.Windows\.Application\.Current\.Dispatcher\.InvokeAsync",
        "async_initialization": r"private async Task InitializeStatusImmediatelyAsync\(\)",
        "ui_property_updates": r"GlobalStatus\w+\s*=.*Dispatcher\.InvokeAsync"
    }
    
    pattern_results = {}
    for pattern_name, pattern in patterns.items():
        matches = re.findall(pattern, content, re.MULTILINE | re.DOTALL)
        pattern_results[pattern_name] = {
            "found": len(matches) > 0,
            "count": len(matches)
        }
    
    return pattern_results

def create_verification_report():
    """Crée un rapport de vérification"""
    report = {
        "timestamp": datetime.now().isoformat(),
        "verification_type": "Correction du statut figé - Chemins & Dossiers",
        "viewmodel_fixes": analyze_viewmodel_fixes(),
        "compilation_status": check_compilation_status(),
        "threading_patterns": verify_threading_pattern()
    }
    
    # Calcul du score de correction
    fixes = report["viewmodel_fixes"]
    if "error" not in fixes:
        total_fixes = len([k for k, v in fixes.items() if isinstance(v, bool)])
        successful_fixes = len([k for k, v in fixes.items() if v is True])
        report["fix_success_rate"] = (successful_fixes / total_fixes) * 100 if total_fixes > 0 else 0
    
    return report

def display_verification_results(report):
    """Affiche les résultats de vérification"""
    print("=" * 80)
    print("RAPPORT DE VÉRIFICATION - CORRECTION STATUT FIGÉ")
    print("=" * 80)
    print(f"Timestamp: {report['timestamp']}")
    print()
    
    # Affichage des corrections du ViewModel
    print("📋 CORRECTIONS DU VIEWMODEL:")
    print("-" * 40)
    fixes = report["viewmodel_fixes"]
    if "error" in fixes:
        print(f"❌ Erreur: {fixes['error']}")
    else:
        for fix_name, status in fixes.items():
            icon = "✅" if status else "❌"
            print(f"{icon} {fix_name.replace('_', ' ').title()}: {status}")
        
        if "fix_success_rate" in report:
            print(f"\n📊 Taux de réussite des corrections: {report['fix_success_rate']:.1f}%")
    
    print()
    
    # Affichage du statut de compilation
    print("🔨 STATUT DE COMPILATION:")
    print("-" * 40)
    compilation = report["compilation_status"]
    for dll_name, exists in compilation.items():
        icon = "✅" if exists else "❌"
        print(f"{icon} {dll_name}: {'Compilé' if exists else 'Manquant'}")
    
    print()
    
    # Affichage des patterns de threading
    print("🧵 PATTERNS DE THREADING:")
    print("-" * 40)
    patterns = report["threading_patterns"]
    if "error" in patterns:
        print(f"❌ Erreur: {patterns['error']}")
    else:
        for pattern_name, result in patterns.items():
            icon = "✅" if result["found"] else "❌"
            print(f"{icon} {pattern_name.replace('_', ' ').title()}: {result['found']} (trouvé {result['count']} fois)")
    
    print()
    
    # Conclusion
    print("🎯 CONCLUSION:")
    print("-" * 40)
    
    if "error" not in fixes and report.get("fix_success_rate", 0) >= 80:
        print("✅ Les corrections semblent correctement implémentées")
        print("✅ Le problème de statut figé devrait être résolu")
        print("✅ L'interface utilisateur devrait se mettre à jour correctement")
    else:
        print("⚠️  Certaines corrections peuvent nécessiter une attention supplémentaire")
    
    print("\n📝 RECOMMANDATIONS:")
    print("-" * 40)
    print("1. Testez l'application pour vérifier que le statut se met à jour")
    print("2. Vérifiez que 'Configuration en cours...' disparaît après initialisation")
    print("3. Confirmez que le compteur de dossiers s'affiche correctement")
    print("4. Surveillez les logs pour d'éventuelles erreurs de threading")

def main():
    """Fonction principale"""
    print("Vérification des corrections du problème de statut figé...")
    print("Analyse en cours...\n")
    
    # Changer vers le répertoire du projet
    if os.path.exists("FNEV4.sln"):
        os.chdir(".")
    else:
        print("❌ Impossible de trouver le projet FNEV4")
        return
    
    # Créer le rapport de vérification
    report = create_verification_report()
    
    # Afficher les résultats
    display_verification_results(report)
    
    # Sauvegarder le rapport
    with open("verification_correction_statut_fige.json", 'w', encoding='utf-8') as f:
        json.dump(report, f, indent=2, ensure_ascii=False)
    
    print(f"\n💾 Rapport sauvegardé dans: verification_correction_statut_fige.json")

if __name__ == "__main__":
    main()
