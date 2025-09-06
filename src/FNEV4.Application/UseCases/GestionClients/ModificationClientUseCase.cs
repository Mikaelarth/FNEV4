using FNEV4.Core.DTOs;
using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces;
using FNEV4.Application.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FNEV4.Application.UseCases.GestionClients
{
    public class ModificationClientUseCase
    {
        private readonly IClientRepository _clientRepository;
        private readonly ILoggingService _loggingService;

        public ModificationClientUseCase(IClientRepository clientRepository, ILoggingService loggingService)
        {
            _clientRepository = clientRepository ?? throw new ArgumentNullException(nameof(clientRepository));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        public async Task<OperationResultDto> ExecuteAsync(ClientDto clientDto)
        {
            try
            {
                if (clientDto == null)
                    return OperationResultDto.Failure("Les données du client sont requises.");

                if (clientDto.Id == Guid.Empty)
                    return OperationResultDto.Failure("L'identifiant du client est requis pour la modification.");

                // Récupérer le client existant
                var existingClient = await _clientRepository.GetByIdAsync(clientDto.Id);
                if (existingClient == null)
                {
                    return OperationResultDto.Failure("Client introuvable.");
                }

                // Validation métier
                var validationResult = ValidateClient(clientDto);
                if (!validationResult.IsSuccess)
                    return validationResult;

                // Vérifier l'unicité du code (sauf pour le client actuel)
                if (existingClient.ClientCode != clientDto.Code)
                {
                    var codeExists = await _clientRepository.ExistsClientCodeAsync(clientDto.Code, existingClient.Id);
                    if (codeExists)
                    {
                        return OperationResultDto.Failure($"Un autre client utilise déjà le code '{clientDto.Code}'.");
                    }
                }

                // Log des modifications
                var modifications = GetModifications(existingClient, clientDto);
                if (modifications.Any())
                {
                    _loggingService.LogInfo($"Modifications du client {existingClient.Name}:\n{string.Join("\n", modifications)}");
                }

                // Mettre à jour les propriétés
                existingClient.ClientCode = clientDto.Code.Trim();
                existingClient.Name = clientDto.Name.Trim();
                existingClient.ClientType = clientDto.Type;
                existingClient.ClientNcc = string.IsNullOrWhiteSpace(clientDto.Ncc) ? null : clientDto.Ncc.Trim();
                existingClient.Email = string.IsNullOrWhiteSpace(clientDto.Email) ? null : clientDto.Email.Trim().ToLowerInvariant();
                existingClient.Phone = string.IsNullOrWhiteSpace(clientDto.Phone) ? null : clientDto.Phone.Trim();
                existingClient.Address = string.IsNullOrWhiteSpace(clientDto.Address) ? null : clientDto.Address.Trim();
                existingClient.Notes = string.IsNullOrWhiteSpace(clientDto.Notes) ? null : clientDto.Notes.Trim();
                existingClient.IsActive = clientDto.IsActive;
                existingClient.LastModifiedDate = DateTime.Now;

                // Sauvegarder en base
                var updatedClient = await _clientRepository.UpdateAsync(existingClient);

                _loggingService.LogInfo($"Client modifié avec succès: {updatedClient.Name} (Code: {updatedClient.ClientCode})");

                return OperationResultDto.Success($"Client '{updatedClient.Name}' modifié avec succès.");
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, $"Erreur lors de la modification du client: {clientDto?.Name}");
                return OperationResultDto.Failure("Erreur technique lors de la modification du client.");
            }
        }

        private OperationResultDto ValidateClient(ClientDto clientDto)
        {
            var errors = new List<string>();

            // Validation du code
            if (string.IsNullOrWhiteSpace(clientDto.Code))
                errors.Add("Le code client est obligatoire.");
            else if (clientDto.Code.Length < 2 || clientDto.Code.Length > 50)
                errors.Add("Le code client doit contenir entre 2 et 50 caractères.");

            // Validation du nom
            if (string.IsNullOrWhiteSpace(clientDto.Name))
                errors.Add("Le nom/raison sociale est obligatoire.");
            else if (clientDto.Name.Length < 2 || clientDto.Name.Length > 200)
                errors.Add("Le nom doit contenir entre 2 et 200 caractères.");

            // Validation du type
            var validTypes = new[] { "Particulier", "Entreprise", "Association", "Administration" };
            if (!validTypes.Contains(clientDto.Type))
                errors.Add("Le type de client n'est pas valide.");

            // Validation de l'email
            if (!string.IsNullOrWhiteSpace(clientDto.Email))
            {
                if (!IsValidEmail(clientDto.Email))
                    errors.Add("L'adresse email n'est pas valide.");
            }

            // Validation du NCC
            if (!string.IsNullOrWhiteSpace(clientDto.Ncc))
            {
                if (clientDto.Ncc.Length < 8 || clientDto.Ncc.Length > 15)
                    errors.Add("Le NCC doit contenir entre 8 et 15 caractères.");
            }

            if (errors.Any())
                return OperationResultDto.Failure(string.Join(Environment.NewLine, errors));

            return OperationResultDto.Success();
        }

        private List<string> GetModifications(Client existing, ClientDto updated)
        {
            var modifications = new List<string>();

            if (existing.ClientCode != updated.Code)
                modifications.Add($"Code: {existing.ClientCode} → {updated.Code}");

            if (existing.Name != updated.Name)
                modifications.Add($"Nom: {existing.Name} → {updated.Name}");

            if (existing.ClientType != updated.Type)
                modifications.Add($"Type: {existing.ClientType} → {updated.Type}");

            if (existing.ClientNcc != updated.Ncc)
                modifications.Add($"NCC: {existing.ClientNcc ?? "vide"} → {updated.Ncc ?? "vide"}");

            if (existing.Email != updated.Email?.ToLowerInvariant())
                modifications.Add($"Email: {existing.Email ?? "vide"} → {updated.Email ?? "vide"}");

            if (existing.Phone != updated.Phone)
                modifications.Add($"Téléphone: {existing.Phone ?? "vide"} → {updated.Phone ?? "vide"}");

            if (existing.IsActive != updated.IsActive)
                modifications.Add($"Statut: {(existing.IsActive ? "Actif" : "Inactif")} → {(updated.IsActive ? "Actif" : "Inactif")}");

            return modifications;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
