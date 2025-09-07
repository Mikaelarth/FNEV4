using FNEV4.Core.Models.ImportTraitement;
using System.Threading.Tasks;

namespace FNEV4.Application.Services.ImportTraitement
{
    /// <summary>
    /// Interface pour le service d'import des factures Sage 100 v15
    /// </summary>
    public interface ISage100ImportService
    {
        /// <summary>
        /// Importe un fichier Excel Sage 100 v15
        /// </summary>
        /// <param name="filePath">Chemin vers le fichier Excel</param>
        /// <returns>Résultat de l'import avec détails</returns>
        Task<Sage100ImportResult> ImportSage100FileAsync(string filePath);

        /// <summary>
        /// Valide la structure d'un fichier Sage 100 v15
        /// </summary>
        /// <param name="filePath">Chemin vers le fichier Excel</param>
        /// <returns>Résultat de la validation</returns>
        Task<Sage100ValidationResult> ValidateFileStructureAsync(string filePath);

        /// <summary>
        /// Obtient un aperçu des données sans les importer
        /// </summary>
        /// <param name="filePath">Chemin vers le fichier Excel</param>
        /// <returns>Aperçu des factures détectées</returns>
        Task<Sage100PreviewResult> PreviewFileAsync(string filePath);
    }
}
