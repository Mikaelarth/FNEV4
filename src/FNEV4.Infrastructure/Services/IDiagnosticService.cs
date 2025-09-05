using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace FNEV4.Infrastructure.Services
{
    /// <summary>
    /// Interface pour le service de diagnostic système
    /// </summary>
    public interface IDiagnosticService
    {
        /// <summary>
        /// Obtient les informations système complètes
        /// </summary>
        Task<SystemInfo> GetSystemInfoAsync();

        /// <summary>
        /// Teste la connectivité de la base de données
        /// </summary>
        Task<DiagnosticResult> TestDatabaseConnectionAsync();

        /// <summary>
        /// Teste la connectivité réseau
        /// </summary>
        Task<DiagnosticResult> TestNetworkConnectivityAsync();

        /// <summary>
        /// Teste l'API FNE
        /// </summary>
        Task<DiagnosticResult> TestApiConnectivityAsync();

        /// <summary>
        /// Nettoie le cache de l'application
        /// </summary>
        Task<DiagnosticResult> CleanCacheAsync();

        /// <summary>
        /// Compacte la base de données
        /// </summary>
        Task<DiagnosticResult> CompactDatabaseAsync();

        /// <summary>
        /// Vérifie l'intégrité des données
        /// </summary>
        Task<DiagnosticResult> CheckDataIntegrityAsync();

        /// <summary>
        /// Exécute un diagnostic complet du système
        /// </summary>
        Task<CompleteDiagnostic> RunCompleteDiagnosticAsync();
    }

    /// <summary>
    /// Informations système complètes
    /// </summary>
    public class SystemInfo
    {
        public string ApplicationVersion { get; set; } = string.Empty;
        public TimeSpan Uptime { get; set; }
        public long MemoryUsageBytes { get; set; }
        public string MemoryUsageFormatted => FormatBytes(MemoryUsageBytes);
        public double CpuUsagePercent { get; set; }
        public string MachineName { get; set; } = Environment.MachineName;
        public string UserName { get; set; } = Environment.UserName;
        public string OperatingSystem { get; set; } = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        public string DotNetVersion { get; set; } = Environment.Version.ToString();
        public int ProcessorCount { get; set; } = Environment.ProcessorCount;
        public long TotalPhysicalMemory { get; set; }
        public long AvailablePhysicalMemory { get; set; }
        public List<DriveInfo> DriveInfos { get; set; } = new();
        public DateTime LastBootTime { get; set; }

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }
    }

    /// <summary>
    /// Résultat d'un test de diagnostic
    /// </summary>
    public class DiagnosticResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public DiagnosticSeverity Severity { get; set; } = DiagnosticSeverity.Info;
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    /// <summary>
    /// Résultat d'un diagnostic complet
    /// </summary>
    public class CompleteDiagnostic
    {
        public SystemInfo SystemInfo { get; set; } = new();
        public DiagnosticResult DatabaseTest { get; set; } = new();
        public DiagnosticResult NetworkTest { get; set; } = new();
        public DiagnosticResult ApiTest { get; set; } = new();
        public DiagnosticResult IntegrityCheck { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TotalDuration => EndTime - StartTime;
        public bool OverallSuccess => DatabaseTest.Success && NetworkTest.Success && ApiTest.Success && IntegrityCheck.Success;
        public int IssuesFound { get; set; }
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Sévérité du diagnostic
    /// </summary>
    public enum DiagnosticSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
}
