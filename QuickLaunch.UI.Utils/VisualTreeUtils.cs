using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace QuickLaunch.UI.Utils;

/// <summary>
/// Utility class for working with the visual tree.
/// </summary>
public static class VisualTreeUtils
{
    /// <summary>
    /// Find the Window ancestor of a given Visual.
    /// </summary>
    public static Window? GetWindow(Visual visual)
    {
        ArgumentNullException.ThrowIfNull(visual, nameof(visual));

        DependencyObject current = visual;

        while (current is not null)
        {
            if (current is Window window)
            {
                return window;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    /// <summary>
    /// Finds the first visual child of a specific type.
    /// </summary>
    /// <typeparam name="T">The type of visual child to find.</typeparam>
    /// <param name="parent">The parent DependencyObject to search within.</param>
    /// <returns>The first visual child of the specified type, or null if none is found.</returns>
    public static T? FindVisualChild<T>(DependencyObject parent) where T : Visual
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject? child = VisualTreeHelper.GetChild(parent, i);

            if (child is T tChild)
            {
                return tChild;
            }
            else if (child is Visual vChild) // Restrict recursion to Visuals
            {
                T? childOfChild = FindVisualChild<T>(vChild);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Finds all visual children of a specific type.
    /// </summary>
    /// <typeparam name="T">The type of visual children to find.</typeparam>
    /// <param name="parent">The parent DependencyObject to search within.</param>
    /// <returns>An enumerable collection of visual children of the specified type.</returns>
    public static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : Visual
    {
        if (parent != null)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T visualChild)
                {
                    yield return visualChild;
                }

                // Check if child itself is a DependencyObject before recursive call, though Visual is a DependencyObject
                if (child is Visual childVisual) // Ensures child is Visual for the recursive call's constraint
                {
                    foreach (T childOfChild in FindVisualChildren<T>(childVisual))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
}
