param([ValidateSet('options','capture','verify')][string]$Mode, [string]$MonitorJson)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type '
using System;
using System.Runtime.InteropServices;
public static class GridScreenNative {
    [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr context);
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct DisplayDevice {
        public int Size;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)] public string Name;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public string Description;
        public int Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public string Id;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public string Key;
    }
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern bool EnumDisplayDevices(string device, uint index, ref DisplayDevice result, uint flags);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern bool GetUserObjectInformation(IntPtr handle, int index, System.Text.StringBuilder value, int size, out int needed);
    public static string HardwareId(string name) {
        var device=new DisplayDevice(); device.Size=Marshal.SizeOf(typeof(DisplayDevice));
        return EnumDisplayDevices(name, 0, ref device, 1) ? device.Id + "|" + device.Key : "";
    }
    [DllImport("user32.dll", SetLastError=true)] public static extern IntPtr OpenInputDesktop(uint flags, bool inherit, uint access);
    [DllImport("user32.dll")] public static extern bool CloseDesktop(IntPtr desktop);
}'
[void][GridScreenNative]::SetProcessDpiAwarenessContext([IntPtr](-4))
function Monitors {
    @([System.Windows.Forms.Screen]::AllScreens | Sort-Object DeviceName | ForEach-Object {
        @{ Device=$_.DeviceName; X=$_.Bounds.X; Y=$_.Bounds.Y; Width=$_.Bounds.Width; Height=$_.Bounds.Height; Primary=$_.Primary; HardwareId=[GridScreenNative]::HardwareId($_.DeviceName) }
    })
}
function Assert-Desktop {
    $desktop=[GridScreenNative]::OpenInputDesktop(0,$false,0x0101)
    if ($desktop -eq [IntPtr]::Zero) { throw 'Interactive desktop is unavailable or locked.' }
    try {
        $name=New-Object System.Text.StringBuilder(256)
        $needed=0
        if (-not [GridScreenNative]::GetUserObjectInformation($desktop,2,$name,512,[ref]$needed) -or $name.ToString() -ne 'Default') { throw 'Only the unlocked default desktop can be captured.' }
    } finally { [void][GridScreenNative]::CloseDesktop($desktop) }
}
if ($Mode -eq 'options') { ConvertTo-Json -InputObject @(Monitors) -Compress; exit 0 }
$bitmap=$null; $graphics=$null; $memory=$null; $random=$null; $software=$null; $font=$null
try {
    Add-Type -AssemblyName System.Runtime.WindowsRuntime
    $null=[Windows.Media.Ocr.OcrEngine,Windows.Foundation,ContentType=WindowsRuntime]
    $null=[Windows.Graphics.Imaging.BitmapDecoder,Windows.Foundation,ContentType=WindowsRuntime]
    $null=[Windows.Graphics.Imaging.SoftwareBitmap,Windows.Foundation,ContentType=WindowsRuntime]
    $null=[Windows.Media.Ocr.OcrResult,Windows.Foundation,ContentType=WindowsRuntime]
    $ocr=[Windows.Media.Ocr.OcrEngine]::TryCreateFromUserProfileLanguages()
    if ($null -eq $ocr) { throw 'No installed OCR language supports the user profile.' }
    if ($Mode -eq 'verify') {
        # Synthetic bitmap only: this probe never captures a desktop.
        $bitmap=New-Object System.Drawing.Bitmap(1000,180)
        $graphics=[System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.Clear([System.Drawing.Color]::White)
        $font=New-Object System.Drawing.Font('Arial',36)
        $graphics.DrawString('GRID SCREEN COBALT 42',$font,[System.Drawing.Brushes]::Black,20,45)
    } else {
        $wanted=$MonitorJson | ConvertFrom-Json
        if ([string]::IsNullOrWhiteSpace($wanted.HardwareId) -or $wanted.HardwareId -eq '|') { throw 'Monitor hardware identity is unavailable; capture refused to avoid selecting another screen.' }
        $screen=@(Monitors | Where-Object { $_.Device -eq $wanted.Device -and $_.X -eq $wanted.X -and $_.Y -eq $wanted.Y -and $_.Width -eq $wanted.Width -and $_.Height -eq $wanted.Height -and $_.Primary -eq $wanted.Primary -and $_.HardwareId -eq $wanted.HardwareId })
        if ($screen.Count -ne 1) { throw 'Selected monitor changed or disconnected. Run screen options again.' }
        Assert-Desktop
        if ($wanted.Width -le 0 -or $wanted.Height -le 0 -or ([long]$wanted.Width*$wanted.Height) -gt 16000000 -or $wanted.Width -gt [Windows.Media.Ocr.OcrEngine]::MaxImageDimension -or $wanted.Height -gt [Windows.Media.Ocr.OcrEngine]::MaxImageDimension) { throw 'Selected display exceeds supported OCR dimensions. Reduce display resolution.' }
        $bitmap=New-Object System.Drawing.Bitmap([int]$wanted.Width,[int]$wanted.Height)
        $graphics=[System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.CopyFromScreen([int]$wanted.X,[int]$wanted.Y,0,0,$bitmap.Size,[System.Drawing.CopyPixelOperation]::SourceCopy)
        Assert-Desktop
    }
    $captured=[DateTimeOffset]::UtcNow
    $memory=New-Object System.IO.MemoryStream
    $bitmap.Save($memory,[System.Drawing.Imaging.ImageFormat]::Png)
    $memory.Position=0
    $random=[System.IO.WindowsRuntimeStreamExtensions]::AsRandomAccessStream($memory)
    $asTask=[System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.IsGenericMethod -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' } | Select-Object -First 1
    function Await($operation,[Type]$type) {
        $task=$asTask.MakeGenericMethod($type).Invoke($null,@($operation))
        $task.Wait(); $task.Result
    }
    $decoder=Await ([Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($random)) ([Windows.Graphics.Imaging.BitmapDecoder])
    $software=Await ($decoder.GetSoftwareBitmapAsync()) ([Windows.Graphics.Imaging.SoftwareBitmap])
    $result=Await ($ocr.RecognizeAsync($software)) ([Windows.Media.Ocr.OcrResult])
    $text=$result.Text
    $truncated=$text.Length -gt 6000
    if ($truncated) { $text=$text.Substring(0,6000) }
    @{ Available=$true; Text=$text; Reason='Windows OCR: text only, not general image understanding'; CapturedUtc=$captured.ToString('O'); Truncated=$truncated } | ConvertTo-Json -Compress
} catch {
    @{ Available=$false; Text=''; Reason=('Screen OCR unavailable: '+$_.Exception.Message); CapturedUtc=[DateTimeOffset]::UtcNow.ToString('O'); Truncated=$false } | ConvertTo-Json -Compress
} finally {
    foreach ($item in @($software,$random,$memory,$graphics,$bitmap,$font)) { if ($null -ne $item -and $item -is [IDisposable]) { $item.Dispose() } }
}

