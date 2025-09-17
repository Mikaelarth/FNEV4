import sqlite3
import os
from datetime import datetime

def add_certification_columns():
    """Ajoute les colonnes de certification FNE manquantes à la table FneInvoices"""
    
    db_path = "data/FNEV4.db"
    
    if not os.path.exists(db_path):
        print(f"❌ Base de données non trouvée : {db_path}")
        return False
    
    print(f"🔧 Migration de la base de données : {db_path}")
    print("=" * 80)
    
    # Colonnes à ajouter
    columns_to_add = [
        {
            'name': 'FneCertificationNumber',
            'type': 'TEXT',
            'description': 'Numéro de certificat FNE'
        },
        {
            'name': 'FneCertificationDate', 
            'type': 'TEXT',
            'description': 'Date de certification FNE retournée par l\'API'
        },
        {
            'name': 'FneQrCode',
            'type': 'TEXT',
            'description': 'Code QR de certification FNE'
        },
        {
            'name': 'FneDigitalSignature',
            'type': 'TEXT', 
            'description': 'Signature numérique FNE'
        },
        {
            'name': 'FneValidationUrl',
            'type': 'TEXT',
            'description': 'URL complète de validation avec token'
        },
        {
            'name': 'IsCertified',
            'type': 'INTEGER',
            'description': 'Indique si la facture est certifiée FNE',
            'default': '0'
        }
    ]
    
    try:
        # Faire une sauvegarde
        backup_path = f"data/FNEV4_backup_{datetime.now().strftime('%Y%m%d_%H%M%S')}.db"
        print(f"📋 Création de la sauvegarde : {backup_path}")
        
        import shutil
        shutil.copy2(db_path, backup_path)
        print("✅ Sauvegarde créée avec succès")
        
        # Connexion à la base de données
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Vérifier les colonnes existantes
        cursor.execute("PRAGMA table_info(FneInvoices)")
        existing_columns = [col[1] for col in cursor.fetchall()]
        
        success_count = 0
        skip_count = 0
        
        print("\n🔨 AJOUT DES COLONNES:")
        print("-" * 40)
        
        for col in columns_to_add:
            col_name = col['name']
            col_type = col['type']
            description = col['description']
            
            if col_name in existing_columns:
                print(f"  ⏭️  {col_name} - Déjà présente")
                skip_count += 1
                continue
            
            try:
                # Construire la requête ALTER TABLE
                sql = f"ALTER TABLE FneInvoices ADD COLUMN {col_name} {col_type}"
                
                # Ajouter une valeur par défaut si spécifiée
                if 'default' in col:
                    sql += f" DEFAULT {col['default']}"
                
                cursor.execute(sql)
                print(f"  ✅ {col_name} - Ajoutée ({description})")
                success_count += 1
                
            except Exception as e:
                print(f"  ❌ {col_name} - Erreur: {e}")
        
        # Valider les changements
        conn.commit()
        
        print(f"\n📊 RÉSULTATS:")
        print("-" * 40)
        print(f"  ✅ Colonnes ajoutées: {success_count}")
        print(f"  ⏭️  Colonnes existantes: {skip_count}")
        print(f"  📁 Sauvegarde: {backup_path}")
        
        # Vérifier la nouvelle structure
        print(f"\n🔍 VÉRIFICATION DE LA STRUCTURE MISE À JOUR:")
        print("-" * 40)
        cursor.execute("PRAGMA table_info(FneInvoices)")
        columns = cursor.fetchall()
        
        certification_columns = []
        for col in columns:
            col_name = col[1]
            if any(cert_col['name'] == col_name for cert_col in columns_to_add):
                certification_columns.append(col_name)
        
        for cert_col in certification_columns:
            print(f"  ✅ {cert_col}")
        
        conn.close()
        
        print(f"\n🎉 MIGRATION TERMINÉE AVEC SUCCÈS!")
        return True
        
    except Exception as e:
        print(f"❌ Erreur lors de la migration : {e}")
        return False

def verify_migration():
    """Vérifie que la migration s'est bien déroulée"""
    
    db_path = "data/FNEV4.db"
    
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        print(f"\n✅ VÉRIFICATION POST-MIGRATION:")
        print("-" * 40)
        
        # Test d'une requête simple avec les nouvelles colonnes
        cursor.execute("""
            SELECT COUNT(*) as total,
                   SUM(CASE WHEN IsCertified = 1 THEN 1 ELSE 0 END) as certified
            FROM FneInvoices
        """)
        
        result = cursor.fetchone()
        total = result[0]
        certified = result[1]
        
        print(f"  📊 Total des factures: {total}")
        print(f"  📋 Factures certifiées: {certified}")
        print(f"  ✅ Les nouvelles colonnes fonctionnent correctement")
        
        conn.close()
        return True
        
    except Exception as e:
        print(f"❌ Erreur lors de la vérification : {e}")
        return False

if __name__ == "__main__":
    print("🚀 MIGRATION DES COLONNES DE CERTIFICATION FNE")
    print("=" * 80)
    
    if add_certification_columns():
        if verify_migration():
            print("\n🎯 Migration réussie ! L'application peut maintenant utiliser les fonctionnalités de certification.")
        else:
            print("\n⚠️  Migration effectuée mais vérification échouée.")
    else:
        print("\n❌ Échec de la migration.")