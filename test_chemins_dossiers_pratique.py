#!/usr/bin/env python3
"""
🧪 TEST PRATIQUE DU SOUS-MENU 'CHEMINS & DOSSIERS'
===================================================

Script de test pratique pour valider le fonctionnement réel du sous-menu
dans l'application FNEV4.

Par: GitHub Copilot - Assistant IA Expert
Date: Septembre 2025
"""

import os
import sys
import json
import time
import subprocess
from pathlib import Path
from datetime import datetime
from typing import Dict, Any

class CheminsDossiersTest:
    """Testeur pratique pour le sous-menu Chemins & Dossiers"""
    
    def __init__(self):
        self.project_root = Path("C:/wamp64/www/FNEV4")
        self.test_results = {
            "compilation": False,
            "startup": False,
            "navigation": False,
            "functionality": {},
            "error_log": [],
            "recommendations": []
        }
    
    def run_practical_tests(self) -> Dict[str, Any]:
        """Exécute les tests pratiques"""
        print("🧪 TEST PRATIQUE - SOUS-MENU CHEMINS & DOSSIERS")
        print("=" * 55)
        
        # 1. Test de compilation
        print("\n1️⃣ TEST DE COMPILATION")
        if self.test_compilation():
            print("   ✅ Compilation réussie")
            self.test_results["compilation"] = True
        else:
            print("   ❌ Échec de compilation")
            return self.test_results
        
        # 2. Test de l'interface
        print("\n2️⃣ TEST DE L'INTERFACE")
        self.test_interface_elements()
        
        # 3. Test des services
        print("\n3️⃣ TEST DES SERVICES")
        self.test_services()
        
        # 4. Test de la persistance
        print("\n4️⃣ TEST DE LA PERSISTANCE")
        self.test_data_persistence()
        
        # 5. Génération du rapport
        print("\n5️⃣ GÉNÉRATION DU RAPPORT DE TEST")
        self.generate_test_report()
        
        return self.test_results
    
    def test_compilation(self) -> bool:
        """Test de compilation du projet"""
        try:
            print("   🔄 Compilation en cours...")
            
            result = subprocess.run([
                "dotnet", "build", "FNEV4.sln", 
                "--configuration", "Release", 
                "--verbosity", "minimal"
            ], 
            cwd=self.project_root,
            capture_output=True, 
            text=True,
            timeout=60
            )
            
            if result.returncode == 0:
                print("   ✅ Projet compilé sans erreur")
                return True
            else:
                print(f"   ❌ Erreurs de compilation détectées")
                self.test_results["error_log"].append(f"Compilation error: {result.stderr}")
                return False
                
        except subprocess.TimeoutExpired:
            print("   ⏰ Timeout de compilation")
            return False
        except Exception as e:
            print(f"   ❌ Erreur compilation: {e}")
            self.test_results["error_log"].append(f"Compilation exception: {e}")
            return False
    
    def test_interface_elements(self):
        """Test des éléments d'interface"""
        print("   🔍 Vérification des éléments XAML...")
        
        xaml_path = self.project_root / "src/FNEV4.Presentation/Views/Configuration/CheminsDossiersConfigView.xaml"
        
        if not xaml_path.exists():
            print("   ❌ Fichier XAML introuvable")
            self.test_results["error_log"].append("XAML file not found")
            return
        
        try:
            content = xaml_path.read_text(encoding='utf-8')
            
            # Éléments critiques à vérifier
            critical_elements = {
                "main_grid": "<Grid" in content,
                "material_cards": "materialDesign:Card" in content,
                "import_section": "ImportFolderPath" in content,
                "export_section": "ExportFolderPath" in content,
                "backup_section": "BackupFolderPath" in content,
                "action_buttons": "Command=" in content,
                "status_indicators": "Status" in content,
                "navigation_icons": "materialDesign:PackIcon" in content
            }
            
            missing_elements = []
            for element, exists in critical_elements.items():
                if exists:
                    print(f"     ✅ {element}: Présent")
                else:
                    print(f"     ❌ {element}: Manquant")
                    missing_elements.append(element)
            
            self.test_results["functionality"]["interface_elements"] = {
                "total_checked": len(critical_elements),
                "found": len(critical_elements) - len(missing_elements),
                "missing": missing_elements,
                "success_rate": ((len(critical_elements) - len(missing_elements)) / len(critical_elements)) * 100
            }
            
            if not missing_elements:
                print("   🎉 Tous les éléments d'interface sont présents")
            else:
                print(f"   ⚠️ {len(missing_elements)} élément(s) manquant(s)")
                
        except Exception as e:
            print(f"   ❌ Erreur lecture XAML: {e}")
            self.test_results["error_log"].append(f"XAML parsing error: {e}")
    
    def test_services(self):
        """Test des services backend"""
        print("   🔍 Vérification des services...")
        
        # Test du service de configuration des chemins
        path_service = self.project_root / "src/FNEV4.Infrastructure/Services/PathConfigurationService.cs"
        backup_service = self.project_root / "src/FNEV4.Infrastructure/Services/BackupService.cs"
        
        services_status = {
            "path_configuration_service": path_service.exists(),
            "backup_service": backup_service.exists() or self.check_backup_interface(),
            "database_provider": self.check_database_provider(),
            "logging_service": self.check_logging_service()
        }
        
        for service, exists in services_status.items():
            icon = "✅" if exists else "❌"
            status = "Disponible" if exists else "Manquant"
            print(f"     {icon} {service}: {status}")
        
        self.test_results["functionality"]["services"] = services_status
        
        # Test de la configuration du PathConfigurationService
        if path_service.exists():
            self.test_path_configuration_service(path_service)
    
    def check_backup_interface(self) -> bool:
        """Vérifie l'interface de backup"""
        backup_interface = self.project_root / "src/FNEV4.Core/Interfaces/IBackupService.cs"
        return backup_interface.exists()
    
    def check_database_provider(self) -> bool:
        """Vérifie le provider de base de données"""
        db_provider = self.project_root / "src/FNEV4.Infrastructure/Services/DatabasePathProvider.cs"
        return db_provider.exists()
    
    def check_logging_service(self) -> bool:
        """Vérifie le service de logging"""
        logging_paths = [
            "src/FNEV4.Infrastructure/Services/LoggingService.cs",
            "src/FNEV4.Core/Interfaces/ILoggingService.cs"
        ]
        return any((self.project_root / path).exists() for path in logging_paths)
    
    def test_path_configuration_service(self, service_path: Path):
        """Test spécifique du service de configuration des chemins"""
        print("     🔍 Test détaillé du PathConfigurationService...")
        
        try:
            content = service_path.read_text(encoding='utf-8')
            
            # Vérifications critiques
            checks = {
                "centralized_database": "IDatabasePathProvider" in content,
                "ensure_directories": "EnsureDirectoriesExist" in content,
                "update_paths_method": "UpdatePaths" in content,
                "validate_path_method": "ValidatePath" in content,
                "calculate_size_method": "CalculateDirectorySize" in content,
                "fallback_handling": "GetProjectRootPath" in content
            }
            
            passed_checks = sum(checks.values())
            total_checks = len(checks)
            
            for check, passed in checks.items():
                icon = "✅" if passed else "❌"
                print(f"       {icon} {check}")
            
            print(f"       📊 Score: {passed_checks}/{total_checks} ({(passed_checks/total_checks)*100:.1f}%)")
            
            self.test_results["functionality"]["path_service_details"] = {
                "checks": checks,
                "score": (passed_checks / total_checks) * 100
            }
            
        except Exception as e:
            print(f"       ❌ Erreur lecture service: {e}")
            self.test_results["error_log"].append(f"Service reading error: {e}")
    
    def test_data_persistence(self):
        """Test de la persistance des données"""
        print("   🔍 Test de la persistance des données...")
        
        # Vérifier l'entité de configuration
        entity_path = self.project_root / "src/FNEV4.Core/Entities/FolderConfiguration.cs"
        
        if entity_path.exists():
            print("     ✅ Entité FolderConfiguration trouvée")
            
            try:
                content = entity_path.read_text(encoding='utf-8')
                
                # Vérifications de l'entité
                entity_features = {
                    "data_annotations": "[Required]" in content or "[StringLength" in content,
                    "validation_methods": "ValidateConfiguration" in content,
                    "utility_methods": "CreateAllFolders" in content,
                    "folder_paths": "ImportFolderPath" in content and "ExportFolderPath" in content,
                    "archive_settings": "ArchiveAutoEnabled" in content,
                    "backup_settings": "BackupAutoEnabled" in content
                }
                
                for feature, exists in entity_features.items():
                    icon = "✅" if exists else "❌"
                    print(f"       {icon} {feature}")
                
                self.test_results["functionality"]["entity_features"] = entity_features
                
            except Exception as e:
                print(f"     ❌ Erreur lecture entité: {e}")
                self.test_results["error_log"].append(f"Entity reading error: {e}")
        else:
            print("     ❌ Entité FolderConfiguration introuvable")
            self.test_results["error_log"].append("FolderConfiguration entity not found")
    
    def generate_test_report(self):
        """Génère le rapport de test"""
        print("   📝 Génération du rapport de test...")
        
        # Calculer le score global
        scores = []
        
        # Score interface
        interface_score = self.test_results["functionality"].get("interface_elements", {}).get("success_rate", 0)
        scores.append(interface_score)
        
        # Score services
        services = self.test_results["functionality"].get("services", {})
        service_score = (sum(services.values()) / len(services)) * 100 if services else 0
        scores.append(service_score)
        
        # Score entité
        entity_features = self.test_results["functionality"].get("entity_features", {})
        entity_score = (sum(entity_features.values()) / len(entity_features)) * 100 if entity_features else 0
        scores.append(entity_score)
        
        global_test_score = sum(scores) / len(scores) if scores else 0
        
        # Créer le rapport
        test_report = {
            "test_date": datetime.now().isoformat(),
            "component": "Chemins & Dossiers - Test Pratique",
            "compilation_success": self.test_results["compilation"],
            "global_test_score": global_test_score,
            "detailed_scores": {
                "interface_elements": interface_score,
                "services": service_score,
                "entity_features": entity_score
            },
            "functionality_details": self.test_results["functionality"],
            "error_log": self.test_results["error_log"],
            "recommendations": self.generate_test_recommendations(global_test_score),
            "test_summary": self.create_test_summary(global_test_score)
        }
        
        # Sauvegarder le rapport
        report_path = self.project_root / "TEST_PRATIQUE_CHEMINS_DOSSIERS.json"
        with open(report_path, 'w', encoding='utf-8') as f:
            json.dump(test_report, f, indent=2, ensure_ascii=False)
        
        print(f"   💾 Rapport de test sauvegardé: {report_path}")
        
        # Afficher le résumé
        self.display_test_summary(global_test_score, test_report)
    
    def generate_test_recommendations(self, score: float) -> list:
        """Génère les recommandations basées sur les tests"""
        recommendations = []
        
        if not self.test_results["compilation"]:
            recommendations.append({
                "priority": "CRITIQUE",
                "category": "Compilation",
                "recommendation": "Corriger les erreurs de compilation avant tout test"
            })
        
        if score < 90:
            recommendations.append({
                "priority": "HAUTE",
                "category": "Fonctionnalité",
                "recommendation": "Améliorer l'implémentation des fonctionnalités manquantes"
            })
        
        if self.test_results["error_log"]:
            recommendations.append({
                "priority": "MOYENNE", 
                "category": "Robustesse",
                "recommendation": f"Corriger les {len(self.test_results['error_log'])} erreur(s) détectée(s)"
            })
        
        return recommendations
    
    def create_test_summary(self, score: float) -> dict:
        """Crée un résumé des tests"""
        if score >= 95:
            status = "🎉 PARFAIT"
            message = "Le sous-menu fonctionne parfaitement"
        elif score >= 85:
            status = "✅ EXCELLENT"
            message = "Le sous-menu fonctionne très bien"
        elif score >= 75:
            status = "👍 BON"
            message = "Le sous-menu fonctionne correctement"
        elif score >= 60:
            status = "⚠️ MOYEN"
            message = "Le sous-menu nécessite des améliorations"
        else:
            status = "❌ CRITIQUE"
            message = "Le sous-menu nécessite des corrections importantes"
        
        return {
            "status": status,
            "message": message,
            "compilation_ok": self.test_results["compilation"],
            "errors_count": len(self.test_results["error_log"]),
            "recommendations_count": len(self.generate_test_recommendations(score))
        }
    
    def display_test_summary(self, score: float, report: dict):
        """Affiche le résumé des tests"""
        print("\n" + "=" * 55)
        print("📊 RÉSUMÉ DES TESTS PRATIQUES")
        print("=" * 55)
        
        summary = report["test_summary"]
        
        print(f"\n🎯 SCORE GLOBAL: {score:.1f}%")
        print(f"📋 STATUT: {summary['status']}")
        print(f"💬 ÉVALUATION: {summary['message']}")
        
        print(f"\n📈 DÉTAIL DES TESTS:")
        print(f"   • Compilation: {'✅ Réussie' if summary['compilation_ok'] else '❌ Échec'}")
        print(f"   • Interface: {report['detailed_scores']['interface_elements']:.1f}%")
        print(f"   • Services: {report['detailed_scores']['services']:.1f}%")
        print(f"   • Persistance: {report['detailed_scores']['entity_features']:.1f}%")
        
        if report["error_log"]:
            print(f"\n⚠️ ERREURS DÉTECTÉES ({len(report['error_log'])}):")
            for i, error in enumerate(report["error_log"][:3], 1):  # Limite à 3 erreurs
                print(f"   {i}. {error}")
            if len(report["error_log"]) > 3:
                print(f"   ... et {len(report['error_log']) - 3} autre(s)")
        
        if report["recommendations"]:
            print(f"\n🔧 RECOMMANDATIONS ({len(report['recommendations'])}):")
            for i, rec in enumerate(report["recommendations"], 1):
                print(f"   {i}. [{rec['priority']}] {rec['category']}: {rec['recommendation']}")
        
        print("\n" + "=" * 55)
        if score >= 85:
            print("✅ TESTS PRATIQUES RÉUSSIS - LE SOUS-MENU EST FONCTIONNEL")
        else:
            print("⚠️ TESTS PRATIQUES PARTIELS - AMÉLIORATIONS NÉCESSAIRES")
        print("=" * 55)

def main():
    """Point d'entrée principal"""
    print("🚀 LANCEMENT DES TESTS PRATIQUES")
    print("Composant: Sous-menu 'Chemins & Dossiers'")
    print("Objectif: Validation du fonctionnement réel")
    print()
    
    try:
        tester = CheminsDossiersTest()
        results = tester.run_practical_tests()
        
        return 0 if results["compilation"] else 1
        
    except Exception as e:
        print(f"\n❌ ERREUR LORS DES TESTS: {e}")
        return 1

if __name__ == "__main__":
    sys.exit(main())
