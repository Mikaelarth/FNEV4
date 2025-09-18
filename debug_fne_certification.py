#!/usr/bin/env python3
"""
Script de diagnostic pour analyser l'erreur de certification FNE
Examine la base de données centralisée et les données de la facture 556481
"""

import sqlite3
import json
import os
from pathlib import Path
from typing import Dict, Any, Optional

def find_database_path() -> Optional[str]:
    """Trouve le chemin de la base de données selon la logique du système centralisé FNEV4"""
    
    # Méthode 1: Variable d'environnement
    env_path = os.getenv("FNEV4_DATABASE_PATH")
    if env_path and os.path.exists(env_path):
        return env_path
    
    # Méthode 2: Chemin du projet (recherche de FNEV4.sln)
    current_dir = Path(__file__).parent
    while current_dir != current_dir.parent:
        if (current_dir / "FNEV4.sln").exists():
            project_db = current_dir / "data" / "FNEV4.db"
            if project_db.exists():
                return str(project_db)
        current_dir = current_dir.parent
    
    # Méthode 3: AppData
    appdata_path = Path(os.getenv("LOCALAPPDATA", "")) / "FNEV4" / "FNEV4.db"
    if appdata_path.exists():
        return str(appdata_path)
    
    # Fallback: chemins possibles
    possible_paths = [
        "data/FNEV4.db",
        "FNEV4/data/FNEV4.db",
        "../data/FNEV4.db",
        "C:/wamp64/www/FNEV4/data/FNEV4.db"
    ]
    
    for path in possible_paths:
        if os.path.exists(path):
            return path
    
    return None

def get_fne_configuration(cursor) -> Dict[str, Any]:
    """Récupère la configuration FNE active"""
    cursor.execute("""
        SELECT Id, ConfigurationName, Environment, BaseUrl, ApiKey, IsActive, WebUrl, ApiVersion
        FROM FneConfigurations 
        WHERE IsActive = 1 
        LIMIT 1
    """)
    result = cursor.fetchone()
    if result:
        columns = [desc[0] for desc in cursor.description]
        return dict(zip(columns, result))
    return {}

def analyze_invoice_556481(cursor) -> Dict[str, Any]:
    """Analyse les données de la facture 556481"""
    cursor.execute("""
        SELECT f.Id, f.InvoiceNumber, f.InvoiceDate, f.InvoiceType, f.PaymentMethod, f.Template,
               f.TotalAmountHT, f.TotalVatAmount, f.TotalAmountTTC, f.Status, f.ClientId,
               f.FneReference, f.VerificationToken, f.FneQrCode, f.IsCertified,
               c.Name as ClientName, c.Email, c.Phone, c.Address, c.ClientType, c.DefaultTemplate, c.DefaultPaymentMethod
        FROM FneInvoices f
        LEFT JOIN Clients c ON f.ClientId = c.Id
        WHERE f.InvoiceNumber = '556481'
        LIMIT 1
    """)
    result = cursor.fetchone()
    if result:
        columns = [desc[0] for desc in cursor.description]
        return dict(zip(columns, result))
    return {}

def analyze_invoice_items(cursor, invoice_id: str) -> list:
    """Analyse les articles de la facture"""
    cursor.execute("""
        SELECT ProductCode, Description, Quantity, UnitPrice, VatRate, LineAmountTTC, 
               LineAmountHT, LineVatAmount, MeasurementUnit
        FROM FneInvoiceItems
        WHERE FneInvoiceId = ?
        ORDER BY LineOrder
    """, (invoice_id,))
    results = cursor.fetchall()
    if results:
        columns = [desc[0] for desc in cursor.description]
        return [dict(zip(columns, row)) for row in results]
    return []

def analyze_client_data(cursor, client_id: int) -> Dict[str, Any]:
    """Analyse les données client"""
    cursor.execute("""
        SELECT Id, ClientCode, ClientNcc, Name, CompanyName, Email, Phone, Address, Country,
               ClientType, DefaultTemplate, DefaultPaymentMethod, IsActive, TaxIdentificationNumber
        FROM Clients
        WHERE Id = ?
    """, (client_id,))
    result = cursor.fetchone()
    if result:
        columns = [desc[0] for desc in cursor.description]
        return dict(zip(columns, result))
    return {}

