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
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        public Form1()
        {
            InitializeComponent();
        }
        Log Log = new Log("ssl-steam");
        private void Form1_Load(object sender, EventArgs e)
        {
            GetShellshockSize();
            CreateControls();
            RegisterHotKey(this.Handle, 1230, 2, (int)Keys.Left);
            RegisterHotKey(this.Handle, 1231, 2, (int)Keys.Right);
            RegisterHotKey(this.Handle, 1232, 2, (int)Keys.Up);
            RegisterHotKey(this.Handle, 1233, 2, (int)Keys.Down);
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x312)
            {
                Keys vk = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
                int fsModifiers = ((int)m.LParam & 0xFFFF);
                if (vk == Keys.Left && fsModifiers == (2))
                {
                    Point curtank = TankLocation.Location;
                    curtank.X -= 1;
                    TankLocation.Location = curtank;
                }
                if (vk == Keys.Right && fsModifiers == (2))
                {
                    Point curtank = TankLocation.Location;
                    curtank.X += 1;
                    TankLocation.Location = curtank;
                }
                if (vk == Keys.Up && fsModifiers == (2))
                {
                    Point curtank = TankLocation.Location;
                    curtank.Y -= 1;
                    TankLocation.Location = curtank;
                }
                if (vk == Keys.Down && fsModifiers == (2))
                {
                    Point curtank = TankLocation.Location;
                    curtank.Y += 1;
                    TankLocation.Location = curtank;
                }
            }
            base.WndProc(ref m);
        }
        int cursorX, CursorY;
        bool Dragging;
        private void Tank_MouseDown(object sender, MouseEventArgs e)
        {
            Dragging = true;
            cursorX = e.X;
            CursorY = e.Y;
        }

        private void Tank_MouseUp(object sender, MouseEventArgs e)
        {
            Dragging = false;
        }

        private void Tank_MouseMove(object sender, MouseEventArgs e)
        {
            if (Dragging)
            {
                Control ctrl = (Control)sender;
                ctrl.Left = (ctrl.Left + e.X) - cursorX;
                ctrl.Top = (ctrl.Top + e.Y) - CursorY;
                ctrl.BringToFront();
            }
        }
        bool AutoPosition = true;
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
        CheckBox AutoCB = new CheckBox();
        ComboBox VariationCB = new ComboBox();
        Timer TracerCheckTimer = new Timer();
        Timer SignatureTimer = new Timer();

        //TextBox GravTest = new TextBox();
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
            VariationCB.Location = new Point(10, 50);
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
            AutoCB.Location = new Point(10, 30);
            AutoCB.Size = new Size(VariationCB.Width, 20);
            AutoCB.Padding = new Padding(5,0,0,0);
            AutoCB.Checked = true;
            AutoCB.Text = "Auto Position";
            AutoCB.Click += AutoCbClick;
            //VariationCB.SelectedIndexChanged += DrawTracer;
            OffsetDict.Add("Power", 0x20);
            OffsetDict.Add("Angle", 0x1c);
            TracerCheckTimer.Tick += TracerCheck_Tick;
            TracerCheckTimer.Interval = 10;
            TracerCheckTimer.Enabled = true;
            SignatureTimer.Tick += SignatureTimer_Tick;
            SignatureTimer.Interval = 1000;
            SignatureTimer.Enabled = true;
            //GravTest.Location = new Point(500, 500);
            //GravTest.Text = "1.48";
            this.Text = "SSL-Steam";
            //this.Controls.Add(GravTest);
            this.Controls.Add(VariationCB);
            this.Controls.Add(TMCB);
            this.Controls.Add(AutoCB);
            this.Controls.Add(AimingContainer);
            this.TopMost = true;
            Log.Add("Controls loaded");
        }
        SigControlsTLoc SSCTL = new SigControlsTLoc();
        private void SignatureTimer_Tick(object sender, EventArgs e)
        {
            if (AutoPosition)
            {
                if (!SSCTL.HasScanned)
                {
                    foreach (Control c in SSCTL.Display())
                    {
                        this.Controls.Add(c);
                        if (c is GroupBox)
                        {
                            Controls.SetChildIndex(c, 1);
                        } else
                        {
                            Controls.SetChildIndex(c, 0);
                        }
                    }
                }
                else if (SSCTL.DecidedAddress != -1)
                {
                    if (SignatureTimer.Interval != 10) SignatureTimer.Interval = 10;

                    int da = SSCTL.DecidedAddress;
                    int TankX = GetProcInt("ShellShockLive", da);
                    int TankY = GetProcInt("ShellShockLive", da + 4);
                    if (TankX != -1 || TankY != -1)
                    {
                        float sx = BitConverter.ToSingle(BitConverter.GetBytes(TankX), 0);
                        float sy = BitConverter.ToSingle(BitConverter.GetBytes(TankY), 0);
                        if (sy < -4.2f || sy > 0.9f || sx < -5f || sy > 5f)
                        {
                            if (sy != -1000f && sx != -1000f)
                            {
                                SSCTL.StatusText("Game no longer in progress. Scan Again");
                            }
                            else
                            {
                                SSCTL.StatusText("Not your turn. Waiting to move");
                            }
                        }
                        else
                        {
                            if (sy.ToString("G", System.Globalization.CultureInfo.InvariantCulture).IndexOf('E') != -1 || sx.ToString("G", System.Globalization.CultureInfo.InvariantCulture).IndexOf('E') != -1)
                            {
                                SSCTL.StatusText("Game no longer in progress. Scan Again");
                            } else
                            {
                                MoveTankDot(TankX, TankY);
                            }
                            
                        }
                    }
                }
            }
        }
        SigControlsAS SSCAS = new SigControlsAS();
        private void TracerCheck_Tick(object sender, EventArgs e)
        {
            if (!SSCAS.HasScanned)
            {
                foreach (Control c in SSCAS.Display())
                {
                    this.Controls.Add(c);
                    if (c is GroupBox)
                    {
                        Controls.SetChildIndex(c, 1);
                    }
                    else
                    {
                        Controls.SetChildIndex(c, 0);
                    }
                }
            } else
            {
                int da = SSCAS.DecidedAddress;
                if (da != -1)
                {
                    int Angle = GetProcInt("ShellShockLive", da);
                    int Strength = GetProcInt("ShellShockLive", da + 4);
                    DrawTracer(Angle, Strength);
                }
            }
        }
        private void AutoCbClick(object sender, EventArgs e)
        {
            if (!AutoCB.Checked)
            {
                AutoPosition = false;
                TankLocation.MouseDown += Tank_MouseDown;
                TankLocation.MouseUp += Tank_MouseUp;
                TankLocation.MouseMove += Tank_MouseMove;
                SSCTL.Disable();
            } else
            {
                AutoPosition = true;
                TankLocation.MouseDown -= Tank_MouseDown;
                TankLocation.MouseUp -= Tank_MouseUp;
                TankLocation.MouseMove -= Tank_MouseMove;
                SSCTL.Enable();
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

                float fy = sy + 4.099999905f;
                fy = fy * 1000000000000000;
                double dy = Convert.ToDouble(fy * 0.0000000000001683673436618855f);
                int iy = Convert.ToInt32(dy);

                float fx = sx + 4.099999905f;
                fx = fx * 1000000000000000;
                double dx = Convert.ToDouble(fx * 0.0000000000001683673436745106f);
                int ix = Convert.ToInt32(dx);

                TankLocation.Location = new Point((Convert.ToInt32(dx) + 145), ((int)(Convert.ToInt32(dy) - 900) * -1));
            } catch (Exception ox)
            {
                Log.Add("Tried to move dot to invalid location");
            }
        }
        private void FClosing(object sender, FormClosingEventArgs e)
        {
            Log.Add("Exiting");
            Log.Dump();
            SSCTL.DumpLog();
            Application.Exit();
        }
        int PowerAnglePtr = -1;
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
        public int GetProcInt(string module, int offset)
        {
            try
            {
                int result = -1;
                byte[] buff = new byte[4];
                IntPtr _handle = (Process.GetProcessesByName(module))[0].Handle;
                ReadProcessMemory(_handle, offset, buff, 4, 0);
                result = BitConverter.ToInt32(buff, 0);
                return result;
            } catch (Exception ex)
            {
                Log.Add("Exception in GetProcInt: " + ex.Message);
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
    class SigControlsTLoc
    {
        Button AskToScanBtn = new Button();
        Button WrongAddressBtn = new Button();
        Label StatusLabel = new Label();
        SigManager sigMgr;
        Log SigLog = new Log("ssl-sig-tl");
        List<int> AddrList = new List<int>();
        public bool AutoPosition = false;
        public bool HasScanned;
        public int DecidedAddress = -1;
        
        private int scanStart = 0x00000000;
        private int scanEnd = 0x70000000;
        public SigControlsTLoc()
        {
            init();
        }
        private void init()
        {
            sigMgr = new SigManager(scanStart, scanEnd, "ShellShockLive");
            sigMgr.AddSignature("Tank Location", "00 00 7A C4 00 00 7A C4");
            AskToScanBtn.Text = "Scan";
            AskToScanBtn.Click += ScanBtnClick;
            AskToScanBtn.Location = new Point(10, 130);
            AskToScanBtn.Size = new Size(120, 20);
            AskToScanBtn.BackColor = Color.Transparent;
            WrongAddressBtn.Text = "Try Another Address";
            WrongAddressBtn.Click += WrongAddressBtnClick;
            WrongAddressBtn.Location = new Point(10, 150);
            WrongAddressBtn.Size = new Size(120, 20);
            WrongAddressBtn.BackColor = Color.Transparent;
            WrongAddressBtn.Enabled = false;
            StatusLabel.Text = "Ready To Scan for Tank Tracker";
            StatusLabel.Size = new Size(120, 40);
            StatusLabel.TextAlign = ContentAlignment.MiddleCenter;
            StatusLabel.Location = new Point(10, 90);
            Green(StatusLabel);
        }
        public void Disable()
        {
            AutoPosition = false;
            AskToScanBtn.Enabled = false;
            WrongAddressBtn.Enabled = false;
            StatusText("Auto Positioning Disabled");
            Red(StatusLabel);
        }
        public void Enable()
        {
            AutoPosition = true;
            AskToScanBtn.Enabled = true;
            WrongAddressBtn.Enabled = (DecidedAddress != 1) ? true : false;
            StatusText("Ready To Scan");
            Green(StatusLabel);
        }
        public void StatusText(string t)
        {
            StatusLabel.Text = t;
        }
        public List<Control> Display()
        {
            List<Control> cc = new List<Control>();
            cc.Add(AskToScanBtn);
            cc.Add(WrongAddressBtn);
            cc.Add(StatusLabel);
            return cc;
        }
        private Label Red(Label l)
        {
            l.ForeColor = Color.Red;
            return l;
        }
        private Label Green(Label l)
        {
            l.ForeColor = Color.Green;
            return l;
        }
        private Label Black(Label l)
        {
            l.ForeColor = Color.Black;
            return l;
        }
        private void ScanBtnClick (object a, object b)
        {
            StatusText("Scan In Progress");
            AskToScanBtn.Enabled = false;
            if (sigMgr.ScanForSignatures("Tank Location"))
            {
                HasScanned = true;
                DecideAddress();
                AskToScanBtn.Enabled = true;
            } else
            {
                StatusText("SS1: Error occured during scan");
                SigLog.Add(sigMgr.LastError);
            }
        }
        private void WrongAddressBtnClick(object a, object b)
        {
            InvalidAddress(DecidedAddress);
        }
        private void DecideAddress(bool fromInvalid = false)
        {
            List<int> adrs = (fromInvalid) ? AddrList : sigMgr.GetAddressList();
            if (adrs.Count == 0)
            {
                StatusText("DA1: No compatible addresses found. \r\nTry Again");
                DecidedAddress = -1;
                Red(StatusLabel);
                WrongAddressBtn.Enabled = false;
                return;
            }
            else
            {
                if (adrs.Count == 1)
                {
                    DecidedAddress = scanStart + adrs[0];
                    string t = ((adrs[0].ToString("x8"))[7] == 'c') ? "a prefered" : "an";
                    StatusText(string.Format("Found {0} address. AutoPos enabled", t));
                    Black(StatusLabel);
                }
                else
                {
                    //Addresses ending in c (hexdec form) have a high probability of working during my tests. Program will prefer c endings
                    List<int> EndsInC = new List<int>();
                    for (var i = 0; i < adrs.Count; i++)
                    {
                        if ((adrs[i].ToString("x8"))[7] == 'c')
                        {
                            EndsInC.Add(i);
                        }
                    }
                    if (EndsInC.Count == 0)
                    {
                        DecidedAddress = scanStart + adrs[0];
                        StatusText("No prefered addresses found. AutoPos enabled");
                    }
                    else
                    {
                        DecidedAddress = scanStart + adrs[EndsInC[0]];
                        StatusText("Preferd address found. AutoPos enabled");
                    }
                    Black(StatusLabel);
                }
            }
            WrongAddressBtn.Enabled = true;
            AddrList = adrs;
        }
        /// <summary>
        /// Called by the user manually. Basically it tells the SSC class to throw out the named address and return another if any exist.
        /// </summary>
        /// <param name="addr"></param>
        public void InvalidAddress(int addr)
        {
            int index = AddrList.IndexOf(addr);
            if (index != -1)
            {
                AddrList.RemoveAt(index);
            }
            DecideAddress(true);
        }
        public void DumpLog()
        {
            SigLog.Dump();
        }
    }
    class SigControlsAS
    {
        Button AskToScanBtn = new Button();
        Button WrongAddressBtn = new Button();
        Label StatusLabel = new Label();
        SigManager sigMgr;
        Log SigLog = new Log("ssl-sig-as");
        List<int> AddrList = new List<int>();
        public bool HasScanned;
        public int DecidedAddress = -1;

        private int scanStart = 0x00000000;
        private int scanEnd = 0x50000000;
        public SigControlsAS()
        {
            init();
        }
        private void init()
        {
            sigMgr = new SigManager(scanStart, scanEnd, "ShellShockLive", true);
            sigMgr.AddSignature("AngleStrength", "3B 01 00 00 64 00 00 00");
            AskToScanBtn.Text = "Scan";
            AskToScanBtn.Click += ScanBtnClick;
            AskToScanBtn.Location = new Point(10, 230);
            AskToScanBtn.Size = new Size(120, 20);
            AskToScanBtn.BackColor = Color.Transparent;
            WrongAddressBtn.Text = "Try Another Address";
            WrongAddressBtn.Click += WrongAddressBtnClick;
            WrongAddressBtn.Location = new Point(10, 250);
            WrongAddressBtn.Size = new Size(120, 20);
            WrongAddressBtn.BackColor = Color.Transparent;
            WrongAddressBtn.Enabled = false;
            StatusLabel.Text = "Ready To Scan for Tracer";
            StatusLabel.Size = new Size(120, 40);
            StatusLabel.TextAlign = ContentAlignment.MiddleCenter;
            StatusLabel.Location = new Point(10, 190);
            Green(StatusLabel);
        }
        public void Disable()
        {
            AskToScanBtn.Enabled = false;
            StatusText("Auto Positioning Disabled");
            Red(StatusLabel);
        }
        public void Enable()
        {
            AskToScanBtn.Enabled = true;
            StatusText("Ready To Scan");
            Green(StatusLabel);
        }
        public void StatusText(string t)
        {
            StatusLabel.Text = t;
        }
        public List<Control> Display()
        {
            List<Control> cc = new List<Control>();
            cc.Add(AskToScanBtn);
            cc.Add(WrongAddressBtn);
            cc.Add(StatusLabel);
            return cc;
        }
        private Label Red(Label l)
        {
            l.ForeColor = Color.Red;
            return l;
        }
        private Label Green(Label l)
        {
            l.ForeColor = Color.Green;
            return l;
        }
        private Label Black(Label l)
        {
            l.ForeColor = Color.Black;
            return l;
        }
        private void ScanBtnClick(object a, object b)
        {
            StatusText("Scan In Progress");
            AddrList = new List<int>();
            DecidedAddress = -1;
            AskToScanBtn.Enabled = false;
            if (sigMgr.ScanForSignatures("AngleStrength"))
            {
                HasScanned = true;
                DecideAddress();
                AskToScanBtn.Enabled = true;
                WrongAddressBtn.Enabled = true;
            }
            else
            {
                StatusText("SS1: Error occured during scan");
                SigLog.Add(sigMgr.LastError);
            }
        }
        private void DecideAddress(bool fromInvalid = false)
        {
            List<int> adrs = (fromInvalid) ? AddrList : sigMgr.GetAddressList();
            if (adrs.Count == 0)
            {
                StatusText("DA1: No compatible addresses found. \r\nTry Again");
                DecidedAddress = -1;
                Red(StatusLabel);
                return;
            }
            else
            {
                DecidedAddress = scanStart + adrs[0];
                string t = ((adrs[0].ToString("x8"))[7] == 'c') ? "a prefered" : "an";
                StatusText(string.Format("Found {0} address. Tracer enabled", t));
                Black(StatusLabel);
            }
            AddrList = adrs;
        }
        public void DumpLog()
        {
            SigLog.Dump();
        }

        private void WrongAddressBtnClick(object a, object b)
        {
            InvalidAddress(DecidedAddress);
        }
        public void InvalidAddress(int addr)
        {
            int index = AddrList.IndexOf(addr);
            if (index != -1)
            {
                AddrList.RemoveAt(index);
            }
            DecideAddress(true);
        }
    }
}
