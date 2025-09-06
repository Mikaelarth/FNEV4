using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace FNEV4.Core.Entities
{
    /// <summary>
    /// Entité représentant la configuration des chemins et dossiers de l'application FNEV4.
    /// Gère tous les chemins d'import, export, archivage, logs et backup.
    /// </summary>
    public class FolderConfiguration : BaseEntity
    {
        #region Chemins Principaux

        /// <summary>
        /// Chemin du dossier d'import des fichiers Excel Sage 100.
        /// </summary>
        [Required]
        [StringLength(500)]
        public string ImportFolderPath { get; set; } = string.Empty;

        /// <summary>
        /// Chemin du dossier d'export des factures certifiées.
        /// </summary>
        [Required]
        [StringLength(500)]
        public string ExportFolderPath { get; set; } = string.Empty;

        /// <summary>
        /// Chemin du dossier d'archivage des anciens fichiers.
        /// </summary>
        [Required]
        [StringLength(500)]
        public string ArchiveFolderPath { get; set; } = string.Empty;

        /// <summary>
        /// Chemin du dossier des logs applicatifs.
        /// </summary>
        [Required]
        [StringLength(500)]
        public string LogsFolderPath { get; set; } = string.Empty;

        /// <summary>
        /// Chemin du dossier de sauvegarde de la base de données.
        /// </summary>
        [Required]
        [StringLength(500)]
        public string BackupFolderPath { get; set; } = string.Empty;

        #endregion

        #region Options de Surveillance

        /// <summary>
        /// Indique si la surveillance automatique du dossier d'import est activée.
        /// </summary>
        public bool ImportFolderWatchEnabled { get; set; } = true;

        /// <summary>
        /// Indique si l'organisation automatique des exports est activée.
        /// </summary>
        public bool ExportAutoOrganizeEnabled { get; set; } = true;

        /// <summary>
        /// Indique si l'archivage automatique est activé.
        /// </summary>
        public bool ArchiveAutoEnabled { get; set; } = false;

        /// <summary>
        /// Indique si la rotation automatique des logs est activée.
        /// </summary>
        public bool LogRotationEnabled { get; set; } = true;

        /// <summary>
        /// Indique si le logging hybride est activé (Error/Warning → DB+Files, Info/Debug/Trace → Files uniquement).
        /// </summary>
        public bool HybridLoggingEnabled { get; set; } = true;

        /// <summary>
        /// Indique si la sauvegarde automatique est activée.
        /// </summary>
        public bool BackupAutoEnabled { get; set; } = true;

        #endregion

        #region Paramètres de Gestion

        /// <summary>
        /// Période de rétention pour l'archivage automatique (ex: "30 jours").
        /// </summary>
        [StringLength(20)]
        public string SelectedArchivePeriod { get; set; } = "30 jours";

        /// <summary>
        /// Niveau de log sélectionné (ex: "Information").
        /// </summary>
        [StringLength(20)]
        public string SelectedLogLevel { get; set; } = "Information";

        /// <summary>
        /// Fréquence de sauvegarde automatique (ex: "Quotidien").
        /// </summary>
        [StringLength(30)]
        public string SelectedBackupFrequency { get; set; } = "Quotidien";

        /// <summary>
        /// Modèle de nommage pour les fichiers exportés.
        /// </summary>
        [StringLength(200)]
        public string SelectedExportNaming { get; set; } = "{Date}_{NomClient}_{NumFacture}.pdf";

        /// <summary>
        /// Modèle d'organisation pour l'archivage.
        /// </summary>
        [StringLength(200)]
        public string SelectedArchiveOrganization { get; set; } = "{Annee}/{Mois}/{Client}";

        #endregion

        #region Statistiques de Surveillance (Non persistées)

        /// <summary>
        /// Nombre de fichiers en surveillance dans le dossier d'import.
        /// </summary>
        public int ImportFileCount { get; set; }

        /// <summary>
        /// Taille totale des fichiers d'export en bytes.
        /// </summary>
        public long ExportTotalSize { get; set; }

        /// <summary>
        /// Nombre de fichiers archivés.
        /// </summary>
        public int ArchiveFileCount { get; set; }

        /// <summary>
        /// Taille totale des logs en bytes.
        /// </summary>
        public long LogsTotalSize { get; set; }

        /// <summary>
        /// Date de la dernière sauvegarde.
        /// </summary>
        public DateTime? LastBackupDate { get; set; }

        /// <summary>
        /// Taille de la dernière sauvegarde en bytes.
        /// </summary>
        public long LastBackupSize { get; set; }

        #endregion

        #region Métadonnées de Configuration

        /// <summary>
        /// Version de la configuration des chemins.
        /// </summary>
        [StringLength(10)]
        public string ConfigurationVersion { get; set; } = "1.0";

        /// <summary>
        /// Indique si la configuration a été validée.
        /// </summary>
        public bool IsValidated { get; set; } = false;

        /// <summary>
        /// Date de la dernière validation de la configuration.
        /// </summary>
        public DateTime? LastValidationDate { get; set; }

        /// <summary>
        /// Messages d'erreur de la dernière validation.
        /// </summary>
        [StringLength(1000)]
        public string ValidationErrors { get; set; } = string.Empty;

        #endregion

        #region Constructeur

        /// <summary>
        /// Initialise une nouvelle instance de FolderConfiguration avec des valeurs par défaut.
        /// </summary>
        public FolderConfiguration()
        {
            // Génération des chemins par défaut basés sur le répertoire de l'application
            var appPath = AppDomain.CurrentDomain.BaseDirectory;
            var dataPath = Path.Combine(appPath, "Data");

            ImportFolderPath = Path.Combine(dataPath, "Import");
            ExportFolderPath = Path.Combine(dataPath, "Export");
            ArchiveFolderPath = Path.Combine(dataPath, "Archive");
            LogsFolderPath = Path.Combine(dataPath, "Logs");
            BackupFolderPath = Path.Combine(dataPath, "Backup");
        }

        #endregion

        #region Méthodes de Validation

        /// <summary>
        /// Valide la configuration et retourne les erreurs éventuelles.
        /// </summary>
        /// <returns>Liste des erreurs de validation</returns>
        public List<string> ValidateConfiguration()
        {
            var errors = new List<string>();

            // Validation des chemins obligatoires
            if (string.IsNullOrWhiteSpace(ImportFolderPath))
                errors.Add("Le chemin du dossier d'import est obligatoire");

            if (string.IsNullOrWhiteSpace(ExportFolderPath))
                errors.Add("Le chemin du dossier d'export est obligatoire");

            if (string.IsNullOrWhiteSpace(LogsFolderPath))
                errors.Add("Le chemin du dossier des logs est obligatoire");

            if (string.IsNullOrWhiteSpace(BackupFolderPath))
                errors.Add("Le chemin du dossier de sauvegarde est obligatoire");

            // Validation des modèles de nommage
            if (string.IsNullOrWhiteSpace(SelectedExportNaming))
                errors.Add("Le modèle de nommage d'export est obligatoire");

            if (string.IsNullOrWhiteSpace(SelectedArchiveOrganization))
                errors.Add("Le modèle d'organisation d'archivage est obligatoire");

            // Validation des paramètres de période
            if (ArchiveAutoEnabled && string.IsNullOrWhiteSpace(SelectedArchivePeriod))
                errors.Add("La période d'archivage est obligatoire quand l'archivage automatique est activé");

            if (BackupAutoEnabled && string.IsNullOrWhiteSpace(SelectedBackupFrequency))
                errors.Add("La fréquence de sauvegarde est obligatoire quand la sauvegarde automatique est activée");

            return errors;
        }

        /// <summary>
        /// Indique si la configuration est valide.
        /// </summary>
        /// <returns>True si la configuration est valide</returns>
        public bool IsValid()
        {
            return ValidateConfiguration().Count == 0;
        }

        #endregion

        #region Méthodes Utilitaires

        /// <summary>
        /// Crée tous les dossiers de la configuration s'ils n'existent pas.
        /// </summary>
        /// <returns>Nombre de dossiers créés</returns>
        public int CreateAllFolders()
        {
            var created = 0;
            var folders = new[] { ImportFolderPath, ExportFolderPath, ArchiveFolderPath, LogsFolderPath, BackupFolderPath };

            foreach (var folder in folders)
            {
                if (!string.IsNullOrWhiteSpace(folder) && !Directory.Exists(folder))
                {
                    try
                    {
                        Directory.CreateDirectory(folder);
                        created++;
                    }
                    catch
                    {
                        // Ignorer les erreurs de création pour l'instant
                    }
                }
            }

            return created;
        }

        /// <summary>
        /// Vérifie l'existence de tous les dossiers configurés.
        /// </summary>
        /// <returns>Dictionnaire avec le statut de chaque dossier</returns>
        public Dictionary<string, bool> CheckFoldersExistence()
        {
            return new Dictionary<string, bool>
            {
                { "Import", Directory.Exists(ImportFolderPath) },
                { "Export", Directory.Exists(ExportFolderPath) },
                { "Archive", Directory.Exists(ArchiveFolderPath) },
                { "Logs", Directory.Exists(LogsFolderPath) },
                { "Backup", Directory.Exists(BackupFolderPath) }
            };
        }

        /// <summary>
        /// Génère un nom de fichier d'export basé sur le modèle configuré.
        /// </summary>
        /// <param name="invoiceNumber">Numéro de facture</param>
        /// <param name="clientName">Nom du client</param>
        /// <param name="clientNcc">NCC du client</param>
        /// <param name="date">Date de la facture</param>
        /// <returns>Nom de fichier formaté</returns>
        public string GenerateExportFileName(string invoiceNumber, string clientName, string clientNcc, DateTime date)
        {
            return SelectedExportNaming
                .Replace("{Date}", date.ToString("yyyyMMdd"))
                .Replace("{NomClient}", SanitizeFileName(clientName))
                .Replace("{NumFacture}", invoiceNumber)
                .Replace("{NCC}", clientNcc)
                .Replace("{Annee}", date.Year.ToString())
                .Replace("{Mois}", date.Month.ToString("D2"))
                .Replace("{Jour}", date.Day.ToString("D2"))
                .Replace("{Timestamp}", DateTime.Now.ToString("HHmmss"));
        }

        /// <summary>
        /// Génère un chemin d'archivage basé sur le modèle configuré.
        /// </summary>
        /// <param name="clientName">Nom du client</param>
        /// <param name="date">Date de la facture</param>
        /// <param name="status">Statut de la facture</param>
        /// <returns>Chemin d'archivage relatif</returns>
        public string GenerateArchivePath(string clientName, DateTime date, string status = "Certified")
        {
            var relativePath = SelectedArchiveOrganization
                .Replace("{Annee}", date.Year.ToString())
                .Replace("{Mois}", date.Month.ToString("D2"))
                .Replace("{Jour}", date.Day.ToString("D2"))
                .Replace("{Client}", SanitizeFileName(clientName))
                .Replace("{Statut}", status);

            return Path.Combine(ArchiveFolderPath, relativePath);
        }

        /// <summary>
        /// Nettoie un nom de fichier des caractères interdits.
        /// </summary>
        /// <param name="fileName">Nom de fichier à nettoyer</param>
        /// <returns>Nom de fichier nettoyé</returns>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "Unknown";

            var invalidChars = Path.GetInvalidFileNameChars();
            var result = fileName;

            foreach (var invalidChar in invalidChars)
            {
                result = result.Replace(invalidChar, '_');
            }

            // Remplacer les espaces par des underscores
            result = result.Replace(' ', '_');

            // Limiter la longueur
            if (result.Length > 50)
                result = result.Substring(0, 50);

            return result;
        }

        #endregion

        #region Override ToString

        /// <summary>
        /// Retourne une représentation textuelle de la configuration.
        /// </summary>
        /// <returns>Description de la configuration</returns>
        public override string ToString()
        {
            var foldersExist = CheckFoldersExistence();
            var existingCount = foldersExist.Values.Count(exists => exists);
            
            return $"Configuration Chemins & Dossiers - {existingCount}/5 dossiers existants";
        }

        #endregion
    }
}
