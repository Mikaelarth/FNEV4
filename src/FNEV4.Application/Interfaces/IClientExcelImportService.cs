using FNEV4.Application.DTOs.GestionClients;

namespace FNEV4.Application.Interfaces
{
    /// <summary>
    /// Interface pour le service d'import Excel des clients
    /// Respecte l'architecture Clean en définissant le contrat dans Application
    /// </summary>
    public interface IClientExcelImportService
    {
        /// <summary>
        /// Génère un aperçu du fichier Excel sans importer les données
        /// </summary>
        Task<ClientImportPreviewDto> PreviewFileAsync(string filePath);

        /// <summary>
        /// Lit et parse le fichier Excel
        /// </summary>
        Task<List<ClientImportModel>> ReadExcelFileAsync(string filePath);

        /// <summary>
        /// Valide les données avant import
        /// </summary>
        Task<List<ClientImportModel>> ValidateDataAsync(List<ClientImportModel> clients);

        /// <summary>
        /// Exporte un modèle Excel vierge
        /// </summary>
        Task ExportTemplateAsync(string filePath);
    }
}
