import os
import json

def find_real_database():
    print("=== RECHERCHE DE LA VRAIE BASE DE DONNÉES FNEV4 ===\n")
    
    # 1. Chercher dans le répertoire du projet (mode développement)
    project_db = r"D:\PROJET\FNEV4\data\FNEV4.db"
    if os.path.exists(project_db):
        print(f"✅ Base trouvée (Projet): {project_db}")
        return project_db
    else:
        print(f"❌ Pas de base dans: {project_db}")
    
    # 2. Chercher dans AppData (mode production)
    appdata_path = os.path.join(os.environ.get('LOCALAPPDATA', ''), 'FNEV4', 'FNEV4.db')
    if os.path.exists(appdata_path):
        print(f"✅ Base trouvée (AppData): {appdata_path}")
        return appdata_path
    else:
        print(f"❌ Pas de base dans: {appdata_path}")
    
    # 3. Chercher dans le répertoire de l'exécutable
    exe_path = r"D:\PROJET\FNEV4\src\FNEV4.Presentation\bin\Debug\net8.0-windows\Data\FNEV4.db"
    if os.path.exists(exe_path):
        print(f"✅ Base trouvée (Exécutable): {exe_path}")
        return exe_path
    else:
        print(f"❌ Pas de base dans: {exe_path}")
    
    # 4. Variable d'environnement
    env_path = os.environ.get('FNEV4_DATABASE_PATH')
    if env_path and os.path.exists(env_path):
        print(f"✅ Base trouvée (Environnement): {env_path}")
        return env_path
    elif env_path:
        print(f"❌ Variable d'environnement définie mais fichier inexistant: {env_path}")
    
    print("❌ Aucune base de données trouvée !")
    return None

# Rechercher la base
db_path = find_real_database()

if db_path:
    print(f"\n=== ANALYSE DE LA BASE : {db_path} ===")
    
    import sqlite3
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Compter les factures
        cursor.execute("SELECT COUNT(*) FROM FneInvoices")
        nb_factures = cursor.fetchone()[0]
        print(f"📊 Nombre de factures: {nb_factures}")
        
        # Compter les articles
        cursor.execute("SELECT COUNT(*) FROM FneInvoiceItems")
        nb_items = cursor.fetchone()[0]
        print(f"📦 Nombre d'articles: {nb_items}")
        
        if nb_factures > 0:
            print(f"\n=== EXEMPLE DE FACTURES ===")
            cursor.execute("SELECT InvoiceNumber, TotalAmountTTC, Status FROM FneInvoices LIMIT 5")
            factures = cursor.fetchall()
            for f in factures:
                print(f"  🧾 {f[0]} - {f[1]}€ - {f[2]}")
        
        conn.close()
        
    except Exception as e:
        print(f"❌ Erreur d'accès à la base: {e}")
else:
    print("\n=== CRÉATION D'UNE NOUVELLE BASE ===")
    # Créer le répertoire data dans le projet
    data_dir = r"D:\PROJET\FNEV4\data"
    os.makedirs(data_dir, exist_ok=True)
    print(f"📁 Répertoire créé: {data_dir}")
    print("ℹ️  Une nouvelle base sera créée au prochain lancement de l'application")