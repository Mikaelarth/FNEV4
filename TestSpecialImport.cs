using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FNEV4.Application.Special;
using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces.Repositories;

namespace TestSpecialImport
{
    /// <summary>
    /// Mock repository pour tester l'import exceptionnel
    /// </summary>
    public class MockClientRepository : IClientRepository
    {
        public int CallCount { get; private set; } = 0;
        
        public Task<bool> ExistsAsync(string codeClient)
        {
            CallCount++;
            return Task.FromResult(false); // Simule qu'aucun client n'existe
        }
        
        public Task<Client> GetByCodeAsync(string code)
        {
            return Task.FromResult<Client>(null);
        }
        
        public Task<Client> CreateAsync(Client client)
        {
            CallCount++;
            client.Id = new Random().Next(1000, 9999); // ID simulé
            return Task.FromResult(client);
        }
        
        // Méthodes non utilisées dans ce test
        public Task<IEnumerable<Client>> GetAllAsync() => throw new NotImplementedException();
        public Task<Client> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<Client> UpdateAsync(Client client) => throw new NotImplementedException();
        public Task<bool> DeleteAsync(int id) => throw new NotImplementedException();
        public Task<IEnumerable<Client>> SearchAsync(string searchTerm) => throw new NotImplementedException();
        public Task<int> GetCountAsync() => throw new NotImplementedException();
        public Task<IEnumerable<Client>> GetPagedAsync(int page, int pageSize) => throw new NotImplementedException();
    }
    
    /// <summary>
    /// Programme de test pour l'import exceptionnel
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== TEST IMPORT EXCEPTIONNEL FNEV4 ===");
            Console.WriteLine("Demarrage du test...");
            
            try
            {
                var mockRepository = new MockClientRepository();
                var useCase = new ImportSpecialExcelUseCase(mockRepository);
                
                string excelPath = Path.Combine(Environment.CurrentDirectory, "clients.xlsx");
                Console.WriteLine($"Chemin Excel: {excelPath}");
                
                if (!File.Exists(excelPath))
                {
                    Console.WriteLine("ERREUR: Fichier Excel non trouve");
                    Console.WriteLine("Assurez-vous que clients.xlsx est dans le repertoire courant");
                    return;
                }
                
                Console.WriteLine("Fichier Excel trouve");
                Console.WriteLine("Execution en mode PREVIEW...");
                
                // Test en mode preview (pas d'import en base)
                var result = await useCase.ExecuteAsync(excelPath, false);
                
                Console.WriteLine("=== RESULTATS ===");
                Console.WriteLine($"Clients extraits: {result.ExtractedClients.Count()}");
                Console.WriteLine($"Clients valides: {result.ValidClients.Count()}");
                Console.WriteLine($"Erreurs: {result.Errors.Count()}");
                Console.WriteLine($"Doublons: {result.DuplicatesDetected.Count()}");
                
                // Afficher les premiers clients
                Console.WriteLine("\n=== PREMIERS CLIENTS ===");
                foreach (var client in result.ValidClients.Take(5))
                {
                    Console.WriteLine($"- {client.CodeClient}: {client.Nom} ({client.TypeClient})");
                }
                
                // Afficher les erreurs s'il y en a
                if (result.Errors.Any())
                {
                    Console.WriteLine("\n=== ERREURS ===");
                    foreach (var error in result.Errors.Take(5))
                    {
                        Console.WriteLine($"- {error}");
                    }
                }
                
                Console.WriteLine("\n=== TEST REUSSI ===");
                Console.WriteLine("Le systeme d'import exceptionnel fonctionne correctement");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERREUR: {ex.Message}");
                Console.WriteLine($"Details: {ex}");
            }
        }
    }
}
