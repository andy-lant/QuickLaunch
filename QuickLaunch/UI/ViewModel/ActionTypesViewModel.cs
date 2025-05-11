using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickLaunch.Core.Actions;

namespace QuickLaunch.UI.ViewModel;

public class ActionTypesViewModel : ObservableObject
{
    public ObservableCollection<ActionType> AvailableTypes { get; private set; }

    public ActionTypesViewModel()
    {
        AvailableTypes = new(ActionFactory.ActionRegistry.Values);
    }
}
