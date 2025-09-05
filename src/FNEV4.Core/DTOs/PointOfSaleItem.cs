namespace FNEV4.Core.DTOs;

/// <summary>
/// Représente un point de vente avec ses informations et état d'activation
/// </summary>
public class PointOfSaleItem
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Validation du point de vente
    /// </summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(Name) && 
                          !string.IsNullOrWhiteSpace(Code) && 
                          !string.IsNullOrWhiteSpace(Address) &&
                          Code.Length <= 10;

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
    public PointOfSaleItem(string name, string code, string address, string phoneNumber = "", bool isActive = true)
    {
        Name = name;
        Code = code;
        Address = address;
        PhoneNumber = phoneNumber;
        IsActive = isActive;
    }
}
