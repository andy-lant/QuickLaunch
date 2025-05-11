#nullable enable

using System.Windows;
using System.Windows.Controls;
using QuickLaunch.UI.ViewModel; // For ConfigEditorWindow constant

// Use file-scoped namespace
namespace QuickLaunch.UI.Controls;

/// <summary>
/// Interaction logic for CommandEditorControl.xaml
/// Provides UI for editing a CommandDefinition (Name, Sequence, and Dispatcher name).
/// </summary>
public partial class CommandEditorControl : UserControl
{
    #region ----- Dependency Properties. -----

    #region Model

    public static readonly DependencyProperty ModelProperty =
        DependencyProperty.Register(nameof(Model), typeof(CommandListViewModel), typeof(CommandEditorControl),
            new PropertyMetadata(null)); // Default value is null

    public CommandListViewModel Model
    {
        get { return (CommandListViewModel)GetValue(ModelProperty); }
        set { SetValue(ModelProperty, value); }
    }

    #endregion

    #region DispacherModel

    public static readonly DependencyProperty DispatcherModelProperty =
        DependencyProperty.Register(nameof(DispatcherModel), typeof(DispatcherListViewModel), typeof(CommandEditorControl),
            new PropertyMetadata(null)); // Default value is null

    public DispatcherListViewModel DispatcherModel
    {
        get { return (DispatcherListViewModel)GetValue(DispatcherModelProperty); }
        set { SetValue(DispatcherModelProperty, value); }
    }

    #endregion

    #endregion

    #region ----- Constructors. -----

    public CommandEditorControl()
    {
        InitializeComponent();
    }

    #endregion

    #region ----- Event Handlers. -----

    private void DispatcherComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        DispatcherModel?.SelectionChanged(sender, e);
    }

    #endregion

}
#nullable disable
