#!/usr/bin/env python3
"""
Script de diagnostic approfondi pour identifier pourquoi l'interface de certification manuelle
ne charge pas les données malgré leur présence dans la base de données.
"""

import sqlite3
import json
from datetime import datetime
import os

def check_database_connection():
    """Vérifie la connexion aux différentes bases de données"""
    databases = [
        "D:/PROJET/FNE/FNEV4/data/FNEV4.db",
        "D:/PROJET/FNE/FNEV4/fnev4_database.db"
    ]
    
    results = {}
    for db_path in databases:
        if os.path.exists(db_path):
            try:
                conn = sqlite3.connect(db_path)
                cursor = conn.cursor()
                
                # Test basic connection
                cursor.execute("SELECT COUNT(*) FROM sqlite_master WHERE type='table'")
                table_count = cursor.fetchone()[0]
                
                results[db_path] = {
                    "exists": True,
                    "accessible": True,
                    "table_count": table_count,
                    "size_mb": round(os.path.getsize(db_path) / (1024*1024), 2)
                }
                
                conn.close()
            except Exception as e:
                results[db_path] = {
                    "exists": True,
                    "accessible": False,
                    "error": str(e)
                }
        else:
            results[db_path] = {
                "exists": False,
                "accessible": False
            }
    
    return results

def analyze_fne_invoices_structure():
    """Analyse détaillée de la structure et contenu de FneInvoices"""
    db_path = "D:/PROJET/FNE/FNEV4/data/FNEV4.db"
    if not os.path.exists(db_path):
        return {"error": "Database not found"}
    
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    try:
        # Structure de la table
        cursor.execute("PRAGMA table_info(FneInvoices)")
        columns = cursor.fetchall()
        
        # Données par statut
        cursor.execute("""
        SELECT 
            Status,
            COUNT(*) as count,
            MIN(CreatedAt) as earliest,
            MAX(CreatedAt) as latest
        FROM FneInvoices 
        GROUP BY Status
        """)
        status_breakdown = cursor.fetchall()
        
        # Échantillon de données complètes
        cursor.execute("""
        SELECT 
            Id, InvoiceNumber, ClientName, Status, TotalAmount, 
            CreatedAt, CertificationStatus
        FROM FneInvoices 
        WHERE Status = 'draft'
        LIMIT 5
        """)
        sample_data = cursor.fetchall()
        
        # Vérifier les champs critiques pour la certification
        cursor.execute("""
        SELECT 
            COUNT(*) as total,
            COUNT(CASE WHEN InvoiceNumber IS NOT NULL AND InvoiceNumber != '' THEN 1 END) as has_number,
            COUNT(CASE WHEN ClientName IS NOT NULL AND ClientName != '' THEN 1 END) as has_client,
            COUNT(CASE WHEN TotalAmount IS NOT NULL AND TotalAmount > 0 THEN 1 END) as has_amount,
            COUNT(CASE WHEN CertificationStatus IS NULL OR CertificationStatus = 'Pending' THEN 1 END) as certifiable
        FROM FneInvoices 
        WHERE Status = 'draft'
        """)
        certification_readiness = cursor.fetchone()
        
        conn.close()
        
        return {
            "columns": [{"name": col[1], "type": col[2], "nullable": not col[3]} for col in columns],
            "status_breakdown": [{"status": row[0], "count": row[1], "earliest": row[2], "latest": row[3]} for row in status_breakdown],
            "sample_data": [dict(zip(["Id", "InvoiceNumber", "ClientName", "Status", "TotalAmount", "CreatedAt", "CertificationStatus"], row)) for row in sample_data],
            "certification_readiness": {
                "total_draft": certification_readiness[0],
                "has_invoice_number": certification_readiness[1],
                "has_client_name": certification_readiness[2],
                "has_amount": certification_readiness[3],
                "certifiable": certification_readiness[4]
            }
        }
        
    except Exception as e:
        conn.close()
        return {"error": str(e)}

