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
        new Window1().Show();

        dispatchTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        dispatchTimer.Tick += DispatchTimer_Tick;
        dispatchTimer.Start();
    }

    private void DispatchTimer_Tick(object? sender, EventArgs e)
    {
        ClockText.Text = DateTime.Now.ToString("HH:mm:ss");

        // Check if the desktop is the active window
        if (IsDesktopActive())
            this.Show();
        else
            this.Hide();
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