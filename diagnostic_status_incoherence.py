#!/usr/bin/env python3
"""
Script de diagnostic pour analyser l'incohérence entre l'affichage
visuel et le compteur de statut dans le sous-menu "Chemins & Dossiers".
"""

import os
import re
import json
from datetime import datetime

def analyze_status_logic():
    """Analyse la logique de calcul des statuts"""
    viewmodel_path = r"src\FNEV4.Presentation\ViewModels\Configuration\CheminsDossiersConfigViewModel.cs"
    
    if not os.path.exists(viewmodel_path):
        return {"error": "Fichier ViewModel non trouvé"}
    
    with open(viewmodel_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Recherche de la méthode UpdateGlobalStatusAsync
    update_global_pattern = r'private\s+Task\s+UpdateGlobalStatusAsync\(\)\s*\{([^}]+(?:\{[^}]*\}[^}]*)*)\}'
    update_match = re.search(update_global_pattern, content, re.DOTALL)
    
    if not update_match:
        return {"error": "Méthode UpdateGlobalStatusAsync non trouvée"}
    
    update_content = update_match.group(1)
    
    # Extraction des statuts par défaut
    status_defaults = {}
    default_patterns = {
        "ImportFolderStatus": r'private\s+string\s+importFolderStatus\s*=\s*"([^"]*)"',
        "ExportFolderStatus": r'private\s+string\s+exportFolderStatus\s*=\s*"([^"]*)"',
        "ArchiveFolderStatus": r'private\s+string\s+archiveFolderStatus\s*=\s*"([^"]*)"',
        "LogsFolderStatus": r'private\s+string\s+logsFolderStatus\s*=\s*"([^"]*)"',
        "BackupFolderStatus": r'private\s+string\s+backupFolderStatus\s*=\s*"([^"]*)"'
    }
    
    for status_name, pattern in default_patterns.items():
        match = re.search(pattern, content)
        if match:
            status_defaults[status_name] = match.group(1)
    
    # Analyse de la logique de comptage
    counting_logic = {
        "uses_status_array": "new[] { ImportFolderStatus" in update_content,
        "counts_valid": "Count(s => s == \"Valid\")" in update_content,
        "counts_warning": "Count(s => s == \"Warning\")" in update_content,
        "counts_invalid": "Count(s => s == \"Invalid\")" in update_content,
        "handles_unknown": "Unknown" in update_content
    }
    
    # Recherche des conditions de message
    message_conditions = []
    condition_patterns = [
        r'if\s*\([^)]*invalidCount[^)]*\)\s*\{[^}]*finalMessage\s*=\s*"([^"]*)"',
        r'else\s+if\s*\([^)]*warningCount[^)]*\)\s*\{[^}]*finalMessage\s*=\s*"([^"]*)"',
        r'else\s+if\s*\([^)]*validCount\s*==\s*5[^)]*\)\s*\{[^}]*finalMessage\s*=\s*"([^"]*)"',
        r'else\s*\{[^}]*finalMessage\s*=\s*.*validCount.*"([^"]*)"'
    ]
    
    for pattern in condition_patterns:
        matches = re.findall(pattern, update_content, re.DOTALL)
        message_conditions.extend(matches)
    
    return {
        "status_defaults": status_defaults,
        "counting_logic": counting_logic,
        "message_conditions": message_conditions,
        "update_content_preview": update_content[:500] + "..." if len(update_content) > 500 else update_content
    }

def analyze_initialization_flow():
    """Analyse le flux d'initialisation"""
    viewmodel_path = r"src\FNEV4.Presentation\ViewModels\Configuration\CheminsDossiersConfigViewModel.cs"
    
    if not os.path.exists(viewmodel_path):
        return {"error": "Fichier ViewModel non trouvé"}
    
    with open(viewmodel_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Recherche du constructeur
    constructor_pattern = r'public\s+CheminsDossiersConfigViewModel\([^)]*\)\s*\{([^}]+(?:\{[^}]*\}[^}]*)*)\}'
    constructor_match = re.search(constructor_pattern, content, re.DOTALL)
    
    constructor_calls = []
    if constructor_match:
        constructor_content = constructor_match.group(1)
        # Recherche des appels de méthodes
        method_calls = re.findall(r'(\w+(?:Async)?)\(\)', constructor_content)
        constructor_calls = method_calls
    
    # Recherche des méthodes d'initialisation
    init_methods = {
        "InitializeCollections": re.search(r'private\s+void\s+InitializeCollections\(\)', content) is not None,
        "InitializePathsFromService": re.search(r'private\s+void\s+InitializePathsFromService\(\)', content) is not None,
        "InitializeLoggingSettings": re.search(r'private\s+void\s+InitializeLoggingSettings\(\)', content) is not None,
        "InitializeStatusImmediatelyAsync": re.search(r'private\s+async\s+Task\s+InitializeStatusImmediatelyAsync\(\)', content) is not None
    }
    
    return {
        "constructor_calls": constructor_calls,
        "init_methods": init_methods
    }

def simulate_status_calculation():
    """Simule le calcul des statuts avec différents scénarios"""
    
    # Scenario 1: Statuts par défaut ("Unknown")
    scenario1 = {
        "ImportFolderStatus": "Unknown",
        "ExportFolderStatus": "Unknown", 
        "ArchiveFolderStatus": "Unknown",
        "LogsFolderStatus": "Unknown",
        "BackupFolderStatus": "Unknown"
    }
    
    # Scenario 2: Ce que montre l'interface (3 verts)
    scenario2 = {
        "ImportFolderStatus": "Valid",
        "ExportFolderStatus": "Valid",
        "ArchiveFolderStatus": "Valid", 
        "LogsFolderStatus": "Unknown",
        "BackupFolderStatus": "Unknown"
    }
    
    # Scenario 3: Test supposé réussi
    scenario3 = {
        "ImportFolderStatus": "Valid",
        "ExportFolderStatus": "Valid", 
        "ArchiveFolderStatus": "Valid",
        "LogsFolderStatus": "Valid",
        "BackupFolderStatus": "Warning"
    }
    
    def calculate_message(statuses):
        statuses_list = list(statuses.values())
        valid_count = statuses_list.count("Valid")
        warning_count = statuses_list.count("Warning")
        invalid_count = statuses_list.count("Invalid")
        
        if invalid_count > 0:
            return f"❌ {invalid_count} dossier(s) avec erreurs"
        elif warning_count > 0:
            return f"⚠️ {warning_count} dossier(s) avec avertissements"
        elif valid_count == 5:
            return "✅ Tous les dossiers sont configurés"
        else:
            return f"⚙️ {valid_count}/5 dossiers configurés"
    
    scenarios = {
        "scenario1_defaults": {
            "statuses": scenario1,
            "expected_message": calculate_message(scenario1)
        },
        "scenario2_interface": {
            "statuses": scenario2,
            "expected_message": calculate_message(scenario2)
        },
        "scenario3_test_success": {
            "statuses": scenario3,
            "expected_message": calculate_message(scenario3)
        }
    }
    
    return scenarios

def create_diagnostic_report():
    """Crée un rapport de diagnostic complet"""
    report = {
        "timestamp": datetime.now().isoformat(),
        "diagnostic_type": "Analyse Incohérence Statut vs Interface",
        "status_logic_analysis": analyze_status_logic(),
        "initialization_analysis": analyze_initialization_flow(),
        "scenario_simulations": simulate_status_calculation()
    }
    
    return report

def display_diagnostic_results(report):
    """Affiche les résultats du diagnostic"""
    print("=" * 80)
    print("DIAGNOSTIC - INCOHÉRENCE STATUT vs INTERFACE")
    print("=" * 80)
    print(f"Timestamp: {report['timestamp']}")
    print()
    
    # Analyse de la logique de statut
    print("🔍 ANALYSE DE LA LOGIQUE DE STATUT:")
    print("-" * 50)
    status_analysis = report["status_logic_analysis"]
    
    if "error" in status_analysis:
        print(f"❌ Erreur: {status_analysis['error']}")
    else:
        print("📋 Valeurs par défaut des statuts:")
        for status, default in status_analysis["status_defaults"].items():
            print(f"  • {status}: '{default}'")
        
        print("\n🧮 Logique de comptage:")
        for logic, present in status_analysis["counting_logic"].items():
            icon = "✅" if present else "❌"
            print(f"  {icon} {logic.replace('_', ' ').title()}: {present}")
        
        print("\n💬 Conditions de message trouvées:")
        for i, condition in enumerate(status_analysis["message_conditions"], 1):
            print(f"  {i}. {condition}")
    
    print()
    
    # Analyse d'initialisation
    print("🚀 ANALYSE D'INITIALISATION:")
    print("-" * 50)
    init_analysis = report["initialization_analysis"]
    
    if "error" in init_analysis:
        print(f"❌ Erreur: {init_analysis['error']}")
    else:
        print("🏗️ Appels dans le constructeur:")
        for call in init_analysis["constructor_calls"]:
            print(f"  • {call}()")
        
        print("\n🔧 Méthodes d'initialisation disponibles:")
        for method, exists in init_analysis["init_methods"].items():
            icon = "✅" if exists else "❌"
            print(f"  {icon} {method}: {exists}")
    
    print()
    
    # Simulations de scénarios
    print("🎭 SIMULATIONS DE SCÉNARIOS:")
    print("-" * 50)
    scenarios = report["scenario_simulations"]
    
    for scenario_name, scenario_data in scenarios.items():
        print(f"\n📋 {scenario_name.replace('_', ' ').title()}:")
        statuses = scenario_data["statuses"]
        expected = scenario_data["expected_message"]
        
        valid_count = list(statuses.values()).count("Valid")
        unknown_count = list(statuses.values()).count("Unknown")
        
        print(f"  Statuts: {statuses}")
        print(f"  Valid: {valid_count}, Unknown: {unknown_count}")
        print(f"  Message attendu: '{expected}'")
    
    print()
    
    # Diagnostic final
    print("🎯 DIAGNOSTIC:")
    print("-" * 50)
    
    # Vérification si le problème vient des statuts "Unknown"
    status_defaults = status_analysis.get("status_defaults", {})
    if any(default == "Unknown" for default in status_defaults.values()):
        print("⚠️  PROBLÈME IDENTIFIÉ: Statuts par défaut 'Unknown'")
        print("   Les statuts 'Unknown' ne sont pas comptés comme valides")
        print("   Mais l'interface peut montrer des dossiers comme valides visuellement")
        print()
        print("🔧 SOLUTION RECOMMANDÉE:")
        print("   1. S'assurer que ValidatePathAsync() est appelée pour tous les dossiers")
        print("   2. Vérifier que SetPathStatus() met bien à jour les bonnes propriétés")
        print("   3. Ajouter un log des statuts dans UpdateGlobalStatusAsync()")
    else:
        print("✅ Statuts par défaut semblent corrects")
    
    # Vérification de la logique de comptage
    counting = status_analysis.get("counting_logic", {})
    if not counting.get("handles_unknown", False):
        print("\n⚠️  PROBLÈME POTENTIEL: Gestion des statuts 'Unknown'")
        print("   La logique ne gère peut-être pas explicitement les statuts 'Unknown'")

def main():
    """Fonction principale"""
    print("Diagnostic de l'incohérence statut vs interface...")
    print("Analyse en cours...\n")
    
    # Changer vers le répertoire du projet
    if os.path.exists("FNEV4.sln"):
        os.chdir(".")
    else:
        print("❌ Impossible de trouver le projet FNEV4")
        return
    
    # Créer le rapport de diagnostic
    report = create_diagnostic_report()
    
    # Afficher les résultats
    display_diagnostic_results(report)
    
    # Sauvegarder le rapport
    with open("diagnostic_incoherence_statut.json", 'w', encoding='utf-8') as f:
        json.dump(report, f, indent=2, ensure_ascii=False)
    
    print(f"\n💾 Rapport de diagnostic sauvegardé dans: diagnostic_incoherence_statut.json")

if __name__ == "__main__":
    main()
