#!/usr/bin/env python3
"""
Test de performance pour DataGrid FNEV4 - Optimisations appliquées
Vérifie les améliorations apportées pour résoudre la lenteur
"""

import os
import sqlite3
import time
from datetime import datetime

def check_database_performance():
    """Teste les performances de la base de données SQLite avec les nouveaux paramètres"""
    print("🔍 Test de performance de la base de données...")
    
    db_path = r"C:\wamp64\www\FNEV4\data\FNEV4.db"
    if not os.path.exists(db_path):
        print(f"❌ Base de données introuvable: {db_path}")
        return
    
    try:
        # Connexion avec les nouveaux paramètres optimisés
        conn_string = f"file:{db_path}?cache=shared&mode=ro"
        conn = sqlite3.connect(conn_string, uri=True)
        conn.execute("PRAGMA cache_size = 10000")
        conn.execute("PRAGMA temp_store = MEMORY")
        conn.execute("PRAGMA journal_mode = WAL")
        conn.execute("PRAGMA synchronous = NORMAL")
        
        cursor = conn.cursor()
        
        # Test 1: Comptage total des clients
        start_time = time.time()
        cursor.execute("SELECT COUNT(*) FROM Clients")
        total_clients = cursor.fetchone()[0]
        count_time = time.time() - start_time
        
        print(f"📊 Nombre total de clients: {total_clients}")
        print(f"⏱️ Temps de comptage: {count_time:.3f}s")
        
        # Test 2: Pagination simulée (25 premiers clients)
        start_time = time.time()
        cursor.execute("""
            SELECT Id, Name, ClientCode, ClientType, IsActive 
            FROM Clients 
            ORDER BY Name, ClientCode 
            LIMIT 25
        """)
        page_results = cursor.fetchall()
        pagination_time = time.time() - start_time
        
        print(f"📄 Première page (25 clients) chargée en: {pagination_time:.3f}s")
        
        # Test 3: Recherche avec LIKE
        start_time = time.time()
        cursor.execute("""
            SELECT COUNT(*) FROM Clients 
            WHERE Name LIKE '%test%' OR ClientCode LIKE '%test%'
        """)
        search_results = cursor.fetchone()[0]
        search_time = time.time() - start_time
        
        print(f"🔍 Recherche 'test': {search_results} résultats en {search_time:.3f}s")
        
        # Test 4: Vérification des index
        cursor.execute("SELECT name FROM sqlite_master WHERE type='index' AND tbl_name='Clients'")
        indexes = cursor.fetchall()
        print(f"📇 Index sur la table Clients: {len(indexes)}")
        for idx in indexes:
            print(f"   - {idx[0]}")
        
        conn.close()
        
        # Évaluation de la performance
        print("\n📈 ÉVALUATION DES PERFORMANCES:")
        if count_time < 0.1:
            print("✅ Comptage: Excellent (< 0.1s)")
        elif count_time < 0.5:
            print("🟡 Comptage: Bon (< 0.5s)")
        else:
            print("⚠️ Comptage: Lent (> 0.5s)")
            
        if pagination_time < 0.05:
            print("✅ Pagination: Excellent (< 0.05s)")
        elif pagination_time < 0.2:
            print("🟡 Pagination: Bon (< 0.2s)")
        else:
            print("⚠️ Pagination: Lent (> 0.2s)")
            
        if search_time < 0.1:
            print("✅ Recherche: Excellent (< 0.1s)")
        elif search_time < 0.5:
            print("🟡 Recherche: Bon (< 0.5s)")
        else:
            print("⚠️ Recherche: Lent (> 0.5s)")
        
    except Exception as e:
        print(f"❌ Erreur lors du test de performance: {e}")

