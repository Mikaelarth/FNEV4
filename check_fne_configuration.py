#!/usr/bin/env python3
"""
Script de diagnostic - Configuration FNE
Vérifie l'état de la configuration API FNE dans la base de données
"""

import sqlite3
import sys
from pathlib import Path

def check_fne_configuration():
    """
    Vérifie la configuration FNE dans la base de données
    """
    
    # Chemins possibles pour la base de données
    db_paths = [
        Path("data/FNEV4.db"),
        Path("FNEV4/data/FNEV4.db"),
        Path("src/FNEV4.Presentation/data/FNEV4.db"),
        Path("../data/FNEV4.db")
    ]
    
    db_path = None
    for path in db_paths:
        if path.exists():
            db_path = path
            break
    
    if not db_path:
        print("❌ Base de données FNEV4.db introuvable")
        print("📍 Chemins vérifiés :", [str(p) for p in db_paths])
        return
    
    print(f"✅ Base de données trouvée : {db_path}")
    
    try:
        # Connexion à la base de données
        conn = sqlite3.connect(str(db_path))
        cursor = conn.cursor()
        
        # Vérifier si la table FneConfigurations existe
        cursor.execute("""
            SELECT name FROM sqlite_master 
            WHERE type='table' AND name='FneConfigurations'
        """)
        
        if not cursor.fetchone():
            print("❌ Table FneConfigurations inexistante")
            return
        
        print("✅ Table FneConfigurations existe")
        
        # Lister toutes les configurations FNE
        cursor.execute("""
            SELECT 
                Id,
                ConfigurationName,
                Environment,
                BaseUrl,
                ApiKey,
                BearerToken,
                IsActive,
                IsValidatedByDgi,
                ValidationDate,
                LastModifiedDate,
                RequestTimeoutSeconds,
                MaxRetryAttempts
            FROM FneConfigurations
            ORDER BY LastModifiedDate DESC
        """)
        
        configurations = cursor.fetchall()
        
        print(f"\n📋 CONFIGURATIONS FNE TROUVÉES : {len(configurations)}")
        print("=" * 80)
        
        if not configurations:
            print("⚠️ AUCUNE CONFIGURATION FNE trouvée")
            print("💡 Créer une configuration via le menu Configuration → API FNE")
            return
        
        for i, config in enumerate(configurations, 1):
            (config_id, name, env, base_url, api_key, bearer_token, 
             is_active, is_validated, validation_date, last_modified, 
             timeout, retries) = config
            
            print(f"\n🔧 CONFIGURATION #{i}")
            print(f"   📛 Nom: {name}")
            print(f"   🌍 Environnement: {env}")
            print(f"   🔗 URL: {base_url}")
            print(f"   🔑 API Key: {'✅ Configurée' if api_key else '❌ Manquante'}")
            print(f"   🎫 Bearer Token: {'✅ Configurée' if bearer_token else '❌ Manquante'}")
            print(f"   ⚡ Active: {'✅ OUI' if is_active else '❌ NON'}")
            print(f"   ✅ Validée DGI: {'✅ OUI' if is_validated else '❌ NON'}")
            print(f"   📅 Dernière modif: {last_modified}")
            print(f"   ⏱️ Timeout: {timeout}s | Tentatives: {retries}")
            
            if is_active:
                print(f"   🎯 *** CONFIGURATION ACTIVE ***")
        
        # Vérifier la configuration active
        cursor.execute("""
            SELECT COUNT(*) FROM FneConfigurations WHERE IsActive = 1
        """)
        active_count = cursor.fetchone()[0]
        
        print(f"\n📊 RÉSUMÉ")
        print(f"   📋 Total configurations: {len(configurations)}")
        print(f"   ⚡ Configurations actives: {active_count}")
        
        if active_count == 0:
            print("   ❌ PROBLÈME: Aucune configuration active")
            print("   💡 Solution: Activer une configuration via l'interface")
        elif active_count > 1:
            print("   ⚠️ ATTENTION: Plusieurs configurations actives détectées")
            print("   💡 Recommandation: N'activer qu'une seule configuration")
        else:
            print("   ✅ Configuration active trouvée")
        
        # Test de base API si configuration active trouvée
        if active_count > 0:
            cursor.execute("""
                SELECT ConfigurationName, BaseUrl, ApiKey, BearerToken 
                FROM FneConfigurations 
                WHERE IsActive = 1 
                LIMIT 1
            """)
            
            active_config = cursor.fetchone()
            name, base_url, api_key, bearer_token = active_config
            
            print(f"\n🔍 TEST CONFIGURATION ACTIVE: {name}")
            
            # Vérifications
            issues = []
            if not base_url:
                issues.append("URL de base manquante")
            elif not (base_url.startswith("http://") or base_url.startswith("https://")):
                issues.append("URL invalide (doit commencer par http:// ou https://)")
            
            if not api_key and not bearer_token:
                issues.append("Ni ApiKey ni BearerToken configurés")
            
            if issues:
                print("   ❌ PROBLÈMES DÉTECTÉS:")
                for issue in issues:
                    print(f"      • {issue}")
                print("   💡 Corriger via Configuration → API FNE")
            else:
                print("   ✅ Configuration semble correcte")
                print("   🔗 URL API:", base_url)
                print("   🔐 Authentification:", "✅ ApiKey" if api_key else "✅ BearerToken")
        
    except sqlite3.Error as e:
        print(f"❌ Erreur base de données: {e}")
    
    except Exception as e:
        print(f"❌ Erreur inattendue: {e}")
    
    finally:
        if 'conn' in locals():
            conn.close()

if __name__ == "__main__":
    print("🔍 DIAGNOSTIC CONFIGURATION FNE")
    print("=" * 50)
    check_fne_configuration()
    print("\n✅ Diagnostic terminé")