using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace AutoClicker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;

        private HwndSource source;
        private IntPtr windowHandle;

        private const int CLICKS_PER_TICK = 20;
        private const int TICK_DURATION = 5; // In milliseconds

        private const int REGISTER_MOUSEPOS_HOTKEY_ID = 9000;
        private const int TOGGLE_AUTOCLICK_HOTKEY_ID = 9001;

        private const uint MOD_ALT = 0x01;
        private const uint MOD_CTRL = 0x02;
        private const uint MOD_SHIFT = 0x04;

        private int mouseX = 0;
        private int mouseY = 0;

        private bool isStarted = false;

        private Thread thread;

        public event PropertyChangedEventHandler PropertyChanged;

        public string StatusText
        {
            get
            {
                if (isStarted)
                {
                    return "Started";
                }
                else
                {
                    return "Stopped";
                }
            }
        }

        public string MousePositionText
        {
            get
            {
                return mouseX + ", " + mouseY;
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            WindowInteropHelper helper = new WindowInteropHelper(this);
            windowHandle = helper.Handle;
            source = HwndSource.FromHwnd(windowHandle);
            source.AddHook(HwndHook);

            RegisterHotKeys();
        }

        protected override void OnClosed(EventArgs e)
        {
            source.RemoveHook(HwndHook);
            source = null;
            UnregisterHotKeys();

            base.OnClosed(e);
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        private void RegisterHotKeys()
        {
            // Register mouse pos hotkey: Ctrl + Shift + F1 (0x70)
            RegisterHotKey(windowHandle, REGISTER_MOUSEPOS_HOTKEY_ID, MOD_CTRL | MOD_SHIFT, 0x70);

            // Toggle auto click hotkey: Ctrl + Shift + F2 (0x71)
            RegisterHotKey(windowHandle, TOGGLE_AUTOCLICK_HOTKEY_ID, MOD_CTRL | MOD_SHIFT, 0x71);
        }

        private void RegisterMousePosition()
        {
            Point mousePos = GetMousePosition();
            mouseX = (int)mousePos.X;
            mouseY = (int)mousePos.Y;

            RaisePropertyChanged("MousePositionText");
        }

        private void ToggleAutoClick()
        {
            isStarted = !isStarted;
            RaisePropertyChanged("StatusText");

            if ((thread != null) && thread.IsAlive)
            {
                thread.Abort();
            }

            if (isStarted)
            {
                thread = new Thread(new ThreadStart(ThreadProc));
                thread.Start();
            }
            else
            {
                thread.Abort();
            }
        }

        private void UnregisterHotKeys()
        {
            UnregisterHotKey(windowHandle, REGISTER_MOUSEPOS_HOTKEY_ID);
            UnregisterHotKey(windowHandle, TOGGLE_AUTOCLICK_HOTKEY_ID);
        }

        private void ThreadProc()
        {
            try
            {
                while (true)
                {
                    for (int i = 0; i < CLICKS_PER_TICK; ++i)
                    {
                        mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, mouseX, mouseY, 0, 0);
                    }

                    Thread.Sleep(TICK_DURATION);
                }
            }
            catch (Exception)
            {

            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case REGISTER_MOUSEPOS_HOTKEY_ID:
                            handled = true;
                            RegisterMousePosition();
                            break;

                        case TOGGLE_AUTOCLICK_HOTKEY_ID:
                            handled = true;
                            ToggleAutoClick();
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };
        public static Point GetMousePosition()
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }
    }
}
