using System;
using ListIt.UI.Views;
using Xunit;

namespace ListIt.Tests.UI;

public class AppViewTests
{
    [Fact]
    public void AppViewType_ExistsAndIsUserControl()
    {
        // Verify UI View type definition and inheritance without requiring STA thread
        var viewType = typeof(AppView);
        Assert.NotNull(viewType);
        Assert.True(typeof(System.Windows.Controls.UserControl).IsAssignableFrom(viewType));
    }
}
