using CommunityToolkit.Mvvm.ComponentModel;

namespace GeometryEditor.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string _greeting { get; set; } = "Hello, world";
}
