using FNEV4.Core.Interfaces;
using FNEV4.Core.Interfaces.Services.Fne;
using System.Threading.Tasks;

namespace FNEV4.Infrastructure.Services
{
    /// <summary>
    /// Adaptateur simple pour DgiService qui implémente l'ancienne interface IDgiService
    /// </summary>
    public class DgiServiceLegacyAdapter : FNEV4.Core.Interfaces.IDgiService
    {
        private readonly FNEV4.Core.Interfaces.Services.Fne.IDgiService _dgiService;

        public DgiServiceLegacyAdapter(FNEV4.Core.Interfaces.Services.Fne.IDgiService dgiService)
        {
            _dgiService = dgiService;
        }

        public async Task<bool> VerifyNccAsync(string ncc)
        {
            var result = await _dgiService.VerifyNccAsync(ncc);
            return result.IsValid;
        }

        public async Task<bool> TestConnectionAsync()
        {
            return await _dgiService.TestConnectionAsync();
        }
    }
}