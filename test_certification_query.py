#!/usr/bin/env python3
"""
Script de test pour vérifier que notre nouvelle méthode GetAvailableForCertificationAsync
fonctionne correctement avec la vraie structure de la base de données.
"""

import sqlite3
import os

def test_new_query_logic():
    """Teste la logique de la nouvelle méthode GetAvailableForCertificationAsync"""
    db_path = "D:/PROJET/FNE/FNEV4/data/FNEV4.db"
    if not os.path.exists(db_path):
        print("Base de données non trouvée")
        return False
    
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    try:
        # Test de la requête équivalente à GetAvailableForCertificationAsync
        # WHERE f.Status == "draft" && f.CertifiedAt == null
        query = """
        SELECT 
            COUNT(*) as count,
            InvoiceNumber,
            ClientCode,
            TotalAmountTTC,
            Status,
            CertifiedAt
        FROM FneInvoices 
        WHERE Status = 'draft' AND CertifiedAt IS NULL
        GROUP BY Status
        """
        
        cursor.execute(query)
        result = cursor.fetchone()
        
        if result:
            count = result[0]
            print(f"✓ Requête GetAvailableForCertificationAsync: {count} factures trouvées")
            print(f"  Statut: {result[4] or 'NULL'}")
            print(f"  CertifiedAt: {result[5] or 'NULL'}")
            
            if count > 0:
                # Obtenir quelques exemples
                cursor.execute("""
                SELECT InvoiceNumber, ClientCode, TotalAmountTTC, CreatedAt
                FROM FneInvoices 
                WHERE Status = 'draft' AND CertifiedAt IS NULL
                ORDER BY CreatedAt DESC
                LIMIT 5
                """)
                
                samples = cursor.fetchall()
                print(f"  Échantillon de {len(samples)} factures:")
                for sample in samples:
                    print(f"    - {sample[0]} | Code client: {sample[1]} | Montant: {sample[2]}€")
                
                conn.close()
                return True
            else:
                print("  ⚠ Aucune facture disponible selon les critères")
                conn.close()
                return False
        else:
            print("✗ Aucun résultat de la requête")
            conn.close()
            return False
            
    except Exception as e:
        print(f"✗ Erreur lors du test: {e}")
        conn.close()
        return False

def check_current_data_state():
    """Vérifie l'état actuel des données pour comprendre pourquoi elles ne s'affichent pas"""
    db_path = "D:/PROJET/FNE/FNEV4/data/FNEV4.db"
    if not os.path.exists(db_path):
        print("Base de données non trouvée")
        return
    
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    try:
        # Vérifier les différentes conditions de certification
        print("ANALYSE DE L'ÉTAT DES DONNÉES:")
        
        # Total des factures
        cursor.execute("SELECT COUNT(*) FROM FneInvoices")
        total = cursor.fetchone()[0]
        print(f"Total factures: {total}")
        
        # Par statut
        cursor.execute("SELECT Status, COUNT(*) FROM FneInvoices GROUP BY Status")
        statuses = cursor.fetchall()
        for status, count in statuses:
            print(f"  {status}: {count} factures")
        
        # Par CertifiedAt
        cursor.execute("SELECT COUNT(*) FROM FneInvoices WHERE CertifiedAt IS NULL")
        not_certified = cursor.fetchone()[0]
        cursor.execute("SELECT COUNT(*) FROM FneInvoices WHERE CertifiedAt IS NOT NULL")
        certified = cursor.fetchone()[0]
        print(f"Non certifiées (CertifiedAt IS NULL): {not_certified}")
        print(f"Certifiées (CertifiedAt IS NOT NULL): {certified}")
        
        # Combinaison draft + non certifiées
        cursor.execute("SELECT COUNT(*) FROM FneInvoices WHERE Status = 'draft' AND CertifiedAt IS NULL")
        available_for_cert = cursor.fetchone()[0]
        print(f"Disponibles pour certification (draft + CertifiedAt NULL): {available_for_cert}")
        
        conn.close()
        
    except Exception as e:
        print(f"Erreur: {e}")
        conn.close()

if __name__ == "__main__":
    print("=== TEST DE LA NOUVELLE LOGIQUE DE CERTIFICATION ===")
    
    print("\n1. ÉTAT ACTUEL DES DONNÉES:")
    check_current_data_state()
    
    print("\n2. TEST DE LA NOUVELLE REQUÊTE:")
    success = test_new_query_logic()
    
    if success:
        print("\n✓ La logique semble correcte. Les corrections devraient fonctionner.")
        print("📝 Fermez l'application FNEV4 et recompilez pour tester les changements.")
    else:
        print("\n⚠ Il pourrait y avoir un problème avec les données ou la logique.")
        print("🔍 Vérifiez les critères de filtrage dans le repository.")