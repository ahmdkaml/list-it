using System;
using System.Threading;
using ListIt.Shell.Windows.Instance;
using Xunit;

namespace ListIt.Tests.Shell.Windows.Instance;

public class SingleInstanceManagerTests
{
    [Fact]
    public void SingleInstance_FirstInstance_AcquiresOwnership()
    {
        var testId = Guid.NewGuid().ToString("N");
        var mutexName = $@"Local\TestMutex_{testId}";
        var signalName = $@"Local\TestSignal_{testId}";

        using var manager = new WindowsSingleInstanceManager(mutexName, signalName);

        Assert.True(manager.IsFirstInstance);
    }

    [Fact]
    public void SingleInstance_SecondInstance_DetectsExistingAndSignals()
    {
        var testId = Guid.NewGuid().ToString("N");
        var mutexName = $@"Local\TestMutex_{testId}";
        var signalName = $@"Local\TestSignal_{testId}";

        using var first = new WindowsSingleInstanceManager(mutexName, signalName);
        Assert.True(first.IsFirstInstance);

        using var second = new WindowsSingleInstanceManager(mutexName, signalName);
        Assert.False(second.IsFirstInstance);

        using var activationSignalReceived = new ManualResetEvent(false);
        first.StartListeningForActivation(() =>
        {
            activationSignalReceived.Set();
        });

        second.SignalFirstInstance();

        var signaled = activationSignalReceived.WaitOne(TimeSpan.FromSeconds(2));
        Assert.True(signaled);
    }
}
