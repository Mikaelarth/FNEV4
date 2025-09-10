#!/usr/bin/env python3
"""
Service de validation Sage 100 intégré à FNEV4
Valide les factures et enrichit avec les informations de template
"""

import sys
import os
import json
import sqlite3
import openpyxl
from datetime import datetime
from typing import List, Dict, Tuple, Optional
import re

class FNEV4Sage100Validator:
    """Validateur intégré pour FNEV4"""
    
    def __init__(self, db_path: str = None):
        self.db_path = db_path or self.find_fnev4_database()
        self.errors = []
        
    def find_fnev4_database(self) -> str:
        """Trouve la base de données FNEV4"""
        possible_paths = [
            "fnev4.db",
            "../fnev4.db", 
            "../../fnev4.db",
            "src/FNEV4.Infrastructure/fnev4.db",
            "../src/FNEV4.Infrastructure/fnev4.db",
            "../../src/FNEV4.Infrastructure/fnev4.db"
        ]
        
        for path in possible_paths:
            if os.path.exists(path):
                return path
        
        return None
    
    def get_client_template(self, code_client: str) -> Dict:
        """Récupère le template du client depuis la base FNEV4"""
        if not self.db_path or not os.path.exists(self.db_path):
            return {'exists': False, 'template': 'N/A', 'active': False}
            
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            # Requête pour récupérer les infos client FNEV4
            cursor.execute("""
                SELECT Id, ClientCode, Name, ClientNcc, DefaultTemplate, IsActive 
                FROM Clients 
                WHERE ClientCode = ? AND IsActive = 1
            """, (code_client,))
            
            result = cursor.fetchone()
            conn.close()
            
            if result:
                return {
                    'exists': True,
                    'id': result[0],
                    'code_client': result[1],
                    'nom_commercial': result[2],
                    'ncc': result[3],
                    'template': result[4] or 'B2B',
                    'active': bool(result[5])
                }
            else:
                return {'exists': False, 'template': 'N/A', 'active': False}
                
        except Exception as e:
            self.errors.append(f"Erreur accès DB pour client {code_client}: {e}")
            return {'exists': False, 'template': 'N/A', 'active': False}
    
    def validate_ncc_format(self, ncc: str) -> bool:
        """Valide le format NCC (7 chiffres + 1 lettre)"""
        if not ncc:
            return False
        return bool(re.match(r'^\d{7}[A-Z]$', ncc.strip().upper()))
    
    def validate_excel_facture(self, worksheet, sheet_name: str) -> Dict:
        """Valide une facture Excel et enrichit avec les infos template"""
        
        def get_cell_value(cell_ref: str) -> str:
            try:
                value = worksheet[cell_ref].value
                return str(value).strip() if value else ""
            except:
                return ""
        
        # Données de base
        numero_facture = get_cell_value('A3')
        code_client = get_cell_value('A5')
        date_facture = get_cell_value('A8')
        point_vente = get_cell_value('A10')
        moyen_paiement = get_cell_value('A18')
        
        # Résultat de validation
        result = {
            'sheet_name': sheet_name,
            'numero_facture': numero_facture,
            'code_client': code_client,
            'date_facture': date_facture,
            'point_vente': point_vente,
            'moyen_paiement': moyen_paiement,
            'template': 'N/A',
            'nom_client': '',
            'ncc_client': '',
            'est_valide': True,
            'erreurs': [],
            'avertissements': [],
            'client_info': {}
        }
        
        # Validation code client
        if not code_client:
            result['erreurs'].append("Code client manquant (A5)")
            result['est_valide'] = False
        else:
            if code_client == "1999":
                # Client divers - template B2C par défaut
                result['template'] = 'B2C'
                result['nom_client'] = get_cell_value('A13')  # Nom réel ligne 13
                result['ncc_client'] = get_cell_value('A15')   # NCC spécifique optionnel
                
                if not result['nom_client']:
                    result['erreurs'].append("Nom réel client divers manquant (A13)")
                    result['est_valide'] = False
                
                if result['ncc_client'] and not self.validate_ncc_format(result['ncc_client']):
                    result['erreurs'].append(f"NCC client divers invalide: {result['ncc_client']}")
                    result['est_valide'] = False
                elif not result['ncc_client']:
                    result['avertissements'].append("NCC client divers non spécifié (template B2C par défaut)")
            
            else:
                # Client B2B - vérifier dans la base
                client_info = self.get_client_template(code_client)
                result['client_info'] = client_info
                
                if not client_info['exists']:
                    result['erreurs'].append(f"Client {code_client} non trouvé dans la base FNEV4")
                    result['est_valide'] = False
                    result['template'] = 'N/A'
                elif not client_info['active']:
                    result['erreurs'].append(f"Client {code_client} inactif")
                    result['est_valide'] = False
                    result['template'] = 'N/A'
                else:
                    # Client valide - récupérer template et infos
                    result['template'] = client_info['template']
                    result['nom_client'] = get_cell_value('A11')  # Nom ligne 11 pour B2B
                    result['ncc_client'] = get_cell_value('A6')   # NCC obligatoire pour B2B
                    
                    if not result['ncc_client']:
                        result['erreurs'].append("NCC client B2B manquant (A6)")
                        result['est_valide'] = False
                    elif not self.validate_ncc_format(result['ncc_client']):
                        result['erreurs'].append(f"NCC client B2B invalide: {result['ncc_client']}")
                        result['est_valide'] = False
        
        # Validation autres champs
        if not numero_facture:
            result['erreurs'].append("Numéro facture manquant (A3)")
            result['est_valide'] = False
            
        if not date_facture:
            result['erreurs'].append("Date facture manquante (A8)")
            result['est_valide'] = False
        
        return result
    
    def validate_excel_file(self, excel_path: str) -> Dict:
        """Valide un fichier Excel complet"""
        try:
            workbook = openpyxl.load_workbook(excel_path, data_only=True)
            
            results = {
                'fichier': excel_path,
                'date_validation': datetime.now().isoformat(),
                'factures': [],
                'statistiques': {
                    'total': 0,
                    'valides': 0,
                    'invalides': 0,
                    'templates': {
                        'B2B': 0,
                        'B2C': 0,
                        'N/A': 0
                    }
                },
                'erreurs_globales': self.errors.copy()
            }
            
            for sheet_name in workbook.sheetnames:
                worksheet = workbook[sheet_name]
                facture_result = self.validate_excel_facture(worksheet, sheet_name)
                results['factures'].append(facture_result)
                
                # Mise à jour statistiques
                results['statistiques']['total'] += 1
                if facture_result['est_valide']:
                    results['statistiques']['valides'] += 1
                else:
                    results['statistiques']['invalides'] += 1
                
                template = facture_result['template']
                if template in results['statistiques']['templates']:
                    results['statistiques']['templates'][template] += 1
            
            return results
            
        except Exception as e:
            return {
                'fichier': excel_path,
                'erreur': f"Impossible de traiter le fichier: {e}",
                'statistiques': {
                    'total': 0,
                    'valides': 0,
                    'invalides': 0
                }
            }

def main():
    """Point d'entrée pour utilisation standalone"""
    if len(sys.argv) < 2:
        print("Usage: python fnev4_sage100_validator.py <fichier_excel> [db_path]")
        sys.exit(1)
    
    excel_file = sys.argv[1]
    db_path = sys.argv[2] if len(sys.argv) > 2 else None
    
    validator = FNEV4Sage100Validator(db_path)
    results = validator.validate_excel_file(excel_file)
    
    # Sortie JSON pour intégration FNEV4
    print(json.dumps(results, indent=2, ensure_ascii=False))

if __name__ == "__main__":
    main()