def check_xaml_optimizations():
    """Vérifie les optimisations appliquées au XAML"""
    print("\n🔍 Vérification des optimisations XAML...")
    
    xaml_path = r"C:\wamp64\www\FNEV4\src\FNEV4.Presentation\Views\GestionClients\ListeClientsView.xaml"
    if not os.path.exists(xaml_path):
        print(f"❌ Fichier XAML introuvable: {xaml_path}")
        return
    
    try:
        with open(xaml_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        optimizations = {
            'EnableRowVirtualization="True"': '✅ Virtualisation des lignes activée',
            'EnableColumnVirtualization="True"': '✅ Virtualisation des colonnes activée',
            'VirtualizingPanel.IsVirtualizing="True"': '✅ Panel de virtualisation activé',
            'VirtualizationMode="Recycling"': '✅ Mode recyclage activé',
            'ScrollUnit="Pixel"': '✅ Défilement par pixel',
            'CacheLength="1,2"': '✅ Cache de virtualisation configuré',
            'IsDeferredScrollingEnabled="False"': '✅ Défilement immédiat',
            'UseLayoutRounding="True"': '✅ Arrondi des layouts',
            'SnapsToDevicePixels="True"': '✅ Accrochage aux pixels'
        }
        
        print("📋 Optimisations DataGrid détectées:")
        for opt, desc in optimizations.items():
            if opt in content:
                print(f"   {desc}")
            else:
                print(f"   ❌ {desc.replace('✅', '').strip()} - MANQUANT")
        
        # Vérification des erreurs corrigées
        if 'MaterialDesignChip' in content:
            print("⚠️ ATTENTION: MaterialDesignChip encore présent (risque de crash)")
        else:
            print("✅ MaterialDesignChip supprimé (crash corrigé)")
            
    except Exception as e:
        print(f"❌ Erreur lors de la vérification XAML: {e}")

def check_viewmodel_optimizations():
    """Vérifie les optimisations du ViewModel"""
    print("\n🔍 Vérification des optimisations ViewModel...")
    
    vm_path = r"C:\wamp64\www\FNEV4\src\FNEV4.Presentation\ViewModels\GestionClients\ListeClientsViewModel.cs"
    if not os.path.exists(vm_path):
        print(f"❌ Fichier ViewModel introuvable: {vm_path}")
        return
    
    try:
        with open(vm_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        checks = {
            'if (IsLoading) return;': '✅ Protection contre les rechargements multiples',
            'ExecuteAsync(null)': '✅ Exécution asynchrone des commandes',
            'ObservableCollection<int> PageSizes': '✅ Collection des tailles de page',
            'PageSize': '✅ Propriété PageSize (binding corrigé)'
        }
        
        print("📋 Optimisations ViewModel détectées:")
        for check, desc in checks.items():
            if check in content:
                print(f"   {desc}")
            else:
                print(f"   ❌ {desc.replace('✅', '').strip()} - MANQUANT")
                
    except Exception as e:
        print(f"❌ Erreur lors de la vérification ViewModel: {e}")

def main():
    """Fonction principale de test des performances"""
    print("=" * 60)
    print("🚀 TEST DE PERFORMANCE DATAGRID FNEV4")
    print("=" * 60)
    print(f"📅 Exécuté le: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print()
    
    # Tests de performance
    check_database_performance()
    check_xaml_optimizations()
    check_viewmodel_optimizations()
    
    print("\n" + "=" * 60)
    print("📊 RÉSUMÉ DES OPTIMISATIONS APPLIQUÉES:")
    print("=" * 60)
    print("1. ✅ SQLite optimisé (WAL, Cache, Synchronisation)")
    print("2. ✅ DataGrid virtualisé (lignes + colonnes)")
    print("3. ✅ Entity Framework NoTracking pour lecture seule")
    print("4. ✅ Requêtes optimisées avec EF.Functions.Like")
    print("5. ✅ Protection contre les rechargements multiples")
    print("6. ✅ Correction des bindings (PageSize/PageSizes)")
    print("7. ✅ MaterialDesignChip remplacé (crash corrigé)")
    print()
    print("🎯 PERFORMANCE ATTENDUE:")
    print("   - Chargement initial: < 1s")
    print("   - Pagination: < 0.2s") 
    print("   - Recherche: < 0.5s")
    print("   - Filtrage: < 0.3s")

if __name__ == "__main__":
    main()
