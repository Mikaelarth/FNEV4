#!/usr/bin/env python3
"""
Script de test pour simuler le comportement du ListeClientsUseCase
"""

import sqlite3
import json
from datetime import datetime

def simulate_liste_clients_use_case():
    """Simule l'exécution du ListeClientsUseCase avec les mêmes paramètres"""
    print("🧪 SIMULATION du ListeClientsUseCase")
    print("=" * 60)
    
    try:
        conn = sqlite3.connect('data/FNEV4.db')
        cursor = conn.cursor()
        
        # Paramètres par défaut du ViewModel
        page_number = 1
        page_size = 50
        search_term = None  # string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm
        client_type = "Tous"  # SelectedClientType
        is_active_filter = True  # IsActiveFilter = true par défaut
        
        print(f"📊 Paramètres de la requête:")
        print(f"  - Page: {page_number}")
        print(f"  - Taille page: {page_size}")
        print(f"  - Terme recherche: {search_term}")
        print(f"  - Type client: {client_type}")
        print(f"  - Filtre actif: {is_active_filter}")
        
        # Construction de la requête SQL comme le ferait le UseCase
        base_query = """
            SELECT ClientCode, Name, CompanyName, ClientType, IsActive, 
                   Email, Phone, Address, ClientNcc, CreatedDate
            FROM Clients 
            WHERE (IsDeleted = 0 OR IsDeleted IS NULL)
        """
        
        params = []
        
        # Filtre IsActive
        if is_active_filter is not None:
            if is_active_filter:
                base_query += " AND IsActive = 1"
            else:
                base_query += " AND IsActive = 0"
        
        # Filtre ClientType (si pas "Tous")
        if client_type and client_type != "Tous":
            base_query += " AND ClientType = ?"
            params.append(client_type)
        
        # Filtre de recherche
        if search_term:
            base_query += " AND (ClientCode LIKE ? OR Name LIKE ? OR CompanyName LIKE ? OR Email LIKE ?)"
            search_pattern = f"%{search_term}%"
            params.extend([search_pattern, search_pattern, search_pattern, search_pattern])
        
        # Pagination
        offset = (page_number - 1) * page_size
        base_query += " ORDER BY ClientCode LIMIT ? OFFSET ?"
        params.extend([page_size, offset])
        
        print(f"\n🔍 Requête SQL générée:")
        print(f"  {base_query}")
        print(f"  Paramètres: {params}")
        
        # Exécution de la requête
        cursor.execute(base_query, params)
        clients = cursor.fetchall()
        
        print(f"\n📋 Résultats ({len(clients)} clients trouvés):")
        for client in clients:
            name = client[1] or client[2] or 'N/A'
            active = "✅" if client[4] else "❌"
            print(f"  - {client[0]}: {name} ({client[3]}) {active}")
        
        # Compte total pour la pagination
        count_query = """
            SELECT COUNT(*) FROM Clients 
            WHERE (IsDeleted = 0 OR IsDeleted IS NULL)
        """
        count_params = []
        
        if is_active_filter is not None:
            if is_active_filter:
                count_query += " AND IsActive = 1"
            else:
                count_query += " AND IsActive = 0"
        
        if client_type and client_type != "Tous":
            count_query += " AND ClientType = ?"
            count_params.append(client_type)
        
        if search_term:
            count_query += " AND (ClientCode LIKE ? OR Name LIKE ? OR CompanyName LIKE ? OR Email LIKE ?)"
            search_pattern = f"%{search_term}%"
            count_params.extend([search_pattern, search_pattern, search_pattern, search_pattern])
        
        cursor.execute(count_query, count_params)
        total_count = cursor.fetchone()[0]
        
        total_pages = (total_count + page_size - 1) // page_size
        has_next_page = page_number < total_pages
        has_previous_page = page_number > 1
        
        print(f"\n📊 Informations de pagination:")
        print(f"  - Total clients: {total_count}")
        print(f"  - Total pages: {total_pages}")
        print(f"  - Page actuelle: {page_number}")
        print(f"  - Page suivante disponible: {has_next_page}")
        print(f"  - Page précédente disponible: {has_previous_page}")
        
        conn.close()
        
        return {
            "success": True,
            "clients": clients,
            "totalCount": total_count,
            "totalPages": total_pages,
            "hasNextPage": has_next_page,
            "hasPreviousPage": has_previous_page
        }
        
    except Exception as e:
        print(f"❌ Erreur: {e}")
        return {"success": False, "error": str(e)}

def main():
    """Fonction principale"""
    print("🧪 SIMULATION COMPLÈTE du ListeClientsUseCase")
    print("=" * 60)
    
    result = simulate_liste_clients_use_case()
    
    print("\n" + "=" * 60)
    print("📊 CONCLUSION:")
    if result["success"]:
        print("✅ La logique métier fonctionne correctement")
        print("💡 Le problème est soit:")
        print("   1. Dans l'appel au UseCase depuis le ViewModel")
        print("   2. Dans la mise à jour de l'UI (thread)")
        print("   3. Dans l'injection de dépendances")
    else:
        print("❌ Problème dans la logique métier")
        print(f"   Erreur: {result.get('error', 'Unknown')}")

if __name__ == "__main__":
    main()
