#!/usr/bin/env python3
"""
Script pour appliquer la migration des colonnes FNE enrichies
Applique les nouvelles colonnes pour exploiter au maximum les données de l'API FNE
"""
import sqlite3
import os
from datetime import datetime

# Configuration de la base de données
DB_PATH = r"data\FNEV4.db"
ABSOLUTE_DB_PATH = os.path.join(os.getcwd(), DB_PATH)

def apply_enriched_fne_columns():
    """Applique les colonnes FNE enrichies pour exploiter les données API"""
    
    print("🔧 Application de la migration des colonnes FNE enrichies...")
    print(f"📂 Base de données: {ABSOLUTE_DB_PATH}")
    
    if not os.path.exists(ABSOLUTE_DB_PATH):
        print(f"❌ Erreur: Base de données non trouvée à {ABSOLUTE_DB_PATH}")
        return False
    
    try:
        with sqlite3.connect(ABSOLUTE_DB_PATH) as conn:
            cursor = conn.cursor()
            
            # Vérifier si les nouvelles colonnes existent déjà
            cursor.execute("PRAGMA table_info(FneInvoices)")
            columns = [row[1] for row in cursor.fetchall()]
            
            new_columns = {
                'FneQrCodeData': 'TEXT',
                'FneBalanceSticker': 'TEXT',
                'FneCertificationTimestamp': 'TEXT',
                'FneProcessingStatus': 'TEXT',
                'FneCertificationHash': 'TEXT'
            }
            
            # Ajouter les colonnes manquantes
            added_columns = []
            for column_name, column_type in new_columns.items():
                if column_name not in columns:
                    try:
                        cursor.execute(f"ALTER TABLE FneInvoices ADD COLUMN {column_name} {column_type}")
                        added_columns.append(column_name)
                        print(f"✅ Colonne ajoutée: {column_name}")
                    except sqlite3.Error as e:
                        print(f"⚠️ Erreur lors de l'ajout de {column_name}: {e}")
                else:
                    print(f"✅ Colonne {column_name} déjà présente")
            
            conn.commit()
            
            # Vérification finale
            cursor.execute("PRAGMA table_info(FneInvoices)")
            final_columns = [row[1] for row in cursor.fetchall()]
            
            fne_columns = [col for col in final_columns if col.startswith('Fne')]
            print(f"\n📊 Colonnes FNE disponibles ({len(fne_columns)}):")
            for col in sorted(fne_columns):
                print(f"   • {col}")
            
            if added_columns:
                print(f"\n✅ Migration terminée ! {len(added_columns)} nouvelles colonnes ajoutées:")
                for col in added_columns:
                    print(f"   + {col}")
            else:
                print(f"\n✅ Toutes les colonnes FNE enrichies sont déjà présentes!")
            
            return True
            
    except Exception as e:
        print(f"❌ Erreur lors de l'application de la migration: {e}")
        return False

def verify_enriched_columns():
    """Vérifie que toutes les colonnes enrichies sont bien présentes"""
    
    expected_columns = [
        'FneCertificationNumber',
        'FneCertificationDate', 
        'FneQrCode',
        'FneReference',
        'FneProcessedAt',
        'FneValidationUrl',
        'FneStickerBalance',
        'FneInvoiceId',
        'FneDownloadUrl',
        'FneCertifiedInvoiceDetails',
        'FneHasWarning',
        'FneWarningMessage',
        'FneCompanyNcc',
        'FnePublicVerificationToken',
        # Nouvelles colonnes enrichies
        'FneQrCodeData',
        'FneBalanceSticker', 
        'FneCertificationTimestamp',
        'FneProcessingStatus',
        'FneCertificationHash'
    ]
    
    try:
        with sqlite3.connect(ABSOLUTE_DB_PATH) as conn:
            cursor = conn.cursor()
            cursor.execute("PRAGMA table_info(FneInvoices)")
            actual_columns = [row[1] for row in cursor.fetchall()]
            
            missing_columns = [col for col in expected_columns if col not in actual_columns]
            present_columns = [col for col in expected_columns if col in actual_columns]
            
            print(f"\n🔍 Vérification des colonnes FNE enrichies:")
            print(f"✅ Colonnes présentes: {len(present_columns)}/{len(expected_columns)}")
            
            if missing_columns:
                print(f"❌ Colonnes manquantes ({len(missing_columns)}):")
                for col in missing_columns:
                    print(f"   - {col}")
                return False
            else:
                print("🎉 Toutes les colonnes FNE enrichies sont présentes !")
                return True
                
    except Exception as e:
        print(f"❌ Erreur lors de la vérification: {e}")
        return False

if __name__ == "__main__":
    print("🚀 Migration des colonnes FNE enrichies")
    print("=" * 50)
    
    success = apply_enriched_fne_columns()
    
    if success:
        print("\n" + "=" * 50)
        verify_enriched_columns()
        print("\n🎯 Les colonnes FNE sont prêtes pour exploiter toutes les données de l'API DGI !")
        print("   • QR Code data pour affichage visuel")
        print("   • Balance sticker pour validation officielle")  
        print("   • Timestamp de certification DGI")
        print("   • Statut de traitement en temps réel")
        print("   • Hash de certification pour intégrité")
    else:
        print("\n❌ Échec de la migration")
        exit(1)