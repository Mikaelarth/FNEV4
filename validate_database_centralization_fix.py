#!/usr/bin/env python3
"""
Script de validation pour vérifier que le problème de centralisation
de la base de données a été corrigé dans EntrepriseConfigViewModel
"""

import os
import json
from datetime import datetime

def validate_centralization_fix():
    """
    Valide que EntrepriseConfigViewModel utilise maintenant le système centralisé
    """
    print("🔍 Validation de la correction de centralisation de la base de données")
    print("=" * 80)
    
    results = {
        "validation_date": datetime.now().isoformat(),
        "status": "UNKNOWN",
        "corrections_applied": [],
        "remaining_issues": [],
        "recommendations": []
    }
    
    # Chemin vers le ViewModel
    enterprise_vm_path = "src/FNEV4.Presentation/ViewModels/Configuration/EntrepriseConfigViewModel.cs"
    app_path = "src/FNEV4.Presentation/App.xaml.cs"
    
    # Vérifications du ViewModel
    print("\n📁 Vérification d'EntrepriseConfigViewModel.cs...")
    if os.path.exists(enterprise_vm_path):
        with open(enterprise_vm_path, 'r', encoding='utf-8') as f:
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
        
        # 3. Vérifier l'utilisation dans LoadExistingConfigurationAsync
        if "await _context.Database.EnsureCreatedAsync();" in content:
            print("✅ LoadExistingConfigurationAsync utilise _context injecté")
            results["corrections_applied"].append("LoadExistingConfigurationAsync corrigé")
        else:
            print("❌ LoadExistingConfigurationAsync n'utilise pas _context")
            results["remaining_issues"].append("LoadExistingConfigurationAsync non corrigé")
        
        # 4. Vérifier l'utilisation dans SaveConfigurationAsync
        save_method_uses_injected = (
            "await _context.Database.EnsureCreatedAsync();" in content and
            "_context.Companies.Update" in content and
            "_context.Companies.Add" in content and
            "await _context.SaveChangesAsync();" in content
        )
        
        if save_method_uses_injected:
            print("✅ SaveConfigurationAsync utilise _context injecté")
            results["corrections_applied"].append("SaveConfigurationAsync corrigé")
        else:
            print("❌ SaveConfigurationAsync n'utilise pas _context correctement")
            results["remaining_issues"].append("SaveConfigurationAsync non corrigé")
        
        # 5. Vérifier l'absence d'ancien pattern
        if "new DbContextOptionsBuilder<FNEV4DbContext>" not in content:
            print("✅ Ancien pattern DbContextOptionsBuilder supprimé")
            results["corrections_applied"].append("Ancien pattern supprimé")
        else:
            print("⚠️ Ancien pattern DbContextOptionsBuilder encore présent")
            results["remaining_issues"].append("Ancien pattern encore présent")
    
    else:
        print("❌ Fichier EntrepriseConfigViewModel.cs introuvable")
        results["remaining_issues"].append("Fichier ViewModel introuvable")
    
    # Vérifications d'App.xaml.cs
    print("\n📁 Vérification d'App.xaml.cs...")
    if os.path.exists(app_path):
        with open(app_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Vérifier l'injection dans le DI
        if "provider.GetRequiredService<FNEV4DbContext>()" in content:
            print("✅ FNEV4DbContext injecté dans la configuration DI")
            results["corrections_applied"].append("DI configuration mise à jour")
        else:
            print("❌ FNEV4DbContext non injecté dans la configuration DI")
            results["remaining_issues"].append("DI configuration non mise à jour")
    else:
        print("❌ Fichier App.xaml.cs introuvable")
        results["remaining_issues"].append("Fichier App.xaml.cs introuvable")
    
    # Déterminer le statut global
    if len(results["remaining_issues"]) == 0:
        results["status"] = "SUCCESS"
        print("\n🎉 SUCCÈS: Toutes les corrections ont été appliquées!")
        results["recommendations"].append("Tester l'application pour vérifier le bon fonctionnement")
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
            "Tester le sous-menu Entreprise"
        ])
    
    # Sauvegarder les résultats
    with open("validation_centralization_fix.json", 'w', encoding='utf-8') as f:
        json.dump(results, f, indent=2, ensure_ascii=False)
    
    print(f"\n📄 Résultats sauvegardés dans: validation_centralization_fix.json")
    
    return results

if __name__ == "__main__":
    validate_centralization_fix()
