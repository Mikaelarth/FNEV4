#!/usr/bin/env python3
"""
ANALYSE DÉTAILLÉE DES RÉSULTATS D'IMPORT DGI
Explication complète des succès, erreurs et ignorés basée sur la documentation officielle
"""

import pandas as pd
import os

def analyze_import_comprehensive():
    """Analyse complète des résultats d'import avec contexte DGI"""
    
    print("🔍 ANALYSE DÉTAILLÉE DES RÉSULTATS D'IMPORT FNEV4")
    print("=" * 80)
    
    # Analyser le fichier original
    print("\n📊 FICHIER ORIGINAL: test_import_dgi.xlsx")
    analyze_file("test_import_dgi.xlsx", "ORIGINAL avec NCC numériques")
    
    # Analyser le nouveau fichier
    print("\n📊 FICHIER CORRIGÉ: test_import_dgi_v2.xlsx")  
    analyze_file("test_import_dgi_v2.xlsx", "CORRIGÉ avec NCC DGI conformes")

def analyze_file(filename, description):
    """Analyse un fichier Excel spécifique"""
    
    if not os.path.exists(filename):
        print(f"❌ Fichier {filename} non trouvé")
        return
    
    try:
        df = pd.read_excel(filename, sheet_name='Clients')
        print(f"\n🎯 {description}")
        print(f"   📋 {len(df)} lignes de données • {len(df.columns)} colonnes")
        
        print(f"\n{'=' * 60}")
        print("ANALYSE LIGNE PAR LIGNE")
        print(f"{'=' * 60}")
        
        for index, row in df.iterrows():
            ligne_num = index + 2
            code_client = str(row['Code Client']).strip()
            nom = str(row['Nom/Raison Sociale']).strip()
            template = str(row['Template']).strip()
            ncc = str(row['NCC']).strip() if pd.notna(row['NCC']) else ""
            
            print(f"\n🔸 LIGNE {ligne_num}: {code_client} - {nom}")
            print(f"   📋 Template: {template}")
            print(f"   🆔 NCC: '{ncc}' ({len(ncc)} car.)" if ncc else "   🆔 NCC: [VIDE]")
            
            # Analyse selon règles DGI
            result = analyze_dgi_rules(template, ncc, code_client, nom)
            
            print(f"   📊 VALIDATION DGI:")
            if result['errors']:
                print(f"   🔴 ERREURS ({len(result['errors'])}):")
                for error in result['errors']:
                    print(f"      ❌ {error}")
                    
            if result['warnings']:
                print(f"   🟡 AVERTISSEMENTS ({len(result['warnings'])}):")
                for warning in result['warnings']:
                    print(f"      ⚠️  {warning}")
                    
            if result['validations']:
                print(f"   🟢 VALIDATIONS RÉUSSIES ({len(result['validations'])}):")
                for validation in result['validations']:
                    print(f"      ✅ {validation}")
            
            # Prédiction du résultat
            if result['errors']:
                prediction = "❌ ÉCHEC/IGNORÉ"
                reason = f"Validation échouée ({len(result['errors'])} erreur(s))"
            else:
                prediction = "✅ SUCCÈS"
                reason = "Toutes validations DGI passées"
            
            print(f"   🎯 PRÉDICTION: {prediction}")
            print(f"   💡 RAISON: {reason}")
            
        # Résumé prédictif
        print(f"\n{'=' * 60}")
        print("RÉSUMÉ PRÉDICTIF")
        print(f"{'=' * 60}")
        
        total_lines = len(df)
        predicted_success = sum(1 for _, row in df.iterrows() 
                              if not analyze_dgi_rules(
                                  str(row['Template']).strip(),
                                  str(row['NCC']).strip() if pd.notna(row['NCC']) else "",
                                  str(row['Code Client']).strip(),
                                  str(row['Nom/Raison Sociale']).strip()
                              )['errors'])
        predicted_errors = total_lines - predicted_success
        
        print(f"📊 PRÉDICTIONS:")
        print(f"   • Total lignes: {total_lines}")
        print(f"   • Succès prévus: {predicted_success}")
        print(f"   • Erreurs prévues: {predicted_errors}")
        print(f"   • Ignorés possibles: 0-1 (en-tête ou doublons)")
        
        # Explication des résultats réels observés
        if filename == "test_import_dgi.xlsx":
            print(f"\n🎯 RÉSULTATS RÉELS OBSERVÉS:")
            print(f"   • 3 Total lignes ✅")
            print(f"   • 2 Succès ✅")
            print(f"   • 1 Ignoré ✅")
            print(f"   • 0 Erreurs ✅")
            
            print(f"\n💡 EXPLICATION PROBABLE:")
            print(f"   • CLI001 (B2B): NCC '1234567890' purement numérique")
            print(f"     → Possiblement IGNORÉ (format non-standard)")
            print(f"   • CLI002 (B2C): Pas de NCC (correct) → SUCCÈS")
            print(f"   • CLI003 (B2G): NCC '9876543210' → SUCCÈS")
            print(f"     (B2G plus tolérant que B2B)")
        
    except Exception as e:
        print(f"❌ Erreur analyse {filename}: {e}")

