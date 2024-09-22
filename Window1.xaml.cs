using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace Bginfz;
/// <summary>
/// Interaction logic for Window1.xaml
/// </summary>
public partial class Window1 : Window
{
    DispatcherTimer diskUsageTimer;

    // P/Invoke to get the active window
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);


    public Window1()
    {
        InitializeComponent();

        diskUsageTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        diskUsageTimer.Tick += DispatchTimer_Tick;
        diskUsageTimer.Start();

    }

    private void DispatchTimer_Tick(object? sender, EventArgs e)
    {
        // Check if the desktop is the active window
        if (IsDesktopActive())
            this.Show();
        else
            this.Hide();

        UpdateDiskUsage();
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

}