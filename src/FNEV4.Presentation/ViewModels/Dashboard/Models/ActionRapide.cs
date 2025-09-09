using System.Windows.Input;
using System.Windows.Media;

namespace FNEV4.Presentation.ViewModels.Dashboard.Models
{
    /// <summary>
    /// Modèle pour une action rapide dans le Dashboard
    /// </summary>
    public class ActionRapide
    {
        /// <summary>
        /// Titre de l'action
        /// </summary>
        public string Titre { get; set; } = string.Empty;

        /// <summary>
        /// Description de l'action
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Icône Material Design pour l'action
        /// </summary>
        public string Icone { get; set; } = "Help";

        /// <summary>
        /// Couleur de fond de l'icône
        /// </summary>
        public Brush Couleur { get; set; } = Brushes.Gray;

        /// <summary>
        /// Commande à exécuter lors du clic
        /// </summary>
        public ICommand? Command { get; set; }

        /// <summary>
        /// Indique si l'action est disponible
        /// </summary>
        public bool EstDisponible { get; set; } = true;

        /// <summary>
        /// Crée une nouvelle action rapide
        /// </summary>
        /// <param name="titre">Titre de l'action</param>
        /// <param name="description">Description de l'action</param>
        /// <param name="icone">Icône Material Design</param>
        /// <param name="couleur">Couleur de fond</param>
        /// <param name="command">Commande à exécuter</param>
        public ActionRapide(string titre, string description, string icone, Brush couleur, ICommand? command = null)
        {
            Titre = titre;
            Description = description;
            Icone = icone;
            Couleur = couleur;
            Command = command;
        }

        /// <summary>
        /// Constructeur par défaut
        /// </summary>
        public ActionRapide()
        {
        }
    }

    /// <summary>
    /// Modèle pour un raccourci dans le Dashboard
    /// </summary>
    public class RaccourciDashboard
    {
        /// <summary>
        /// Nom du raccourci
        /// </summary>
        public string Nom { get; set; } = string.Empty;

        /// <summary>
        /// Icône Material Design pour le raccourci
        /// </summary>
        public string Icone { get; set; } = "Link";

        /// <summary>
        /// Commande à exécuter lors du clic
        /// </summary>
        public ICommand? Command { get; set; }

        /// <summary>
        /// Crée un nouveau raccourci
        /// </summary>
        /// <param name="nom">Nom du raccourci</param>
        /// <param name="icone">Icône Material Design</param>
        /// <param name="command">Commande à exécuter</param>
        public RaccourciDashboard(string nom, string icone, ICommand? command = null)
        {
            Nom = nom;
            Icone = icone;
            Command = command;
        }

        /// <summary>
        /// Constructeur par défaut
        /// </summary>
        public RaccourciDashboard()
        {
        }
    }
}
