#!/usr/bin/env python3
"""
Script d'analyse des différences entre les accès base de données
entre le menu Maintenance et le sous-menu Configuration → Entreprise
"""

import os
import re
from pathlib import Path

def analyze_database_access_patterns():
    """Analyse les patterns d'accès à la base de données"""
    results = {
        "maintenance_pattern": {
            "uses_injected_context": False,
            "uses_database_service": False,
            "uses_centralized_path": False,
            "creates_own_context": False,
            "details": []
        },
        "enterprise_pattern": {
            "uses_injected_context": False,
            "uses_database_service": False,
            "uses_centralized_path": False,
            "creates_own_context": False,
            "details": []
        },
        "inconsistencies": []
    }
    
    # 1. Analyser BaseDonneesViewModel (Maintenance)
    maintenance_file = Path("src/FNEV4.Presentation/ViewModels/Maintenance/BaseDonneesViewModel.cs")
    if maintenance_file.exists():
        content = maintenance_file.read_text(encoding='utf-8')
        
        # Vérifier l'injection du contexte
        if "FNEV4DbContext" in content and "constructor" in content.lower():
            if "FNEV4DbContext context" in content:
                results["maintenance_pattern"]["uses_injected_context"] = True
                results["maintenance_pattern"]["details"].append("✅ Utilise le contexte EF injecté")
        
        # Vérifier l'utilisation du DatabaseService
        if "_databaseService" in content:
            results["maintenance_pattern"]["uses_database_service"] = True
            results["maintenance_pattern"]["details"].append("✅ Utilise IDatabaseService")
        
        # Vérifier l'utilisation du chemin centralisé
        if "_pathConfigurationService.DatabasePath" in content:
            results["maintenance_pattern"]["uses_centralized_path"] = True
            results["maintenance_pattern"]["details"].append("✅ Utilise le chemin centralisé")
        
        # Vérifier la création manuelle de contexte
        if "new DbContextOptionsBuilder" in content:
            results["maintenance_pattern"]["creates_own_context"] = True
            results["maintenance_pattern"]["details"].append("⚠️ Crée son propre contexte")
    
    # 2. Analyser EntrepriseConfigViewModel (Configuration)
    enterprise_file = Path("src/FNEV4.Presentation/ViewModels/Configuration/EntrepriseConfigViewModel.cs")
    if enterprise_file.exists():
        content = enterprise_file.read_text(encoding='utf-8')
        
        # Vérifier l'injection du contexte
        if "FNEV4DbContext" in content and "constructor" in content.lower():
            if "FNEV4DbContext context" in content:
                results["enterprise_pattern"]["uses_injected_context"] = True
                results["enterprise_pattern"]["details"].append("✅ Utilise le contexte EF injecté")
        
        # Vérifier l'utilisation du DatabaseService
        if "_databaseService" in content:
            results["enterprise_pattern"]["uses_database_service"] = True
            results["enterprise_pattern"]["details"].append("✅ Utilise IDatabaseService")
        
        # Vérifier l'utilisation du chemin centralisé
        if "_databaseService.GetConnectionString()" in content:
            results["enterprise_pattern"]["uses_centralized_path"] = True
            results["enterprise_pattern"]["details"].append("✅ Utilise la chaîne de connexion centralisée")
        
        # Vérifier la création manuelle de contexte
        if "new DbContextOptionsBuilder" in content:
            results["enterprise_pattern"]["creates_own_context"] = True
            results["enterprise_pattern"]["details"].append("❌ Crée son propre contexte au lieu d'utiliser l'injection")
    
    # 3. Identifier les incohérences
    if results["maintenance_pattern"]["uses_injected_context"] != results["enterprise_pattern"]["uses_injected_context"]:
        results["inconsistencies"].append("🔴 INCOHÉRENCE: Utilisation différente du contexte EF injecté")
    
    if results["enterprise_pattern"]["creates_own_context"]:
        results["inconsistencies"].append("🔴 PROBLÈME: EntrepriseConfig crée son propre contexte au lieu d'utiliser l'injection")
    
    if results["maintenance_pattern"]["uses_centralized_path"] != results["enterprise_pattern"]["uses_centralized_path"]:
        results["inconsistencies"].append("🟡 DIFFÉRENCE: Méthodes différentes pour accéder au chemin DB")
    
    return results

def generate_diagnosis(results):
    """Génère un diagnostic des incohérences"""
    print("=" * 90)
    print("🔍 DIAGNOSTIC: INCOHÉRENCES D'ACCÈS BASE DE DONNÉES")
    print("=" * 90)
    
    # Analyse du pattern Maintenance
    print("\n🔧 PATTERN MENU MAINTENANCE (BaseDonneesViewModel)")
    print("-" * 60)
    for detail in results["maintenance_pattern"]["details"]:
        print(f"   {detail}")
    
    # Analyse du pattern Entreprise
    print("\n🏢 PATTERN SOUS-MENU ENTREPRISE (EntrepriseConfigViewModel)")
    print("-" * 60)
    for detail in results["enterprise_pattern"]["details"]:
        print(f"   {detail}")
    
    # Incohérences détectées
    print("\n⚠️ INCOHÉRENCES DÉTECTÉES")
    print("-" * 40)
    if results["inconsistencies"]:
        for inconsistency in results["inconsistencies"]:
            print(f"   {inconsistency}")
    else:
        print("   ✅ Aucune incohérence détectée")
    
    # Diagnostic principal
    print("\n🎯 DIAGNOSTIC PRINCIPAL")
    print("-" * 30)
    
    enterprise_creates_own = results["enterprise_pattern"]["creates_own_context"]
    enterprise_uses_service = results["enterprise_pattern"]["uses_database_service"]
    
    if enterprise_creates_own and enterprise_uses_service:
        print("🔴 PROBLÈME IDENTIFIÉ:")
        print("   Le sous-menu Entreprise utilise DEUX mécanismes différents:")
        print("   1. ✅ Il obtient la chaîne de connexion via IDatabaseService (BIEN)")
        print("   2. ❌ Mais il crée son propre DbContext au lieu d'utiliser l'injection (PROBLÈME)")
        print()
        print("🔍 EXPLICATION:")
        print("   Cela peut créer des instances de base de données différentes car:")
        print("   • Le contexte injecté est configuré avec Scoped lifetime")
        print("   • Le contexte créé manuellement est une nouvelle instance")
        print("   • Les deux peuvent pointer vers des fichiers DB différents temporairement")
        print()
        print("💡 SOLUTION RECOMMANDÉE:")
        print("   Modifier EntrepriseConfigViewModel pour utiliser le contexte EF injecté")
        print("   au lieu de créer son propre DbContextOptionsBuilder")
        
        return False
    else:
        print("✅ Accès cohérent à la base de données")
        return True

def main():
    """Point d'entrée principal"""
    try:
        # Changer vers le répertoire du projet
        os.chdir(Path(__file__).parent)
        
        # Analyser les patterns d'accès
        results = analyze_database_access_patterns()
        
        # Générer le diagnostic
        is_consistent = generate_diagnosis(results)
        
        return 0 if is_consistent else 1
        
    except Exception as e:
        print(f"❌ Erreur lors du diagnostic: {e}")
        return 1

if __name__ == "__main__":
    exit(main())
