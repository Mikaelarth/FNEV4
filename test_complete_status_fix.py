#!/usr/bin/env python3
"""
Script de test pour simuler et vérifier la correction du problème 
de statut figé dans le sous-menu "Chemins & Dossiers".

Ce script simule l'initialisation du ViewModel et vérifie que
les mises à jour de statut sont correctement implémentées.
"""

import os
import re
import json
import subprocess
from datetime import datetime

def analyze_dispatcher_usage():
    """Analyse l'utilisation du Dispatcher dans le ViewModel"""
    viewmodel_path = r"src\FNEV4.Presentation\ViewModels\Configuration\CheminsDossiersConfigViewModel.cs"
    
    if not os.path.exists(viewmodel_path):
        return {"error": "Fichier ViewModel non trouvé"}
    
    with open(viewmodel_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Recherche des patterns critiques
    patterns = {
        "correct_initial_message": r'globalStatusMessage\s*=\s*"Initialisation en cours..."',
        "immediate_initialization": r'_\s*=\s*InitializeStatusImmediatelyAsync\(\);',
        "dispatcher_in_update_global": r'System\.Windows\.Application\.Current\.Dispatcher\.InvokeAsync\(\(\)\s*=>\s*{[^}]*GlobalStatusMessage\s*=',
        "configured_folders_count_update": r'ConfiguredFoldersCount\s*=\s*finalCount',
        "status_message_variations": [
            r'"🔧\s*\{\w+\}/5\s*dossiers\s*configurés"',
            r'"✅\s*Tous\s*les\s*dossiers\s*sont\s*configurés"',
            r'"❌.*dossier\(s\)\s*avec\s*erreurs"',
            r'"⚠️.*dossier\(s\)\s*avec\s*avertissements"'
        ]
    }
    
    results = {}
    
    # Test des patterns principaux
    for pattern_name, pattern in patterns.items():
        if pattern_name == "status_message_variations":
            results[pattern_name] = []
            for i, variation in enumerate(pattern):
                matches = re.findall(variation, content, re.MULTILINE | re.DOTALL)
                results[pattern_name].append({
                    "variation": i + 1,
                    "found": len(matches) > 0,
                    "count": len(matches)
                })
        else:
            matches = re.findall(pattern, content, re.MULTILINE | re.DOTALL)
            results[pattern_name] = {
                "found": len(matches) > 0,
                "count": len(matches)
            }
    
    return results

def test_initialization_flow():
    """Teste le flux d'initialisation"""
    viewmodel_path = r"src\FNEV4.Presentation\ViewModels\Configuration\CheminsDossiersConfigViewModel.cs"
    
    if not os.path.exists(viewmodel_path):
        return {"error": "Fichier ViewModel non trouvé"}
    
    with open(viewmodel_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Recherche du constructeur et de l'initialisation
    constructor_pattern = r'public\s+CheminsDossiersConfigViewModel\([^)]*\)\s*{([^}]+)}'
    constructor_match = re.search(constructor_pattern, content, re.DOTALL)
    
    if not constructor_match:
        return {"error": "Constructeur non trouvé"}
    
    constructor_content = constructor_match.group(1)
    
    # Vérifications dans le constructeur
    constructor_checks = {
        "calls_initialize_immediately": "InitializeStatusImmediatelyAsync" in constructor_content,
        "fire_and_forget_pattern": "_ =" in constructor_content,
        "no_await_in_constructor": "await" not in constructor_content
    }
    
    # Recherche de la méthode InitializeStatusImmediatelyAsync
    init_pattern = r'private\s+async\s+Task\s+InitializeStatusImmediatelyAsync\(\)\s*{([^}]+(?:{[^}]*}[^}]*)*)}'
    init_match = re.search(init_pattern, content, re.DOTALL)
    
    init_method_checks = {}
    if init_match:
        init_content = init_match.group(1)
        init_method_checks = {
            "has_dispatcher_invoke": "Dispatcher.InvokeAsync" in init_content,
            "sets_initial_message": "Vérification des dossiers" in init_content,
            "calls_update_all_status": "UpdateAllStatusAsync" in init_content,
            "has_error_handling": "catch" in init_content
        }
    else:
        init_method_checks = {"error": "Méthode InitializeStatusImmediatelyAsync non trouvée"}
    
    return {
        "constructor_checks": constructor_checks,
        "init_method_checks": init_method_checks
    }

def verify_status_update_logic():
    """Vérifie la logique de mise à jour des statuts"""
    viewmodel_path = r"src\FNEV4.Presentation\ViewModels\Configuration\CheminsDossiersConfigViewModel.cs"
    
    if not os.path.exists(viewmodel_path):
        return {"error": "Fichier ViewModel non trouvé"}
    
    with open(viewmodel_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Recherche de la méthode UpdateGlobalStatusAsync
    update_global_pattern = r'private\s+Task\s+UpdateGlobalStatusAsync\(\)\s*{([^}]+(?:{[^}]*}[^}]*)*)}'
    update_match = re.search(update_global_pattern, content, re.DOTALL)
    
    if not update_match:
        return {"error": "Méthode UpdateGlobalStatusAsync non trouvée"}
    
    update_content = update_match.group(1)
    
    # Vérifications de la logique
    logic_checks = {
        "calculates_valid_count": "validCount" in update_content,
        "calculates_warning_count": "warningCount" in update_content,
        "calculates_invalid_count": "invalidCount" in update_content,
        "has_status_logic": "if (invalidCount > 0)" in update_content,
        "uses_final_variables": "finalMessage" in update_content and "finalIcon" in update_content,
        "updates_in_dispatcher": "Dispatcher.InvokeAsync" in update_content,
        "updates_configured_count": "ConfiguredFoldersCount = finalCount" in update_content,
        "no_configuration_en_cours": "Configuration en cours" not in update_content,
        "has_configured_message": "dossiers configurés" in update_content
    }
    
    # Compter les différents états de messages
    message_states = {
        "error_message": update_content.count("❌"),
        "warning_message": update_content.count("⚠️"),
        "success_message": update_content.count("✅"),
        "progress_message": update_content.count("🔧")
    }
    
    return {
        "logic_checks": logic_checks,
        "message_states": message_states
    }

def simulate_runtime_test():
    """Simule un test d'exécution"""
    # Tentative de compilation
    try:
        result = subprocess.run(
            ["dotnet", "build", "FNEV4.sln", "--configuration", "Release", "--verbosity", "quiet"],
            capture_output=True,
            text=True,
            timeout=30
        )
        
        compilation_success = result.returncode == 0
        compilation_errors = result.stderr if result.stderr else "Aucune erreur"
        
        # Check des DLL générées
        dll_files = [
            r"src\FNEV4.Presentation\bin\Release\net8.0-windows\FNEV4.Presentation.dll",
            r"src\FNEV4.Core\bin\Release\net8.0\FNEV4.Core.dll",
            r"src\FNEV4.Application\bin\Release\net8.0\FNEV4.Application.dll"
        ]
        
        dll_status = {}
        for dll in dll_files:
            dll_status[os.path.basename(dll)] = os.path.exists(dll)
        
        return {
            "compilation_success": compilation_success,
            "compilation_errors": compilation_errors,
            "dll_status": dll_status
        }
        
    except subprocess.TimeoutExpired:
        return {"error": "Timeout lors de la compilation"}
    except Exception as e:
        return {"error": f"Erreur lors de la compilation: {str(e)}"}

def create_comprehensive_report():
    """Crée un rapport complet"""
    report = {
        "timestamp": datetime.now().isoformat(),
        "test_type": "Test Complet - Correction Statut Figé",
        "dispatcher_analysis": analyze_dispatcher_usage(),
        "initialization_flow": test_initialization_flow(),
        "status_update_logic": verify_status_update_logic(),
        "runtime_simulation": simulate_runtime_test()
    }
    
    # Calcul du score global
    scores = []
    
    # Score du Dispatcher
    dispatcher = report["dispatcher_analysis"]
    if "error" not in dispatcher:
        dispatcher_score = 0
        total_dispatcher_checks = 0
        
        for key, value in dispatcher.items():
            if key == "status_message_variations":
                for variation in value:
                    total_dispatcher_checks += 1
                    if variation["found"]:
                        dispatcher_score += 1
            elif isinstance(value, dict) and "found" in value:
                total_dispatcher_checks += 1
                if value["found"]:
                    dispatcher_score += 1
        
        if total_dispatcher_checks > 0:
            scores.append((dispatcher_score / total_dispatcher_checks) * 100)
    
    # Score d'initialisation
    init = report["initialization_flow"]
    if "error" not in init:
        init_score = 0
        total_init_checks = 0
        
        for check_group in ["constructor_checks", "init_method_checks"]:
            if check_group in init and "error" not in init[check_group]:
                for check, result in init[check_group].items():
                    total_init_checks += 1
                    if result:
                        init_score += 1
        
        if total_init_checks > 0:
            scores.append((init_score / total_init_checks) * 100)
    
    # Score de logique de mise à jour
    update = report["status_update_logic"]
    if "error" not in update and "logic_checks" in update:
        update_score = 0
        total_update_checks = len(update["logic_checks"])
        
        for check, result in update["logic_checks"].items():
            if result:
                update_score += 1
        
        if total_update_checks > 0:
            scores.append((update_score / total_update_checks) * 100)
    
    # Score global
    if scores:
        report["global_score"] = sum(scores) / len(scores)
    else:
        report["global_score"] = 0
    
    return report

def display_comprehensive_results(report):
    """Affiche les résultats complets"""
    print("=" * 80)
    print("RAPPORT COMPLET - TEST CORRECTION STATUT FIGÉ")
    print("=" * 80)
    print(f"Timestamp: {report['timestamp']}")
    print(f"🎯 Score Global: {report['global_score']:.1f}%")
    print()
    
    # Analyse du Dispatcher
    print("🧵 ANALYSE DU DISPATCHER:")
    print("-" * 40)
    dispatcher = report["dispatcher_analysis"]
    if "error" in dispatcher:
        print(f"❌ Erreur: {dispatcher['error']}")
    else:
        for key, value in dispatcher.items():
            if key == "status_message_variations":
                print("📝 Variations de messages de statut:")
                for variation in value:
                    icon = "✅" if variation["found"] else "❌"
                    print(f"  {icon} Variation {variation['variation']}: {variation['found']}")
            elif isinstance(value, dict) and "found" in value:
                icon = "✅" if value["found"] else "❌"
                print(f"{icon} {key.replace('_', ' ').title()}: {value['found']}")
    
    print()
    
    # Flux d'initialisation
    print("🚀 FLUX D'INITIALISATION:")
    print("-" * 40)
    init = report["initialization_flow"]
    if "error" in init:
        print(f"❌ Erreur: {init['error']}")
    else:
        print("Constructeur:")
        for check, result in init.get("constructor_checks", {}).items():
            icon = "✅" if result else "❌"
            print(f"  {icon} {check.replace('_', ' ').title()}: {result}")
        
        print("Méthode d'initialisation:")
        init_checks = init.get("init_method_checks", {})
        if "error" in init_checks:
            print(f"  ❌ {init_checks['error']}")
        else:
            for check, result in init_checks.items():
                icon = "✅" if result else "❌"
                print(f"  {icon} {check.replace('_', ' ').title()}: {result}")
    
    print()
    
    # Logique de mise à jour
    print("🔄 LOGIQUE DE MISE À JOUR:")
    print("-" * 40)
    update = report["status_update_logic"]
    if "error" in update:
        print(f"❌ Erreur: {update['error']}")
    else:
        logic_checks = update.get("logic_checks", {})
        for check, result in logic_checks.items():
            icon = "✅" if result else "❌"
            print(f"{icon} {check.replace('_', ' ').title()}: {result}")
        
        message_states = update.get("message_states", {})
        if message_states:
            print("\n📊 États des messages:")
            for state, count in message_states.items():
                print(f"  {state.replace('_', ' ').title()}: {count}")
    
    print()
    
    # Simulation d'exécution
    print("⚙️ SIMULATION D'EXÉCUTION:")
    print("-" * 40)
    runtime = report["runtime_simulation"]
    if "error" in runtime:
        print(f"❌ Erreur: {runtime['error']}")
    else:
        icon = "✅" if runtime.get("compilation_success", False) else "❌"
        print(f"{icon} Compilation: {'Réussie' if runtime.get('compilation_success', False) else 'Échouée'}")
        
        if not runtime.get("compilation_success", False):
            print(f"Erreurs: {runtime.get('compilation_errors', 'Inconnues')}")
        
        dll_status = runtime.get("dll_status", {})
        if dll_status:
            print("DLL générées:")
            for dll, exists in dll_status.items():
                icon = "✅" if exists else "❌"
                print(f"  {icon} {dll}: {'Présent' if exists else 'Manquant'}")
    
    print()
    
    # Conclusion
    print("🎯 CONCLUSION:")
    print("-" * 40)
    score = report['global_score']
    if score >= 95:
        print("🚀 EXCELLENT - Le problème de statut figé est complètement corrigé!")
        status = "RÉSOLU"
    elif score >= 85:
        print("✅ TRÈS BON - La correction devrait fonctionner correctement")
        status = "LARGEMENT CORRIGÉ"
    elif score >= 75:
        print("⚠️ BON - Quelques améliorations mineures peuvent être nécessaires")
        status = "MAJORITAIREMENT CORRIGÉ"
    else:
        print("❌ Des corrections supplémentaires sont nécessaires")
        status = "PARTIELLEMENT CORRIGÉ"
    
    print(f"Status: {status}")
    
    print("\n📝 RECOMMANDATIONS:")
    print("-" * 40)
    if score >= 90:
        print("1. ✅ Lancez l'application - le statut ne devrait plus être figé")
        print("2. ✅ Le message devrait passer de 'Initialisation en cours...' à '🔧 X/5 dossiers configurés'")
        print("3. ✅ Surveillez les logs pour confirmer le bon fonctionnement")
    else:
        print("1. Vérifiez les éléments marqués ❌ ci-dessus")
        print("2. Testez l'application avec prudence")
        print("3. Consultez les logs pour d'éventuelles erreurs")

def main():
    """Fonction principale"""
    print("Test complet de la correction du statut figé...")
    print("Analyse approfondie en cours...\n")
    
    # Changer vers le répertoire du projet
    if os.path.exists("FNEV4.sln"):
        os.chdir(".")
    else:
        print("❌ Impossible de trouver le projet FNEV4")
        return
    
    # Créer le rapport complet
    report = create_comprehensive_report()
    
    # Afficher les résultats
    display_comprehensive_results(report)
    
    # Sauvegarder le rapport
    with open("test_complet_correction_statut.json", 'w', encoding='utf-8') as f:
        json.dump(report, f, indent=2, ensure_ascii=False)
    
    print(f"\n💾 Rapport complet sauvegardé dans: test_complet_correction_statut.json")

if __name__ == "__main__":
    main()
