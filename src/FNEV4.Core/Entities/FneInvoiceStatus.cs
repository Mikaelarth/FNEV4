namespace FNEV4.Core.Entities
{
    /// <summary>
    /// Statuts de certification FNE
    /// </summary>
    public enum FneInvoiceStatus
    {
        /// <summary>
        /// Facture en brouillon, non prête pour certification
        /// </summary>
        Draft = 0,

        /// <summary>
        /// Facture en attente de certification
        /// </summary>
        Pending = 1,

        /// <summary>
        /// Facture certifiée avec succès
        /// </summary>
        Certified = 2,

        /// <summary>
        /// Erreur lors de la certification
        /// </summary>
        Error = 3,

        /// <summary>
        /// Facture annulée
        /// </summary>
        Cancelled = 4
    }
}