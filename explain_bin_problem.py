#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Démonstration pourquoi la base allait dans bin/
"""

import os

def demonstrate_working_directory_problem():
    """Montre comment le répertoire de travail affecte les chemins relatifs"""
    
    print("🔍 DÉMONSTRATION DU PROBLÈME DE RÉPERTOIRE DE TRAVAIL")
    print("=" * 70)
    
    scenarios = [
        {
            "method": "dotnet run depuis FNEV4.Presentation",
            "working_dir": r"C:\wamp64\www\FNEV4\src\FNEV4.Presentation",
            "relative_path": r"Data\fnev4.db"
        },
        {
            "method": "Lancement via Visual Studio (Debug)",
            "working_dir": r"C:\wamp64\www\FNEV4\src\FNEV4.Presentation\bin\Debug\net8.0-windows",
            "relative_path": r"Data\fnev4.db"
        },
        {
            "method": "Double-clic sur .exe",
            "working_dir": r"C:\wamp64\www\FNEV4\src\FNEV4.Presentation\bin\Debug\net8.0-windows",
            "relative_path": r"Data\fnev4.db"
        },
        {
            "method": "dotnet run depuis racine FNEV4",
            "working_dir": r"C:\wamp64\www\FNEV4",
            "relative_path": r"Data\fnev4.db"
        }
    ]
    
    print("📋 SCÉNARIOS DE LANCEMENT ET CHEMINS RÉSULTANTS:")
    print()
    
    for i, scenario in enumerate(scenarios, 1):
        print(f"🔸 SCÉNARIO {i}: {scenario['method']}")
        print(f"   📁 Répertoire de travail: {scenario['working_dir']}")
        
        # Calculer le chemin absolu résultant
        full_path = os.path.join(scenario['working_dir'], scenario['relative_path'])
        full_path = os.path.normpath(full_path)
        
        print(f"   🗄️  Base créée dans: {full_path}")
        
        # Analyser si c'est problématique
        if "bin" in full_path.lower():
            print(f"   ❌ PROBLÉMATIQUE: Base dans dossier de build !")
        elif full_path.endswith(r"FNEV4\Data\fnev4.db"):
            print(f"   ✅ CORRECT: Base dans dossier data principal")
        else:
            print(f"   ⚠️  INATTENDU: Emplacement non standard")
        
        print()
    
    print("🎯 CONCLUSION:")
    print("   • Chemin relatif = Base créée selon répertoire de travail")
    print("   • Visual Studio/Debug = Répertoire bin/ = Base dans bin/")
    print("   • Solution: Chemin ABSOLU via PathConfigurationService")
    
    print("\n" + "=" * 70)
    print("✅ SOLUTION IMPLÉMENTÉE:")
    print("   PathConfigurationService trouve toujours C:\\wamp64\\www\\FNEV4")
    print("   → Base toujours dans C:\\wamp64\\www\\FNEV4\\data\\FNEV4.db")
    print("   → Peu importe d'où l'application est lancée !")

if __name__ == "__main__":
    demonstrate_working_directory_problem()
