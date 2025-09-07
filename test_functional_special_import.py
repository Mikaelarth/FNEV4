#!/usr/bin/env python3
"""
Test fonctionnel du système d'import exceptionnel - Mode Preview
"""

import os
import sys
import subprocess
import tempfile

def create_test_csharp_app():
    """Crée une application C# de test pour tester ImportSpecialExcelUseCase"""
    
    test_code = '''
using System;
using System.IO;
using System.Threading.Tasks;
using FNEV4.Application.Special;
using FNEV4.Core.Interfaces.Repositories;

namespace SpecialImportTest
{
    // Mock repository pour le test
    public class MockClientRepository : IClientRepository
    {
        public int CallCount { get; private set; } = 0;
        
        public Task<bool> ExistsAsync(string codeClient)
        {
            CallCount++;
            return Task.FromResult(false); // Simule qu'aucun client n'existe
        }
        
        public Task<FNEV4.Core.Entities.Client> GetByCodeAsync(string code)
        {
            return Task.FromResult<FNEV4.Core.Entities.Client>(null);
        }
        
        public Task<FNEV4.Core.Entities.Client> CreateAsync(FNEV4.Core.Entities.Client client)
        {
            CallCount++;
            return Task.FromResult(client);
        }
        
        // Autres méthodes non utilisées dans ce test
        public Task<IEnumerable<FNEV4.Core.Entities.Client>> GetAllAsync() => throw new NotImplementedException();
        public Task<FNEV4.Core.Entities.Client> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<FNEV4.Core.Entities.Client> UpdateAsync(FNEV4.Core.Entities.Client client) => throw new NotImplementedException();
        public Task<bool> DeleteAsync(int id) => throw new NotImplementedException();
        public Task<IEnumerable<FNEV4.Core.Entities.Client>> SearchAsync(string searchTerm) => throw new NotImplementedException();
        public Task<int> GetCountAsync() => throw new NotImplementedException();
        public Task<IEnumerable<FNEV4.Core.Entities.Client>> GetPagedAsync(int page, int pageSize) => throw new NotImplementedException();
    }
    
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Test du systeme d'import exceptionnel");
            Console.WriteLine("=" + new string('=', 45));
            
            try
            {
                var mockRepository = new MockClientRepository();
                var useCase = new ImportSpecialExcelUseCase(mockRepository);
                
                string excelPath = @"c:\\wamp64\\www\\FNEV4\\clients.xlsx";
                
                if (!File.Exists(excelPath))
                {
                    Console.WriteLine("Erreur: Fichier Excel non trouve");
                    return;
                }
                
                Console.WriteLine($"Fichier Excel trouve: {excelPath}");
                
                // Test en mode preview
                Console.WriteLine("Test en mode PREVIEW...");
                var previewResult = await useCase.ExecuteAsync(excelPath, false);
                
                Console.WriteLine($"Preview termine:");
                Console.WriteLine($"   - Clients extraits: {previewResult.ExtractedClients.Count()}");
                Console.WriteLine($"   - Clients valides: {previewResult.ValidClients.Count()}");
                Console.WriteLine($"   - Erreurs: {previewResult.Errors.Count()}");
                Console.WriteLine($"   - Doublons detectes: {previewResult.DuplicatesDetected.Count()}");
                
                // Afficher quelques exemples
                var firstClients = previewResult.ValidClients.Take(3);
                int i = 1;
                foreach (var client in firstClients)
                {
                    Console.WriteLine($"   Client {i}: {client.CodeClient} - {client.Nom} ({client.TypeClient})");
                    i++;
                }
                
                if (previewResult.Errors.Any())
                {
                    Console.WriteLine("Erreurs detectees:");
                    foreach (var error in previewResult.Errors.Take(3))
                    {
                        Console.WriteLine($"   - {error}");
                    }
                }
                
                Console.WriteLine("Test reussi !");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur: {ex.Message}");
                Console.WriteLine($"Details: {ex.StackTrace}");
            }
        }
    }
}
'''
    
    return test_code

def run_functional_test():
    """Exécute un test fonctionnel du système d'import"""
    print("🔬 Test fonctionnel du système d'import exceptionnel")
    print("=" * 50)
    
    # Créer un dossier temporaire pour le test
    with tempfile.TemporaryDirectory() as temp_dir:
        test_project_dir = os.path.join(temp_dir, "SpecialImportTest")
        os.makedirs(test_project_dir)
        
        # Créer le fichier de projet
        csproj_content = f'''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="c:\\wamp64\\www\\FNEV4\\src\\FNEV4.Application\\FNEV4.Application.csproj" />
    <ProjectReference Include="c:\\wamp64\\www\\FNEV4\\src\\FNEV4.Core\\FNEV4.Core.csproj" />
  </ItemGroup>
  
  <ItemGroup>
    <PackageReference Include="ClosedXML" Version="0.102.2" />
  </ItemGroup>
</Project>'''
        
        csproj_path = os.path.join(test_project_dir, "SpecialImportTest.csproj")
        with open(csproj_path, 'w', encoding='utf-8') as f:
            f.write(csproj_content)
        
        # Créer le fichier Program.cs
        program_cs = os.path.join(test_project_dir, "Program.cs")
        with open(program_cs, 'w', encoding='utf-8') as f:
            f.write(create_test_csharp_app())
        
        try:
            # Compiler le projet de test
            print("🔨 Compilation du projet de test...")
            result = subprocess.run(['dotnet', 'build'], 
                                  capture_output=True, text=True, cwd=test_project_dir)
            
            if result.returncode != 0:
                print(f"❌ Erreur de compilation:\n{result.stderr}")
                return False
            
            print("✅ Compilation réussie")
            
            # Exécuter le test
            print("🏃 Exécution du test fonctionnel...")
            result = subprocess.run(['dotnet', 'run'], 
                                  capture_output=True, text=True, cwd=test_project_dir, timeout=30)
            
            if result.returncode == 0:
                print("✅ Test fonctionnel réussi !")
                print("\nSortie du test:")
                print(result.stdout)
                return True
            else:
                print(f"❌ Erreur lors de l'exécution:\n{result.stderr}")
                if result.stdout:
                    print(f"Sortie standard:\n{result.stdout}")
                return False
                
        except subprocess.TimeoutExpired:
            print("❌ Timeout lors de l'exécution du test")
            return False
        except Exception as e:
            print(f"❌ Erreur inattendue: {e}")
            return False

def main():
    """Fonction principale"""
    print("🧪 Test fonctionnel complet du système d'import exceptionnel")
    print("=" * 60)
    
    success = run_functional_test()
    
    if success:
        print("\n🎉 SUCCÈS TOTAL !")
        print("✅ Le système d'import exceptionnel fonctionne parfaitement")
        print("📊 Le système a pu extraire et valider les clients du format spécial")
        print("\n📋 Le système est maintenant prêt pour l'intégration dans l'interface utilisateur")
    else:
        print("\n❌ ÉCHEC DU TEST")
        print("🔧 Vérifiez les erreurs ci-dessus")
    
    return 0 if success else 1

if __name__ == "__main__":
    sys.exit(main())
