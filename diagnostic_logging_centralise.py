#!/usr/bin/env python3
"""
Outil de diagnostic avancé du système de logging centralisé FNEV4
Surveille les logs en temps réel et diagnostique les problèmes de chargement des données
"""

import sqlite3
import os
import time
import sys
from datetime import datetime, timedelta
from pathlib import Path

def find_fnev4_paths():
    """Trouve automatiquement les chemins de données de FNEV4"""
    base_path = Path("d:/PROJET/FNE/FNEV4")
    
    # Chemins possibles pour la base de données
    db_paths = [
        base_path / "Data" / "FNEV4.db",
        base_path / "data" / "FNEV4.db", 
        base_path / "src" / "FNEV4.Presentation" / "bin" / "Debug" / "net8.0-windows" / "Data" / "FNEV4.db",
    ]
    
    # Chemins possibles pour les logs
    logs_paths = [
        base_path / "Data" / "Logs",
        base_path / "data" / "Logs",
        base_path / "src" / "FNEV4.Presentation" / "bin" / "Debug" / "net8.0-windows" / "Data" / "Logs",
        base_path / "Logs",
    ]
    
    db_path = None
    logs_path = None
    
    for path in db_paths:
        if path.exists():
            db_path = str(path)
            break
            
    for path in logs_paths:
        if path.exists():
            logs_path = str(path)
            break
    
    return db_path, logs_path

def get_latest_log_entries(db_path, category=None, minutes=5):
    """Récupère les dernières entrées de logs depuis la base de données"""
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Calculer le timestamp de début (dernières N minutes)
        since_time = datetime.now() - timedelta(minutes=minutes)
        since_timestamp = since_time.strftime('%Y-%m-%d %H:%M:%S')
        
        query = """
            SELECT Timestamp, Level, Category, Message, ExceptionDetails 
            FROM LogEntries 
            WHERE Timestamp >= ?
        """
        params = [since_timestamp]
        
        if category:
            query += " AND Category LIKE ?"
            params.append(f"%{category}%")
            
        query += " ORDER BY Timestamp DESC LIMIT 100"
        
        cursor.execute(query, params)
        logs = cursor.fetchall()
        conn.close()
        
        return logs
    except Exception as e:
        print(f"Erreur lecture logs BDD: {e}")
        return []

def get_latest_file_logs(logs_path, minutes=5):
    """Récupère les dernières entrées depuis les fichiers de logs"""
    try:
        today_log = Path(logs_path) / f"FNEV4_{datetime.now().strftime('%Y%m%d')}.log"
        
        if not today_log.exists():
            return []
        
        # Lire les dernières lignes du fichier
        with open(today_log, 'r', encoding='utf-8') as f:
            lines = f.readlines()
            
        # Filtrer les dernières minutes
        recent_lines = []
        since_time = datetime.now() - timedelta(minutes=minutes)
        
        for line in lines[-200:]:  # Prendre les 200 dernières lignes max
            if line.strip():
                try:
                    # Extraire le timestamp de la ligne de log
                    timestamp_str = line.split(']')[0][1:]  # Enlever le premier [
                    log_time = datetime.strptime(timestamp_str, '%Y-%m-%d %H:%M:%S.%f')
                    
                    if log_time >= since_time:
                        recent_lines.append(line.strip())
                except:
                    continue
        
        return recent_lines
    except Exception as e:
        print(f"Erreur lecture fichier logs: {e}")
        return []

def analyze_certification_logs(db_path):
    """Analyse spécifiquement les logs liés à la certification"""
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Chercher tous les logs liés à la certification dans les dernières heures
        since_time = datetime.now() - timedelta(hours=2)
        since_timestamp = since_time.strftime('%Y-%m-%d %H:%M:%S')
        
        query = """
            SELECT Timestamp, Level, Category, Message, ExceptionDetails 
            FROM LogEntries 
            WHERE Timestamp >= ? 
            AND (
                Category LIKE '%Certification%' OR 
                Message LIKE '%certification%' OR
                Message LIKE '%Certification%' OR
                Message LIKE '%facture%' OR
                Message LIKE '%FneInvoice%' OR
                Message LIKE '%LoadAvailable%' OR
                Message LIKE '%GetAvailable%'
            )
            ORDER BY Timestamp ASC
        """
        
        cursor.execute(query, [since_timestamp])
        logs = cursor.fetchall()
        conn.close()
        
        return logs
    except Exception as e:
        print(f"Erreur analyse logs certification: {e}")
        return []

