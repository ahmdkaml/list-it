using ListIt.Core.Services;
using Xunit;

namespace ListIt.Tests.Core;

public class ContentProviderTests
{
    [Fact]
    public void ContentProvider_ReturnsExpectedContent()
    {
        // Arrange
        IContentProvider provider = new StaticContentProvider();

        // Act & Assert
        Assert.Equal("LIST-IT", provider.GetHeader());
        Assert.Equal("hello there", provider.GetBody());
    }
}
