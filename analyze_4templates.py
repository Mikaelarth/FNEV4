#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Analyse spécifique du fichier avec les 4 templates DGI (B2B, B2C, B2G, B2F)
"""

import pandas as pd
from pathlib import Path

def analyze_4templates_file():
    """Analyse le fichier Excel avec les 4 templates DGI"""
    
    file_path = Path('test_import_dgi_4templates.xlsx')
    
    if not file_path.exists():
        print(f"❌ Fichier non trouvé: {file_path}")
        return
    
    # Charger le fichier
    try:
        df = pd.read_excel(file_path, engine='openpyxl')
        print(f"🔍 ANALYSE DU FICHIER: {file_path}")
        print("=" * 80)
        print(f"📊 {len(df)} lignes de données • {len(df.columns)} colonnes")
        print()
        
        # Analyser chaque ligne
        line_num = 2  # Commence à 2 (ligne 1 = en-tête)
        for idx, row in df.iterrows():
            print(f"🔸 LIGNE {line_num}: {row['Code Client']} - {row['Nom/Raison Sociale']}")
            line_num += 1
            print(f"   📋 Template: {row['Template']}")
            print(f"   🆔 NCC: '{row['NCC']}'")
            print(f"   💰 Devise: {row['Devise']}")
            print(f"   🌍 Pays: {row['Pays']}")
            
            # Validation selon template
            validations = []
            errors = []
            warnings = []
            
            # Validation générale
            if pd.notna(row['Code Client']) and row['Code Client'].strip():
                validations.append("✅ Code Client présent")
            else:
                errors.append("❌ Code Client manquant")
                
            if pd.notna(row['Nom/Raison Sociale']) and row['Nom/Raison Sociale'].strip():
                validations.append("✅ Nom/Raison Sociale présent")
            else:
                errors.append("❌ Nom/Raison Sociale manquant")
            
            # Validation template
            template = row['Template']
            if template in ['B2B', 'B2C', 'B2G', 'B2F']:
                validations.append(f"✅ Template '{template}' valide")
                
                # Validation spécifique par template
                ncc = str(row['NCC']).strip() if pd.notna(row['NCC']) else ""
                currency = row['Devise']
                country = row['Pays']
                
                if template == 'B2B':
                    if ncc:
                        if len(ncc) >= 8 and len(ncc) <= 11:
                            validations.append("✅ NCC B2B format valide")
                        else:
                            errors.append(f"❌ NCC B2B longueur invalide: {len(ncc)} (8-11 requis)")
                    else:
                        errors.append("❌ NCC obligatoire pour B2B")
                        
                elif template == 'B2C':
                    if not ncc:
                        validations.append("✅ NCC absent (correct pour B2C)")
                    else:
                        warnings.append("⚠️  NCC présent pour B2C (devrait être vide)")
                        
                elif template == 'B2G':
                    if ncc:
                        validations.append("✅ NCC B2G présent")
                    else:
                        warnings.append("⚠️  NCC absent pour B2G (optionnel)")
                        
                elif template == 'B2F':
                    if ncc:
                        validations.append("✅ NCC B2F présent (identification internationale)")
                    else:
                        errors.append("❌ NCC obligatoire pour B2F (clients internationaux)")
                    
                    # Validation devise étrangère
                    if currency != 'XOF':
                        validations.append(f"✅ Devise étrangère {currency} (correct pour B2F)")
                    else:
                        errors.append("❌ B2F doit utiliser une devise étrangère (non XOF)")
                    
                    # Validation pays étranger
                    if country and not any(x in country.lower() for x in ['côte d\'ivoire', 'cote d\'ivoire']):
                        validations.append(f"✅ Pays étranger {country} (correct pour B2F)")
                    else:
                        errors.append("❌ B2F doit être basé à l'étranger")
                        
            else:
                errors.append(f"❌ Template '{template}' invalide")
            
            # Afficher résultats
            if validations:
                print(f"   🟢 VALIDATIONS RÉUSSIES ({len(validations)}):")
                for v in validations:
                    print(f"      {v}")
            
            if warnings:
                print(f"   🟡 AVERTISSEMENTS ({len(warnings)}):")
                for w in warnings:
                    print(f"      {w}")
                    
            if errors:
                print(f"   🔴 ERREURS ({len(errors)}):")
                for e in errors:
                    print(f"      {e}")
                print(f"   🎯 PRÉDICTION: ❌ ÉCHEC/IGNORÉ")
            else:
                print(f"   🎯 PRÉDICTION: ✅ SUCCÈS")
                
            print()
        
        # Résumé par template
        print("=" * 60)
        print("RÉSUMÉ PAR TEMPLATE")
        print("=" * 60)
        
        template_counts = df['Template'].value_counts()
        for template, count in template_counts.items():
            print(f"📋 {template}: {count} client(s)")
            
            # Description du template
            if template == 'B2B':
                print("   • Business to Business (entreprises)")
                print("   • NCC obligatoire (format alphanumérique)")
                print("   • Devise: généralement XOF")
            elif template == 'B2C':
                print("   • Business to Consumer (particuliers)")
                print("   • NCC interdit")
                print("   • Devise: généralement XOF")
            elif template == 'B2G':
                print("   • Business to Government (administrations)")
                print("   • NCC optionnel")
                print("   • Devise: généralement XOF")
            elif template == 'B2F':
                print("   • Business to Foreign (clients internationaux)")
                print("   • NCC obligatoire (identification internationale)")
                print("   • Devise: obligatoirement étrangère (USD, EUR, etc.)")
                print("   • Pays: obligatoirement étranger")
        
        print()
        print("✅ Analyse terminée !")
        
    except Exception as e:
        print(f"❌ Erreur lors de l'analyse: {e}")

if __name__ == "__main__":
    analyze_4templates_file()
