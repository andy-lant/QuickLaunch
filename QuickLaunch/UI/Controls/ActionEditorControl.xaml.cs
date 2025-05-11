#nullable enable

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using QuickLaunch.Core.Config;   // For ActionRegistration
using QuickLaunch.UI.Parsers; // For OpenFileDialog
using QuickLaunch.UI.ViewModel; // For ParameterViewModel

namespace QuickLaunch.UI.Controls;

/// <summary>
/// Interaction logic for ActionRegistrationEditorControl.xaml.
/// Dynamically generates editor UI based on ActionType.ExpectedParameters.
/// </summary>
public partial class ActionEditorControl : UserControl
{
    // The ViewModel
    private readonly ActionRegistrationVM _viewModel;

    private readonly ActionRepresentationConverter _converter = new();

    // DependencyProperty for the ActionRegistration being edited
    public static readonly DependencyProperty ActionRegistrationProperty =
        DependencyProperty.Register(
            nameof(ActionRegistration),
            typeof(ActionRegistration),
            typeof(ActionEditorControl),
            new PropertyMetadata(null, OnActionRegistrationChanged)
        );


    /// <summary>
    /// Gets or sets the ActionRegistration object to be edited.
    /// </summary>
    public ActionRegistration? ActionRegistration
    {
        get { return (ActionRegistration?)GetValue(ActionRegistrationProperty); }
        set
        {
            SetValue(ActionRegistrationProperty, value);
            _viewModel.ActionData = value;
        }
    }

    // ----- Constructors ----- 
    public ActionEditorControl()
    {
        InitializeComponent();

        _viewModel = new ActionRegistrationVM();
        DataContext = _viewModel;

        _viewModel.PropertyChanged += OnModelPropertyChanged;
    }


    // ----- Event Handlers ----- 

    /// <summary>
    /// Callback when the ActionRegistration DependencyProperty changes.
    /// </summary>
    private static void OnActionRegistrationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ActionEditorControl control)
        {
            ActionRegistration? newAction = e.NewValue as ActionRegistration;
            control._viewModel.ActionData = newAction;
        }
    }

    private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_viewModel.ActionData))
        {
            ActionRegistration = _viewModel.ActionData;
        }
    }

    private void Input_GotKeyboardFocus(object? sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        // do nothing.
    }

    private void Input_TextChanged(object? sender, TextChangedEventArgs e)
    {
        OnTextChanged(Input.Text);
    }

    private void Input_LostFocus(object? sender, RoutedEventArgs e)
    {
        _viewModel.AttemptUpdateSource();
    }

    private void Input_KeyUp(object sender, KeyEventArgs e)
    {
        // do nothing.
    }

    private void Input_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            BindingExpression be = Input.GetBindingExpression(TextBox.TextProperty);
            be?.UpdateSource();

            _viewModel.AttemptUpdateSource();
            e.Handled = true;
        }
    }

    private void OnTextChanged(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
    }

}

#nullable disable
