using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FNEV4.Core.Interfaces
{
    /// <summary>
    /// Interface pour le service de notifications utilisateur.
    /// Gère les notifications toast, les alertes et les confirmations.
    /// </summary>
    public interface INotificationService
    {
        #region Basic Notifications

        /// <summary>
        /// Affiche une notification d'information.
        /// </summary>
        /// <param name="title">Titre de la notification</param>
        /// <param name="message">Message de la notification</param>
        /// <param name="duration">Durée d'affichage en millisecondes</param>
        Task ShowInformationAsync(string title, string message, int duration = 3000);

        /// <summary>
        /// Affiche une notification de succès.
        /// </summary>
        /// <param name="title">Titre de la notification</param>
        /// <param name="message">Message de la notification</param>
        /// <param name="duration">Durée d'affichage en millisecondes</param>
        Task ShowSuccessAsync(string title, string message, int duration = 3000);

        /// <summary>
        /// Affiche une notification d'avertissement.
        /// </summary>
        /// <param name="title">Titre de la notification</param>
        /// <param name="message">Message de la notification</param>
        /// <param name="duration">Durée d'affichage en millisecondes</param>
        Task ShowWarningAsync(string title, string message, int duration = 4000);

        /// <summary>
        /// Affiche une notification d'erreur.
        /// </summary>
        /// <param name="title">Titre de la notification</param>
        /// <param name="message">Message de la notification</param>
        /// <param name="duration">Durée d'affichage en millisecondes</param>
        Task ShowErrorAsync(string title, string message, int duration = 5000);

        #endregion

        #region Advanced Notifications

        /// <summary>
        /// Affiche une notification avec actions personnalisées.
        /// </summary>
        /// <param name="notification">Configuration de la notification</param>
        /// <returns>Action sélectionnée par l'utilisateur</returns>
        Task<NotificationResult> ShowNotificationWithActionsAsync(NotificationConfig notification);

        /// <summary>
        /// Affiche une notification de progression.
        /// </summary>
        /// <param name="title">Titre de la notification</param>
        /// <param name="message">Message de la notification</param>
        /// <param name="progress">Progression (0-100)</param>
        /// <param name="cancellationToken">Token d'annulation optionnel</param>
        Task ShowProgressNotificationAsync(string title, string message, int progress, System.Threading.CancellationToken? cancellationToken = null);

        /// <summary>
        /// Met à jour une notification de progression existante.
        /// </summary>
        /// <param name="notificationId">ID de la notification</param>
        /// <param name="progress">Nouvelle progression</param>
        /// <param name="message">Nouveau message optionnel</param>
        Task UpdateProgressNotificationAsync(string notificationId, int progress, string? message = null);

        /// <summary>
        /// Ferme une notification spécifique.
        /// </summary>
        /// <param name="notificationId">ID de la notification</param>
        Task CloseNotificationAsync(string notificationId);

        #endregion

        #region Dialog Messages

        /// <summary>
        /// Affiche une boîte de dialogue de confirmation.
        /// </summary>
        /// <param name="title">Titre de la boîte</param>
        /// <param name="message">Message de confirmation</param>
        /// <param name="confirmText">Texte du bouton de confirmation</param>
        /// <param name="cancelText">Texte du bouton d'annulation</param>
        /// <returns>True si confirmé, False sinon</returns>
        Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "Oui", string cancelText = "Non");

        /// <summary>
        /// Affiche une boîte de dialogue avec choix multiple.
        /// </summary>
        /// <param name="title">Titre de la boîte</param>
        /// <param name="message">Message</param>
        /// <param name="options">Options disponibles</param>
        /// <returns>Index de l'option sélectionnée (-1 si annulé)</returns>
        Task<int> ShowMultipleChoiceAsync(string title, string message, string[] options);

        /// <summary>
        /// Affiche une boîte de dialogue de saisie de texte.
        /// </summary>
        /// <param name="title">Titre de la boîte</param>
        /// <param name="message">Message</param>
        /// <param name="defaultValue">Valeur par défaut</param>
        /// <param name="placeholder">Texte d'indication</param>
        /// <returns>Texte saisi (null si annulé)</returns>
        Task<string?> ShowInputDialogAsync(string title, string message, string defaultValue = "", string placeholder = "");

        #endregion

        #region System Notifications

        /// <summary>
        /// Affiche une notification système (dans la zone de notification Windows).
        /// </summary>
        /// <param name="title">Titre de la notification</param>
        /// <param name="message">Message de la notification</param>
        /// <param name="icon">Type d'icône</param>
        Task ShowSystemNotificationAsync(string title, string message, SystemNotificationIcon icon = SystemNotificationIcon.Information);

        /// <summary>
        /// Configure les préférences de notification.
        /// </summary>
        /// <param name="settings">Paramètres de notification</param>
        Task ConfigureNotificationSettingsAsync(NotificationSettings settings);

        #endregion

        #region Events

        /// <summary>
        /// Événement déclenché quand l'utilisateur clique sur une notification.
        /// </summary>
        event EventHandler<NotificationClickedEventArgs>? NotificationClicked;

        /// <summary>
        /// Événement déclenché quand une notification expire.
        /// </summary>
        event EventHandler<NotificationExpiredEventArgs>? NotificationExpired;

        #endregion
    }

    #region Enums and Data Classes

    /// <summary>
    /// Types d'icône pour les notifications système.
    /// </summary>
    public enum SystemNotificationIcon
    {
        None,
        Information,
        Warning,
        Error,
        Success
    }

    /// <summary>
    /// Types de notification.
    /// </summary>
    public enum NotificationType
    {
        Information,
        Success,
        Warning,
        Error,
        Question
    }

    /// <summary>
    /// Configuration d'une notification.
    /// </summary>
    public class NotificationConfig
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; } = NotificationType.Information;
        public int Duration { get; set; } = 3000;
        public bool Persistent { get; set; } = false;
        public List<NotificationAction> Actions { get; set; } = new();
        public object? Data { get; set; }
    }

    /// <summary>
    /// Action disponible dans une notification.
    /// </summary>
    public class NotificationAction
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public bool IsPrimary { get; set; } = false;
        public bool IsDestructive { get; set; } = false;
    }

    /// <summary>
    /// Résultat d'une notification avec actions.
    /// </summary>
    public class NotificationResult
    {
        public bool IsConfirmed { get; set; }
        public string? SelectedActionId { get; set; }
        public object? Data { get; set; }
    }

    /// <summary>
    /// Paramètres de notification.
    /// </summary>
    public class NotificationSettings
    {
        public bool EnableToastNotifications { get; set; } = true;
        public bool EnableSystemNotifications { get; set; } = true;
        public bool EnableSounds { get; set; } = true;
        public int DefaultDuration { get; set; } = 3000;
        public bool ShowOnlyWhenInactive { get; set; } = false;
        public Dictionary<NotificationType, bool> TypeSettings { get; set; } = new();
    }

    /// <summary>
    /// Arguments d'événement pour un clic sur notification.
    /// </summary>
    public class NotificationClickedEventArgs : EventArgs
    {
        public string NotificationId { get; set; } = string.Empty;
        public string? ActionId { get; set; }
        public object? Data { get; set; }
    }

    /// <summary>
    /// Arguments d'événement pour l'expiration d'une notification.
    /// </summary>
    public class NotificationExpiredEventArgs : EventArgs
    {
        public string NotificationId { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    #endregion
}
