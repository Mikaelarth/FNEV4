using System;
using System.Linq;
using System.Threading.Tasks;
using FNEV4.Core.Interfaces;
using FNEV4.Core.Models.ImportTraitement;
using FNEV4.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FNEV4.Infrastructure.Services.ImportTraitement
{
    /// <summary>
    /// Service pour récupérer les templates clients depuis la base de données FNEV4
    /// Plus efficace que l'approche Python pour les requêtes individuelles
    /// </summary>
    public class ClientTemplateService
    {
        private readonly ILoggingService _loggingService;
        private readonly FNEV4DbContext _context;

        public ClientTemplateService(ILoggingService loggingService, FNEV4DbContext context)
        {
            _loggingService = loggingService;
            _context = context;
        }

        /// <summary>
        /// Récupère les informations de template pour un client depuis la base FNEV4
        /// </summary>
        public async Task<ClientTemplateInfo?> GetClientTemplateAsync(string clientCode)
        {
            try
            {
                // Client divers (1999) est toujours considéré comme valide
                if (clientCode == "1999")
                {
                    return new ClientTemplateInfo
                    {
                        ClientCode = "1999",
                        Template = "B2C",
                        NomCommercial = "CLIENT DIVERS",
                        Ncc = "",
                        Active = true,
                        Exists = true
                    };
                }

                // Requête pour les autres clients - DOIT exister en base
                var client = await _context.Clients
                    .Where(c => c.ClientCode == clientCode)
                    .Select(c => new ClientTemplateInfo
                    {
                        ClientCode = c.ClientCode,
                        Template = c.DefaultTemplate ?? "B2C",
                        NomCommercial = c.Name ?? "",
                        Ncc = c.ClientNcc ?? "",
                        Active = c.IsActive,
                        Exists = true
                    })
                    .FirstOrDefaultAsync();

                // Si client non trouvé, retourner null (= client inexistant)
                return client;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Erreur récupération template client {clientCode}: {ex.Message}", "ClientTemplate", ex);
                return null;
            }
        }

        /// <summary>
        /// Détermine le template par défaut selon les règles métier
        /// </summary>
        public string GetDefaultTemplate(string clientCode)
        {
            // Logique métier pour déterminer le template par défaut
            if (clientCode == "1999")
                return "B2C"; // Client divers

            if (clientCode.StartsWith("GOV") || clientCode.StartsWith("ETAT"))
                return "B2G"; // Gouvernement

            if (clientCode.StartsWith("INT") || clientCode.StartsWith("EXT"))
                return "B2F"; // International

            return "B2B"; // Entreprise par défaut
        }
    }
}
