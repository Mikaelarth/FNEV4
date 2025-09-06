using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces;
using FNEV4.Application.DTOs.GestionClients;
using FNEV4.Application.Interfaces;

namespace FNEV4.Application.UseCases.GestionClients
{
    /// <summary>
    /// Use Case pour l'import en masse de clients depuis Excel
    /// Respecte l'architecture Clean en utilisant les interfaces
    /// </summary>
    public class ImportClientsExcelUseCase
    {
        private readonly IClientRepository _clientRepository;
        private readonly IClientExcelImportService _excelImportService;
        private readonly ILoggingService _loggingService;

        public ImportClientsExcelUseCase(
            IClientRepository clientRepository,
            IClientExcelImportService excelImportService,
            ILoggingService loggingService)
        {
            _clientRepository = clientRepository ?? throw new ArgumentNullException(nameof(clientRepository));
            _excelImportService = excelImportService ?? throw new ArgumentNullException(nameof(excelImportService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        /// <summary>
        /// Génère un aperçu du fichier Excel avant import
        /// </summary>
        public async Task<ClientImportPreviewDto> PreviewFileAsync(string filePath)
        {
            try
            {
                await _loggingService.LogInformationAsync($"Génération aperçu fichier: {Path.GetFileName(filePath)}");

                var preview = await _excelImportService.PreviewFileAsync(filePath);
                
                await _loggingService.LogInformationAsync($"Aperçu généré: {preview.TotalRows} lignes analysées");
                return preview;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Erreur génération aperçu: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Exécute l'import des clients depuis le fichier Excel
        /// </summary>
        public async Task<ClientImportResultDto> ExecuteImportAsync(string filePath, ClientImportOptionsDto options)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new ClientImportResultDto
            {
                ImportedAt = DateTime.Now,
                ImportedBy = options.ImportedBy
            };

            try
            {
                await _loggingService.LogInformationAsync($"Début import clients Excel: {Path.GetFileName(filePath)}");

                // 1. Lire et valider le fichier Excel
                var importedClients = await _excelImportService.ReadExcelFileAsync(filePath);
                result.ProcessedCount = importedClients.Count;

                var validatedClients = await _excelImportService.ValidateDataAsync(importedClients);

                // 2. Séparer les clients valides et invalides
                var validClients = validatedClients.Where(c => c.IsValid).ToList();
                var invalidClients = validatedClients.Where(c => !c.IsValid).ToList();

                // 3. Traiter les erreurs de validation
                foreach (var invalidClient in invalidClients)
                {
                    foreach (var error in invalidClient.ValidationErrors)
                    {
                        result.RowErrors.Add(new ClientImportErrorDto
                        {
                            RowNumber = invalidClient.RowNumber,
                            ErrorMessage = error,
                            RowData = $"{invalidClient.ClientCode} - {invalidClient.Name}",
                            ErrorType = "Validation"
                        });
                    }
                }

                result.ErrorCount = invalidClients.Count;

                // 4. Si validation uniquement, retourner le résultat
                if (options.ValidateOnly)
                {
                    result.SuccessCount = validClients.Count;
                    result.IsSuccess = true;
                    await _loggingService.LogInformationAsync($"Validation terminée: {result.SuccessCount} clients valides sur {result.ProcessedCount}");
                    return result;
                }

                // 5. Traitement des doublons
                var clientsToProcess = new List<ClientImportModel>();
                foreach (var client in validClients)
                {
                    var existingClient = await _clientRepository.GetByClientCodeAsync(client.ClientCode);
                    
                    if (existingClient != null)
                    {
                        if (options.IgnoreDuplicates)
                        {
                            result.SkippedCount++;
                            continue;
                        }
                        else if (options.UpdateExisting)
                        {
                            clientsToProcess.Add(client);
                        }
                        else
                        {
                            result.RowErrors.Add(new ClientImportErrorDto
                            {
                                RowNumber = client.RowNumber,
                                ErrorMessage = "Client déjà existant",
                                RowData = $"{client.ClientCode} - {client.Name}",
                                ErrorType = "Duplicate"
                            });
                            result.ErrorCount++;
                        }
                    }
                    else
                    {
                        clientsToProcess.Add(client);
                    }
                }

                // 6. Sauvegarde en base de données
                int savedCount = 0;
                foreach (var clientModel in clientsToProcess)
                {
                    try
                    {
                        var client = ConvertToEntity(clientModel);
                        await _clientRepository.CreateAsync(client);
                        savedCount++;
                    }
                    catch (Exception ex)
                    {
                        await _loggingService.LogErrorAsync($"Erreur sauvegarde client {clientModel.ClientCode}: {ex.Message}", ex);
                        
                        result.RowErrors.Add(new ClientImportErrorDto
                        {
                            RowNumber = clientModel.RowNumber,
                            ErrorMessage = $"Erreur base de données: {ex.Message}",
                            RowData = $"{clientModel.ClientCode} - {clientModel.Name}",
                            ErrorType = "Database"
                        });
                        result.ErrorCount++;
                    }
                }

                result.SuccessCount = savedCount;
                result.IsSuccess = result.ErrorCount == 0;
                
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;

                await _loggingService.LogInformationAsync($"Import terminé: {result.GetSummary()} en {result.Duration.TotalSeconds:F2}s");
                return result;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Erreur import clients: {ex.Message}", ex);
                
                result.IsSuccess = false;
                result.GeneralErrors.Add($"Erreur critique: {ex.Message}");
                
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                
                throw;
            }
        }

        /// <summary>
        /// Convertit un modèle d'import en entité Client
        /// </summary>
        private Client ConvertToEntity(ClientImportModel model)
        {
            return new Client
            {
                ClientCode = model.ClientCode,
                Name = model.Name,
                ClientType = model.ClientType,
                ClientNcc = model.ClientNcc,
                CompanyName = model.CommercialName,
                Address = model.Address,
                Phone = model.Phone,
                Email = model.Email,
                Country = model.Country,
                DefaultCurrency = model.Currency,
                SellerName = model.Representative,
                TaxIdentificationNumber = model.TaxNumber,
                IsActive = model.IsActive,
                Notes = model.Notes,
                CreatedDate = DateTime.Now,
                LastModifiedDate = DateTime.Now
            };
        }
    }
}
