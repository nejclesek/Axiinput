using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Threading;
using System.Net;

namespace Axiinput
{
    public partial class MainWindow : Window
    {
        public InputHook MainHook = null;
        private InputHook ShortcutsHook = null;
        InputSender MainInputSender = null;
        UDPServer ListenServer = null;        
        InputDispatcher MainInputDispatcher = null;
        UDPClient MainUdpClient = null;
        Maintainer MainMaintainer = null;
        private static MainWindow pInstance = null;
        public event DPortChanged EPortChanged = null;
        public delegate void DPortChanged(ushort pPort);
        public bool ServerRunning
        {
            get
            {
                return (ListenServer != null && ListenServer.Running);
            }
        }      
        public bool ClientRunning
        {
            get
            {
                return (MainUdpClient != null);
            }
        }
        public bool ClientServerIdle
        {
            get
            {
                return (!ClientRunning && !ServerRunning);
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            pInstance = this;
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            this.ResizeMode = System.Windows.ResizeMode.CanResize;
            Shortcuts.EQuitInput += Shortcuts_EQuitInput;
            Shortcuts.EToggleConnect += Shortcuts_EToggleConnect;
            Shortcuts.EToggleMinimize += Shortcuts_EToggleMinimize;
            StartStopListeningButton.Click += StartStopListeningButton_Click;
            ConnectButton.Click += ConnectButton_Click;
            this.Closing += MainWindow_Closing;
            this.UseKeyboardCheckBox.Checked += UseKeyboardCheckBox_CheckedChange;
            this.UseKeyboardCheckBox.Unchecked += UseKeyboardCheckBox_CheckedChange;
            this.UseMouseCheckBox.Checked += UseMouseCheckBox_CheckedChange;
            this.UseMouseCheckBox.Unchecked += UseMouseCheckBox_CheckedChange;
            this.PortTextBox.TextChanged += PortTextBox_TextChanged;
            this.PortTextBox.LostFocus += PortTextBox_LostFocus;
            this.PoolingTextBox.TextChanged += PoolingTextBox_TextChanged;
            this.PoolingTextBox.LostFocus += PoolingTextBox_LostFocus;
            EPortChanged += MainWindow_EPortChanged;
            Properties.Settings.Default.PropertyChanged += Default_PropertyChanged;
            RegisterShorctusHook();
            StopListeningLocalPanelButton.Click += StopListeningLocalPanelButton_Click;
            StopListeningRemotePanelButton.Click += StopListeningLocalPanelButton_Click;
            InitialiteSettings();
            RefreshIp();
            MainMaintainer = new Maintainer();
            MainMaintainer.AddMethod(new Maintainer.MaintainerMethod(MaintainerWork));
        }

        void MainWindow_EPortChanged(ushort pPort)
        {
            HelpPortTextBox.Text = pPort.ToString();
        }
        void MaintainerWork()
        {
            if(ServerRunning)
            {
                Common.ManageVisibleWindows();
            }
        }
        void Shortcuts_EToggleMinimize()
        {
            this.Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)(() =>
                {                    
                    if(this.WindowState == System.Windows.WindowState.Normal)
                    {
                        this.WindowState = System.Windows.WindowState.Minimized;
                    }
                    else if (this.WindowState == System.Windows.WindowState.Minimized)
                    {
                        this.WindowState = System.Windows.WindowState.Normal;
                    }
                }));
        }

