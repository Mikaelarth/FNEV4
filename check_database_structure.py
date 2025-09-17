import sqlite3
import os

def check_database_structure():
    """Vérifie la structure de la base de données FNEV4"""
    
    # Chemin vers la base de données
    db_path = "data/FNEV4.db"
    
    if not os.path.exists(db_path):
        print(f"❌ Base de données non trouvée : {db_path}")
        return
    
    print(f"🔍 Examination de la base de données : {db_path}")
    print("=" * 80)
    
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Lister toutes les tables
        cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
        tables = cursor.fetchall()
        
        print("📋 TABLES DISPONIBLES:")
        print("-" * 40)
        for table in tables:
            print(f"  • {table[0]}")
        print()
        
        # Vérifier spécifiquement la table FneInvoices
        if any('FneInvoices' in table[0] for table in tables):
            print("🔎 STRUCTURE DE LA TABLE FneInvoices:")
            print("-" * 40)
            cursor.execute("PRAGMA table_info(FneInvoices)")
            columns = cursor.fetchall()
            
            certification_columns = []
            for col in columns:
                col_name = col[1]  # nom de la colonne
                col_type = col[2]  # type de la colonne
                is_nullable = "NULL" if col[3] == 0 else "NOT NULL"
                
                print(f"  • {col_name:<30} {col_type:<15} {is_nullable}")
                
                # Chercher les colonnes liées à la certification
                if 'certif' in col_name.lower() or 'fne' in col_name.lower():
                    certification_columns.append(col_name)
            
            print()
            print("🎯 COLONNES LIÉES À LA CERTIFICATION FNE:")
            print("-" * 40)
            if certification_columns:
                for col in certification_columns:
                    print(f"  ✅ {col}")
            else:
                print("  ❌ Aucune colonne de certification trouvée")
            print()
            
            # Compter les enregistrements
            cursor.execute("SELECT COUNT(*) FROM FneInvoices")
            count = cursor.fetchone()[0]
            print(f"📊 Nombre d'enregistrements dans FneInvoices: {count}")
            
            # Afficher quelques exemples d'enregistrements
            if count > 0:
                print("\n📝 EXEMPLE D'ENREGISTREMENTS (5 premiers):")
                print("-" * 40)
                cursor.execute("SELECT Id, InvoiceNumber, InvoiceDate, Status LIMIT 5")
                records = cursor.fetchall()
                for record in records:
                    print(f"  • ID: {record[0]}, N°: {record[1]}, Date: {record[2]}, Statut: {record[3]}")
        else:
            print("❌ Table FneInvoices non trouvée")
        
        # Vérifier la table FneConfigurations
        print("\n🔧 CONFIGURATION FNE:")
        print("-" * 40)
        if any('FneConfigurations' in table[0] for table in tables):
            cursor.execute("SELECT COUNT(*) FROM FneConfigurations WHERE IsActive = 1 AND IsDeleted = 0")
            active_configs = cursor.fetchone()[0]
            print(f"  ✅ Configurations FNE actives: {active_configs}")
            
            if active_configs > 0:
                cursor.execute("""
                    SELECT ConfigurationName, Environment, BaseUrl, IsValidatedByDgi 
                    FROM FneConfigurations 
                    WHERE IsActive = 1 AND IsDeleted = 0
                    LIMIT 3
                """)
                configs = cursor.fetchall()
                for config in configs:
                    print(f"    • {config[0]} ({config[1]}) - {config[2]} - Validé DGI: {config[3]}")
        else:
            print("  ❌ Table FneConfigurations non trouvée")
            
    except Exception as e:
        print(f"❌ Erreur lors de l'examination : {e}")
    finally:
        conn.close()

def check_needed_columns():
    """Vérifie quelles colonnes de certification sont manquantes"""
    
    expected_columns = [
        'FneCertificationNumber',
        'FneCertificationDate', 
        'FneQrCode',
        'FneDigitalSignature',
        'FneValidationUrl',
        'IsCertified',
        'CertifiedAt'
    ]
    
    db_path = "data/FNEV4.db"
    
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Obtenir la structure actuelle
        cursor.execute("PRAGMA table_info(FneInvoices)")
        existing_columns = [col[1] for col in cursor.fetchall()]
        
        print("\n🔍 ANALYSE DES COLONNES DE CERTIFICATION:")
        print("-" * 40)
        
        missing_columns = []
        for col in expected_columns:
            if col in existing_columns:
                print(f"  ✅ {col} - Présente")
            else:
                print(f"  ❌ {col} - Manquante")
                missing_columns.append(col)
        
        if missing_columns:
            print(f"\n⚠️  COLONNES MANQUANTES ({len(missing_columns)}):")
            for col in missing_columns:
                print(f"    • {col}")
        else:
            print("\n✅ Toutes les colonnes de certification sont présentes")
            
        conn.close()
        return missing_columns
        
    except Exception as e:
        print(f"❌ Erreur lors de la vérification : {e}")
        return []

if __name__ == "__main__":
    print("🔧 DIAGNOSTIC DE LA BASE DE DONNÉES FNEV4")
    print("=" * 80)
    
    check_database_structure()
    missing_cols = check_needed_columns()
    
    if missing_cols:
        print(f"\n🚨 ACTION REQUISE: {len(missing_cols)} colonnes doivent être ajoutées")
    else:
        print("\n✅ STRUCTURE DE BASE CORRECTE")