def check_required_fne_fields(invoice_data: Dict[str, Any], client_data: Dict[str, Any], items: list) -> Dict[str, list]:
    """Vérifie les champs requis selon l'erreur API FNE"""
    errors = {
        'missing_fields': [],
        'invalid_values': [],
        'recommendations': []
    }
    
    # Vérification PaymentMethod
    payment_method = invoice_data.get('PaymentMethod')
    valid_payment_methods = ['card', 'check', 'cash', 'mobile-money', 'transfer']
    if not payment_method or payment_method not in valid_payment_methods:
        errors['invalid_values'].append(f"PaymentMethod: '{payment_method}' - Doit être: {', '.join(valid_payment_methods)}")
    
    # Vérification InvoiceType
    invoice_type = invoice_data.get('InvoiceType')
    valid_invoice_types = ['sale', 'purchase']
    if not invoice_type or invoice_type not in valid_invoice_types:
        errors['invalid_values'].append(f"InvoiceType: '{invoice_type}' - Doit être: {', '.join(valid_invoice_types)}")
    
    # Vérification Template
    template = invoice_data.get('Template')
    valid_templates = ['B2B', 'B2C', 'B2G', 'B2F']
    if not template or template not in valid_templates:
        errors['invalid_values'].append(f"Template: '{template}' - Doit être: {', '.join(valid_templates)}")
    
    # Vérification données client
    if not client_data.get('Name'):
        errors['missing_fields'].append("ClientCompanyName: Nom client requis")
    
    if not client_data.get('Phone'):
        errors['missing_fields'].append("ClientPhone: Téléphone client requis")
    
    if not client_data.get('Email'):
        errors['missing_fields'].append("ClientEmail: Email client requis")
    
    # Vérification articles
    if not items:
        errors['missing_fields'].append("Items: Au moins un article requis")
    else:
        for i, item in enumerate(items):
            if not item.get('Description'):
                errors['missing_fields'].append(f"Article {i+1}: Description manquante")
            if not item.get('VatRate'):
                errors['missing_fields'].append(f"Article {i+1}: Taux TVA manquant")
    
    # Recommandations
    if errors['missing_fields'] or errors['invalid_values']:
        errors['recommendations'].append("Vérifiez la configuration des codes FNE dans les paramètres")
        errors['recommendations'].append("Assurez-vous que tous les champs client obligatoires sont remplis")
        errors['recommendations'].append("Vérifiez que les articles ont des descriptions et taux TVA")
    
    return errors

