#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Diagnostic de la configuration des chemins - Comparaison des sources
Analyse pourquoi l'interface affiche "4/5" alors que les dossiers existent
"""

import os
import json
from datetime import datetime

def analyze_path_configuration():
    """Analyse les différentes sources de configuration de chemins"""
    
    results = {
        "timestamp": datetime.now().isoformat(),
        "database_provider_analysis": {},
        "real_folders_status": {},
        "expected_paths_from_code": {},
        "discrepancy_analysis": {}
    }
    
    print("🔍 Diagnostic configuration des chemins - Sources multiples")
    print("=" * 60)
    
    # 1. Analyser où DatabasePathProvider pointe
    print("\n📍 1. CONFIGURATION DatabasePathProvider:")
    print("-" * 40)
    
    # Simuler la logique GetFixedDatabasePath()
    env_path = os.environ.get("FNEV4_DATABASE_PATH")
    print(f"  🌍 Variable d'environnement FNEV4_DATABASE_PATH: {env_path or 'Non définie'}")
    
    dev_path = r"C:\wamp64\www\FNEV4\data\FNEV4.db"
    dev_dir_exists = os.path.exists(os.path.dirname(dev_path))
    print(f"  🛠️  Chemin dev fixe: {dev_path}")
    print(f"  📁 Répertoire dev existe: {dev_dir_exists}")
    
    # Le DatabasePathProvider utiliserait dev_path
    database_path = dev_path if dev_dir_exists else "FALLBACK"
    data_root = os.path.dirname(database_path) if database_path != "FALLBACK" else None
    
    print(f"  ✅ DatabasePath choisi: {database_path}")
    print(f"  📂 DataRootPath déduit: {data_root}")
    
    results["database_provider_analysis"] = {
        "env_path": env_path,
        "dev_path": dev_path,
        "dev_dir_exists": dev_dir_exists,
        "final_database_path": database_path,
        "final_data_root": data_root
    }
    
    # 2. Vérifier les vrais dossiers
    print(f"\n📂 2. DOSSIERS RÉELS dans {data_root}:")
    print("-" * 40)
    
    expected_folders = ["Import", "Export", "Archive", "Logs", "Backup"]
    real_status = {}
    
    for folder in expected_folders:
        if data_root:
            folder_path = os.path.join(data_root, folder)
            exists = os.path.exists(folder_path)
            is_dir = os.path.isdir(folder_path) if exists else False
            
            if exists and is_dir:
                try:
                    files = os.listdir(folder_path)
                    file_count = len([f for f in files if os.path.isfile(os.path.join(folder_path, f))])
                    status = "Valid"
                    reason = f"Opérationnel ({file_count} fichiers)"
                except:
                    status = "Warning"
                    reason = "Problème d'accès"
            elif exists:
                status = "Invalid"
                reason = "Chemin pointe vers un fichier"
            else:
                status = "Warning" 
                reason = "Dossier n'existe pas"
            
            print(f"  📁 {folder}: {status} - {reason}")
            print(f"     📍 {folder_path}")
            
            real_status[folder] = {
                "path": folder_path,
                "status": status,
                "reason": reason,
                "exists": exists
            }
        else:
            print(f"  ❓ {folder}: UNKNOWN - DataRoot introuvable")
    
    results["real_folders_status"] = real_status
    
    # 3. Compter selon la logique ViewModel
    print(f"\n🔢 3. COMPTAGE SELON LOGIQUE ViewModel:")
    print("-" * 40)
    
    valid_count = sum(1 for folder in real_status.values() if folder["status"] == "Valid")
    warning_count = sum(1 for folder in real_status.values() if folder["status"] == "Warning")
    invalid_count = sum(1 for folder in real_status.values() if folder["status"] == "Invalid")
    
    print(f"  ✅ Valid (comptés): {valid_count}")
    print(f"  🟠 Warning (non comptés): {warning_count}")
    print(f"  🔴 Invalid (non comptés): {invalid_count}")
    print(f"  📊 Compteur devrait afficher: {valid_count}/5")
    
    # 4. Analyser l'incohérence "4/5"
    print(f"\n❗ 4. ANALYSE INCOHÉRENCE '4/5 configurés':")
    print("-" * 40)
    
    if valid_count == 4:
        print(f"  ✅ COHÉRENT: Le vrai comptage donne bien 4/5")
        print(f"  💡 L'interface affiche correctement les données")
        discrepancy = "None - Interface shows correct count"
    elif valid_count == 5:
        print(f"  ❗ INCOHÉRENT: Tous les dossiers existent (5/5) mais interface dit 4/5")
        print(f"  🔍 Il y a un délai de mise à jour ou cache obsolète")
        discrepancy = "Interface shows outdated count - all folders exist"
    else:
        print(f"  ❓ MYSTÈRE: Comptage réel {valid_count}/5 ne correspond ni à 4/5 ni à 5/5")
        print(f"  🔧 Il y a un problème de configuration ou source de données")
        discrepancy = f"Unexpected count: real={valid_count}, interface=4"
    
    results["discrepancy_analysis"] = {
        "real_valid_count": valid_count,
        "interface_shows": "4/5",
        "discrepancy": discrepancy,
        "explanation": "Check if interface uses different data source or has caching issue"
    }
    
    # 5. Vérifier s'il y a d'autres sources de chemins
    print(f"\n🔍 5. AUTRES SOURCES POTENTIELLES:")
    print("-" * 40)
    
    # Vérifier appsettings.json
    app_settings_path = r"C:\wamp64\www\FNEV4\src\FNEV4.Presentation\appsettings.json"
    if os.path.exists(app_settings_path):
        print(f"  📄 appsettings.json trouvé: {app_settings_path}")
        try:
            with open(app_settings_path, 'r') as f:
                settings = json.load(f)
                path_settings = settings.get("PathSettings", {})
                print(f"     📂 UsePortableMode: {path_settings.get('UsePortableMode')}")
                print(f"     📁 ImportFolder: {path_settings.get('ImportFolder')}")
                results["appsettings_analysis"] = path_settings
        except Exception as e:
            print(f"     ❌ Erreur lecture appsettings: {e}")
    
    # Sauvegarder
    with open('diagnostic_configuration_chemins.json', 'w', encoding='utf-8') as f:
        json.dump(results, f, indent=2, ensure_ascii=False)
    
    print(f"\n💾 Diagnostic sauvegardé: diagnostic_configuration_chemins.json")
    
    return results

if __name__ == "__main__":
    analyze_path_configuration()