        void Shortcuts_EToggleConnect()
        {
             this.Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)(() =>
                {
                    if(ClientRunning)
                    {
                        StopClient();
                    }
                    else
                    {
                        InitClient();
                    }
                }));
        }

        void PoolingTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PoolingTextBox.Text))
            {
                PoolingToLastValid();
            }
        }
        void RegisterShorctusHook()
        {
            ShortcutsHook = new InputHook(true, false);
            ShortcutsHook.EKeyboardInput += ShortcutsHook_EKeyboardInput;
        }
        void ReRegisterShortcutsHook()
        {
            StopShortcutsHook();
            RegisterShorctusHook();
        }
        void PoolingToLastValid()
        {
            PoolingTextBox.Text = Common.PoolingInterval.ToString();
        }
        void PoolingTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(PoolingTextBox.Text))
            {
                try
                {
                    ushort pNewPooling = Convert.ToUInt16(PoolingTextBox.Text);
                    Properties.Settings.Default.PoolingInterval = pNewPooling;                    
                }
                catch (FormatException)
                {
                    MessageBox.Show("Please enter a valid number.");
                    PoolingToLastValid();
                }
                catch (OverflowException)
                {
                    MessageBox.Show("Pooling interval is out of range.");
                }
            }
        }

        void PortTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrWhiteSpace(PortTextBox.Text))
            {
                PortToLastValid();
            }
        }

        void StopListeningLocalPanelButton_Click(object sender, RoutedEventArgs e)
        {
            ShutdownCommunication();
        }

        void ShortcutsHook_EKeyboardInput(bool pKeyUp, KBDLLHOOKSTRUCTFlags pFlags, byte pScanCode)
        {
            Shortcuts.ProcessShortcutsSend(pKeyUp, pScanCode);
        }
        void PortToLastValid()
        {
            PortTextBox.Text = Common.DefaultPort.ToString();
        }
        void PortTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(!string.IsNullOrWhiteSpace(PortTextBox.Text))
            {
                try
                {
                    ushort pNewPort = Convert.ToUInt16(PortTextBox.Text);
                    Properties.Settings.Default.DefaultPort = pNewPort;
                    if(EPortChanged != null)
                    {
                        EPortChanged(pNewPort);
                    }
                }
                catch (FormatException)
                {
                    MessageBox.Show("Please enter a valid port. Numbers only.");
                    PortToLastValid();
                }
                catch(OverflowException)
                {
                    MessageBox.Show("Port number is out of valid range.");
                }
            }
        }

        void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Properties.Settings.Default.Save();
        }       

        void InitialiteSettings()
        {
            this.UseKeyboardCheckBox.IsChecked = Common.UseKeyboard;
            this.UseMouseCheckBox.IsChecked = Common.UseMouse;
            this.PortTextBox.Text = Common.DefaultPort.ToString();
            this.PoolingTextBox.Text = Common.PoolingInterval.ToString();
        }

        void UseKeyboardCheckBox_CheckedChange(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.UseKeyboard = ((this.UseKeyboardCheckBox.IsChecked.HasValue) ? (bool)this.UseKeyboardCheckBox.IsChecked : true);
            RefreshSettingsInRuntime();           
        }

        void UseMouseCheckBox_CheckedChange(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.UseMouse = ((this.UseMouseCheckBox.IsChecked.HasValue) ? (bool)this.UseMouseCheckBox.IsChecked : true);
            RefreshSettingsInRuntime();
        }

        void RefreshSettingsInRuntime()
        {
            if (ClientRunning)
            {
                MainInputSender.InputKeyboard = Common.UseKeyboard;
                MainInputSender.InputMouse = Common.UseMouse;
                if (Common.UseKeyboard)
                {
                    MainHook.StartKeyboardHook();
                }
                else
                {
                    MainHook.StopKeyboardHook();
                }
                if (Common.UseMouse)
                {
                    MainHook.StartMouseHook();
                }
                else
                {
                    MainHook.StopMouseHook();
                }
            }
        }

        public static MainWindow Instance()
        {
            if(pInstance == null)
            {
                pInstance = new MainWindow();
            }
            return pInstance;
        }
        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ShutdownCommunication();
            StopShortcutsHook();
            StopMaintainer();
        }
        public void StopMaintainer()
        {
            if(MainMaintainer != null)
            {
                MainMaintainer.Dispose();
                MainMaintainer = null;
            }
        }
        public void StopShortcutsHook()
        {
            if (ShortcutsHook != null)
            {
                ShortcutsHook.DisposeHook();
                ShortcutsHook = null;
            }
        }
        public void ShutdownCommunication()
        {
            if(MainUdpClient != null)
            {
                MainUdpClient.Dispose();
                MainUdpClient = null;
                CursorHelper.CursorVisibility(true);
            }
            if(ListenServer != null)
            {
                ListenServer.Dispose();
                ListenServer = null;
            }
            if(MainInputSender != null)
            {
                MainInputSender.Dispose();
                MainInputSender = null;
            }
            if(MainInputDispatcher != null)
            {
                MainInputDispatcher.Dispose();
                MainInputDispatcher = null;
            }
            if(MainHook != null)
            {
                MainHook.DisposeHook();
                MainHook = null;
            }
            ClientServerRunningChanged();
        }
        void Shortcuts_EQuitInput()
        {
            StopClient();
        }
        public void StopClient()
        {
            this.Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)(() =>
            {
                ShutdownCommunication();
                if (this.WindowState == System.Windows.WindowState.Minimized)
                {
                    this.WindowState = System.Windows.WindowState.Normal;
                }
            }));
        }
        public void RefreshIp()
        {
            Common.GetMyIpAsync(RefreshIpWorker);
        }
        private void RefreshIpWorker(string pIp)
        {
            if(this.Dispatcher.CheckAccess() == false)
            {
                this.Dispatcher.Invoke(new Common.DGetMyIp(RefreshIpWorker), pIp);
                return;
            }
            YourIpAddressTextBox.Text = pIp;
            RemoteDisabledIpTextBox.Text = pIp;
            HelpIpTextBox.Text = pIp;
        }
        void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            InitClient();
        }
        void InitClient()
        {
            if (ClientServerIdle)
            {
                IPAddress pAddress;
                if (IPAddress.TryParse(IpAddressTextBox.Text, out pAddress))
                {
                    MainUdpClient = new UDPClient(pAddress, Common.DefaultPort);
                    MainInputDispatcher = new InputDispatcher(MainUdpClient);
                    MainHook = new InputHook(Common.UseKeyboard, Common.UseMouse, Common.ConsumeLocalKeyboardInput, Common.ConsumeLocalMouseInput);
                    MainHook.EKeyboardInput += MainHook_EKeyboardInput;
                    MainHook.EMouseInput += MainHook_EMouseInput;
                    if (Common.UseMouse)
                    {
                        CursorHelper.CursorVisibility(false);
                    }
                    ClientServerRunningChanged();
                    ReRegisterShortcutsHook();
                }
                else
                {
                    MessageBox.Show("Please enter a valid IP address.");
                }
            }
        }

        void ClientServerRunningChanged()
        {
            if(ServerRunning)
            {
                LocalDisabledGrid.Visibility = System.Windows.Visibility.Visible;
                RemoteDisabledGrid.Visibility = System.Windows.Visibility.Visible;
                PortTextBox.IsEnabled = false;
            }
            else if(ClientRunning)
            {
                LocalDisabledGrid.Visibility = System.Windows.Visibility.Collapsed;
                RemoteDisabledGrid.Visibility = System.Windows.Visibility.Collapsed;
                LocalConnectedGrid.Visibility = System.Windows.Visibility.Visible;
                LocalTabItem.IsSelected = true;
                PortTextBox.IsEnabled = false;
            }
            else
            {
                LocalDisabledGrid.Visibility = System.Windows.Visibility.Collapsed;
                RemoteDisabledGrid.Visibility = System.Windows.Visibility.Collapsed;
                LocalConnectedGrid.Visibility = System.Windows.Visibility.Collapsed;
                PortTextBox.IsEnabled = true;
            }
        }
        void InitServer()
        {
            if (ClientServerIdle)
            {
                MainInputSender = new InputSender();
                ListenServer = new UDPServer();
                ListenServer.EDataRecivedContinue += DataRecivedContinue;
                ClientServerRunningChanged();
            }
        }

        void MainHook_EMouseInput(MOUSEEVENTF State, short xChange, short yChange, short pWheelDelta)
        {
            MainInputDispatcher.AddInput(new InputSender.InputData() { KeyState = State, PosX = xChange, PosY = yChange, WheelDelta = pWheelDelta });
        }

        void MainHook_EKeyboardInput(bool pKeyUp, KBDLLHOOKSTRUCTFlags pFlags, byte pScanCode)
        {
            MainInputDispatcher.AddInput(new InputSender.InputData() { IsKeyboard = true, KeyboardState = pFlags, ScanCode = pScanCode });
        }

        void StartStopListeningButton_Click(object sender, RoutedEventArgs e)
        {
            InitServer();
        }
        bool DataRecivedContinue(byte[] pData)
        {
            if(pData != null)
            {
                List<InputSender.InputData> pInput = new List<InputSender.InputData>();
                for(short x = 0; x < pData.Length; )
                {
                    bool pIsKeyboard = Convert.ToBoolean(pData[x++]);
                    if(pIsKeyboard)
                    {
                        if (Common.UseKeyboard)
                        {
                            pInput.Add(new InputSender.InputData() { IsKeyboard = true, KeyboardState = (KBDLLHOOKSTRUCTFlags)pData[x], ScanCode = pData[x + 1] });
                        }
                        x += 2;
                    }
                    else
                    {
                        MOUSEEVENTF pState = (MOUSEEVENTF)BitConverter.ToUInt16(new byte[] { pData[x], pData[x + 1] }, 0);
                        x += 2;
                        short xChange = 0;
                        short yChange = 0;
                        short wheelDelta = 0;
                        if ((pState & MOUSEEVENTF.MOVE) == MOUSEEVENTF.MOVE)
                        {                            
                            xChange = BitConverter.ToInt16(pData, x);
                            x += 2;
                            yChange = BitConverter.ToInt16(pData, x);
                            x += 2;
                        }
                        if ((pState & MOUSEEVENTF.WHEEL) == MOUSEEVENTF.WHEEL || (pState & MOUSEEVENTF.HWHEEL) == MOUSEEVENTF.HWHEEL)
                        {
                            wheelDelta = BitConverter.ToInt16(pData, x);
                            x += 2;
                        }
                        if (Common.UseMouse)
                        {
                            pInput.Add(new InputSender.InputData() { IsKeyboard = false, PosX = xChange, PosY = yChange, KeyState = pState, WheelDelta = wheelDelta });
                        }
                    }
                }
                MainInputSender.InputNow(pInput);                
            }
            return true;
        }

        private void HyperlinkPortHelp_Click(object sender, RoutedEventArgs e)
        {
            Common.OpenWebSite("https://youtu.be/vFHVeWNP9Sk");
        }

        private void HyperlinkAnyDesk_Click(object sender, RoutedEventArgs e)
        {
            Common.OpenWebSite("http://anydesk.com");
        }

        private void HyperlinkHelpVisitWebSite_Click(object sender, RoutedEventArgs e)
        {
            Common.OpenWebSite("https://github.com/nejclesek/Axiinput");
        }
    }
}
