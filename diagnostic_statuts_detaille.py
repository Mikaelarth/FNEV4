#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Script de diagnostic détaillé des statuts de dossiers FNEV4
Analyse l'incohérence entre affichage visuel et compteur de statut
"""

import os
import json
from datetime import datetime
import sqlite3

def analyze_database_paths():
    """Analyse les chemins stockés dans la base de données"""
    db_path = r"C:\wamp64\www\FNEV4\data\fnev4.db"
    
    if not os.path.exists(db_path):
        print(f"❌ Base de données introuvable: {db_path}")
        return {}
    
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Récupérer tous les paramètres de chemins
        cursor.execute("""
            SELECT Key, Value, Description, LastModified 
            FROM AppSettings 
            WHERE Key LIKE '%Path%' OR Key LIKE '%Folder%'
            ORDER BY Key
        """)
        
        paths = {}
        for row in cursor.fetchall():
            key, value, description, last_modified = row
            paths[key] = {
                'value': value,
                'description': description,
                'last_modified': last_modified,
                'exists': os.path.exists(value) if value else False,
                'is_directory': os.path.isdir(value) if value and os.path.exists(value) else False
            }
        
        conn.close()
        return paths
        
    except Exception as e:
        print(f"❌ Erreur lors de la lecture de la base: {e}")
        return {}

def check_folder_status(path):
    """Détermine le statut réel d'un dossier"""
    if not path:
        return "Invalid", "Chemin vide"
    
    if not os.path.exists(path):
        return "Invalid", "Dossier inexistant"
    
    if not os.path.isdir(path):
        return "Invalid", "Chemin pointe vers un fichier"
    
    try:
        # Test d'écriture
        test_file = os.path.join(path, "test_write_permission.tmp")
        with open(test_file, 'w') as f:
            f.write("test")
        os.remove(test_file)
        
        # Compter les fichiers
        files = os.listdir(path)
        file_count = len([f for f in files if os.path.isfile(os.path.join(path, f))])
        
        if file_count == 0:
            return "Warning", f"Dossier vide ({file_count} fichiers)"
        else:
            return "Valid", f"Dossier opérationnel ({file_count} fichiers)"
            
    except PermissionError:
        return "Invalid", "Pas de permissions d'écriture"
    except Exception as e:
        return "Warning", f"Problème d'accès: {str(e)[:50]}"

def main():
    print("🔍 Diagnostic détaillé des statuts de dossiers FNEV4")
    print("=" * 60)
    
    # Chemins par défaut attendus
    expected_folders = {
        'ImportFolderPath': 'Dossier Import Excel Sage',
        'ExportFolderPath': 'Dossier Export Factures Certifiées', 
        'ArchiveFolderPath': 'Dossier Archivage',
        'LogsFolderPath': 'Dossier Logs Applicatifs',
        'BackupFolderPath': 'Dossier Sauvegarde Base'
    }
    
    # Analyser la base de données
    print("📊 Analyse de la base de données...")
    db_paths = analyze_database_paths()
    
    results = {
        'timestamp': datetime.now().isoformat(),
        'database_analysis': db_paths,
        'folder_status': {},
        'visual_vs_counter_analysis': {},
        'recommendations': []
    }
    
    print("\n📁 Analyse des statuts de dossiers:")
    print("-" * 40)
    
    valid_count = 0
    warning_count = 0
    invalid_count = 0
    unknown_count = 0
    
    for key, display_name in expected_folders.items():
        print(f"\n{display_name}:")
        
        if key in db_paths:
            path = db_paths[key]['value']
            print(f"  📍 Chemin DB: {path}")
            
            real_status, reason = check_folder_status(path)
            print(f"  🔍 Statut réel: {real_status} - {reason}")
            
            results['folder_status'][key] = {
                'path': path,
                'real_status': real_status,
                'reason': reason,
                'exists': os.path.exists(path) if path else False,
                'writable': real_status == "Valid"
            }
            
            if real_status == "Valid":
                valid_count += 1
                print(f"  ✅ Visuellement: VERT (configuré)")
            elif real_status == "Warning":
                warning_count += 1
                print(f"  🟠 Visuellement: ORANGE (avertissement)")
            elif real_status == "Invalid":
                invalid_count += 1
                print(f"  🔴 Visuellement: ROUGE (erreur)")
        else:
            print(f"  ❓ Pas dans la base de données")
            unknown_count += 1
            print(f"  ⚪ Visuellement: GRIS (inconnu)")
            
            results['folder_status'][key] = {
                'path': None,
                'real_status': 'Unknown',
                'reason': 'Pas configuré dans la base',
                'exists': False,
                'writable': False
            }
    
    print(f"\n📊 Résumé des statuts:")
    print(f"  ✅ Valid: {valid_count}")
    print(f"  🟠 Warning: {warning_count}")
    print(f"  🔴 Invalid: {invalid_count}")
    print(f"  ⚪ Unknown: {unknown_count}")
    
    # Analyse de l'incohérence
    print(f"\n🔍 Analyse de l'incohérence visuel vs compteur:")
    print("-" * 50)
    
    visual_configured = valid_count + warning_count  # Ce que l'utilisateur voit comme "configuré"
    counter_configured = valid_count  # Ce que le compteur affiche
    
    print(f"  👁️  Apparence visuelle: {visual_configured}/5 dossiers semblent configurés")
    print(f"  🔢 Compteur interne: {counter_configured}/5 dossiers validés")
    print(f"  ❗ Différence: {visual_configured - counter_configured} dossier(s)")
    
    results['visual_vs_counter_analysis'] = {
        'visual_configured': visual_configured,
        'counter_configured': counter_configured,
        'difference': visual_configured - counter_configured,
        'explanation': f"Les dossiers avec Warning apparaissent configurés visuellement mais ne sont pas comptés"
    }
    
    if visual_configured != counter_configured:
        print(f"\n🎯 Explication de l'incohérence:")
        print(f"  - Le convertisseur visuel affiche en VERT les statuts 'Valid'")
        print(f"  - Le convertisseur visuel affiche en ORANGE les statuts 'Warning'")
        print(f"  - Le compteur ne compte que les statuts 'Valid'")
        print(f"  - L'utilisateur voit {visual_configured} dossiers 'configurés' mais le compteur dit {counter_configured}")
        
        results['recommendations'].extend([
            "Modifier le compteur pour inclure les Warning comme 'partiellement configurés'",
            "Ou modifier l'affichage visuel pour distinguer Valid et Warning plus clairement",
            "Ajouter une légende expliquant la différence entre les couleurs"
        ])
    
    # Sauvegarder les résultats
    with open('diagnostic_statuts_detaille_results.json', 'w', encoding='utf-8') as f:
        json.dump(results, f, indent=2, ensure_ascii=False)
    
    print(f"\n💾 Résultats sauvegardés dans: diagnostic_statuts_detaille_results.json")
    print(f"\n✅ Diagnostic terminé à {datetime.now().strftime('%H:%M:%S')}")

if __name__ == "__main__":
    main()
