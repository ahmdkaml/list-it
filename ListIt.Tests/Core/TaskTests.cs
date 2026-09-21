using System;
using ListIt.Core.Models;
using ListIt.Core.Services;
using Xunit;
using TaskItem = ListIt.Core.Models.Task;

namespace ListIt.Tests.Core;

public class TaskTests
{
    [Fact]
    public void TaskModel_InitializesCorrectly()
    {
        // Arrange & Act
        var task = new TaskItem
        {
            Title = "Test desktop widget task"
        };

        // Assert
        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal("Test desktop widget task", task.Title);
    }

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