def main():
    print("🔍 Diagnostic de l'erreur de certification FNE - Facture 556481")
    print("=" * 70)
    
    # 1. Trouver la base de données
    db_path = find_database_path()
    if not db_path:
        print("❌ Base de données FNEV4 non trouvée!")
        return
    
    print(f"✅ Base de données trouvée: {db_path}")
    
    try:
        # 2. Se connecter à la base
        with sqlite3.connect(db_path) as conn:
            cursor = conn.cursor()
            
            # 3. Configuration FNE
            print("\n🔧 Configuration FNE Active:")
            print("-" * 30)
            config = get_fne_configuration(cursor)
            if config:
                print(f"Configuration: {config.get('ConfigurationName', 'N/A')}")
                print(f"Environnement: {config.get('Environment', 'N/A')}")
                print(f"URL API: {config.get('BaseUrl', 'N/A')}")
                print(f"URL Web: {config.get('WebUrl', 'N/A')}")
                print(f"Version API: {config.get('ApiVersion', 'N/A')}")
                print(f"Clé API: {'*' * 8}...{config.get('ApiKey', '')[-8:] if config.get('ApiKey') else 'N/A'}")
            else:
                print("❌ Aucune configuration FNE active trouvée!")
            
            # 4. Données de la facture
            print("\n📄 Analyse de la facture 556481:")
            print("-" * 35)
            invoice = analyze_invoice_556481(cursor)
            if invoice:
                print(f"ID: {invoice.get('Id')}")
                print(f"Numéro: {invoice.get('InvoiceNumber')}")
                print(f"Date: {invoice.get('InvoiceDate')}")
                print(f"Client: {invoice.get('ClientName', 'N/A')}")
                print(f"Montant HT: {invoice.get('TotalAmountHT')} FCFA")
                print(f"Montant TVA: {invoice.get('TotalVatAmount')} FCFA")
                print(f"Montant TTC: {invoice.get('TotalAmountTTC')} FCFA")
                print(f"Type: {invoice.get('InvoiceType', 'N/A')}")
                print(f"Template: {invoice.get('Template', 'N/A')}")
                print(f"Méthode Paiement: {invoice.get('PaymentMethod', 'N/A')}")
                print(f"Statut: {invoice.get('Status', 'N/A')}")
                print(f"Certifiée: {invoice.get('IsCertified', False)}")
                print(f"Référence FNE: {invoice.get('FneReference', 'N/A')}")
                
                # 5. Données client
                client_id = invoice.get('ClientId')
                if client_id:
                    print(f"\n👤 Données du client (ID: {client_id}):")
                    print("-" * 25)
                    client = analyze_client_data(cursor, client_id)
                    if client:
                        print(f"Code: {client.get('ClientCode', 'N/A')}")
                        print(f"NCC: {client.get('ClientNcc', 'N/A')}")
                        print(f"Nom: {client.get('Name', 'N/A')}")
                        print(f"Nom Entreprise: {client.get('CompanyName', 'N/A')}")
                        print(f"Email: {client.get('Email', 'N/A')}")
                        print(f"Téléphone: {client.get('Phone', 'N/A')}")
                        print(f"Adresse: {client.get('Address', 'N/A')}")
                        print(f"Pays: {client.get('Country', 'N/A')}")
                        print(f"Type Client: {client.get('ClientType', False)}")
                        print(f"Template Par Défaut: {client.get('DefaultTemplate', 'N/A')}")
                        print(f"Méthode Paiement Par Défaut: {client.get('DefaultPaymentMethod', 'N/A')}")
                        print(f"N° Identification Fiscale: {client.get('TaxIdentificationNumber', 'N/A')}")
                
                # 6. Articles de la facture
                invoice_id = invoice.get('Id')
                if invoice_id:
                    items = analyze_invoice_items(cursor, invoice_id)
                    print(f"\n📦 Articles de la facture ({len(items)} articles):")
                    print("-" * 30)
                    for i, item in enumerate(items, 1):
                        print(f"Article {i}:")
                        print(f"  Code: {item.get('ProductCode', 'N/A')}")
                        print(f"  Description: {item.get('Description', 'N/A')}")
                        print(f"  Quantité: {item.get('Quantity', 0)}")
                        print(f"  Unité: {item.get('MeasurementUnit', 'N/A')}")
                        print(f"  Prix unitaire: {item.get('UnitPrice', 0)} FCFA")
                        print(f"  Taux TVA: {item.get('VatRate', 0)}%")
                        print(f"  Montant HT: {item.get('LineAmountHT', 0)} FCFA")
                        print(f"  Montant TVA: {item.get('LineVatAmount', 0)} FCFA")
                        print(f"  Total TTC: {item.get('LineAmountTTC', 0)} FCFA")
                        print()
                
                # 7. Analyse des erreurs
                print("\n🚨 Analyse des erreurs selon l'API FNE:")
                print("-" * 40)
                analysis = check_required_fne_fields(invoice, client or {}, items)
                
                if analysis['missing_fields']:
                    print("❌ Champs manquants:")
                    for field in analysis['missing_fields']:
                        print(f"  • {field}")
                
                if analysis['invalid_values']:
                    print("\n❌ Valeurs invalides:")
                    for value in analysis['invalid_values']:
                        print(f"  • {value}")
                
                if analysis['recommendations']:
                    print("\n💡 Recommandations:")
                    for rec in analysis['recommendations']:
                        print(f"  • {rec}")
                
                if not analysis['missing_fields'] and not analysis['invalid_values']:
                    print("✅ Tous les champs semblent corrects côté base de données")
                    print("📋 L'erreur peut venir du mapping des données vers l'API")
                
            else:
                print("❌ Facture 556481 non trouvée dans la base de données!")
            
    except Exception as e:
        print(f"❌ Erreur lors de l'analyse: {e}")

if __name__ == "__main__":
    main()