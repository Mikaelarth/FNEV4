using Microsoft.EntityFrameworkCore;
using FNEV4.Infrastructure.Data;
using FNEV4.Infrastructure.Services;

// Test simple de création de base de données
var options = new DbContextOptionsBuilder<FNEV4DbContext>()
    .UseSqlite("Data Source=test_temp.db")
    .Options;

using var context = new FNEV4DbContext(options);
var service = new DatabaseService(context);

try 
{
    Console.WriteLine("Création de la base de données...");
    var success = await service.ResetDatabaseAsync();
    Console.WriteLine($"Résultat: {success}");
    
    var info = await service.GetDatabaseInfoAsync();
    Console.WriteLine($"Nombre de tables: {info.TableCount}");
    Console.WriteLine($"Chemin: {info.Path}");
    Console.WriteLine($"État: {info.ConnectionStatus}");
}
catch (Exception ex)
{
    Console.WriteLine($"Erreur: {ex.Message}");
    Console.WriteLine($"Détails: {ex}");
}

Console.WriteLine("Test terminé.");
