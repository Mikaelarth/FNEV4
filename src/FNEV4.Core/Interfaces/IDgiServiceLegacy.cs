using System;
using System.Threading.Tasks;

namespace FNEV4.Core.Interfaces
{
    /// <summary>
    /// Interface legacy pour les services DGI (utilisée par EntrepriseConfigViewModel)
    /// Pour compatibilité temporaire
    /// </summary>
    public interface IDgiService
    {
        /// <summary>
        /// Vérifie la validité d'un NCC
        /// </summary>
        Task<bool> VerifyNccAsync(string ncc);

        /// <summary>
        /// Teste la connexion à l'API DGI
        /// </summary>
        Task<bool> TestConnectionAsync();
    }
}