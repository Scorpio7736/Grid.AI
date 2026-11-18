namespace Grid.Terminal;

/// <summary>Host-process emergency gates. Future remote listeners must consult RemoteDisabled.</summary>
public sealed class EmergencyControlState
{
    private int remoteDisabled;
    private int safeMode;
    public bool RemoteDisabled => Volatile.Read(ref remoteDisabled) != 0;
    public bool SafeMode => Volatile.Read(ref safeMode) != 0;
    public void DisableRemote() => Interlocked.Exchange(ref remoteDisabled, 1);
    public void EnterSafeMode() { DisableRemote(); Interlocked.Exchange(ref safeMode, 1); }
}
