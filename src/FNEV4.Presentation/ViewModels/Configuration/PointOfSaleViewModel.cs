using CommunityToolkit.Mvvm.ComponentModel;
using FNEV4.Core.DTOs;
using MaterialDesignThemes.Wpf;

namespace FNEV4.Presentation.ViewModels.Configuration;

/// <summary>
/// ViewModel wrapper pour PointOfSaleItem avec propriétés UI
/// </summary>
public partial class PointOfSaleViewModel : ObservableObject
{
    private readonly PointOfSaleItem _item;

    [ObservableProperty]
    private PackIconKind statusIcon = PackIconKind.Store;

    public string Name
    {
        get => _item.Name;
        set
        {
            _item.Name = value;
            OnPropertyChanged();
            UpdateStatusIcon();
        }
    }

    public string Code
    {
        get => _item.Code;
        set
        {
            _item.Code = value;
            OnPropertyChanged();
        }
    }

    public string Address
    {
        get => _item.Address;
        set
        {
            _item.Address = value;
            OnPropertyChanged();
        }
    }

    public string PhoneNumber
    {
        get => _item.PhoneNumber;
        set
        {
            _item.PhoneNumber = value;
            OnPropertyChanged();
        }
    }

    public bool IsActive
    {
        get => _item.IsActive;
        set
        {
            _item.IsActive = value;
            OnPropertyChanged();
            UpdateStatusIcon();
        }
    }

    public bool IsDefault
    {
        get => _item.IsDefault;
        set
        {
            _item.IsDefault = value;
            OnPropertyChanged();
            UpdateStatusIcon();
        }
    }

    public string Notes
    {
        get => _item.Notes;
        set
        {
            _item.Notes = value;
            OnPropertyChanged();
        }
    }

    public DateTime CreatedDate => _item.CreatedDate;
    public bool IsValid => _item.IsValid;
    public string FullDescription => _item.FullDescription;

    public PointOfSaleViewModel(PointOfSaleItem item)
    {
        _item = item ?? new PointOfSaleItem();
        UpdateStatusIcon();
    }

    public PointOfSaleViewModel() : this(new PointOfSaleItem())
    {
    }

    public PointOfSaleItem GetModel() => _item;

    private void UpdateStatusIcon()
    {
        StatusIcon = IsDefault ? PackIconKind.Home :
                     IsActive ? PackIconKind.Store : 
                     PackIconKind.StoreOff;
    }
}
