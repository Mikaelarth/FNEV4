namespace FNEV4.Core.Enums
{
    /// <summary>
    /// Types de clients selon les spécifications API DGI FNE
    /// Correspondance avec les templates de facturation
    /// </summary>
    public enum ClientType
    {
        /// <summary>
        /// B2C - Business to Consumer (Particulier)
        /// Facture sans NCC obligatoire
        /// </summary>
        Individual,

        /// <summary>
        /// B2B - Business to Business (Entreprise)
        /// Facture avec NCC obligatoire
        /// </summary>
        Company,

        /// <summary>
        /// B2G - Business to Government (Gouvernement)
        /// Facture avec NCC obligatoire, institution gouvernementale
        /// </summary>
        Government,

        /// <summary>
        /// B2F - Business to Foreign (International)
        /// Facture avec NCC et devise étrangère obligatoires
        /// </summary>
        International
    }

    /// <summary>
    /// Statuts de client
    /// </summary>
    public enum ClientStatus
    {
        /// <summary>
        /// Client actif (peut recevoir des factures)
        /// </summary>
        Active,

        /// <summary>
        /// Client inactif (ne peut plus recevoir de factures)
        /// </summary>
        Inactive,

        /// <summary>
        /// Client suspendu temporairement
        /// </summary>
        Suspended,

        /// <summary>
        /// Client archivé (historique uniquement)
        /// </summary>
        Archived
    }

    /// <summary>
    /// Templates de facturation API DGI FNE
    /// </summary>
    public enum FneTemplate
    {
        /// <summary>
        /// Business to Consumer - Particulier
        /// </summary>
        B2C,

        /// <summary>
        /// Business to Business - Entreprise
        /// </summary>
        B2B,

        /// <summary>
        /// Business to Government - Gouvernement
        /// </summary>
        B2G,

        /// <summary>
        /// Business to Foreign - International
        /// </summary>
        B2F
    }
}
