using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
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
        static extern bool GetWindowRect(IntPtr hWnd, ref RECT Rect);
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int Width, int Height, bool Repaint);
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
            RECT Rect = new RECT();
            if (processes.Length == 0)
            {
                MessageBox.Show("Please launch ShellShock Live before trying to use aimbot", "Game not running");
                Application.Exit();
                return;
            }
            foreach (Process p in processes)
            {
                IntPtr handle = p.MainWindowHandle;
                GetWindowRect(handle, ref Rect);
            }
            this.Top = Rect.Top;
            this.Left = Rect.Left - 150;
            this.Width = (Rect.Right - Rect.Left) + 150;
            this.Height = (Rect.Bottom - Rect.Top);
        }
        PictureBox AimingContainer = new PictureBox();
        TrackBar AngleTrack = new TrackBar();
        TrackBar StrengthTrack = new TrackBar();
        Label StrengthLbl = new Label();
        Label AngleLbl = new Label();
        private void CreateControls()
        {
            PictureBox LeftContainer = new PictureBox();
            LeftContainer.Location = new Point(0, 0);
            LeftContainer.Height = this.Height;
            LeftContainer.Width = 150;
            LeftContainer.BackColor = SystemColors.Control;
            //
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
            CheckBox TMCB = new CheckBox();
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
            this.Text = "SSL-Steam";
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
            DisplayAngle(GetAngle(strength, angle, dir));
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
            if (variation == null)
            {
                double _grav = 9.80665;
                double theta = angle * (Math.PI / 180);
                power = power * 1.46;
                for (var i = 1; i < AimingContainer.Width;i++)
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
            return XYDict;
        }
        private void DisplayAngle(List<KeyValuePair<int,int>> d)
        {
            Size imgsize = AimingContainer.Image.Size;
            AimingContainer.Image = new Bitmap(imgsize.Width, imgsize.Height);
            for(var i = 0; i < d.Count; i++)
            {
                KeyValuePair<int, int> kp = d[i];
                int x = TankLocation.Location.X + kp.Key - 150 + 4;
                int y = TankLocation.Location.Y - kp.Value;
                Graphics.FromImage(AimingContainer.Image).FillRectangle(Brushes.White, x, y, 1, 1);
            }
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
                DrawTracer();
            }
        }
    }
}
