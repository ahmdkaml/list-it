using System.Windows.Controls;
using System.Windows.Input;

namespace ListIt.UI.Views;

/// <summary>
/// Interaction logic for AppView.xaml.
/// Primary entry point for application UI components and styling.
/// Inner content has normal mouse interaction.
/// </summary>
public partial class AppView : UserControl
{
    public AppView()
    {
        InitializeComponent();
    }

    private void AppView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Normal mouse behavior: content clicks do not trigger window dragging
        e.Handled = true;
    }
}
