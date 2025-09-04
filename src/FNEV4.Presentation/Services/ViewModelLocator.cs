using Microsoft.Extensions.DependencyInjection;
using FNEV4.Presentation.ViewModels.Maintenance;
using System;

namespace FNEV4.Presentation.Services
{
    /// <summary>
    /// Service locator pour les ViewModels avec injection de dépendances
    /// </summary>
    public class ViewModelLocator
    {
        private readonly IServiceProvider _serviceProvider;

        public ViewModelLocator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// ViewModel pour la gestion de la base de données
        /// </summary>
        public BaseDonneesViewModel BaseDonneesViewModel => 
            _serviceProvider.GetRequiredService<BaseDonneesViewModel>();

        /// <summary>
        /// Méthode générique pour récupérer n'importe quel ViewModel
        /// </summary>
        /// <typeparam name="T">Type du ViewModel</typeparam>
        /// <returns>Instance du ViewModel avec ses dépendances injectées</returns>
        public T GetViewModel<T>() where T : class
        {
            return _serviceProvider.GetRequiredService<T>();
        }
    }
}
