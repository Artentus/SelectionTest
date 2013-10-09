using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace SelectionTest
{
    public partial class Form1 : Form
    {
        Bitmap bmp;
        Point startPoint;
        Point currentPoint;
        bool dragging;
        SolidBrush brush;
        IntPtr hBmp;

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hObjSource, int nXSrc, int nYSrc, TernaryRasterOperations dwRop);

        [DllImport("gdi32.dll")]
        private static extern bool StretchBlt(IntPtr hdcDest, int nXOriginDest, int nYOriginDest, int nWidthDest, int nHeightDest, IntPtr hdcSrc, int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc, TernaryRasterOperations dwRop);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        private enum TernaryRasterOperations : uint
        {
            Srccopy = 0x00CC0020,
        }

        public Form1()
        {
            InitializeComponent();

            brush = new SolidBrush(Color.FromArgb(150, Color.Black));

            Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
            Location = screenBounds.Location;
            Size = screenBounds.Size;
            WindowState = FormWindowState.Maximized;

            bmp = new Bitmap(screenBounds.Width, screenBounds.Height);
            Graphics g = Graphics.FromImage(bmp);
            g.CopyFromScreen(screenBounds.Location, new Point(0, 0), bmp.Size, CopyPixelOperation.SourceCopy);
            hBmp = bmp.GetHbitmap();

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            UpdateStyles();
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            startPoint = e.Location;
            currentPoint = e.Location;
            dragging = true;
            Cursor = Cursors.SizeAll;
            Invalidate();
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                currentPoint = e.Location;
                Invalidate();
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;
            Cursor = Cursors.Arrow;
            Invalidate();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighSpeed;
            g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            IntPtr pTarget = g.GetHdc();
            IntPtr pSource = CreateCompatibleDC(pTarget);
            IntPtr pOrig = SelectObject(pSource, hBmp);
            BitBlt(pTarget, 0, 0, bmp.Width, bmp.Height, pSource, 0, 0, TernaryRasterOperations.Srccopy);
            Rectangle loupeDest = new Rectangle();
            if (dragging)
            {
                Rectangle loupeSource = new Rectangle(Cursor.Position.X - 20, Cursor.Position.Y - 20, 40, 40);
                loupeDest = new Rectangle(Cursor.Position.X + 15, Cursor.Position.Y + 15, 120, 120);
                StretchBlt(pTarget, loupeDest.X, loupeDest.Y, loupeDest.Width, loupeDest.Height, pSource, loupeSource.X, loupeSource.Y, loupeSource.Width, loupeSource.Height, TernaryRasterOperations.Srccopy);
            }
            DeleteDC(pSource);
            g.ReleaseHdc(pTarget);

            int rectX = Math.Min(startPoint.X, currentPoint.X);
            int rectY = Math.Min(startPoint.Y, currentPoint.Y);
            int rectWidth = Math.Abs(startPoint.X - currentPoint.X);
            int rectHeight = Math.Abs(startPoint.Y - currentPoint.Y);
            Rectangle rect = new Rectangle(rectX, rectY, rectWidth, rectHeight);

            using (GraphicsPath path = new GraphicsPath(FillMode.Alternate))
            {
                path.AddRectangle(new Rectangle(0, 0, bmp.Width, bmp.Height));
                path.AddRectangle(rect);
                path.AddRectangle(loupeDest);
                g.FillPath(brush, path);
            }
            g.DrawRectangle(Pens.Gray, rect);

            string widthText = rectWidth.ToString();
            string heightText = rectHeight.ToString();
            SizeF widthSizeF = g.MeasureString(widthText, Font);
            SizeF heightSizeF = g.MeasureString(heightText, Font);
            Size widthSize = new Size((int)Math.Ceiling(widthSizeF.Width), (int)Math.Ceiling(widthSizeF.Height));
            Size heightSize = new Size((int)Math.Ceiling(heightSizeF.Width), (int)Math.Ceiling(heightSizeF.Height));
            Rectangle widthRect = new Rectangle((rectWidth - widthSize.Width) / 2 + rectX, rectY - widthSize.Height, widthSize.Width, widthSize.Height);
            Rectangle heightRect = new Rectangle(rectX - heightSize.Width, (rectHeight - heightSize.Height) / 2 + rectY, heightSize.Width, heightSize.Height);
            g.FillRectangle(Brushes.Gray, widthRect);
            g.FillRectangle(Brushes.Gray, heightRect);
            g.DrawString(widthText, Font, Brushes.White, widthRect.Location);
            g.DrawString(heightText, Font, Brushes.White, heightRect.Location);

            if (dragging) g.DrawRectangle(Pens.Gray, loupeDest);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            brush.Dispose();
            DeleteObject(hBmp);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Application.Exit();
        }
    }
}