def check_entity_framework_queries():
    """Simule les requêtes Entity Framework pour voir ce qui est retourné"""
    db_path = "D:/PROJET/FNE/FNEV4/data/FNEV4.db"
    if not os.path.exists(db_path):
        return {"error": "Database not found"}
    
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    try:
        # Requête similaire à celle utilisée par GetAvailableForCertificationAsync
        cursor.execute("""
        SELECT COUNT(*) 
        FROM FneInvoices 
        WHERE Status = 'draft' 
        AND (CertificationStatus IS NULL OR CertificationStatus = 'Pending')
        """)
        available_count = cursor.fetchone()[0]
        
        # Requête avec tous les champs nécessaires
        cursor.execute("""
        SELECT 
            Id, InvoiceNumber, ClientName, TotalAmount, CreatedAt,
            Status, CertificationStatus
        FROM FneInvoices 
        WHERE Status = 'draft' 
        AND (CertificationStatus IS NULL OR CertificationStatus = 'Pending')
        ORDER BY CreatedAt DESC
        LIMIT 10
        """)
        available_invoices = cursor.fetchall()
        
        conn.close()
        
        return {
            "available_count": available_count,
            "sample_available": [
                {
                    "Id": row[0],
                    "InvoiceNumber": row[1],
                    "ClientName": row[2],
                    "TotalAmount": row[3],
                    "CreatedAt": row[4],
                    "Status": row[5],
                    "CertificationStatus": row[6]
                }
                for row in available_invoices
            ]
        }
        
    except Exception as e:
        conn.close()
        return {"error": str(e)}

def main():
    """Fonction principale de diagnostic"""
    print("=== DIAGNOSTIC CERTIFICATION FNE ===")
    print(f"Exécuté le: {datetime.now()}")
    print()
    
    # 1. Vérification des connexions base de données
    print("1. VÉRIFICATION DES BASES DE DONNÉES")
    db_connections = check_database_connection()
    for db_path, info in db_connections.items():
        print(f"   {db_path}:")
        if info['exists'] and info['accessible']:
            print(f"     ✓ Accessible - {info['table_count']} tables - {info['size_mb']} MB")
        elif info['exists']:
            print(f"     ✗ Erreur: {info.get('error', 'Inaccessible')}")
        else:
            print(f"     ✗ Fichier inexistant")
    print()
    
    # 2. Analyse de la structure FneInvoices
    print("2. ANALYSE DE LA STRUCTURE FNEINVOICES")
    invoice_analysis = analyze_fne_invoices_structure()
    if 'error' in invoice_analysis:
        print(f"   ✗ Erreur: {invoice_analysis['error']}")
    else:
        print(f"   Colonnes: {len(invoice_analysis['columns'])}")
        for col in invoice_analysis['columns'][:5]:  # Afficher les 5 premières
            print(f"     - {col['name']} ({col['type']})")
        if len(invoice_analysis['columns']) > 5:
            print(f"     ... et {len(invoice_analysis['columns']) - 5} autres")
        
        print(f"   Répartition par statut:")
        for status in invoice_analysis['status_breakdown']:
            print(f"     - {status['status']}: {status['count']} factures")
        
        print(f"   Préparation pour certification:")
        readiness = invoice_analysis['certification_readiness']
        print(f"     - Total draft: {readiness['total_draft']}")
        print(f"     - Avec numéro: {readiness['has_invoice_number']}")
        print(f"     - Avec client: {readiness['has_client_name']}")
        print(f"     - Avec montant: {readiness['has_amount']}")
        print(f"     - Certifiables: {readiness['certifiable']}")
    print()
    
    # 3. Test des requêtes de certification
    print("3. TEST DES REQUÊTES DE CERTIFICATION")
    ef_queries = check_entity_framework_queries()
    if 'error' in ef_queries:
        print(f"   ✗ Erreur: {ef_queries['error']}")
    else:
        print(f"   Factures disponibles pour certification: {ef_queries['available_count']}")
        if ef_queries['sample_available']:
            print("   Échantillon des factures disponibles:")
            for invoice in ef_queries['sample_available'][:3]:
                print(f"     - {invoice['InvoiceNumber']} | {invoice['ClientName']} | {invoice['TotalAmount']}€")
        else:
            print("   ⚠ Aucune facture dans l'échantillon!")
    print()
    
    # 4. Recommandations
    print("4. RECOMMANDATIONS")
    if 'error' not in invoice_analysis and 'error' not in ef_queries:
        if ef_queries['available_count'] > 0:
            print("   ✓ Les données sont présentes et accessibles")
            print("   → Le problème est probablement dans le ViewModel ou la liaison de données")
            print("   → Vérifiez les logs de l'application lors du clic sur 'Certification manuelle'")
        else:
            print("   ⚠ Aucune facture disponible pour certification")
            print("   → Vérifiez les critères de filtrage dans le repository")
    else:
        print("   ✗ Problème d'accès aux données détecté")
        print("   → Vérifiez la configuration de la base de données")

if __name__ == "__main__":
    main()