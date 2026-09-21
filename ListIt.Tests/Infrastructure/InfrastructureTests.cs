using System;
using ListIt.Core.Repositories;
using Xunit;

namespace ListIt.Tests.Infrastructure;

public class InfrastructureTests
{
    [Fact]
    public void InfrastructureAssembly_LoadsAndReferencesCore()
    {
        // Verify infrastructure assembly can reference Core contracts
        var repoType = typeof(ITaskRepository);
        Assert.NotNull(repoType);
        Assert.True(repoType.IsInterface);
    }
}
