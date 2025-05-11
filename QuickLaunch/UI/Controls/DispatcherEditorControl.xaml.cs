// DispatcherEditorControl.xaml.cs
#nullable enable

using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using QuickLaunch.Core.Logging;
using QuickLaunch.Core.Utils;
using QuickLaunch.UI.Utils;

// Use file-scoped namespace
namespace QuickLaunch.UI.Controls;

/// <summary>
/// Interaction logic for DispatcherEditorControl.xaml.
/// Provides UI for editing a DispatcherDefinition (Name, Type) and its associated Actions.
/// Uses ActionRegistrationEditorControl to dynamically edit selected action parameters.
/// </summary>
public partial class DispatcherEditorControl : UserControl
{
    public DispatcherEditorControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Adjust the GridView's second column width to fit all available space.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ActionsListView_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 0 &&
            sender is AddRemoveListView listView &&
            listView.View is GridView gridView &&
            gridView.Columns.Count > 1)
        {
            ScrollViewer? scrollViewer = VisualTreeUtils.FindVisualChild<ScrollViewer>(listView);
            scrollViewer.Map((scrollViewer) =>
            {
                double totalFixedWidth = 0;
                foreach (var column in gridView.Columns)
                {
                    totalFixedWidth += column.ActualWidth > 0 ? column.ActualWidth : column.Width;
                }
                totalFixedWidth -= gridView.Columns[1].ActualWidth > 0 ? gridView.Columns[1].ActualWidth : gridView.Columns[1].Width;

                var newWidth = (int)Math.Floor(scrollViewer.ViewportWidth - totalFixedWidth - 10); // FIXME: avoid horizontal scrollbar
                newWidth = newWidth >= 50 ? newWidth : 50;
                Log.Logger?.LogTrace($"Adjusting ActionListView GridView column width to {newWidth}.");
                gridView.Columns[1].Width = newWidth;
            });
        }
    }

}
#nullable disable
