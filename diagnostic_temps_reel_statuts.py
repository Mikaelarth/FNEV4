#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Script de diagnostic en temps réel des statuts de dossiers
Capture l'état exact au moment de l'incohérence "4/5 dossiers configurés"
"""

import os
import json
from datetime import datetime

def check_real_folder_status():
    """Vérifie l'état réel des dossiers selon la configuration"""
    
    # Chemins depuis appsettings.json
    base_path = r"C:\wamp64\www\FNEV4\src\FNEV4.Presentation"
    
    folders_to_check = {
        "ImportFolder": "Data\\Import",
        "ExportFolder": "Data\\Export", 
        "ArchiveFolder": "Data\\Archive",
        "LogsFolder": "Data\\Logs",
        "BackupFolder": "Data\\Backup"
    }
    
    results = {
        "timestamp": datetime.now().isoformat(),
        "base_path": base_path,
        "folder_analysis": {},
        "expected_vs_actual": {},
        "potential_cause": None
    }
    
    print("🔍 Diagnostic en temps réel des statuts de dossiers")
    print("=" * 55)
    
    valid_count = 0
    warning_count = 0
    invalid_count = 0
    
    for folder_key, relative_path in folders_to_check.items():
        full_path = os.path.join(base_path, relative_path)
        full_path = os.path.normpath(full_path)
        
        print(f"\n📁 {folder_key}:")
        print(f"  📍 Chemin: {full_path}")
        
        # Déterminer le statut selon la logique de ValidatePathAsync
        if not relative_path or relative_path.strip() == "":
            status = "Invalid"
            reason = "Chemin vide"
            exists = False
        elif not os.path.exists(full_path):
            status = "Warning"
            reason = "Dossier n'existe pas"
            exists = False
        elif not os.path.isdir(full_path):
            status = "Invalid"
            reason = "Chemin pointe vers un fichier"
            exists = True
        else:
            # Vérifier les permissions (approximatif)
            try:
                test_file = os.path.join(full_path, "test_permissions.tmp")
                with open(test_file, 'w') as f:
                    f.write("test")
                os.remove(test_file)
                status = "Valid"
                reason = "Dossier opérationnel"
                exists = True
            except Exception as e:
                status = "Warning"
                reason = f"Problème permissions: {str(e)[:30]}"
                exists = True
        
        # Compter selon la logique du ViewModel
        if status == "Valid":
            valid_count += 1
            visual_color = "🟢 VERT"
        elif status == "Warning":
            warning_count += 1
            visual_color = "🟠 ORANGE"
        elif status == "Invalid":
            invalid_count += 1
            visual_color = "🔴 ROUGE"
        else:
            visual_color = "⚪ GRIS"
        
        print(f"  🔍 Statut logique: {status}")
        print(f"  🎨 Apparence visuelle: {visual_color}")
        print(f"  📂 Existe: {exists}")
        print(f"  💬 Raison: {reason}")
        
        # Informations fichiers si le dossier existe
        if exists and os.path.isdir(full_path):
            try:
                files = os.listdir(full_path)
                file_count = len([f for f in files if os.path.isfile(os.path.join(full_path, f))])
                print(f"  📄 Fichiers: {file_count}")
            except:
                print(f"  📄 Fichiers: Non accessible")
        
        results["folder_analysis"][folder_key] = {
            "path": full_path,
            "status": status,
            "reason": reason,
            "exists": exists,
            "visual_color": visual_color
        }
    
    print(f"\n📊 RÉSUMÉ DES COMPTEURS:")
    print(f"  ✅ Valid (comptés comme configurés): {valid_count}")
    print(f"  🟠 Warning (non comptés): {warning_count}")
    print(f"  🔴 Invalid (non comptés): {invalid_count}")
    print(f"  📊 Total configurés (Valid): {valid_count}/5")
    print(f"  👁️  Visuellement configurés (Valid+Warning): {valid_count + warning_count}/5")
    
    results["expected_vs_actual"] = {
        "valid_count": valid_count,
        "warning_count": warning_count,
        "invalid_count": invalid_count,
        "counter_shows": f"{valid_count}/5",
        "visually_appears": f"{valid_count + warning_count}/5"
    }
    
    # Diagnostic de l'incohérence
    if valid_count == 4:
        print(f"\n🎯 DIAGNOSTIC DE L'INCOHÉRENCE:")
        print(f"  📟 Le compteur affiche correctement: {valid_count}/5 dossiers configurés")
        if warning_count > 0:
            print(f"  ⚠️  {warning_count} dossier(s) en Warning s'affichent comme configurés visuellement")
            print(f"  💡 Solution: Les Warning ne devraient pas compter comme 'configurés'")
            results["potential_cause"] = "Warning folders appear configured visually but aren't counted"
        else:
            print(f"  🤔 Tous les statuts semblent cohérents, problème ailleurs")
            results["potential_cause"] = "Status logic appears correct, issue may be in UI binding"
    else:
        print(f"\n❓ COMPTEUR INATTENDU:")
        print(f"  📟 Le compteur devrait afficher {valid_count}/5, pas 4/5")
        print(f"  🔍 Il y a peut-être un décalage temporel ou une initialisation incorrecte")
        results["potential_cause"] = "Counter mismatch suggests timing or initialization issue"
    
    # Sauvegarder
    with open('diagnostic_temps_reel_statuts.json', 'w', encoding='utf-8') as f:
        json.dump(results, f, indent=2, ensure_ascii=False)
    
    print(f"\n💾 Diagnostic sauvegardé: diagnostic_temps_reel_statuts.json")
    return results

if __name__ == "__main__":
    check_real_folder_status()
