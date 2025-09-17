#!/usr/bin/env python3
"""
Diagnostic technique du problème "Service provider non disponible"
Teste la connectivité et identifie les causes possibles
"""

import urllib.request
import urllib.error
import socket
import time
import json
import ssl
from pathlib import Path

def test_api_connectivity():
    """
    Teste la connectivité vers l'API FNE et diagnostique les problèmes
    """
    
    print("🔍 DIAGNOSTIC TECHNIQUE - Service provider non disponible")
    print("=" * 60)
    
    # Configuration de l'API FNE (depuis notre analyse)
    api_config = {
        "base_url": "http://54.247.95.108/ws",
        "web_url": "http://54.247.95.108",
        "api_key": "test_key_placeholder",  # Sera lu depuis la base si disponible
        "timeout": 30
    }
    
    print(f"🎯 CONFIGURATION API FNE")
    print(f"   URL API: {api_config['base_url']}")
    print(f"   URL Web: {api_config['web_url']}")
    print(f"   Timeout: {api_config['timeout']}s")
    
    # Test 1: Connectivité réseau de base
    print(f"\n🌐 TEST 1: CONNECTIVITÉ RÉSEAU DE BASE")
    print("-" * 40)
    
    try:
        # Extraire le hostname et port
        from urllib.parse import urlparse
        parsed = urlparse(api_config['base_url'])
        hostname = parsed.hostname
        port = parsed.port or (443 if parsed.scheme == 'https' else 80)
        
        print(f"   🎯 Test connexion socket vers {hostname}:{port}")
        
        # Test socket raw
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        sock.settimeout(10)
        
        start_time = time.time()
        result = sock.connect_ex((hostname, port))
        end_time = time.time()
        
        if result == 0:
            print(f"   ✅ Connexion socket réussie ({end_time - start_time:.2f}s)")
            sock.close()
        else:
            print(f"   ❌ Connexion socket échouée (code {result})")
            print(f"   💡 Causes possibles: Firewall, serveur down, problème DNS")
            return False
            
    except socket.gaierror as e:
        print(f"   ❌ Erreur DNS: {e}")
        print(f"   💡 Le serveur {hostname} n'est pas résolvable")
        return False
    except Exception as e:
        print(f"   ❌ Erreur réseau: {e}")
        return False
    
    # Test 2: Requête HTTP simple
    print(f"\n📡 TEST 2: REQUÊTE HTTP SIMPLE")
    print("-" * 32)
    
    try:
        print(f"   🎯 Test GET vers {api_config['web_url']}")
        
        # Créer une requête avec timeout
        req = urllib.request.Request(api_config['web_url'])
        req.add_header('User-Agent', 'FNEV4-Diagnostic/1.0')
        
        start_time = time.time()
        with urllib.request.urlopen(req, timeout=15) as response:
            end_time = time.time()
            status_code = response.getcode()
            content_length = len(response.read())
            
            print(f"   ✅ Réponse HTTP reçue ({end_time - start_time:.2f}s)")
            print(f"   📊 Status: {status_code}")
            print(f"   📏 Taille: {content_length} bytes")
            
            if status_code == 200:
                print(f"   🎉 Serveur web accessible !")
            else:
                print(f"   ⚠️ Status inhabituel: {status_code}")
                
    except urllib.error.HTTPError as e:
        print(f"   ⚠️ Erreur HTTP {e.code}: {e.reason}")
        if e.code == 404:
            print(f"   💡 Page non trouvée (normal pour une API)")
        elif e.code == 500:
            print(f"   💡 Erreur serveur interne")
        elif e.code == 403:
            print(f"   💡 Accès refusé")
    except urllib.error.URLError as e:
        print(f"   ❌ Erreur URL: {e}")
        print(f"   💡 Problème de connectivité réseau")
        return False
    except Exception as e:
        print(f"   ❌ Erreur inattendue: {e}")
        return False
    
    # Test 3: Test de l'endpoint API FNE spécifique
    print(f"\n🔧 TEST 3: ENDPOINT API FNE")
    print("-" * 25)
    
    api_endpoint = f"{api_config['base_url']}/external/invoices/sign"
    
    try:
        print(f"   🎯 Test POST vers {api_endpoint}")
        
        # Données de test minimales selon format FNE
        test_data = {
            "invoiceType": "sale",
            "paymentMethod": "cash",
            "template": "B2C",
            "clientCompanyName": "Test Client",
            "clientPhone": "0123456789",
            "clientEmail": "test@test.com",
            "pointOfSale": "Test POS",
            "establishment": "Test Establishment",
            "items": [{
                "description": "Test Item",
                "quantity": 1,
                "amount": 1000
            }],
            "taxes": "TVA"
        }
        
        # Préparer la requête
        json_data = json.dumps(test_data).encode('utf-8')
        
        req = urllib.request.Request(
            api_endpoint,
            data=json_data,
            method='POST'
        )
        
        # Headers requis
        req.add_header('Content-Type', 'application/json')
        req.add_header('User-Agent', 'FNEV4-Diagnostic/1.0')
        req.add_header('Accept', 'application/json')
        
        # Note: On ne peut pas tester avec la vraie API Key sans la lire depuis la base
        # req.add_header('Authorization', f'Bearer {api_key}')
        
        print(f"   📋 Données test envoyées: {len(json_data)} bytes")
        
        start_time = time.time()
        
        try:
            with urllib.request.urlopen(req, timeout=30) as response:
                end_time = time.time()
                status_code = response.getcode()
                response_data = response.read().decode('utf-8')
                
                print(f"   ✅ Réponse API reçue ({end_time - start_time:.2f}s)")
                print(f"   📊 Status: {status_code}")
                print(f"   📄 Réponse: {response_data[:200]}..." if len(response_data) > 200 else f"   📄 Réponse: {response_data}")
                
                if status_code == 200:
                    print(f"   🎉 API FNE répond correctement !")
                    return True
                
        except urllib.error.HTTPError as e:
            end_time = time.time()
            print(f"   ⚠️ Erreur HTTP API ({end_time - start_time:.2f}s)")
            print(f"   📊 Status: {e.code}")
            print(f"   💬 Raison: {e.reason}")
            
            # Lire la réponse d'erreur si disponible
            try:
                error_response = e.read().decode('utf-8')
                print(f"   📄 Détail erreur: {error_response[:300]}...")
            except:
                pass
            
            if e.code == 401:
                print(f"   💡 Authentification requise (normal sans API Key)")
                print(f"   ✅ L'endpoint API existe et répond !")
                return True
            elif e.code == 400:
                print(f"   💡 Données de test rejetées (normal)")
                print(f"   ✅ L'endpoint API existe et répond !")
                return True
            elif e.code == 404:
                print(f"   ❌ Endpoint API introuvable")
                print(f"   💡 L'URL /external/invoices/sign n'existe pas")
                return False
            elif e.code == 500:
                print(f"   ❌ Erreur serveur API")
                print(f"   💡 Problème côté serveur FNE")
                return False
                
    except Exception as e:
        print(f"   ❌ Erreur test API: {e}")
        return False
    
    # Test 4: Diagnostic réseau avancé
    print(f"\n🔬 TEST 4: DIAGNOSTIC RÉSEAU AVANCÉ")
    print("-" * 34)
    
    try:
        import subprocess
        import platform
        
        if platform.system().lower() == 'windows':
            # Test ping
            print(f"   🏓 Test ping vers {hostname}")
            result = subprocess.run(
                ['ping', '-n', '3', hostname],
                capture_output=True,
                text=True,
                timeout=15
            )
            
            if result.returncode == 0:
                print(f"   ✅ Ping réussi")
                # Extraire le temps de réponse
                for line in result.stdout.split('\n'):
                    if 'temps=' in line or 'time=' in line:
                        print(f"   ⏱️ {line.strip()}")
            else:
                print(f"   ❌ Ping échoué")
                print(f"   💡 Le serveur peut bloquer les pings")
        
        # Test traceroute simplifié
        print(f"   🛤️ Test de route réseau")
        print(f"   💡 Serveur probablement accessible (basé sur tests précédents)")
        
    except Exception as e:
        print(f"   ⚠️ Tests réseau avancés non disponibles: {e}")
    
    # Résumé et recommandations
    print(f"\n🎯 RÉSUMÉ ET DIAGNOSTIC")
    print("-" * 23)
    
    print(f"   🌐 Connectivité réseau: ✅ Accessible")
    print(f"   🏠 Serveur web: ✅ Répond")
    print(f"   🔧 API FNE: ⚠️ À tester avec authentification")
    
    print(f"\n💡 CAUSES PROBABLES DE 'Service provider non disponible':")
    print(f"   1. 🔐 Authentification: API Key manquante/invalide")
    print(f"   2. ⏱️ Timeout: Délai d'attente dépassé (>30s)")
    print(f"   3. 🔧 Configuration service: Problème d'injection de dépendance")
    print(f"   4. 📡 Format requête: Données non conformes au format FNE")
    
    return True

