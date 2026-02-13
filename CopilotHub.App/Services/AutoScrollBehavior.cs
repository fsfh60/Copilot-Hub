using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace CopilotHub.App.Services;

/// <summary>
/// Attached behavior to auto-scroll a ListBox when items are added.
/// </summary>
public static class AutoScrollBehavior
{
    public static readonly DependencyProperty AutoScrollProperty =
        DependencyProperty.RegisterAttached(
            "AutoScroll",
            typeof(bool),
            typeof(AutoScrollBehavior),
            new PropertyMetadata(false, OnAutoScrollChanged));

    public static bool GetAutoScroll(DependencyObject obj) =>
        (bool)obj.GetValue(AutoScrollProperty);

    public static void SetAutoScroll(DependencyObject obj, bool value) =>
        obj.SetValue(AutoScrollProperty, value);

    private static void OnAutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListBox listBox) return;

        if ((bool)e.NewValue)
        {
            listBox.Loaded += (_, _) =>
            {
                if (listBox.Items is INotifyCollectionChanged collection)
                {
                    collection.CollectionChanged += (_, _) =>
                    {
                        if (listBox.Items.Count > 0)
                            listBox.ScrollIntoView(listBox.Items[^1]);
                    };
                }
            };
        }
    }
}