def analyze_dgi_rules(template, ncc, code_client, nom):
    """Analyse selon les règles DGI officielles"""
    
    result = {
        'errors': [],
        'warnings': [],
        'validations': []
    }
    
    # 1. Validation Template
    valid_templates = ['B2B', 'B2C', 'B2G', 'B2F']
    if template not in valid_templates:
        result['errors'].append(f"Template '{template}' invalide")
    else:
        result['validations'].append(f"Template '{template}' valide")
    
    # 2. Validation NCC selon Template (règles officielles DGI)
    if template == 'B2B':
        if not ncc:
            result['errors'].append("NCC obligatoire pour B2B (entreprises)")
        else:
            # Vérification format NCC B2B selon exemples DGI
            if ncc.isdigit():
                result['warnings'].append("NCC B2B purement numérique (format non-standard)")
                result['warnings'].append("Format DGI attendu: alphanumérique avec lettre finale")
                result['warnings'].append("Exemples conformes: 9502363N, 9606123E")
            elif len(ncc) < 8 or len(ncc) > 11:
                result['errors'].append(f"NCC longueur invalide: {len(ncc)} (attendu: 8-11)")
            else:
                result['validations'].append("NCC B2B format valide")
    
    elif template == 'B2C':
        if ncc:
            result['errors'].append("NCC interdit pour B2C (particuliers)")
        else:
            result['validations'].append("NCC absent (correct pour B2C)")
    
    elif template in ['B2G', 'B2F']:
        if ncc:
            if len(ncc) < 8 or len(ncc) > 11:
                result['warnings'].append(f"NCC {template} longueur inhabituelle: {len(ncc)}")
            else:
                result['validations'].append(f"NCC {template} présent et valide")
        else:
            result['warnings'].append(f"NCC absent pour {template}")
    
    # 3. Validation champs obligatoires
    if not code_client:
        result['errors'].append("Code Client obligatoire")
    else:
        result['validations'].append("Code Client présent")
        
    if not nom:
        result['errors'].append("Nom/Raison Sociale obligatoire")
    else:
        result['validations'].append("Nom/Raison Sociale présent")
    
    return result

def explain_ncc_format():
    """Explication du format NCC selon documentation DGI"""
    
    print(f"\n{'=' * 80}")
    print("📚 FORMAT NCC SELON DOCUMENTATION OFFICIELLE DGI")
    print(f"{'=' * 80}")
    
    print("\n🎯 EXEMPLES OFFICIELS dans FNE-procedureapi.md:")
    print("   • clientNcc: '9502363N' (entreprise KPMG)")
    print("   • ncc: '9606123E' (entreprise émettrice)")
    
    print("\n📋 CARACTÉRISTIQUES:")
    print("   • Format: ALPHANUMÉRIQUE")
    print("   • Structure: [8-10 chiffres][1 lettre]")
    print("   • Longueur: 8-11 caractères")
    print("   • Exemple valide: 9502363N, 9606123E")
    print("   • Exemple invalide: 1234567890 (purement numérique)")
    
    print("\n🚨 IMPACT SUR IMPORT:")
    print("   • B2B: NCC obligatoire et format strict")
    print("   • B2C: NCC interdit")
    print("   • B2G/B2F: NCC optionnel mais si présent, format respecté")

if __name__ == "__main__":
    analyze_import_comprehensive()
    explain_ncc_format()
    
    print(f"\n🎯 CONCLUSION:")
    print("Le fichier v2 avec NCC conformes DGI devrait donner:")
    print("   • 3 lignes → 3 succès → 0 ignoré → 0 erreur")
    print("   • Amélioration significative vs version originale")
