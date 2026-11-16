namespace Grid.Terminal;

public static class PortableInferenceTiming
{
    public static readonly TimeSpan Normal = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan PortableColdStart = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan AttachedTransport = TimeSpan.FromSeconds(320);

    public static TimeSpan ColdStart(string? packageRoot) => packageRoot is null ? Normal : PortableColdStart;

    public static string Notice(string? packageRoot) => packageRoot is null ? "" :
        "Portable AI cold start: the first request may take up to five minutes while model weights load from the package. Later requests retain the normal two-minute limit.";
}
