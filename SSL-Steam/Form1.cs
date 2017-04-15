using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT Rect);
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [Flags]
        public enum KeyModifier
        {
            None = 0x0000,
            Alt = 0x0001,
            Ctrl = 0x0002,
            NoRepeat = 0x4000,
            Shift = 0x0004,
            Win = 0x0008
        }
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            GetShellshockSize();
            CreateControls();
            //2 = Ctrl
            //4 = Shift
            RegisterHotKey(this.Handle, 1230, 2, (int)Keys.Left);
            RegisterHotKey(this.Handle, 1231, 2, (int)Keys.Right);
            RegisterHotKey(this.Handle, 1232, 2, (int)Keys.Up);
            RegisterHotKey(this.Handle, 1233, 2, (int)Keys.Down);

            RegisterHotKey(this.Handle, 1234, 2|4, (int)Keys.Left);
            RegisterHotKey(this.Handle, 1235, 2|4, (int)Keys.Right);
            RegisterHotKey(this.Handle, 1236, 2|4, (int)Keys.Up);
            RegisterHotKey(this.Handle, 1237, 2|4, (int)Keys.Down);
        }
        protected override void WndProc(ref Message m)
        {

            if (m.Msg == 0x312)
            {
                Keys vk = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
                int fsModifiers = ((int)m.LParam & 0xFFFF);
                if (vk == Keys.Left && fsModifiers == (2 | 4))
                {
                    try
                    {
                        Point curtank = TankLocation.Location;
                        curtank.X -= 1;
                        TankLocation.Location = curtank;
                        DrawTracer();
                    }
                    catch (Exception ex)
                    {

                    }
                }
                if (vk == Keys.Right && fsModifiers == (2 | 4))
                {
                    try
                    {
                        Point curtank = TankLocation.Location;
                        curtank.X += 1;
                        TankLocation.Location = curtank;
                        DrawTracer();
                    }
                    catch (Exception ex)
                    {

                    }
                }
                if (vk == Keys.Up && fsModifiers == (2 | 4))
                {
                    try
                    {
                        Point curtank = TankLocation.Location;
                        curtank.Y -= 1;
                        TankLocation.Location = curtank;
                        DrawTracer();
                    }
                    catch (Exception ex)
                    {

                    }
                }
                if (vk == Keys.Down && fsModifiers == (2 | 4))
                {
                    try
                    {
                        Point curtank = TankLocation.Location;
                        curtank.Y += 1;
                        TankLocation.Location = curtank;
                        DrawTracer();
                    }
                    catch (Exception ex)
                    {

                    }
                }
                if (vk == Keys.Left && fsModifiers == (2))
                {
                    try
                    {
                        AngleTrack.Value += 1;
                    } catch (Exception ex)
                    {

                    }
                }
                if (vk == Keys.Right && fsModifiers == (2))
                {
                    try
                    {
                        AngleTrack.Value -= 1;
                    }
                    catch (Exception ex)
                    {

                    }
                }
                if (vk == Keys.Up && fsModifiers == (2))
                {
                    try
                    {
                        StrengthTrack.Value -= 1;
                    }
                    catch (Exception ex)
                    {

                    }
                }
                if (vk == Keys.Down && fsModifiers == (2))
                {
                    try
                    {
                        StrengthTrack.Value += 1;
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            base.WndProc(ref m);
        }
        private void GetShellshockSize()
        {
            Process[] processes = Process.GetProcessesByName("ShellShockLive");
            if (processes.Length == 0)
            {
                MessageBox.Show("Please launch ShellShock Live before trying to use aimbot", "Game not running");
                Application.Exit();
                return;
            }
            else
            {
                RECT Rect = new RECT();
                foreach (Process p in processes)
                {
                    IntPtr handle = p.MainWindowHandle;
                    if (!GetWindowRect(handle, out Rect)) throw new Win32Exception();
                }
                this.Top = Rect.Top;
                this.Left = Rect.Left - 150;
                this.Width = (Rect.Right - Rect.Left) + 150;
                this.Height = (Rect.Bottom - Rect.Top);
            }
        }
        PictureBox AimingContainer = new PictureBox();
        TrackBar AngleTrack = new TrackBar();
        TrackBar StrengthTrack = new TrackBar();
        Label StrengthLbl = new Label();
        Label AngleLbl = new Label();
        CheckBox TMCB = new CheckBox();
        ComboBox VariationCB = new ComboBox();
        private void CreateControls()
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            PictureBox LeftContainer = new PictureBox();
            LeftContainer.Location = new Point(0, 0);
            LeftContainer.Height = this.Height;
            LeftContainer.Width = 150;
            LeftContainer.BackColor = SystemColors.Control;
            AimingContainer.Location = new Point(150,0);
            AimingContainer.Height = this.Height;
            AimingContainer.Width = this.Width - 150;
            AimingContainer.BackColor = Color.Lime;
            AimingContainer.Image = new Bitmap(AimingContainer.Width, AimingContainer.Height);
            TankLocation.Image = new Bitmap(7, 7);
            Graphics.FromImage(TankLocation.Image).FillEllipse(Brushes.DeepSkyBlue, 0, 0, 7, 7);
            TankLocation.Refresh();
            TankLocation.MouseDown += Tank_MouseDown;
            TankLocation.MouseUp += Tank_MouseUp;
            TankLocation.MouseMove += Tank_MouseMove;
            TMCB.Location = new Point(15, 10);
            TMCB.Size = new Size(104, 17);
            TMCB.Checked = true;
            TMCB.Text = "Top Most Window";
            TMCB.Click += TopMostClick;
            AngleTrack.SetRange(-180, 0);
            AngleTrack.Location = new Point(95, 40);
            AngleTrack.Orientation = Orientation.Vertical;
            AngleTrack.Height = 600;
            AngleTrack.ValueChanged += DrawTracer;
            AngleLbl.Location = new Point(95, 635);
            AngleLbl.Size = new Size(30, 30);
            AngleLbl.Text = "0";
            StrengthTrack.SetRange(-100, -1);
            StrengthTrack.Location = new Point(25, 40);
            StrengthTrack.Orientation = Orientation.Vertical;
            StrengthTrack.Height = 600;
            StrengthTrack.ValueChanged += DrawTracer;
            StrengthLbl.Location = new Point(25, 635);
            StrengthLbl.Text = "0";
            StrengthLbl.Size = new Size(30, 30);
            VariationCB.Location = new Point(15, 660);
            VariationCB.Items.Add("Default/Single Shot");
            VariationCB.Items.Add("Three-Ball");
            VariationCB.Items.Add("Five-Ball");
            VariationCB.SelectedIndex = 0;
            VariationCB.DropDownStyle = ComboBoxStyle.DropDownList;
            VariationCB.SelectedIndexChanged += DrawTracer;
            this.Text = "SSL-Steam";
            this.Controls.Add(VariationCB);
            this.Controls.Add(TMCB);
            this.Controls.Add(StrengthLbl);
            this.Controls.Add(AngleLbl);
            this.Controls.Add(StrengthTrack);
            this.Controls.Add(AngleTrack);
            this.Controls.Add(LeftContainer);
            this.Controls.Add(AimingContainer);
        }
        void DrawTracer()
        {
            DrawTracer(null, null);
        }
        private void DrawTracer(object a, object b)
        {
            double strength = StrengthTrack.Value * -1;
            int angle = AngleTrack.Value * -1;
            string dir;
            if (angle > 90)
            {
                angle = (angle - 180) * -1;
                dir = "right";
            } else
            {
                dir = "left";
            }
            StrengthLbl.Text = strength.ToString();
            AngleLbl.Text = angle.ToString();
            DisplayAngle(GetAngle(strength, angle, dir, VariationCB.SelectedIndex.ToString()));
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
        private List<KeyValuePair<int,int>> GetAngle(double power, int angle, string direction, string variation = null)
        {
            int Lastx = -1;
            int Lasty = -1;
            List<KeyValuePair<int, int>> XYDict = new List<KeyValuePair<int, int>>();
            power = power * 1.46;
            if (variation == null || variation == "0")
            {
                double _grav = 9.80665;
                double theta = angle * (Math.PI / 180);
                for (var i = 1; i < AimingContainer.Width; i++)
                {
                    double dist = i;
                    double y = 0 + (dist * Math.Tan(theta)) - ((_grav * Math.Pow(dist, 2)) / (2 * (Math.Pow(power * Math.Cos(theta), 2))));
                    if (y > -1600)
                    {
                        if (direction == "left")
                        {
                            XYDict.Add(new KeyValuePair<int, int>(i * -1, Convert.ToInt32(y)));
                        }
                        else
                        {
                            XYDict.Add(new KeyValuePair<int, int>(i, Convert.ToInt32(y)));
                        }
                    }
                }
            }
            else if (variation == "1") //3ball
            {
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
                    double theta = tangle * (Math.PI / 180);

                    for (var i = 1; i < AimingContainer.Width; i++)
                    {
                        double dist = i;
                        double y = 0 + (dist * Math.Tan(theta)) - ((_grav * Math.Pow(dist, 2)) / (2 * (Math.Pow(power * Math.Cos(theta), 2))));
                        if (y > -1600)
                        {
                            if (direction == "left")
                            {
                                XYDict.Add(new KeyValuePair<int, int>(i * -1, Convert.ToInt32(y)));
                            }
                            else
                            {
                                XYDict.Add(new KeyValuePair<int, int>(i, Convert.ToInt32(y)));
                            }
                        }
                    }
                }
            }
            else if (variation == "2") //5ball
            {
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
                    double theta = tangle * (Math.PI / 180);

                    for (var i = 1; i < AimingContainer.Width; i++)
                    {
                        double dist = i;
                        double y = 0 + (dist * Math.Tan(theta)) - ((_grav * Math.Pow(dist, 2)) / (2 * (Math.Pow(power * Math.Cos(theta), 2))));
                        if (y > -1600)
                        {
                            if (direction == "left")
                            {
                                XYDict.Add(new KeyValuePair<int, int>(i * -1, Convert.ToInt32(y)));
                            }
                            else
                            {
                                XYDict.Add(new KeyValuePair<int, int>(i, Convert.ToInt32(y)));
                            }
                        }
                    }
                }
            }
                return XYDict;
        }
        private void DisplayAngle(List<KeyValuePair<int,int>> d)
        {
            Size imgsize = AimingContainer.Image.Size;
            AimingContainer.Image.Dispose();
            AimingContainer.Image = new Bitmap(imgsize.Width, imgsize.Height);
            Graphics g = Graphics.FromImage(AimingContainer.Image);
            for (var i = 0; i < d.Count; i++)
            {
                KeyValuePair<int, int> kp = d[i];
                int x = TankLocation.Location.X + kp.Key - 150 + 4;
                int y = TankLocation.Location.Y - kp.Value;
                g.FillRectangle(Brushes.White, x, y, 1, 1);
            }
            string DisplayText = string.Format("{0},{1}", StrengthTrack.Value * -1, AngleTrack.Value*-1 > 90 ? (AngleTrack.Value+180) : AngleTrack.Value * -1);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawString(DisplayText, new Font("Lucida Console", 14), Brushes.LightGray, new PointF(TankLocation.Location.X - 187, TankLocation.Location.Y + 65));
            AimingContainer.Refresh();
        }

        int cursorX, CursorY;
        bool Dragging;
        private void Tank_MouseDown(System.Object sender, System.Windows.Forms.MouseEventArgs e)
        {
            // Set the flag
            Dragging = true;
            // Note positions of cursor when pressed
            cursorX = e.X;
            CursorY = e.Y;
        }

        private void Tank_MouseUp(System.Object sender, System.Windows.Forms.MouseEventArgs e)
        {
            // Reset the flag
            Dragging = false;
        }

        private void Tank_MouseMove(System.Object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (Dragging)
            {
                Control ctrl = (Control)sender;
                // Move the control according to mouse movement
                ctrl.Left = (ctrl.Left + e.X) - cursorX;
                ctrl.Top = (ctrl.Top + e.Y) - CursorY;
                // Ensure moved control stays on top of anything it is dragged on to
                ctrl.BringToFront();
            }
        }
    }
}
