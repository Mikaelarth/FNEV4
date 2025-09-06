using System.ComponentModel.DataAnnotations;

namespace FNEV4.Application.DTOs.GestionClients
{
    /// <summary>
    /// Modèle d'import Excel pour les clients
    /// Représente une ligne du fichier Excel à importer
    /// </summary>
    public class ClientImportModel
    {
        [Required(ErrorMessage = "Le code client est obligatoire")]
        public string ClientCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom/raison sociale est obligatoire")]
        public string Name { get; set; } = string.Empty;

        public string ClientType { get; set; } = string.Empty;

        public string ClientNcc { get; set; } = string.Empty;

        public string CommercialName { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;

        public string Country { get; set; } = "Côte d'Ivoire";

        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; } = string.Empty;

        public string Representative { get; set; } = string.Empty;

        public string TaxNumber { get; set; } = string.Empty;

        public string Currency { get; set; } = "XOF";

        public bool IsActive { get; set; } = true;

        public string Notes { get; set; } = string.Empty;

        // Propriétés de suivi
        public int RowNumber { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public bool IsValid => ValidationErrors.Count == 0;
    }
}
