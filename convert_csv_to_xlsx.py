#!/usr/bin/env python3
"""
Script pour convertir le modèle CSV en fichier Excel XLSX
avec formatage approprié pour l'import DGI
"""

import pandas as pd
import os
from pathlib import Path

def convert_csv_to_xlsx():
    """Convertit le fichier CSV en Excel XLSX avec formatage"""
    
    # Chemins des fichiers
    csv_file = r"data\templates\modele_import_clients_dgi.csv"
    xlsx_file = r"data\templates\modele_import_clients_dgi.xlsx"
    
    # Vérifier que le fichier CSV existe
    if not os.path.exists(csv_file):
        print(f"Erreur: Le fichier {csv_file} n'existe pas")
        return False
    
    try:
        # Lire le fichier CSV
        df = pd.read_csv(csv_file, encoding='utf-8')
        
        # Créer le répertoire si nécessaire
        Path(xlsx_file).parent.mkdir(parents=True, exist_ok=True)
        
        # Écrire le fichier Excel avec formatage
        with pd.ExcelWriter(xlsx_file, engine='openpyxl') as writer:
            df.to_excel(writer, index=False, sheet_name='Clients')
            
            # Obtenir la feuille de calcul pour le formatage
            worksheet = writer.sheets['Clients']
            
            # Ajuster la largeur des colonnes
            for column in worksheet.columns:
                max_length = 0
                column_letter = column[0].column_letter
                
                for cell in column:
                    try:
                        if len(str(cell.value)) > max_length:
                            max_length = len(str(cell.value))
                    except:
                        pass
                
                # Définir une largeur minimum et maximum
                adjusted_width = min(max(max_length + 2, 12), 50)
                worksheet.column_dimensions[column_letter].width = adjusted_width
            
            # Formater l'en-tête
            header_row = worksheet[1]
            for cell in header_row:
                cell.font = cell.font.copy(bold=True)
                cell.fill = cell.fill.copy(fgColor="E6E6FA")  # Couleur lavande claire
        
        print(f"✅ Fichier Excel créé avec succès: {xlsx_file}")
        print(f"   - {len(df)} lignes de données")
        print(f"   - {len(df.columns)} colonnes")
        return True
        
    except Exception as e:
        print(f"❌ Erreur lors de la conversion: {e}")
        return False

if __name__ == "__main__":
    print("🔄 Conversion du modèle CSV vers Excel XLSX...")
    success = convert_csv_to_xlsx()
    
    if success:
        print("\n📋 Modèle Excel DGI prêt pour l'import!")
        print("   Templates disponibles:")
        print("   - B2B: Entreprise avec NCC obligatoire")
        print("   - B2C: Particulier sans NCC")
        print("   - B2G: Gouvernement avec codes spéciaux")
    else:
        print("\n❌ Échec de la conversion")
