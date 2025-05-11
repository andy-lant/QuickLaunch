using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuickLaunch.UI.Controls;

public partial class AddRemoveListView : ListView
{
    // ----- Constructors. -----
    #region Constructors 

    static AddRemoveListView()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AddRemoveListView),
            new FrameworkPropertyMetadata(typeof(AddRemoveListView)));
    }

    #endregion

    // ----- Dependency Properties. -----
    #region Dependency Properties

    #region * Add Button

    #region * AddCommand Property

    public static readonly DependencyProperty AddCommandProperty =
    DependencyProperty.Register(nameof(AddCommand), typeof(ICommand), typeof(AddRemoveListView),
                                  new PropertyMetadata(null)); // Default value is null

    public ICommand AddCommand
    {
        get { return (ICommand)GetValue(AddCommandProperty); }
        set { SetValue(AddCommandProperty, value); }
    }

    #endregion

    #region ** AddButtonContent Property

    // Using 'object' type allows any content (string, UIElement, etc.)
    public static readonly DependencyProperty AddButtonContentProperty =
        DependencyProperty.Register(nameof(AddButtonContent), typeof(object), typeof(AddRemoveListView),
            new PropertyMetadata()); // Set default value from resource

    public object AddButtonContent
    {
        get { return GetValue(AddButtonContentProperty); }
        set { SetValue(AddButtonContentProperty, value); }
    }

    #endregion

    #endregion

    #region * Remove Button

    #region ** RemoveCommand Property

    public static readonly DependencyProperty RemoveCommandProperty =
        DependencyProperty.Register(nameof(RemoveCommand), typeof(ICommand), typeof(AddRemoveListView),
                                      new PropertyMetadata(null)); // Default value is null

    public ICommand RemoveCommand
    {
        get { return (ICommand)GetValue(RemoveCommandProperty); }
        set { SetValue(RemoveCommandProperty, value); }
    }

    #endregion


    #region ** RemoveButtonContent Property

    // Using 'object' type allows any content (string, UIElement, etc.)
    public static readonly DependencyProperty RemoveButtonContentProperty =
        DependencyProperty.Register(nameof(RemoveButtonContent), typeof(object), typeof(AddRemoveListView),
             new PropertyMetadata()); // Set default value from resource

    public object RemoveButtonContent
    {
        get { return GetValue(RemoveButtonContentProperty); }
        set { SetValue(RemoveButtonContentProperty, value); }
    }

    #endregion

    #endregion

    #endregion
}
