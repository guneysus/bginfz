using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace Bginfz;

public struct MEMORYSTATUSEX
{
    public uint dwLength;
    public uint dwMemoryLoad;
    public ulong ullTotalPhys;
    public ulong ullAvailPhys;
    public ulong ullTotalPageFile;
    public ulong ullAvailPageFile;
    public ulong ullTotalVirtual;
    public ulong ullAvailVirtual;
    public ulong ullAvailExtendedVirtual;
}

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    DispatcherTimer dispatchTimer;
    DispatcherTimer cpuTimer;
    private PerformanceCounter cpuCounter;
    private PerformanceCounter totalMemoryCounter;
    private PerformanceCounter availableMemoryCounter;
    private PerformanceCounter downloadCounter;
    private PerformanceCounter uploadCounter;
    private DispatcherTimer networkTimer;

    // P/Invoke to get the active window
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("kernel32.dll")]
    public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    public MainWindow()
    {
        InitializeComponent();

        dispatchTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };

        dispatchTimer.Tick += DispatchTimer_Tick;
        dispatchTimer.Start();

        InitializeCpuCounter();
        StartCpuUsageTimer();

        InitializeMemoryCounter();
        UpdateMemoryUsage();

#if false
        var wifiAdapter = NetworkInterface.GetAllNetworkInterfaces()
    .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
    //.Where(nic => nic.Name.Contains("wifi", StringComparison.InvariantCultureIgnoreCase))
    .Skip(3)
    //.Where(nic => nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
    .FirstOrDefault();

        InitializeNetworkCounters(wifiAdapter.Name);
        StartNetworkMonitoring(); 
#endif

    }

    private void InitializeNetworkCounters(string networkAdapterName)
    {
        // Replace "YourNetworkAdapterName" with the actual name of your network adapter
        downloadCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", networkAdapterName);
        uploadCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", networkAdapterName);
    }

    private void StartNetworkMonitoring()
    {
        networkTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };

    }

    private void UpdateNetworkSpeed()
    {
        return;

        // Get the current download and upload speeds
        float downloadSpeed = downloadCounter.NextValue() / (1024 * 1024); // Convert to Mbps
        float uploadSpeed = uploadCounter.NextValue() / (1024 * 1024); // Convert to Mbps

        // Update the UI (ensure you are on the UI thread)
        NetworkSpeedText.Text = $"⬇️ {downloadSpeed:F1} Mbps  ⬆️ {uploadSpeed:F1} Mbps";
    }

    private void UpdateMemoryUsage()
    {
        // Get available memory in MB
        float availableMemory = totalMemoryCounter.NextValue();
        float totalPhysicalMemory = GetTotalPhysicalMemory(); // Retrieve total physical memory
        float memoryUsagePercentage = 100 - (availableMemory / totalPhysicalMemory * 100);

        MemoryUsageBar.Value = memoryUsagePercentage;
        MemoryUsageText.Text = $"Memory: {memoryUsagePercentage:F1}%";
    }

    private float GetTotalPhysicalMemory()
    {
        // Use Win32 API to get total physical memory
        ulong totalMemory = 0;
        var memStatus = new MEMORYSTATUSEX();
        memStatus.dwLength = (uint) Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        GlobalMemoryStatusEx(ref memStatus);
        totalMemory = memStatus.ullTotalPhys;

        return totalMemory / (1024 * 1024); // Convert to MB
    }

    private void InitializeMemoryCounter()
    {
        totalMemoryCounter = new PerformanceCounter("Memory", "Available MBytes");
        availableMemoryCounter = new PerformanceCounter("Memory", "Commit Limit");
    }

    private void StartCpuUsageTimer()
    {
        cpuTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        cpuTimer.Tick += CpuTimer_Tick;
        cpuTimer.Start();
    }


    private void CpuTimer_Tick(object? sender, EventArgs e)
    {
        float cpuUsage = cpuCounter.NextValue();
        CpuUsageBar.Value = cpuUsage;
        CpuUsageText.Text = $"CPU: {cpuUsage:F1}%";
    }

    private void InitializeCpuCounter()
    {
        cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
    }


    private void UpdateGateway()
    {
        var gateway = NetworkInterface.GetAllNetworkInterfaces()
                        .SelectMany(n => n.GetIPProperties().GatewayAddresses)
                        .FirstOrDefault()?.Address.ToString();

        DefaultGatewayText.Text = $"Default Gateway: {gateway}" ?? "No Gateway";
    }

    private void DispatchTimer_Tick(object? sender, EventArgs e)
    {
        UpdateClock();
        UpdateDiskUsage();
        UpdateGateway();
        UpdateMemoryUsage();
        UpdateNetworkSpeed();

#if RELEASE
        // Check if the desktop is the active window
        if (IsDesktopActive())
            this.Show();
        else
            this.Hide(); 
#endif
    }

    private void UpdateClock() => ClockText.Text = DateTime.Now.ToString("HH:mm:ss d MMMM dddd", System.Globalization.CultureInfo.CreateSpecificCulture("tr-TR"));
    private bool IsDesktopActive()
    {
        const int nChars = 256;
        IntPtr handle = GetForegroundWindow();
        var className = new System.Text.StringBuilder(nChars);

        if (GetClassName(handle, className, nChars))
        {
            // Class names related to desktop and tray
            string currentClassName = className.ToString();
            return currentClassName == "Progman" ||
                currentClassName == "WorkerW" ||
                currentClassName == "Shell_TrayWnd" ||
                currentClassName == "TrayiconMessageWindow" ||
                currentClassName == "Shell_TrayWnd" ||
                currentClassName == "Windows.UI.Core.CoreWindow";
        }

        return false;
    }

    private void UpdateDiskUsage()
    {
        // Get the C: drive info (modify this if you want another drive)
        DriveInfo drive = new DriveInfo("C");

        if (drive.IsReady)
        {
            // Calculate disk usage percentage
            long totalSpace = drive.TotalSize;
            long freeSpace = drive.TotalFreeSpace;
            long usedSpace = totalSpace - freeSpace;

            double usagePercentage = ((double) usedSpace / totalSpace) * 100;

            // Update ProgressBar and TextBlock
            DiskUsageBar.Value = usagePercentage;
            DiskUsageText.Text = $"{usagePercentage:F0}% ({FormatBytes(freeSpace)} Free)";
        }
        else
        {
            DiskUsageText.Text = "Drive is not ready";
        }
    }

    private string FormatBytes(long bytes)
    {
        const long scale = 1024;
        string[] orders = new string[] { "B", "KB", "MB", "GB", "TB" };
        double max = bytes;
        int i = 0;
        while (max >= scale && i < orders.Length - 1)
        {
            max /= scale;
            i++;
        }
        return $"{max:0.0} {orders[i]}";
    }
}