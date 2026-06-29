using System.Threading;

namespace PCL.Online.OpenNel;

public static class OpenNelRuntime
{
    private static readonly object SyncRoot = new();
    private static int _initialized;

    public static void EnsureInitialized()
    {
        if (Volatile.Read(ref _initialized) == 1)
            return;

        lock (SyncRoot)
        {
            if (_initialized == 1)
                return;

            OpenNEL.Backend.Initialize();
            OpenNEL.Backend.WaitForInitAsync().GetAwaiter().GetResult();
            Volatile.Write(ref _initialized, 1);
        }
    }
}