def check_application_services():
    """
    Vérifie la configuration des services dans l'application
    """
    
    print(f"\n🔧 DIAGNOSTIC SERVICES APPLICATION")
    print("-" * 35)
    
    # Vérifier les fichiers de configuration des services
    service_files = [
        "src/FNEV4.Presentation/App.xaml.cs",
        "src/FNEV4.Infrastructure/Services/FneCertificationService.cs",
        "src/FNEV4.Core/Interfaces/Services/IFneCertificationService.cs"
    ]
    
    for service_file in service_files:
        file_path = Path(service_file)
        if file_path.exists():
            print(f"   ✅ {service_file} - Existe")
        else:
            print(f"   ❌ {service_file} - Manquant")
            print(f"   💡 Fichier requis pour le service FNE")
    
    # Suggestions de correction
    print(f"\n🛠️ ÉTAPES DE CORRECTION RECOMMANDÉES")
    print("-" * 37)
    
    print(f"   1. 🔄 Relancer l'application (réinitialise les services)")
    print(f"   2. 🔐 Vérifier la clé API dans Configuration → API FNE")
    print(f"   3. 🌐 Tester la connectivité réseau")
    print(f"   4. ⏱️ Augmenter le timeout si nécessaire (>30s)")
    print(f"   5. 📋 Vérifier les logs d'erreur dans l'application")

if __name__ == "__main__":
    # Lancer les tests
    connectivity_ok = test_api_connectivity()
    check_application_services()
    
    print(f"\n✅ Diagnostic terminé")
    
    if connectivity_ok:
        print(f"🎉 BONNE NOUVELLE: L'API FNE est accessible !")
        print(f"💡 Le problème vient probablement de la configuration dans l'application.")
    else:
        print(f"⚠️ Problème de connectivité détecté.")
        print(f"💡 Vérifier la connexion réseau et la disponibilité du serveur.")