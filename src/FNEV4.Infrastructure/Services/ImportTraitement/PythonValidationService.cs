using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FNEV4.Core.Interfaces;
using FNEV4.Core.Models.ImportTraitement;

namespace FNEV4.Infrastructure.Services.ImportTraitement
{
    /// <summary>
    /// Service pour exécuter les validations Python avec intégration base de données
    /// </summary>
    public class PythonValidationService
    {
        private readonly ILoggingService _loggingService;
        private readonly string _pythonExecutable;
        private readonly string _validatorScriptPath;
        private readonly string _databasePath;

        public PythonValidationService(ILoggingService loggingService)
        {
            _loggingService = loggingService;
            _pythonExecutable = "python"; // Peut être configuré
            _validatorScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fnev4_sage100_validator.py");
            _databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "FNEV4.db");
        }

        /// <summary>
        /// Exécute la validation Python et récupère les informations des templates
        /// </summary>
        public async Task<PythonValidationResult?> ValidateWithTemplatesAsync(string excelFilePath)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = _pythonExecutable,
                    Arguments = $"\"{_validatorScriptPath}\" \"{excelFilePath}\" \"{_databasePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    await _loggingService.LogErrorAsync("Impossible de démarrer le processus Python", "PythonValidation");
                    return null;
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    await _loggingService.LogErrorAsync($"Erreur validation Python: {error}", "PythonValidation");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(output))
                {
                    await _loggingService.LogWarningAsync("Aucune sortie du validateur Python", "PythonValidation");
                    return null;
                }

                // Parser le JSON de sortie
                var validationResult = JsonSerializer.Deserialize<PythonValidationResult>(output, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                await _loggingService.LogInfoAsync($"Validation Python réussie: {validationResult?.Factures?.Count ?? 0} factures validées", "PythonValidation");
                
                return validationResult;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Erreur lors de l'exécution de la validation Python: {ex.Message}", "PythonValidation", ex);
                return null;
            }
        }

        /// <summary>
        /// Récupère les informations de template pour un client spécifique depuis la base de données
        /// </summary>
        public async Task<ClientTemplateInfo?> GetClientTemplateAsync(string clientCode)
        {
            try
            {
                // Script Python simple pour récupérer les informations d'un client
                var tempScript = Path.GetTempFileName() + ".py";
                var scriptContent = $@"
import sqlite3
import json
import sys

def get_client_template(db_path, client_code):
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        query = '''
        SELECT ClientCode, DefaultTemplate, Name, ClientNcc, IsActive 
        FROM Clients 
        WHERE ClientCode = ?
        '''
        
        cursor.execute(query, (client_code,))
        row = cursor.fetchone()
        
        if row:
            result = {{
                'ClientCode': row[0],
                'Template': row[1],
                'NomCommercial': row[2],
                'Ncc': row[3],
                'Active': bool(row[4])
            }}
        else:
            result = None
            
        conn.close()
        return result
        
    except Exception as e:
        return None

if __name__ == '__main__':
    if len(sys.argv) != 3:
        sys.exit(1)
        
    db_path = sys.argv[1]
    client_code = sys.argv[2]
    
    result = get_client_template(db_path, client_code)
    
    if result:
        print(json.dumps(result))
    else:
        print(json.dumps({{""error"": ""Client not found""}}))
";

                await File.WriteAllTextAsync(tempScript, scriptContent);

                var processInfo = new ProcessStartInfo
                {
                    FileName = _pythonExecutable,
                    Arguments = $"\"{tempScript}\" \"{_databasePath}\" \"{clientCode}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    return null;
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();

                // Nettoyer le fichier temporaire
                try { File.Delete(tempScript); } catch { }

                if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
                {
                    return null;
                }

                // Parser le JSON de sortie
                var result = JsonSerializer.Deserialize<ClientTemplateInfo>(output, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Erreur récupération template client {clientCode}: {ex.Message}", "PythonValidation", ex);
                return null;
            }
        }
    }

    /// <summary>
    /// Résultat de la validation Python
    /// </summary>
    public class PythonValidationResult
    {
        public List<PythonFactureValidation>? Factures { get; set; }
        public PythonStatistiques? Statistiques { get; set; }
        public List<string>? ErreursGlobales { get; set; }
    }

    /// <summary>
    /// Validation d'une facture par Python
    /// </summary>
    public class PythonFactureValidation
    {
        public string? SheetName { get; set; }
        public string? NumeroFacture { get; set; }
        public string? CodeClient { get; set; }
        public string? DateFacture { get; set; }
        public string? PointVente { get; set; }
        public string? MoyenPaiement { get; set; }
        public string? Template { get; set; }
        public string? NomClient { get; set; }
        public string? NccClient { get; set; }
        public bool EstValide { get; set; }
        public List<string>? Erreurs { get; set; }
        public List<string>? Avertissements { get; set; }
        public PythonClientInfo? ClientInfo { get; set; }
    }

    /// <summary>
    /// Informations client depuis Python
    /// </summary>
    public class PythonClientInfo
    {
        public bool Exists { get; set; }
        public string? Id { get; set; }
        public string? CodeClient { get; set; }
        public string? NomCommercial { get; set; }
        public string? Ncc { get; set; }
        public string? Template { get; set; }
        public bool Active { get; set; }
    }

    /// <summary>
    /// Statistiques de validation Python
    /// </summary>
    public class PythonStatistiques
    {
        public int Total { get; set; }
        public int Valides { get; set; }
        public int Invalides { get; set; }
        public Dictionary<string, int>? Templates { get; set; }
    }
}
