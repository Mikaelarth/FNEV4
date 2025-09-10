using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using FNEV4.Core.Models.ImportTraitement;

namespace FNEV4.Presentation.ViewModels.ImportTraitement
{
    public class Sage100FactureDetailsViewModel : INotifyPropertyChanged
    {
        private Sage100FacturePreview _facture;
        private ObservableCollection<Sage100ProduitData> _produits;

        public Sage100FactureDetailsViewModel(Sage100FacturePreview facture)
        {
            _facture = facture ?? throw new ArgumentNullException(nameof(facture));
            _produits = new ObservableCollection<Sage100ProduitData>(facture.Produits ?? new List<Sage100ProduitData>());
            
            InitializeCommands();
            CalculateProperties();
        }

        public Sage100FacturePreview Facture
        {
            get => _facture;
            set
            {
                _facture = value;
                OnPropertyChanged();
                CalculateProperties();
            }
        }

        public ObservableCollection<Sage100ProduitData> Produits
        {
            get => _produits;
            set
            {
                _produits = value;
                OnPropertyChanged();
            }
        }

        // Propriétés calculées pour l'affichage
        public string FactureTitle => $"Facture N° {Facture?.NumeroFacture}";
        
        public string FactureSubtitle => $"Client: {Facture?.NomClient} ({Facture?.CodeClient})";

        public string StatusText
        {
            get
            {
                if (Facture?.EstValide == true)
                    return "VALIDE";
                else
                    return "INVALIDE";
            }
        }

        public Brush StatusBackground
        {
            get
            {
                if (Facture?.EstValide == true)
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Vert
                else
                    return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Rouge
            }
        }

        public Brush TemplateBackground
        {
            get
            {
                return Facture?.Template switch
                {
                    "B2B" => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Bleu
                    "B2C" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),  // Vert
                    "B2F" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),  // Orange
                    _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))     // Gris
                };
            }
        }

        public decimal MontantTVA
        {
            get
            {
                return Facture.MontantTVA;
            }
        }

        // Commandes
        public ICommand ExporterCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            ExporterCommand = new RelayCommand(ExecuteExporter, CanExecuteExporter);
        }

        private bool CanExecuteExporter()
        {
            return Facture != null && Produits?.Count > 0;
        }

        private void ExecuteExporter()
        {
            // TODO: Implémenter l'export Excel
            // Pour l'instant, on peut afficher un message
            System.Windows.MessageBox.Show(
                "Fonctionnalité d'export en cours de développement.",
                "Export Excel",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        private void CalculateProperties()
        {
            OnPropertyChanged(nameof(FactureTitle));
            OnPropertyChanged(nameof(FactureSubtitle));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusBackground));
            OnPropertyChanged(nameof(TemplateBackground));
            OnPropertyChanged(nameof(MontantTVA));
        }

        /// <summary>
        /// Obtient la couleur associée au type de TVA selon les spécifications FNE
        /// </summary>
        /// <param name="codeTva">Code TVA (TVA, TVAB, TVAC, TVAD)</param>
        /// <returns>Couleur correspondante</returns>
        public static Brush GetTvaColorBrush(string codeTva)
        {
            return codeTva?.ToUpper() switch
            {
                "TVA" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),   // Vert - TVA normal 18%
                "TVAB" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),  // Orange - TVA réduit 9%
                "TVAC" => new SolidColorBrush(Color.FromRgb(158, 158, 158)), // Gris - TVA exec conv 0%
                "TVAD" => new SolidColorBrush(Color.FromRgb(96, 125, 139)),  // Gris bleu - TVA exec leg 0%
                _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))       // Gris par défaut
            };
        }

        /// <summary>
        /// Obtient la description complète du type de TVA selon les spécifications FNE
        /// </summary>
        /// <param name="codeTva">Code TVA (TVA, TVAB, TVAC, TVAD)</param>
        /// <returns>Description du type de TVA conforme à la documentation FNE</returns>
        public static string GetTvaDescription(string codeTva)
        {
            return codeTva?.ToUpper() switch
            {
                "TVA" => "TVA normal de 18%",
                "TVAB" => "TVA réduit de 9%",
                "TVAC" => "TVA exec conv de 0%",
                "TVAD" => "TVA exec leg de 0% pour TEE et RME",
                _ => "Type inconnu"
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Commande simplifiée pour WPF
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { System.Windows.Input.CommandManager.RequerySuggested += value; }
            remove { System.Windows.Input.CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute();
        }
    }
}
