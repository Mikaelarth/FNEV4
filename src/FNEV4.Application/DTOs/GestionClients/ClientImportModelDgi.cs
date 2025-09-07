using System.ComponentModel.DataAnnotations;

namespace FNEV4.Application.DTOs.GestionClients
{
    /// <summary>
    /// Modèle d'import Excel pour les clients aligné sur les spécifications API DGI FNE
    /// Selon FNE-procedureapi.md - Templates B2B, B2C, B2G, B2F
    /// </summary>
    public class ClientImportModelDgi
    {
        [Required(ErrorMessage = "Le code client est obligatoire")]
        public string ClientCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom/raison sociale est obligatoire")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Template de facturation selon API DGI
        /// B2B: Entreprise avec NCC (NCC obligatoire)
        /// B2C: Particulier (NCC interdit)  
        /// B2G: Gouvernemental
        /// B2F: International
        /// </summary>
        [Required(ErrorMessage = "Le template de facturation est obligatoire")]
        public string Template { get; set; } = string.Empty; // B2B, B2C, B2G, B2F

        /// <summary>
        /// NCC du client - Obligatoire si Template = B2B, interdit si Template = B2C
        /// </summary>
        public string ClientNcc { get; set; } = string.Empty;

        /// <summary>
        /// Nom commercial/raison sociale pour l'API DGI
        /// Correspond à clientCompanyName dans l'API
        /// </summary>
        public string CommercialName { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;

        public string Country { get; set; } = "Côte d'Ivoire";

        [Phone(ErrorMessage = "Format de téléphone invalide")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; } = string.Empty;

        public string Representative { get; set; } = string.Empty;

        /// <summary>
        /// Numéro fiscal/TIN - Optionnel selon le template
        /// </summary>
        public string TaxNumber { get; set; } = string.Empty;

        /// <summary>
        /// Devise par défaut du client
        /// XOF, USD, EUR selon API DGI
        /// </summary>
        public string Currency { get; set; } = "XOF";

        /// <summary>
        /// Moyen de paiement par défaut du client
        /// cash, card, mobile-money, bank-transfer, check, credit
        /// Obligatoire pour la certification des factures selon API DGI
        /// </summary>
        [Required(ErrorMessage = "Le moyen de paiement par défaut est obligatoire")]
        public string PaymentMethod { get; set; } = "cash";

        public bool IsActive { get; set; } = true;

        public string Notes { get; set; } = string.Empty;

        // Propriétés de suivi
        public int RowNumber { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public bool IsValid => ValidationErrors.Count == 0;

        /// <summary>
        /// Validation métier selon les règles API DGI
        /// </summary>
        public void ValidateBusinessRules()
        {
            ValidationErrors.Clear();

            // Validation Template
            var validTemplates = new[] { "B2B", "B2C", "B2G", "B2F" };
            if (!validTemplates.Contains(Template))
            {
                ValidationErrors.Add("Template doit être B2B, B2C, B2G ou B2F");
            }

            // Validation NCC selon Template
            if (Template == "B2B" && string.IsNullOrWhiteSpace(ClientNcc))
            {
                ValidationErrors.Add("NCC obligatoire pour les clients B2B (entreprises)");
            }

            if (Template == "B2C" && !string.IsNullOrWhiteSpace(ClientNcc))
            {
                ValidationErrors.Add("NCC interdit pour les clients B2C (particuliers)");
            }

            // Validation NCC format (si présent)
            if (!string.IsNullOrWhiteSpace(ClientNcc))
            {
                if (ClientNcc.Length < 8 || ClientNcc.Length > 11)
                {
                    ValidationErrors.Add("NCC doit contenir entre 8 et 11 caractères");
                }
                else if (!System.Text.RegularExpressions.Regex.IsMatch(ClientNcc, @"^[A-Za-z0-9]+$"))
                {
                    ValidationErrors.Add("NCC ne peut contenir que des lettres et des chiffres");
                }
            }

            // Validation devise
            var validCurrencies = new[] { "XOF", "USD", "EUR", "JPY", "CAD", "GBP", "AUD", "CNH", "CHF", "HKD", "NZD" };
            if (!validCurrencies.Contains(Currency))
            {
                ValidationErrors.Add($"Devise non supportée. Devises valides: {string.Join(", ", validCurrencies)}");
            }

            // Validation moyen de paiement
            var validPaymentMethods = new[] { "cash", "card", "mobile-money", "bank-transfer", "check", "credit" };
            if (!validPaymentMethods.Contains(PaymentMethod))
            {
                ValidationErrors.Add($"Moyen de paiement non supporté. Moyens valides: {string.Join(", ", validPaymentMethods)}");
            }

            // Validation spécifique B2F (clients internationaux)
            if (Template == "B2F")
            {
                if (Currency == "XOF")
                {
                    ValidationErrors.Add("Clients B2F (internationaux) doivent utiliser une devise étrangère");
                }

                if (string.IsNullOrWhiteSpace(ClientNcc))
                {
                    ValidationErrors.Add("NCC obligatoire pour les clients B2F (identification internationale)");
                }

                // Validation pays étranger (non Côte d'Ivoire)
                if (string.IsNullOrWhiteSpace(Country) || Country.ToLower().Contains("côte d'ivoire") || Country.ToLower().Contains("cote d'ivoire"))
                {
                    ValidationErrors.Add("Clients B2F doivent être basés à l'étranger");
                }
            }

            // Validation email si présent
            if (!string.IsNullOrWhiteSpace(Email))
            {
                try
                {
                    var addr = new System.Net.Mail.MailAddress(Email);
                    if (addr.Address != Email)
                    {
                        ValidationErrors.Add("Format d'email invalide");
                    }
                }
                catch
                {
                    ValidationErrors.Add("Format d'email invalide");
                }
            }
        }

        /// <summary>
        /// Convertit vers le modèle Client entity
        /// </summary>
        public FNEV4.Core.Entities.Client ToClientEntity()
        {
            return new FNEV4.Core.Entities.Client
            {
                ClientCode = ClientCode,
                Name = Name,
                ClientType = Template, // B2B, B2C, B2G, B2F
                ClientNcc = ClientNcc,
                CompanyName = CommercialName,
                Address = Address,
                Phone = Phone,
                Email = Email,
                Country = Country,
                DefaultCurrency = Currency,
                DefaultPaymentMethod = PaymentMethod, // Nouveau: mapping du moyen de paiement
                SellerName = Representative,
                TaxIdentificationNumber = TaxNumber,
                IsActive = IsActive,
                Notes = Notes,
                CreatedDate = DateTime.Now,
                LastModifiedDate = DateTime.Now
            };
        }
    }
}
