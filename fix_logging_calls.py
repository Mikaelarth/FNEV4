#!/usr/bin/env python3
"""
Script pour corriger les appels de logging dans CertificationManuelleViewModel
Remplace les appels _logger par des méthodes async appropriées
"""

import re

def fix_logging_calls():
    file_path = r"d:\PROJET\FNE\FNEV4\src\FNEV4.Presentation\ViewModels\CertificationFne\CertificationManuelleViewModel.cs"
    
    # Lire le contenu du fichier
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Remplacements
    replacements = [
        # Supprimer les logs secondaires pour simplifier
        (r'\s*_logger\.LogDebug\([^)]+\);?\s*\n', ''),
        (r'\s*_logger\.LogInformation\("Toutes les factures[^)]+\);?\s*\n', ''),
        (r'\s*_logger\.LogInformation\("Sélection vidée"\);?\s*\n', ''),
        (r'\s*_logger\.LogInformation\("Ouverture détails[^)]+\);?\s*\n', ''),
        (r'\s*_logger\.LogError\(ex, "Erreur ouverture détails[^)]+\);?\s*\n', ''),
        
        # Corriger les logs de certification
        (r'_logger\.LogInformation\("Début certification manuelle[^)]+\);?', 'await LogInfoAsync($"Début certification manuelle de {invoicesToCertify.Count} factures");'),
        (r'_logger\.LogInformation\("Facture \{InvoiceNumber\} certifiée[^)]+\);?', 'await LogInfoAsync($"Facture {invoice.InvoiceNumber} certifiée avec succès");'),
        (r'_logger\.LogWarning\("Échec certification[^)]+\);?', 'await LogWarningAsync($"Échec certification facture {invoice.InvoiceNumber}: {certificationResult.ErrorMessage}");'),
        (r'_logger\.LogError\(ex, "Exception lors de la certification[^)]+\);?', 'await LogErrorAsync($"Exception lors de la certification facture {invoice.InvoiceNumber}", exception: ex);'),
        (r'_logger\.LogInformation\("Certification manuelle terminée[^)]+\);?', 'await LogInfoAsync($"Certification manuelle terminée: {successCount} succès, {errorCount} erreurs");'),
        (r'_logger\.LogError\(ex, "Erreur générale lors de la certification[^)]+\);?', 'await LogErrorAsync("Erreur générale lors de la certification manuelle", exception: ex);'),
        (r'_logger\.LogError\(ex, "Erreur lors de la récupération[^)]+\);?', 'await LogErrorAsync("Erreur lors de la récupération de la configuration FNE active", exception: ex);'),
        
        # Corriger le problème du loggerFactory
        (r'loggerFactory\.CreateLogger<ViewModels\.GestionFactures\.FactureDetailsViewModel>\(\)', 'Microsoft.Extensions.Logging.LoggerFactory.Create(builder => { }).CreateLogger("FactureDetailsViewModel")'),
    ]
    
    # Appliquer les remplacements
    for pattern, replacement in replacements:
        content = re.sub(pattern, replacement, content, flags=re.MULTILINE | re.DOTALL)
    
    # Écrire le contenu modifié
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print("✅ Corrections appliquées au CertificationManuelleViewModel")

if __name__ == "__main__":
    fix_logging_calls()