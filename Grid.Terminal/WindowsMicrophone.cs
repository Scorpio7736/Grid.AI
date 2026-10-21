using System.Runtime.InteropServices;
namespace Grid.Terminal;

/// <summary>Bounded PCM capture in memory. Only opens a device after an explicit hold-to-talk gesture.</summary>
public static class WindowsMicrophone
{
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    private struct Format { public ushort Tag, Channels; public uint Samples, Bytes; public ushort Align, Bits, Extra; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Header { public IntPtr Data; public uint Length, Recorded; public UIntPtr User; public uint Flags, Loops; public IntPtr Next; public UIntPtr Reserved; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Caps { public ushort Manufacturer, Product; public uint Version; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Name; public uint Formats; public ushort Channels, Reserved; }
    [DllImport("winmm.dll")] private static extern uint waveInGetNumDevs();
    [DllImport("winmm.dll", CharSet = CharSet.Unicode)] private static extern uint waveInGetDevCapsW(UIntPtr device, out Caps caps, uint size);
    [DllImport("winmm.dll")] private static extern uint waveInOpen(out IntPtr device, uint id, ref Format format, IntPtr callback, IntPtr instance, uint flags);
    [DllImport("winmm.dll")] private static extern uint waveInPrepareHeader(IntPtr device, IntPtr header, uint size);
    [DllImport("winmm.dll")] private static extern uint waveInAddBuffer(IntPtr device, IntPtr header, uint size);
    [DllImport("winmm.dll")] private static extern uint waveInStart(IntPtr device);
    [DllImport("winmm.dll")] private static extern uint waveInReset(IntPtr device);
    [DllImport("winmm.dll")] private static extern uint waveInUnprepareHeader(IntPtr device, IntPtr header, uint size);
    [DllImport("winmm.dll")] private static extern uint waveInClose(IntPtr device);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    public static bool EscapePressed => OperatingSystem.IsWindows() && (GetAsyncKeyState(0x1B) & 0x8000) != 0;
    public static string Describe()
    {
        if (!OperatingSystem.IsWindows()) return "Microphones unavailable: Windows required.";
        var lines = new List<string> { "Microphones (-1 = Windows default; IDs are local to this PC):" };
        for (uint i = 0; i < waveInGetNumDevs(); i++)
            if (waveInGetDevCapsW(new UIntPtr(i), out var caps, (uint)Marshal.SizeOf<Caps>()) == 0) lines.Add($"{i}: {TerminalTable.Safe(caps.Name)}");
        return string.Join("\n", lines);
    }
    public static async Task<byte[]> RecordAsync(int microphone, TextWriter output, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows() || Console.IsInputRedirected || Console.IsOutputRedirected) throw new IOException("Microphone recording requires an interactive Windows terminal.");
        if (microphone < -1 || microphone >= waveInGetNumDevs()) throw new IOException("Selected microphone unavailable; use voice devices and voice microphone.");
        await output.WriteLineAsync("MIC OFF // Hold SPACE to talk; release to transcribe. Esc cancels. Maximum 20 seconds.");
        while (true)
        {
            token.ThrowIfCancellationRequested();
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Escape) throw new OperationCanceledException("Voice capture cancelled.");
                if (key == ConsoleKey.Spacebar) break;
            }
            await Task.Delay(20, token);
        }
        var format = new Format { Tag = 1, Channels = 1, Samples = 16000, Bytes = 32000, Align = 2, Bits = 16 };
        Check(waveInOpen(out var device, unchecked((uint)microphone), ref format, IntPtr.Zero, IntPtr.Zero, 0));
        IntPtr data = IntPtr.Zero, header = IntPtr.Zero; var prepared = false;
        var size = (uint)Marshal.SizeOf<Header>();
        try
        {
            data = Marshal.AllocHGlobal(640000); header = Marshal.AllocHGlobal((int)size);
            Marshal.StructureToPtr(new Header { Data = data, Length = 640000 }, header, false);
            Check(waveInPrepareHeader(device, header, size)); prepared = true;
            Check(waveInAddBuffer(device, header, size)); Check(waveInStart(device));
            await output.WriteLineAsync("MIC ON // Recording while SPACE is held...");
            var started = System.Diagnostics.Stopwatch.StartNew();
            while ((GetAsyncKeyState(0x20) & 0x8000) != 0 && started.Elapsed < TimeSpan.FromSeconds(20))
            {
                while (Console.KeyAvailable) if (Console.ReadKey(true).Key == ConsoleKey.Escape) throw new OperationCanceledException("Voice capture cancelled.");
                await Task.Delay(20, token);
            }
            token.ThrowIfCancellationRequested(); Check(waveInReset(device));
            var captured = Marshal.PtrToStructure<Header>(header);
            if (captured.Recorded > 640000) throw new IOException("Invalid microphone data length.");
            await output.WriteLineAsync($"Captured {captured.Recorded / 32000.0:F2} seconds of audio.");
            if (captured.Recorded < 8000) throw new IOException("Recording was too short. Hold Space while speaking for at least a quarter second, then release.");
            var pcm = new byte[captured.Recorded - captured.Recorded % 2]; Marshal.Copy(data, pcm, 0, pcm.Length);
            try { return Wave(pcm); } finally { Array.Clear(pcm); }
        }
        finally
        {
            waveInReset(device);
            if (prepared) waveInUnprepareHeader(device, header, size);
            waveInClose(device);
            if (header != IntPtr.Zero) Marshal.FreeHGlobal(header);
            if (data != IntPtr.Zero) { Marshal.Copy(new byte[640000], 0, data, 640000); Marshal.FreeHGlobal(data); }
            while (Console.KeyAvailable) Console.ReadKey(true);
            await output.WriteLineAsync("MIC OFF // Audio discarded after transcription; no recording is saved.");
        }
    }
    private static void Check(uint code) { if (code != 0) throw new IOException($"Microphone unavailable (Windows audio error {code}); typed talk remains available."); }
    private static byte[] Wave(byte[] pcm)
    {
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8); writer.Write(36 + pcm.Length); writer.Write("WAVEfmt "u8); writer.Write(16);
        writer.Write((ushort)1); writer.Write((ushort)1); writer.Write(16000); writer.Write(32000); writer.Write((ushort)2); writer.Write((ushort)16);
        writer.Write("data"u8); writer.Write(pcm.Length); writer.Write(pcm); return stream.ToArray();
    }
}
