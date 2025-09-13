using System;
using System.Threading.Tasks;

namespace FNEV4.Infrastructure.Services.ImportFactures
{
    /// <summary>
    /// Interface pour rapporter la progression d'une opération
    /// </summary>
    public interface IProgressReporter
    {
        /// <summary>
        /// Met à jour le pourcentage de progression (0-100)
        /// </summary>
        void UpdateProgress(int percentage);

        /// <summary>
        /// Met à jour le message de status
        /// </summary>
        void UpdateStatus(string status);

        /// <summary>
        /// Met à jour la progression avec pourcentage et message
        /// </summary>
        void UpdateProgress(int percentage, string status);

        /// <summary>
        /// Marque l'opération comme terminée
        /// </summary>
        void Complete();

        /// <summary>
        /// Marque l'opération comme échouée
        /// </summary>
        void Failed(string error);

        /// <summary>
        /// Indique si l'opération a été annulée
        /// </summary>
        bool IsCancelled { get; }
    }

    /// <summary>
    /// Service de gestion de la progression des opérations d'import
    /// </summary>
    public interface IProgressService
    {
        /// <summary>
        /// Crée un rapporteur de progression pour une opération
        /// </summary>
        IProgressReporter CreateReporter(string operationName);

        /// <summary>
        /// Exécute une opération avec suivi de progression
        /// </summary>
        Task<T> WithProgressAsync<T>(string operationName, Func<IProgressReporter, Task<T>> operation);

        /// <summary>
        /// Exécute une opération avec suivi de progression (sans résultat)
        /// </summary>
        Task WithProgressAsync(string operationName, Func<IProgressReporter, Task> operation);
    }
}