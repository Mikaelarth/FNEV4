#!/usr/bin/env python3
"""
Script de validation pour vérifier que le problème de centralisation
de la base de données a été corrigé dans ApiFneConfigViewModel
"""

import os
import json
from datetime import datetime

def validate_api_fne_centralization_fix():
    """
    Valide que ApiFneConfigViewModel utilise maintenant le système centralisé
    """
    print("🔍 Validation de la correction de centralisation pour ApiFneConfigViewModel")
    print("=" * 80)
    
    results = {
        "validation_date": datetime.now().isoformat(),
        "status": "UNKNOWN",
        "corrections_applied": [],
        "remaining_issues": [],
        "recommendations": []
    }
    
    # Chemin vers le ViewModel
    api_fne_vm_path = "src/FNEV4.Presentation/ViewModels/Configuration/ApiFneConfigViewModel.cs"
    app_path = "src/FNEV4.Presentation/App.xaml.cs"
    
    # Vérifications du ViewModel
    print("\n📁 Vérification d'ApiFneConfigViewModel.cs...")
    if os.path.exists(api_fne_vm_path):
        with open(api_fne_vm_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # 1. Vérifier l'injection du contexte
        if "private readonly FNEV4DbContext _context;" in content:
            print("✅ Champ _context correctement déclaré")
            results["corrections_applied"].append("Champ _context déclaré")
        else:
            print("❌ Champ _context manquant")
            results["remaining_issues"].append("Champ _context non déclaré")
        
        # 2. Vérifier le constructeur
        if "FNEV4DbContext? context = null" in content:
            print("✅ Constructeur accepte le paramètre FNEV4DbContext")
            results["corrections_applied"].append("Constructeur modifié pour injection")
        else:
            print("❌ Constructeur ne supporte pas l'injection du contexte")
            results["remaining_issues"].append("Constructeur non modifié")
        
        # 3. Vérifier SaveConfigurationAsync
        if ("await _context.FneConfigurations" in content and 
            "_context.FneConfigurations.Update" in content and
            "_context.FneConfigurations.Add" in content and
            "await _context.SaveChangesAsync();" in content):
            print("✅ SaveConfigurationAsync utilise _context injecté")
            results["corrections_applied"].append("SaveConfigurationAsync corrigé")
        else:
            print("❌ SaveConfigurationAsync n'utilise pas _context correctement")
            results["remaining_issues"].append("SaveConfigurationAsync non corrigé")
        
        # 4. Vérifier LoadConfigurationAsync
        load_method_uses_injected = (
            "await _context.FneConfigurations" in content and
            ".FirstOrDefaultAsync(c => c.ConfigurationName == cleanConfigName)" in content
        )
        
        if load_method_uses_injected:
            print("✅ LoadConfigurationAsync utilise _context injecté")
            results["corrections_applied"].append("LoadConfigurationAsync corrigé")
        else:
            print("❌ LoadConfigurationAsync n'utilise pas _context")
            results["remaining_issues"].append("LoadConfigurationAsync non corrigé")
        
        # 5. Vérifier LoadAvailableConfigurationsAsync
        if "await _context.FneConfigurations" in content and ".OrderByDescending(c => c.LastModifiedDate)" in content:
            print("✅ LoadAvailableConfigurationsAsync utilise _context injecté")
            results["corrections_applied"].append("LoadAvailableConfigurationsAsync corrigé")
        else:
            print("❌ LoadAvailableConfigurationsAsync n'utilise pas _context")
            results["remaining_issues"].append("LoadAvailableConfigurationsAsync non corrigé")
        
        # 6. Vérifier DeleteConfigurationAsync
        if ("_context.FneConfigurations.Remove" in content and 
            "await _context.SaveChangesAsync();" in content):
            print("✅ DeleteConfigurationAsync utilise _context injecté")
            results["corrections_applied"].append("DeleteConfigurationAsync corrigé")
        else:
            print("❌ DeleteConfigurationAsync n'utilise pas _context")
            results["remaining_issues"].append("DeleteConfigurationAsync non corrigé")
        
        # 7. Vérifier l'absence d'ancien pattern
        if "new DbContextOptionsBuilder<FNEV4DbContext>" not in content:
            print("✅ Ancien pattern DbContextOptionsBuilder supprimé")
            results["corrections_applied"].append("Ancien pattern supprimé")
        else:
            print("⚠️ Ancien pattern DbContextOptionsBuilder encore présent")
            results["remaining_issues"].append("Ancien pattern encore présent")
    
    else:
        print("❌ Fichier ApiFneConfigViewModel.cs introuvable")
        results["remaining_issues"].append("Fichier ViewModel introuvable")
    
    # Vérifications d'App.xaml.cs
    print("\n📁 Vérification d'App.xaml.cs...")
    if os.path.exists(app_path):
        with open(app_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Vérifier l'injection dans le DI
        if ("provider.GetRequiredService<IDatabaseService>()," in content and
            "provider.GetRequiredService<FNEV4DbContext>()" in content and
            "new ApiFneConfigViewModel(" in content):
            print("✅ FNEV4DbContext injecté dans la configuration DI pour ApiFneConfigViewModel")
            results["corrections_applied"].append("DI configuration mise à jour pour ApiFneConfigViewModel")
        else:
            print("❌ FNEV4DbContext non injecté dans la configuration DI pour ApiFneConfigViewModel")
            results["remaining_issues"].append("DI configuration non mise à jour pour ApiFneConfigViewModel")
    else:
        print("❌ Fichier App.xaml.cs introuvable")
        results["remaining_issues"].append("Fichier App.xaml.cs introuvable")
    
    # Déterminer le statut global
    if len(results["remaining_issues"]) == 0:
        results["status"] = "SUCCESS"
        print("\n🎉 SUCCÈS: Toutes les corrections ApiFneConfigViewModel ont été appliquées!")
        results["recommendations"].append("Tester l'application pour vérifier le bon fonctionnement du sous-menu API FNE")
    elif len(results["corrections_applied"]) > 0:
        results["status"] = "PARTIAL"
        print(f"\n⚠️ PARTIEL: {len(results['corrections_applied'])} corrections appliquées, {len(results['remaining_issues'])} problèmes restants")
    else:
        results["status"] = "FAILED"
        print(f"\n❌ ÉCHEC: {len(results['remaining_issues'])} problèmes détectés")
    
    # Recommandations
    if results["status"] != "SUCCESS":
        results["recommendations"].extend([
            "Terminer les corrections restantes",
            "Vérifier la compilation du projet",
            "Tester le sous-menu API FNE"
        ])
    
    # Sauvegarder les résultats
    with open("validation_api_fne_centralization_fix.json", 'w', encoding='utf-8') as f:
        json.dump(results, f, indent=2, ensure_ascii=False)
    
    print(f"\n📄 Résultats sauvegardés dans: validation_api_fne_centralization_fix.json")
    
    return results

if __name__ == "__main__":
    validate_api_fne_centralization_fix()