def monitor_logs_realtime(db_path, logs_path, duration_seconds=60):
    """Monitore les logs en temps réel"""
    print(f"🔍 MONITORING DES LOGS EN TEMPS RÉEL ({duration_seconds}s)")
    print("=" * 70)
    
    start_time = datetime.now()
    last_check = start_time
    
    while (datetime.now() - start_time).seconds < duration_seconds:
        # Vérifier les nouveaux logs depuis la dernière vérification
        new_db_logs = get_latest_log_entries(db_path, minutes=1)
        
        current_time = datetime.now()
        
        # Afficher seulement les logs très récents (dernière minute)
        recent_logs = [log for log in new_db_logs 
                      if datetime.strptime(log[0], '%Y-%m-%d %H:%M:%S.%f') > last_check]
        
        for log in recent_logs:
            timestamp, level, category, message, exception = log
            level_icon = {"Error": "❌", "Warning": "⚠️", "Info": "ℹ️", "Debug": "🐛"}.get(level, "📝")
            print(f"{level_icon} [{timestamp}] [{category}] {message}")
            if exception:
                print(f"    Exception: {exception[:100]}...")
        
        last_check = current_time
        time.sleep(2)  # Vérifier toutes les 2 secondes
    
    print(f"\n✅ Monitoring terminé après {duration_seconds}s")

def main():
    print("=" * 70)
    print("DIAGNOSTIC AVANCÉ DU SYSTÈME DE LOGGING CENTRALISÉ FNEV4")
    print("=" * 70)
    print(f"Heure: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print()
    
    # 1. Découvrir les chemins
    db_path, logs_path = find_fnev4_paths()
    
    print("📍 LOCALISATION DES FICHIERS")
    print(f"   Base de données: {db_path or 'NON TROUVÉE'}")
    print(f"   Dossier logs: {logs_path or 'NON TROUVÉ'}")
    print()
    
    if not db_path:
        print("❌ Impossible de localiser la base de données")
        return
    
    # 2. Analyser les logs de certification
    print("🎯 ANALYSE DES LOGS DE CERTIFICATION")
    cert_logs = analyze_certification_logs(db_path)
    
    if cert_logs:
        print(f"   Trouvé {len(cert_logs)} entrées liées à la certification:")
        for log in cert_logs[-10:]:  # Les 10 dernières
            timestamp, level, category, message, exception = log
            level_icon = {"Error": "❌", "Warning": "⚠️", "Info": "ℹ️", "Debug": "🐛"}.get(level, "📝")
            print(f"   {level_icon} [{timestamp}] [{category}] {message[:80]}...")
            if exception:
                print(f"      Exception: {exception[:100]}...")
    else:
        print("   ⚠️ Aucun log de certification trouvé dans les 2 dernières heures")
    print()
    
    # 3. Vérifier les logs récents
    print("📊 LOGS RÉCENTS (5 dernières minutes)")
    recent_logs = get_latest_log_entries(db_path, minutes=5)
    
    if recent_logs:
        print(f"   {len(recent_logs)} entrées récentes:")
        for log in recent_logs[:5]:  # Les 5 plus récentes
            timestamp, level, category, message, exception = log
            level_icon = {"Error": "❌", "Warning": "⚠️", "Info": "ℹ️", "Debug": "🐛"}.get(level, "📝")
            print(f"   {level_icon} [{category}] {message[:60]}...")
    else:
        print("   ⚠️ Aucun log récent trouvé")
    print()
    
    # 4. Vérifier les fichiers de logs
    if logs_path:
        print("📄 FICHIERS DE LOGS")
        log_files = list(Path(logs_path).glob("FNEV4_*.log"))
        log_files.sort(key=lambda x: x.stat().st_mtime, reverse=True)
        
        for log_file in log_files[:3]:  # Les 3 plus récents
            stat = log_file.stat()
            size_kb = stat.st_size / 1024
            mod_time = datetime.fromtimestamp(stat.st_mtime)
            print(f"   📝 {log_file.name}: {size_kb:.1f} KB (modifié: {mod_time})")
        
        # Analyser le fichier le plus récent
        if log_files:
            recent_file_logs = get_latest_file_logs(logs_path, minutes=5)
            if recent_file_logs:
                print(f"\n   Dernières entrées du fichier ({len(recent_file_logs)}):")
                for line in recent_file_logs[-3:]:
                    print(f"   {line[:80]}...")
        print()
    
    # 5. Proposer le monitoring en temps réel
    response = input("🔍 Voulez-vous monitorer les logs en temps réel ? (o/N): ").strip().lower()
    if response == 'o':
        duration = input("Durée en secondes (défaut: 60): ").strip()
        try:
            duration = int(duration) if duration else 60
        except:
            duration = 60
        
        print(f"\n🚀 Lancement du monitoring pour {duration}s...")
        print("💡 Conseil: Maintenant, naviguez vers Certification FNE -> Certification manuelle dans l'app")
        print()
        
        monitor_logs_realtime(db_path, logs_path, duration)
    
    print("\n" + "=" * 70)
    print("💡 RECOMMANDATIONS POUR LE DÉBOGAGE:")
    print("   1. Utilisez ce script avec monitoring en temps réel")
    print("   2. Naviguez vers l'interface pendant le monitoring")
    print("   3. Recherchez les messages d'erreur ou d'exception")
    print("   4. Vérifiez les logs de catégorie 'CertificationManuelle'")
    print("=" * 70)

if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n\n⏹️ Arrêt du monitoring par l'utilisateur")
    except Exception as e:
        print(f"\n❌ Erreur: {e}")
        import traceback
        traceback.print_exc()