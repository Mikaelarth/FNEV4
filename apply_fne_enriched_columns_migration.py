#!/usr/bin/env python3
"""
Script d'application de la migration FNE enrichie
Ajoute les nouvelles colonnes pour exploiter toutes les données de l'API FNE DGI
Auteur: Assistant IA
Date: 17/09/2025 23:58
"""

import sqlite3
import os
import sys
from datetime import datetime

def apply_fne_enriched_columns_migration():
    """Applique la migration pour ajouter les colonnes FNE enrichies"""
    
    # Chemin vers la base de données
    db_path = os.path.join(os.path.dirname(__file__), 'data', 'FNEV4.db')
    
    if not os.path.exists(db_path):
        print(f"❌ Base de données non trouvée: {db_path}")
        return False
        
    print(f"🔍 Application de la migration FNE enrichie sur: {db_path}")
    
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Vérifier d'abord si les colonnes existent déjà
        cursor.execute("PRAGMA table_info(FneInvoices)")
        columns = [column[1] for column in cursor.fetchall()]
        
        print(f"📋 Colonnes existantes dans FneInvoices: {len(columns)}")
        
        # Liste des nouvelles colonnes à ajouter
        new_columns = [
            ("FneStickerBalance", "INTEGER"),
            ("FneInvoiceId", "TEXT"),
            ("FneDownloadUrl", "TEXT"),
            ("FneCertifiedInvoiceDetails", "TEXT"),
            ("FneHasWarning", "INTEGER DEFAULT 0"),
            ("FneWarningMessage", "TEXT"),
            ("FneCompanyNcc", "TEXT"),
            ("FnePublicVerificationToken", "TEXT")
        ]
        
        added_columns = []
        
        for column_name, column_type in new_columns:
            if column_name not in columns:
                try:
                    sql = f"ALTER TABLE FneInvoices ADD COLUMN {column_name} {column_type}"
                    print(f"➕ Ajout colonne: {column_name} ({column_type})")
                    cursor.execute(sql)
                    added_columns.append(column_name)
                except sqlite3.Error as e:
                    print(f"⚠️  Erreur ajout {column_name}: {e}")
            else:
                print(f"✅ Colonne {column_name} existe déjà")
        
        # Valider les changements
        conn.commit()
        
        # Vérifier le résultat
        cursor.execute("PRAGMA table_info(FneInvoices)")
        final_columns = [column[1] for column in cursor.fetchall()]
        
        print(f"✅ Migration terminée !")
        print(f"📊 Colonnes totales après migration: {len(final_columns)}")
        print(f"🆕 Nouvelles colonnes ajoutées: {len(added_columns)}")
        
        if added_columns:
            print(f"📝 Colonnes ajoutées:")
            for col in added_columns:
                print(f"   - {col}")
        
        # Test de la structure étendue
        cursor.execute("""
            SELECT COUNT(*) as total_invoices,
                   COUNT(FneReference) as with_reference,
                   COUNT(VerificationToken) as with_token,
                   COUNT(FneStickerBalance) as with_balance
            FROM FneInvoices
        """)
        
        stats = cursor.fetchone()
        if stats:
            print(f"📈 Statistiques après migration:")
            print(f"   - Total factures: {stats[0]}")
            print(f"   - Avec référence FNE: {stats[1]}")
            print(f"   - Avec token vérification: {stats[2]}")
            print(f"   - Nouvelles données balance: {stats[3]}")
        
        conn.close()
        return True
        
    except sqlite3.Error as e:
        print(f"❌ Erreur SQLite: {e}")
        return False
    except Exception as e:
        print(f"❌ Erreur inattendue: {e}")
        return False

def main():
    """Point d'entrée principal"""
    print("=" * 60)
    print("🚀 MIGRATION FNE ENRICHIE - EXPLOITATION MAXIMALE API DGI")
    print("=" * 60)
    
    success = apply_fne_enriched_columns_migration()
    
    print("=" * 60)
    if success:
        print("✅ MIGRATION RÉUSSIE!")
        print("🎯 Le système peut maintenant exploiter toutes les données FNE:")
        print("   • Balance de stickers en temps réel")
        print("   • ID facture FNE pour les avoirs")
        print("   • URL de téléchargement PDF certifié")
        print("   • Détails complets facture certifiée")
        print("   • Warnings et alertes DGI")
        print("   • Token de vérification publique")
        print("   • NCC entreprise retourné par l'API")
    else:
        print("❌ ÉCHEC DE LA MIGRATION")
        sys.exit(1)

if __name__ == "__main__":
    main()