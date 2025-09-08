#!/usr/bin/env python3
"""
Script de validation de la correction du crash MaterialDesignChip
"""

import os
import sys

def check_xaml_file():
    """Vérifie que le fichier XAML ne contient plus la référence problématique"""
    xaml_file = r"src\FNEV4.Presentation\Views\GestionClients\ListeClientsView.xaml"
    
    if not os.path.exists(xaml_file):
        print("❌ Fichier XAML non trouvé")
        return False
    
    with open(xaml_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Vérifier que MaterialDesignChip n'est plus référencé
    if 'MaterialDesignChip' in content:
        print("❌ Référence MaterialDesignChip encore présente")
        return False
    
    # Vérifier que la nouvelle implémentation est présente
    if 'Border HorizontalAlignment="Center"' not in content:
        print("❌ Nouvelle implémentation Border non trouvée")
        return False
    
    if 'DataTrigger Binding="{Binding IsActive}"' not in content:
        print("❌ DataTriggers pour IsActive non trouvés")
        return False
    
    print("✅ Fichier XAML corrigé avec succès")
    print("  - Référence MaterialDesignChip supprimée")
    print("  - Implémentation Border avec DataTriggers ajoutée")
    return True

def main():
    print("🔍 Validation de la correction du crash MaterialDesignChip")
    print("=" * 60)
    
    if check_xaml_file():
        print("\n🎉 Correction validée avec succès!")
        print("   L'application ne devrait plus crasher sur 'Liste des clients'")
        return 0
    else:
        print("\n❌ Problème détecté dans la correction")
        return 1

if __name__ == "__main__":
    sys.exit(main())
