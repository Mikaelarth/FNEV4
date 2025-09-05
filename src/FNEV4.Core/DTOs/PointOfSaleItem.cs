namespace FNEV4.Core.DTOs;

/// <summary>
/// Représente un point de vente avec ses informations et état d'activation
/// </summary>
public class PointOfSaleItem
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Validation du point de vente
    /// </summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Address);

    /// <summary>
    /// Description complète du point de vente
    /// </summary>
    public string FullDescription => $"{Name} - {Address}";

    /// <summary>
    /// Constructeur par défaut
    /// </summary>
    public PointOfSaleItem()
    {
    }

    /// <summary>
    /// Constructeur avec paramètres
    /// </summary>
    public PointOfSaleItem(string name, string address, string phoneNumber = "", bool isActive = true)
    {
        Name = name;
        Address = address;
        PhoneNumber = phoneNumber;
        IsActive = isActive;
    }
}
