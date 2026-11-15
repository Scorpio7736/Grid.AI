namespace Grid.Agent;

public enum DeviceOperation { Launch, Focus, InspectWindow, CreateFolder }
public sealed record DeviceRequest(int Version, DeviceOperation Operation, string Target)
{
    public const int CurrentVersion = 1;
    public void Validate()
    {
        if (Version != CurrentVersion || !Enum.IsDefined(Operation) || string.IsNullOrWhiteSpace(Target) ||
            Target.Length > 1024 || Target.Any(char.IsControl)) throw new ArgumentException("Invalid device request version, operation or target.");
    }
}
public sealed record DeviceCapability(string Id, string Policy, string Limits, string Source);
public static class DeviceCapabilities
{
    public static IReadOnlyList<DeviceCapability> Catalog { get; } = Array.AsReadOnly(new[] {
        new DeviceCapability("device.discovery", "Master Control; metadata only; discovery grants no action", "256 rows; checked 2-second traversal budget; no titles/arguments", "Windows current desktop session"),
        new DeviceCapability("device.launch", "Original host; exact executable; review/approve/run", "No arguments, shell or elevation; 256 MiB executable limit", "Selected file SHA256"),
        new DeviceCapability("device.focus", "Original host; selected process/window; review/approve/run", "One window; Windows may refuse foreground activation", "PID/start time/window handle"),
        new DeviceCapability("device.inspect", "Original host; selected window grant; review/approve/run", "Title only, 256 characters; no control tree or capture", "Selected live window"),
        new DeviceCapability("device.mkdir", "Original host; selected local parent; review/approve/run", "One new empty folder; existing targets refused", "Validated parent path"),
        new DeviceCapability("files", "Existing 6.23 read/stage/review/approve/apply gateway", "Existing text/artifact/path and rollback limits", "Host file receipts"),
        new DeviceCapability("screen", "Existing explicit monitor selection and active screen scope", "One-shot OCR; no implicit window capture", "Existing screen provider"),
        new DeviceCapability("clipboard", "Disabled; no grant or access", "Unsupported", "Not collected"),
        new DeviceCapability("notifications", "Only existing approved RELAY local inbox/speech", "No new Windows notification access", "Saved RELAY receipts")
    });
}
public sealed record DeviceTarget(string Locator, string Fingerprint, string Display);
public sealed record DeviceObservation(string Id, string Name, string State);
public sealed record DeviceDiscovery(IReadOnlyList<DeviceObservation> Items, bool Incomplete, string Source, DateTimeOffset ObservedUtc);
public sealed record DeviceReview(Guid Id, DeviceRequest Request, DeviceTarget Target, DateTimeOffset ExpiresUtc, bool Approved);
public sealed record DeviceReceipt(Guid Id, DeviceOperation Operation, string Status, string Detail, DateTimeOffset AtUtc, string Source);
/// <summary>Trusted local OS adapter. The host owns identity, grants, approvals and audit.</summary>
public interface IDeviceIntegration
{
    string DeviceId { get; }
    DeviceDiscovery Discover(bool windowsOnly);
    DeviceTarget Resolve(DeviceRequest request);
    string Execute(DeviceRequest request, DeviceTarget expected);
}
