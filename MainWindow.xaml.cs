using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Bginfz;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    DispatcherTimer dispatchTimer;

    // P/Invoke to get the active window
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);


    public MainWindow()
    {
        InitializeComponent();
#if false
        new Window1().Show();
#endif

        dispatchTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        dispatchTimer.Tick += DispatchTimer_Tick;
        dispatchTimer.Start();
    }

    private static System.Net.IPAddress GetDefaultGateway()
    {
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up);

        foreach (var netInterface in networkInterfaces)
        {
            var gateway = netInterface.GetIPProperties()?.GatewayAddresses
                ?.FirstOrDefault()?.Address;
            if (gateway != null)
            {
                return gateway;
            }
        }

        return null;
    }

    private void UpdateGateway()
    {
        var gateway = NetworkInterface.GetAllNetworkInterfaces()
                        .SelectMany(n => n.GetIPProperties().GatewayAddresses)
                        .FirstOrDefault()?.Address.ToString();

        DefaultGatewayText.Text = gateway ?? "No Gateway";
    }

    private void DispatchTimer_Tick(object? sender, EventArgs e)
    {
        UpdateClock();
        UpdateDiskUsage();
        UpdateGateway();
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