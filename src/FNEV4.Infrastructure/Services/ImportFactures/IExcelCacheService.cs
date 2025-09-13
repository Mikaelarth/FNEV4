using FNEV4.Core.Models.ImportTraitement;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FNEV4.Infrastructure.Services.ImportFactures
{
    /// <summary>
    /// Données en cache pour un fichier Excel
    /// </summary>
    public class CachedExcelData
    {
        public string FilePath { get; set; } = string.Empty;
        public DateTime FileLastModified { get; set; }
        public DateTime CachedAt { get; set; }
        public Sage100ValidationResult? ValidationResult { get; set; }
        public Sage100PreviewResult? PreviewResult { get; set; }
        public List<Sage100FacturePreview> Factures { get; set; } = new();
        public bool IsValid => ValidationResult?.IsValid == true;
        public TimeSpan Age => DateTime.Now - CachedAt;
    }

    /// <summary>
    /// Service de cache pour éviter les réanalyses répétées des fichiers Excel
    /// </summary>
    public interface IExcelCacheService
    {
        /// <summary>
        /// Récupère les données en cache ou les charge si nécessaire
        /// </summary>
        Task<CachedExcelData> GetOrLoadAsync(string filePath, Func<string, Task<CachedExcelData>> loader);

        /// <summary>
        /// Invalide le cache pour un fichier spécifique
        /// </summary>
        void InvalidateCache(string filePath);

        /// <summary>
        /// Vide tout le cache
        /// </summary>
        void ClearAll();

        /// <summary>
        /// Nettoie le cache des entrées anciennes
        /// </summary>
        void CleanupOldEntries(TimeSpan maxAge);

        /// <summary>
        /// Vérifie si un fichier est en cache et valide
        /// </summary>
        bool IsCached(string filePath);

        /// <summary>
        /// Statistiques du cache
        /// </summary>
        (int Count, long SizeInBytes, TimeSpan OldestEntry) GetStats();
    }
}