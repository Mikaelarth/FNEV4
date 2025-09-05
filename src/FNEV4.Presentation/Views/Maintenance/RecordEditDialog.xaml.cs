using System.Data;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using FNEV4.Infrastructure.Services;

namespace FNEV4.Presentation.Views.Maintenance
{
    public partial class RecordEditDialog : Window
    {
        private readonly DataTable _tableStructure;
        private readonly DataRow? _existingRecord;
        private readonly string _tableName;
        private readonly bool _isNew;
        private readonly IDatabaseService _databaseService;
        private readonly Dictionary<string, TextBox> _fieldControls = new();

        public RecordEditDialog(DataTable tableStructure, DataRow? existingRecord, string tableName, bool isNew)
        {
            InitializeComponent();
            
            _tableStructure = tableStructure;
            _existingRecord = existingRecord;
            _tableName = tableName;
            _isNew = isNew;
            
            // Injection de dépendance simplifiée - Dans un vrai projet, utiliser un container IoC
            _databaseService = Application.Current.Properties["DatabaseService"] as IDatabaseService 
                              ?? throw new InvalidOperationException("DatabaseService non trouvé");

            InitializeDialog();
        }

        private void InitializeDialog()
        {
            // Configuration du titre
            TitleTextBlock.Text = _isNew ? "Ajouter un enregistrement" : "Modifier l'enregistrement";
            SubtitleTextBlock.Text = $"Table : {_tableName}";

            // Générer les champs dynamiquement
            GenerateFields();
        }

        private void GenerateFields()
        {
            FieldsContainer.Children.Clear();
            _fieldControls.Clear();

            foreach (DataColumn column in _tableStructure.Columns)
            {
                // Ignorer les colonnes techniques
                if (ShouldExcludeColumn(column.ColumnName))
                    continue;

                // Créer le champ de saisie
                var fieldPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };

                var textBox = new TextBox
                {
                    Style = (Style)FindResource("MaterialDesignOutlinedTextBox"),
                    Tag = column
                };

                // Configuration de l'hint
                HintAssist.SetHint(textBox, GetFieldDisplayName(column));

                // Valeur existante pour modification
                if (!_isNew && _existingRecord != null && _existingRecord[column.ColumnName] != DBNull.Value)
                {
                    textBox.Text = _existingRecord[column.ColumnName]?.ToString() ?? "";
                }

                // Configuration spéciale selon le type
                ConfigureFieldByType(textBox, column);

                fieldPanel.Children.Add(textBox);
                FieldsContainer.Children.Add(fieldPanel);
                
                _fieldControls[column.ColumnName] = textBox;
            }
        }

        private bool ShouldExcludeColumn(string columnName)
        {
            var excludedColumns = new[] { "Id", "CreatedAt", "UpdatedAt", "IsDeleted", "CreatedBy", "UpdatedBy" };
            return excludedColumns.Any(col => columnName.Equals(col, StringComparison.OrdinalIgnoreCase));
        }

        private string GetFieldDisplayName(DataColumn column)
        {
            // Conversion du nom technique en nom d'affichage
            var name = column.ColumnName;
            
            // Règles de conversion simples
            name = name.Replace("_", " ");
            name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
            
            // Ajout d'indication si requis
            if (!column.AllowDBNull)
                name += " *";

            return name;
        }

        private void ConfigureFieldByType(TextBox textBox, DataColumn column)
        {
            // Configuration selon le type de données
            switch (Type.GetTypeCode(column.DataType))
            {
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                    textBox.PreviewTextInput += NumericTextBox_PreviewTextInput;
                    break;
                    
                case TypeCode.DateTime:
                    // Pour les dates, on pourrait utiliser un DatePicker
                    HintAssist.SetHelperText(textBox, "Format: YYYY-MM-DD HH:MM:SS");
                    break;
                    
                case TypeCode.Boolean:
                    // Pour les booléens, remplacer par un CheckBox serait mieux
                    HintAssist.SetHelperText(textBox, "Valeurs: true, false, 0, 1");
                    break;
            }

            // Validation pour les champs requis
            if (!column.AllowDBNull)
            {
                textBox.LostFocus += RequiredField_LostFocus;
            }
        }

