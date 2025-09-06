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
    public class AjoutClientUseCase
    {
        private readonly IClientRepository _clientRepository;
        private readonly ILoggingService _loggingService;

        public AjoutClientUseCase(IClientRepository clientRepository, ILoggingService loggingService)
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

                // Validation métier
                var validationResult = await ValidateClientAsync(clientDto);
                if (!validationResult.IsSuccess)
                    return validationResult;

                // Vérifier l'unicité du code client
                var codeExists = await _clientRepository.ExistsClientCodeAsync(clientDto.Code);
                if (codeExists)
                {
                    return OperationResultDto.Failure($"Un client avec le code '{clientDto.Code}' existe déjà.");
                }

                // Mapper DTO vers Entity
                var client = new Client
                {
                    ClientCode = clientDto.Code.Trim(),
                    Name = clientDto.Name.Trim(),
                    ClientType = clientDto.Type,
                    ClientNcc = string.IsNullOrWhiteSpace(clientDto.Ncc) ? null : clientDto.Ncc.Trim(),
                    Email = string.IsNullOrWhiteSpace(clientDto.Email) ? null : clientDto.Email.Trim().ToLowerInvariant(),
                    Phone = string.IsNullOrWhiteSpace(clientDto.Phone) ? null : clientDto.Phone.Trim(),
                    Address = string.IsNullOrWhiteSpace(clientDto.Address) ? null : clientDto.Address.Trim(),
                    Notes = string.IsNullOrWhiteSpace(clientDto.Notes) ? null : clientDto.Notes.Trim(),
                    IsActive = clientDto.IsActive,
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now,
                    DefaultTemplate = GetDefaultTemplate(clientDto.Type)
                };

                // Sauvegarder en base
                var createdClient = await _clientRepository.CreateAsync(client);

                _loggingService.LogInfo($"Nouveau client créé: {createdClient.Name} (Code: {createdClient.ClientCode})");

                return OperationResultDto.Success($"Client '{createdClient.Name}' créé avec succès.");
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, $"Erreur lors de la création du client: {clientDto?.Name}");
                return OperationResultDto.Failure("Erreur technique lors de la création du client.");
            }
        }

        private string GetDefaultTemplate(string clientType)
        {
            return clientType switch
            {
                "Particulier" => "B2C",
                "Entreprise" => "B2B",
                "Administration" => "B2G",
                "Association" => "B2C",
                _ => "B2C"
            };
        }

        private Task<OperationResultDto> ValidateClientAsync(ClientDto clientDto)
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
                return Task.FromResult(OperationResultDto.Failure(string.Join(Environment.NewLine, errors)));

            return Task.FromResult(OperationResultDto.Success());
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
