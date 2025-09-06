namespace FNEV4.Presentation.Messages
{
    /// <summary>
    /// Message envoyé quand des clients ont été importés avec succès
    /// </summary>
    public class ClientsImportedMessage
    {
        public int ImportedCount { get; }
        
        public ClientsImportedMessage(int importedCount)
        {
            ImportedCount = importedCount;
        }
    }
}