        private void NumericTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Autoriser seulement les chiffres, point et virgule pour les nombres
            e.Handled = !IsNumericInput(e.Text);
        }

        private bool IsNumericInput(string input)
        {
            return input.All(c => char.IsDigit(c) || c == '.' || c == ',' || c == '-');
        }

        private void RequiredField_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && string.IsNullOrWhiteSpace(textBox.Text))
            {
                // Indiquer visuellement que le champ est requis
                textBox.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            else if (textBox != null)
            {
                textBox.ClearValue(BorderBrushProperty);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validation
                if (!ValidateFields())
                    return;

                // Collecte des valeurs
                var values = CollectFieldValues();

                bool success;
                if (_isNew)
                {
                    success = await _databaseService.InsertRecordAsync(_tableName, values);
                }
                else
                {
                    var id = _existingRecord?[0]?.ToString() ?? "";
                    success = await _databaseService.UpdateRecordAsync(_tableName, id, values);
                }

                if (success)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    ShowError("Erreur lors de l'enregistrement des données.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Erreur : {ex.Message}");
            }
        }

        private bool ValidateFields()
        {
            ErrorTextBlock.Visibility = Visibility.Collapsed;
            var errors = new List<string>();

            foreach (var kvp in _fieldControls)
            {
                var column = _tableStructure.Columns[kvp.Key];
                var textBox = kvp.Value;

                // Validation des champs requis
                if (!column!.AllowDBNull && string.IsNullOrWhiteSpace(textBox.Text))
                {
                    errors.Add($"Le champ '{GetFieldDisplayName(column)}' est requis.");
                    textBox.BorderBrush = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    textBox.ClearValue(BorderBrushProperty);
                }

                // Validation des types
                if (!string.IsNullOrWhiteSpace(textBox.Text) && !ValidateFieldType(textBox.Text, column.DataType))
                {
                    errors.Add($"Le champ '{GetFieldDisplayName(column)}' contient une valeur invalide.");
                    textBox.BorderBrush = System.Windows.Media.Brushes.Red;
                }
            }

            if (errors.Any())
            {
                ShowError(string.Join("\n", errors));
                return false;
            }

            return true;
        }

        private bool ValidateFieldType(string value, Type targetType)
        {
            try
            {
                switch (Type.GetTypeCode(targetType))
                {
                    case TypeCode.Int32:
                        int.Parse(value);
                        break;
                    case TypeCode.Int64:
                        long.Parse(value);
                        break;
                    case TypeCode.Decimal:
                        decimal.Parse(value);
                        break;
                    case TypeCode.Double:
                        double.Parse(value);
                        break;
                    case TypeCode.DateTime:
                        DateTime.Parse(value);
                        break;
                    case TypeCode.Boolean:
                        // Accepter plusieurs formats pour les booléens
                        var lowerValue = value.ToLower();
                        if (lowerValue != "true" && lowerValue != "false" && 
                            lowerValue != "1" && lowerValue != "0")
                            return false;
                        break;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private Dictionary<string, object> CollectFieldValues()
        {
            var values = new Dictionary<string, object>();

            foreach (var kvp in _fieldControls)
            {
                var column = _tableStructure.Columns[kvp.Key];
                var textBox = kvp.Value;
                var value = textBox.Text;

                if (string.IsNullOrWhiteSpace(value))
                {
                    if (column!.AllowDBNull)
                        values[kvp.Key] = DBNull.Value;
                    continue;
                }

                // Conversion selon le type
                object convertedValue = ConvertValue(value, column!.DataType);
                values[kvp.Key] = convertedValue;
            }

            return values;
        }

        private object ConvertValue(string value, Type targetType)
        {
            switch (Type.GetTypeCode(targetType))
            {
                case TypeCode.Int32:
                    return int.Parse(value);
                case TypeCode.Int64:
                    return long.Parse(value);
                case TypeCode.Decimal:
                    return decimal.Parse(value);
                case TypeCode.Double:
                    return double.Parse(value);
                case TypeCode.DateTime:
                    return DateTime.Parse(value);
                case TypeCode.Boolean:
                    var lowerValue = value.ToLower();
                    return lowerValue == "true" || lowerValue == "1";
                default:
                    return value;
            }
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
