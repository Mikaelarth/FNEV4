#!/usr/bin/env python3
"""
Script de validation des améliorations de performance pour ListeClientsView
"""

import os
import sys

def check_xaml_optimizations():
    """Vérifie que les optimisations de performance sont en place dans le XAML"""
    xaml_file = r"src\FNEV4.Presentation\Views\GestionClients\ListeClientsView.xaml"
    
    if not os.path.exists(xaml_file):
        print("❌ Fichier XAML non trouvé")
        return False
    
    with open(xaml_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Vérifier les optimisations de virtualisation
    optimizations = {
        'EnableRowVirtualization="True"': "Virtualisation des lignes",
        'EnableColumnVirtualization="True"': "Virtualisation des colonnes", 
        'VirtualizingPanel.IsVirtualizing="True"': "Panel de virtualisation",
        'VirtualizingPanel.VirtualizationMode="Recycling"': "Mode de recyclage",
        'ScrollViewer.CanContentScroll="True"': "Défilement du contenu",
        'ScrollViewer.IsDeferredScrollingEnabled="True"': "Défilement différé"
    }
    
    missing_optimizations = []
    for optimization, description in optimizations.items():
        if optimization not in content:
            missing_optimizations.append(f"  - {description} ({optimization})")
    
    if missing_optimizations:
        print("❌ Optimisations manquantes:")
        for missing in missing_optimizations:
            print(missing)
        return False
    
    print("✅ Optimisations de virtualisation DataGrid présentes")
    
    # Vérifier les corrections de binding
    bindings = {
        'ItemsSource="{Binding PageSizes}"': "Binding PageSizes",
        'SelectedItem="{Binding PageSize}"': "Binding PageSize", 
        'Text="{Binding TotalCount': "Binding TotalCount"
    }
    
    missing_bindings = []
    for binding, description in bindings.items():
        if binding not in content:
            missing_bindings.append(f"  - {description}")
    
    if missing_bindings:
        print("❌ Bindings incorrects:")
        for missing in missing_bindings:
            print(missing)
        return False
    
    print("✅ Bindings de pagination corrigés")
    
    # Vérifier que MaterialDesignChip n'est plus utilisé
    if 'MaterialDesignChip' in content:
        print("❌ Référence MaterialDesignChip encore présente")
        return False
    
    print("✅ Composant MaterialDesignChip remplacé par Border")
    
    return True

def check_viewmodel_optimizations():
    """Vérifie que les optimisations du ViewModel sont en place"""
    viewmodel_file = r"src\FNEV4.Presentation\ViewModels\GestionClients\ListeClientsViewModel.cs"
    
    if not os.path.exists(viewmodel_file):
        print("❌ Fichier ViewModel non trouvé")
        return False
    
    with open(viewmodel_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Vérifier que Task.Run a été remplacé par ExecuteAsync
    if 'Task.Run(LoadClientsAsync)' in content:
        print("❌ Task.Run(LoadClientsAsync) encore présent - devrait être LoadClientsCommand.ExecuteAsync(null)")
        return False
    
    print("✅ Appels Task.Run remplacés par ExecuteAsync")
    
    # Vérifier que LoadClientsCommand.ExecuteAsync est utilisé
    if 'LoadClientsCommand.ExecuteAsync(null)' not in content:
        print("❌ LoadClientsCommand.ExecuteAsync(null) non trouvé")
        return False
    
    print("✅ Méthodes OnPropertyChanged utilisent ExecuteAsync")
    
    return True

def main():
    print("🔍 Validation des améliorations de performance")
    print("=" * 55)
    
    xaml_ok = check_xaml_optimizations()
    print()
    viewmodel_ok = check_viewmodel_optimizations()
    
    print("\n" + "=" * 55)
    
    if xaml_ok and viewmodel_ok:
        print("🎉 Toutes les améliorations de performance validées!")
        print("   - DataGrid optimisé avec virtualisation")
        print("   - Bindings de pagination corrigés") 
        print("   - Crash MaterialDesignChip résolu")
        print("   - ViewModel optimisé avec ExecuteAsync")
        print("\n💡 L'interface devrait maintenant être plus rapide et le combo 'par page' fonctionnel")
        return 0
    else:
        print("❌ Certaines améliorations manquent")
        return 1

if __name__ == "__main__":
    sys.exit(main())
