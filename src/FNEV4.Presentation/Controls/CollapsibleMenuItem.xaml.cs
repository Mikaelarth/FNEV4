using System.Windows;
using System.Windows.Controls;

namespace FNEV4.Presentation.Controls
{
    /// <summary>
    /// Élément de menu collapsible qui s'adapte à l'état du menu principal
    /// </summary>
    public partial class CollapsibleMenuItem : UserControl
    {
        public static readonly DependencyProperty IsMenuExpandedProperty =
            DependencyProperty.Register("IsMenuExpanded", typeof(bool), typeof(CollapsibleMenuItem), 
                new PropertyMetadata(true, OnIsMenuExpandedChanged));

        public static readonly DependencyProperty IconKindProperty =
            DependencyProperty.Register("IconKind", typeof(object), typeof(CollapsibleMenuItem));

        public static readonly DependencyProperty HeaderTextProperty =
            DependencyProperty.Register("HeaderText", typeof(string), typeof(CollapsibleMenuItem));

        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register("IsExpanded", typeof(bool), typeof(CollapsibleMenuItem), 
                new PropertyMetadata(false, OnIsExpandedChanged));

        public bool IsMenuExpanded
        {
            get { return (bool)GetValue(IsMenuExpandedProperty); }
            set { SetValue(IsMenuExpandedProperty, value); }
        }

        public object IconKind
        {
            get { return GetValue(IconKindProperty); }
            set { SetValue(IconKindProperty, value); }
        }

        public string HeaderText
        {
            get { return (string)GetValue(HeaderTextProperty); }
            set { SetValue(HeaderTextProperty, value); }
        }

        public bool IsExpanded
        {
            get { return (bool)GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        public CollapsibleMenuItem()
        {
            InitializeComponent();
        }

        private static void OnIsMenuExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CollapsibleMenuItem item)
            {
                // Si le menu principal se réduit, fermer les sections ouvertes
                if (!(bool)e.NewValue)
                {
                    item.IsExpanded = false;
                }
            }
        }

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CollapsibleMenuItem item)
            {
                item.UpdateExpandedState();
            }
        }

        private void UpdateExpandedState()
        {
            // Logique pour mettre à jour l'affichage selon l'état
            // Cette méthode sera appelée depuis le XAML
        }

        private void HeaderButton_Click(object sender, RoutedEventArgs e)
        {
            // Ne permet l'expansion que si le menu principal est ouvert
            if (IsMenuExpanded)
            {
                IsExpanded = !IsExpanded;
            }
        }
    }
}
