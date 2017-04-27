using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Linq;

namespace SSL_Steam
{
    public partial class Form1 : Form
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, int lpBaseAddress, byte[] buffer, int size, int lpNumberOfBytesRead);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT Rect);
        
        public Form1()
        {
            InitializeComponent();
        }
        Log Log = new Log();
        private void Form1_Load(object sender, EventArgs e)
        {
            GetShellshockSize();
            CreateControls();
        }
        ProcessModuleCollection SSModules;
        private void GetShellshockSize()
        {
            Log.Add("Getting shellshock size");
            Process[] processes = Process.GetProcessesByName("ShellShockLive");
            if (processes.Length == 0)
            {
                MessageBox.Show("Please launch ShellShock Live before trying to use aimbot", "Game not running");
                Log.Add("No game found. Exiting.");
                Application.Exit();
                return;
            }
            else if (processes.Length == 1)
            {
                RECT Rect = new RECT();
                
                foreach (Process p in processes)
                {
                    IntPtr handle = p.MainWindowHandle;
                    SSModules = p.Modules;
                    try
                    {
                        if (!GetWindowRect(handle, out Rect)) throw new Win32Exception();
                    } catch (Win32Exception ex)
                    {
                        Log.Add("Error" + ex.Message);
                    }
                }
                if (Rect.Bottom == 0 || Rect.Left == 0 || Rect.Right == 0 || Rect.Top == 0) Log.Add(string.Format("{0},{1},{2},{3}", Rect.Top, Rect.Bottom, Rect.Left, Rect.Right));
                if ((Rect.Right - Rect.Left)-46 < 1640 || (Rect.Right - Rect.Left) - 46 > 1660)
                {
                    MessageBox.Show("Width of game is not within tolerance. Please use 1680x1050 windowed mode.");
                    Log.Add(string.Format("Exited for unsupported Width: {0},{1},{2},{3};{4}", Rect.Top, Rect.Bottom, Rect.Left, Rect.Right,(Rect.Right - Rect.Left) - 46));
                    Application.Exit();
                }
                if ((Rect.Bottom - Rect.Top)-9 < 1070 || (Rect.Bottom - Rect.Top)-9 > 1090)
                {
                    MessageBox.Show(((Rect.Bottom - Rect.Top) - 9 < 1090).ToString());
                    MessageBox.Show("Height of game is not within tolerance. Please use 1680x1050 windowed mode.");
                    Log.Add(string.Format("Exited for unsupported height: {0},{1},{2},{3};{4}", Rect.Top, Rect.Bottom, Rect.Left, Rect.Right, (Rect.Bottom - Rect.Top)-9));
                    Application.Exit();
                }
                this.Top = Rect.Top;
                this.Left = Rect.Left;
                this.Width = (Rect.Right - Rect.Left);
                this.Height = (Rect.Bottom - Rect.Top);
                Log.Add("ShellShock found. Setting client bounds to " + string.Format("{0},{1},{2},{3}", Rect.Top, Rect.Bottom, Rect.Left, Rect.Right));
            } else
            {
                MessageBox.Show("Too many instances running. Please only run a single instance.", "Multiple Game instances");
                Log.Add("Too many games found. Exiting.");
                Application.Exit();
            }
            
        }
        PictureBox AimingContainer = new PictureBox();
        TrackBar AngleTrack = new TrackBar();
        TrackBar StrengthTrack = new TrackBar();
        Label StrengthLbl = new Label();
        Label AngleLbl = new Label();
        CheckBox TMCB = new CheckBox();
        ComboBox VariationCB = new ComboBox();
        Timer MemoryCheckTimer = new Timer();
        private void CreateControls()
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            AimingContainer.Location = new Point(0,0);
            AimingContainer.Height = this.Height;
            AimingContainer.Width = this.Width;
            AimingContainer.BackColor = Color.Lime;
            AimingContainer.Image = new Bitmap(AimingContainer.Width, AimingContainer.Height);
            TankLocation.Image = new Bitmap(7, 7);
            Graphics.FromImage(TankLocation.Image).FillEllipse(Brushes.Orange, 0, 0, 7, 7);
            TankLocation.Refresh();
            VariationCB.Location = new Point(10, 30);
            VariationCB.Items.Add("Default/Single Shot");
            VariationCB.Items.Add("Three-Ball");
            VariationCB.Items.Add("Five-Ball");
            VariationCB.Items.Add("Rainbow");
            VariationCB.Items.Add("Yin Yang");
            VariationCB.Items.Add("Counter 3000");
            //VariationCB.Items.Add("Gravies"); //Broken pls ignore
            VariationCB.Items.Add("Payload");
            VariationCB.SelectedIndex = 0;
            VariationCB.DropDownStyle = ComboBoxStyle.DropDownList;
            TMCB.Location = new Point(10, 10);
            TMCB.Size = new Size(VariationCB.Width, 20);
            TMCB.Padding = new Padding(5,0,0,0);
            TMCB.Checked = true;
            TMCB.Text = "Top Most Window";
            TMCB.Click += TopMostClick;
            //VariationCB.SelectedIndexChanged += DrawTracer;
            OffsetDict.Add("Power", 0x20);
            OffsetDict.Add("Angle", 0x1c);
            MemoryCheckTimer.Tick += MemoryCheck_Tick;
            MemoryCheckTimer.Interval = 10;
            MemoryCheckTimer.Enabled = true;
            this.Text = "SSL-Steam";
            this.Controls.Add(VariationCB);
            this.Controls.Add(TMCB);
            this.Controls.Add(AimingContainer);
            this.TopMost = true;
            Log.Add("Controls loaded");
        }
        void DrawTracer()
        {
            //DrawTracer(null, null);
        }
        private void MemoryCheck_Tick(object sender, EventArgs e)
        {
            PowerAnglePtr = Pointer(0x010AEC98, new int[] { 0x0,0x14,0x14,0x60,0x384,0x10 }, "shellshocklive.exe");
            HorizontalTankPtr = Pointer(0x001F62c8, new int[] { 0x0,0x50,0x330,0x60,0x14}, "mono.dll");
            VerticalTankPtr = Pointer(0x001f6994, new int[] { 0x1c,0x2a0,0x68,0x58,0x16c}, "mono.dll");
            int Power = GetProcInt(PowerAnglePtr, OffsetDict["Power"]);
            int Angle = GetProcInt(PowerAnglePtr, OffsetDict["Angle"]);
            int TankX = GetProcInt(HorizontalTankPtr, 0x40);
            int TankY = GetProcInt(VerticalTankPtr, 0x22c);
            if (Power != -1 || Angle != -1 || TankX != -1 || TankY != -1)
            {
                DrawTracer(Angle, Power);
                MoveTankDot(TankX, TankY);
            }
        }
        private void DrawTracer(int angle, int power)
        {
            DisplayAngle(GetAngle(power, angle, VariationCB.SelectedIndex.ToString()));
        }
        private void TopMostClick(object a, EventArgs b)
        {
            CheckBox c = (CheckBox) a;
            if (c.Checked)
            {
                this.TopMost = true;
            } else
            {
                this.TopMost = false;
            }
        }
        private List<KeyValuePair<int,int>> GetAngle(double power, int angle, string variation = null)
        {
            List<KeyValuePair<int, int>> XYDict = new List<KeyValuePair<int, int>>();
            bool shotLeft = false;
            if (angle < 181 && angle > -1)
            {
                angle = (angle - 90) * -1;
            }
            else
            {
                shotLeft = true;
                angle = (((angle - 360) * -1) - 90) * -1;
            }
            if (variation == null || variation == "0")
            {
                double _grav = 9.8067;
                power = power * 1.48;
                double theta = (angle) * (Math.PI / 180);
                for (var i = 1; i < AimingContainer.Width; i++)
                {
                    double dist = i;
                    double y = 0 + (dist * Math.Tan(theta)) - ((_grav * Math.Pow(dist, 2)) / (2 * (Math.Pow(power * Math.Cos(theta), 2))));
                    if (y > -1600)
                    {
                        XYDict.Add(new KeyValuePair<int, int>(shotLeft ? i*-1:i, Convert.ToInt32(y)));
                    }
                }
            }
            else if (variation == "1") //3ball
            {
                power = power * 1.48;
                for (var k = 0; k < 3; k++)
                {
                    int tangle = 0;
                    switch (k)
                    {
                        case 0:
                            tangle = angle - 5;
                            break;
                        case 1:
                            tangle = angle;
                            break;
                        case 2:
                            tangle = angle + 5;
                            break;
                    }
                    double _grav = 9.80665;
                    double theta = (tangle) * (Math.PI / 180);

                    for (var i = 1; i < AimingContainer.Width; i++)
                    {
                        double dist = i;
                        double y = 0 + (dist * Math.Tan(theta)) - ((_grav * Math.Pow(dist, 2)) / (2 * (Math.Pow(power * Math.Cos(theta), 2))));
                        if (y > -1600)
                        {
                            XYDict.Add(new KeyValuePair<int, int>(shotLeft ? i * -1 : i, Convert.ToInt32(y)));
                        }
                    }
                }
            }
            else if (variation == "2") //5ball
            {
                power = power * 1.48;
                for (var k = 0; k < 5; k++)
                {
                    int tangle = 0;
                    switch (k)
                    {
                        case 0:
                            tangle = angle - 11;
                            break;
                        case 1:
                            tangle = angle - 5;
                            break;
                        case 2:
                            tangle = angle;
                            break;
                        case 3:
                            tangle = angle + 5;
                            break;
                        case 4:
                            tangle = angle + 11;
                            break;
                    }
                    double _grav = 9.80665;
                    double theta = (tangle) * (Math.PI / 180);

                    for (var i = 1; i < AimingContainer.Width; i++)
                    {
                        double dist = i;
                        double y = 0 + (dist * Math.Tan(theta)) - ((_grav * Math.Pow(dist, 2)) / (2 * (Math.Pow(power * Math.Cos(theta), 2))));
                        if (y > -1600)
                        {
                            XYDict.Add(new KeyValuePair<int, int>(shotLeft ? i * -1 : i, Convert.ToInt32(y)));
                        }
                    }
                }
            }
            else if (variation == "3") //Rainbow
            {
                power = power * 1.48;
                for (var k = 0; k < 6; k++)
                {
                    int yOffset = 0;
                    switch (k)
                    {
                        case 0:
                            yOffset = -50;
                            break;
                        case 1:
                            yOffset = -30;
                            break;
                        case 2:
                            yOffset = -10;
                            break;
                        case 3:
                            yOffset = 10;
                            break;
                        case 4:
                            yOffset = 30;
                            break;
                        case 5:
                            yOffset = 50;
                            break;
                    }
                    double _grav = 9.80665;
                    double theta = (angle) * (Math.PI / 180);

                    for (var i = 1; i < AimingContainer.Width; i++)
                    {
                        double dist = i;
                        double y = 0 + (dist * Math.Tan(theta)) - ((_grav * Math.Pow(dist, 2)) / (2 * (Math.Pow(power * Math.Cos(theta), 2))));
                        if (y > -1600)
                        {
                            XYDict.Add(new KeyValuePair<int, int>(shotLeft ? i * -1 : i, Convert.ToInt32(y+yOffset)));
                        }
                    }
                }
            }
            else if (variation == "4") //Yin Yang
            {
                for (var k = 0; k < 2; k++)
                {
                    int tangle = (k == 1) ? angle+2:angle;
                    double pow = (k == 1) ? power + 3 : power;
                    pow = pow * 1.48;
                    double _grav = 9.80665;
                    double theta = (angle) * (Math.PI / 180);

                    for (var i = 1; i < AimingContainer.Width; i++)
                    {
                        double dist = i;
                        double y = 0 + (dist * Math.Tan(theta)) - ((_grav * Math.Pow(dist, 2)) / (2 * (Math.Pow(pow * Math.Cos(theta), 2))));
                        if (y > -1600)
                        {
                            if (k == 1) y = y * -1;
                            XYDict.Add(new KeyValuePair<int, int>(shotLeft ? i * -1 : i, Convert.ToInt32(y)));
                        }
                    }
                }
            }
            else if (variation == "5") //Counter 3k
            {
                power = power * 1.48;
                for (var k = 0; k < 5; k++)
                {
                    int tangle = 0;
                    switch (k)
                    {
                        case 0:
                            tangle = angle - 6;
                            break;
                        case 1:
                            tangle = angle - 3;
                            break;
                        case 2:
                            tangle = angle;
                            break;
                        case 3:
                            tangle = angle + 3;
                            break;
                        case 4:
                            tangle = angle + 6;
                            break;
                    }
                    double _grav = 9.80665;
                    double theta = (tangle) * (Math.PI / 180);

                    for (var i = 1; i < AimingContainer.Width; i++)
                    {
                        double dist = i;
                        double y = 0 + (dist * Math.Tan(theta)) - ((_grav * Math.Pow(dist, 2)) / (2 * (Math.Pow(power * Math.Cos(theta), 2))));
                        if (y > -1600)
                        {
                            XYDict.Add(new KeyValuePair<int, int>(shotLeft ? i * -1 : i, Convert.ToInt32(y)));
                        }
                    }
                }
            //} else if (variation == "6") //Gravies is fuckin broken...pls ignore
            //{
            //    for (var k = 0; k < 4; k++)
            //    {
            //        double pow = 0;
            //        switch (k)
            //        {
            //            case 0:
            //                pow = power + 18;
            //                break;
            //            case 1:
            //                pow = power + 5;
            //                break;
            //            case 2:
            //                pow = power - 4;
            //                break;
            //            case 3:
            //                pow = power - 11;
            //                break;
            //        }
            //        double _grav = 9.80665;
            //        double tpower = pow * 1.48;
            //        double theta = (angle < -90?angle*-1:angle) * (Math.PI / 180);

            //        for (var i = 1; i < AimingContainer.Width; i++)
            //        {
            //            double dist = i;
            //            double y = 0 + (dist * Math.Tan(theta)) - ((_grav * Math.Pow(dist, 2)) / (2 * (Math.Pow(tpower * Math.Cos(theta), 2))));
            //            if (y > -1600)
            //            {
            //                if (direction == "left")
            //                {
            //                    XYDict.Add(new KeyValuePair<int, int>(i * -1, Convert.ToInt32(y)));
            //                }
            //                else
            //                {
            //                    XYDict.Add(new KeyValuePair<int, int>(i, Convert.ToInt32(y)));
            //                }
            //            }
            //        }
            //    }
            } else if (variation == "6") //Payload
            {
                double _grav = 9.80665;
                power = power * 1.48;
                double theta = (angle) * (Math.PI / 180);
                KeyValuePair <int, int> apex = new KeyValuePair<int, int>(0,0);
                int apexCount = 0;
                for (var i = 1; i < AimingContainer.Width; i++)
                {
                    double dist = i;
                    double y = 0 + (dist * Math.Tan(theta)) - ((_grav * Math.Pow(dist, 2)) / (2 * (Math.Pow(power * Math.Cos(theta), 2))));
                    if (y > -1600)
                    {
                        XYDict.Add(new KeyValuePair<int, int>(shotLeft ? i * -1 : i, Convert.ToInt32(y)));
                    }
                    if (apex.Value < Convert.ToInt32(y))
                    {
                        apex = new KeyValuePair<int, int>(shotLeft ? i * -1 : i, Convert.ToInt32(y));
                    }
                    
                }
                apexCount = XYDict.Count(x => x.Value == apex.Value);
                for (var i = 0; i < 1500; i++)
                {
                    XYDict.Add(new KeyValuePair<int, int>(apex.Key-(int)Math.Ceiling((double)apexCount/2), apex.Value-i));
                }
            }
            return XYDict;
        }
        Bitmap BMPHolder = null;
        Size BMPSize = new Size(0,0);
        private void DisplayAngle(List<KeyValuePair<int,int>> d)
        {
            if (BMPSize == new Size(0,0))
            {
                BMPSize = AimingContainer.Image.Size;
                BMPHolder = new Bitmap(BMPSize.Width, BMPSize.Height);
            } else
            {
                AimingContainer.Image.Dispose();
            }
           AimingContainer.Image = new Bitmap(BMPHolder);
            Graphics g = Graphics.FromImage(AimingContainer.Image);
            for (var i = 0; i < d.Count; i++)
            {
                KeyValuePair<int, int> kp = d[i];
                int x = TankLocation.Location.X + kp.Key + 4;
                int y = TankLocation.Location.Y - kp.Value;
                g.FillRectangle(Brushes.White, x, y, 1, 1);
            }
            AimingContainer.Refresh();
            GC.Collect();
        }
        private void MoveTankDot(int x, int y)
        {
            try
            {
                float sx = BitConverter.ToSingle(BitConverter.GetBytes(x), 0);
                float sy = BitConverter.ToSingle(BitConverter.GetBytes(y), 0);
                float f = sy + 4.099999905f;
                f = f * 1000000000000000;
                double d = Convert.ToDouble(f * 0.0000000000001683673436618855f);
                int i = Convert.ToInt32(d);
                TankLocation.Location = new Point((int)(sx * 1.68) - 3, ((int)(Convert.ToInt32(d) - 895) * -1));
            } catch (OverflowException ox)
            {
                Log.Add("Tried to move dot to invalid location");
            }
        }
        private void FClosing(object sender, FormClosingEventArgs e)
        {
            Log.Add("Exiting");
            Log.Dump();
            Application.Exit();
        }
        int PowerAnglePtr = -1;
        int HorizontalTankPtr = -1;
        int VerticalTankPtr = -1;
        Dictionary<string, int> OffsetDict = new Dictionary<string, int>();
        public int Pointer(int Address, int[] Offsets, string modulename)
        {
            try
            {
                int BaseAddy = 0x0;
                int Addy = -1;

                foreach (ProcessModule M in SSModules)
                {
                    if (M.ModuleName.ToLower() == modulename)
                    {
                        BaseAddy = M.BaseAddress.ToInt32() + Address;
                        break;
                    }
                }
                IntPtr SSHandle = (Process.GetProcessesByName("ShellShockLive"))[0].Handle;
                byte[] buff = new byte[4];
                ReadProcessMemory(SSHandle, BaseAddy, buff, 4, 0);
                BaseAddy = BitConverter.ToInt32(buff, 0);
                for (int i = 0; i < Offsets.Length; i++)
                {
                    ReadProcessMemory(SSHandle, BaseAddy + Offsets[i], buff, 4, 0);
                    BaseAddy = BitConverter.ToInt32(buff, 0);
                }
                return BaseAddy;
            } catch (Exception ex)
            {
                Log.Add("Exception in Pointer(): " + ex.Message);
                return -1;
            }
        }
        public int GetProcInt(int _baseaddy, int _offset)
        {
            try
            {
                int result = -1;
                byte[] buff = new byte[4];
                IntPtr _handle = (Process.GetProcessesByName("ShellShockLive"))[0].Handle;
                ReadProcessMemory(_handle, _baseaddy + _offset, buff, 4, 0);
                result = BitConverter.ToInt32(buff, 0);
                return result;
            } catch (Exception ex)
            {
                Log.Add("Exception in GetProcInt: " + ex.Message);
                return -1;
            }
        }
    }
    class Log
    {
        private Dictionary<string, string> ELog;
        public Log()
        {
            ELog = new Dictionary<string, string>();
            Add("------------------------------");
            Add("Something not working? Message me on Discord: 'Simple_AOB#5526' with this log ready");
            Add("------------------------------");
            Add(Environment.OSVersion);
            Add(Environment.Version);
        }
        private static long lastTimeStamp = DateTime.UtcNow.Ticks;
        public static long UtcNowTicks
        {
            get
            {
                long original, origts;
                do
                {
                    original = lastTimeStamp;
                    long now = DateTime.UtcNow.Ticks;
                    origts = Math.Max(now, original + 1);
                } while (System.Threading.Interlocked.CompareExchange
                             (ref lastTimeStamp, origts, original) != original);

                return origts;
            }
        }
        public void Add(object msg)
        {
            ELog.Add(UtcNowTicks.ToString(), msg.ToString());
        }
        public void Dump()
        {
            using (StreamWriter sw = new StreamWriter(Environment.CurrentDirectory + @"\ssl-steam.log"))
            {
                foreach (KeyValuePair<string, string> kp in ELog)
                {
                    sw.WriteLine(string.Format("{0}>> {1}", kp.Key, kp.Value));
                }
            }
        }
    }
}
