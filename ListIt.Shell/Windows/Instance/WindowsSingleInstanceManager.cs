using System;
using System.Threading;

namespace ListIt.Shell.Windows.Instance;

/// <summary>
/// Windows OS implementation of ISingleInstanceManager using named Mutex and EventWaitHandle.
/// Guarantees that only one instance of the application runs per user session,
/// and routes secondary launches to activate the primary instance.
/// </summary>
public class WindowsSingleInstanceManager : ISingleInstanceManager
{
    public const string DefaultMutexName = @"Local\ListIt_SingleInstanceMutex";
    public const string DefaultSignalName = @"Local\ListIt_SingleInstanceSignal";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _signalEvent;
    private readonly bool _isFirstInstance;
    private RegisteredWaitHandle? _waitHandleRegistration;
    private bool _isDisposed;

    public bool IsFirstInstance => _isFirstInstance;

    public WindowsSingleInstanceManager(
        string mutexName = DefaultMutexName,
        string signalName = DefaultSignalName)
    {
        _mutex = new Mutex(true, mutexName, out _isFirstInstance);
        _signalEvent = new EventWaitHandle(false, EventResetMode.AutoReset, signalName);
    }

    public void SignalFirstInstance()
    {
        try
        {
            _signalEvent.Set();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Failed to signal primary instance: {ex}");
        }
    }

    public void StartListeningForActivation(Action onActivated)
    {
        if (!_isFirstInstance || onActivated == null) return;

        _waitHandleRegistration = ThreadPool.RegisterWaitForSingleObject(
            _signalEvent,
            (state, timedOut) =>
            {
                if (!timedOut)
                {
                    try
                    {
                        onActivated();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.TraceError($"Error handling single instance activation: {ex}");
                    }
                }
            },
            null,
            -1,
            false);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _waitHandleRegistration?.Unregister(null);

        if (_isFirstInstance)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch
            {
                // Mutex may already be abandoned or released
            }
        }

        _mutex.Dispose();
        _signalEvent.Dispose();
    }
}
