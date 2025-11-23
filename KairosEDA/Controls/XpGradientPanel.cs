using System;
using System.Drawing;
using System.Windows.Forms;

namespace KairosEDA.Controls
{
    /// <summary>
    /// A panel that draws a Windows XP / VS.NET 2003 style gradient background
    /// </summary>
    public class XpGradientPanel : Panel
    {
        private Color _gradientStart = Win32Native.XpColors.ToolbarGradientStart;
        private Color _gradientEnd = Win32Native.XpColors.ToolbarGradientEnd;
        private bool _vertical = false;
        private bool _drawBorder = true;
        private Color _borderColor = Win32Native.XpColors.BorderLight;

        public XpGradientPanel()
        {
            // Enable double buffering for smooth painting
            SetStyle(ControlStyles.UserPaint | 
                     ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();
        }

        public Color GradientStart
        {
            get => _gradientStart;
            set { _gradientStart = value; Invalidate(); }
        }

        public Color GradientEnd
        {
            get => _gradientEnd;
            set { _gradientEnd = value; Invalidate(); }
        }

        public bool Vertical
        {
            get => _vertical;
            set { _vertical = value; Invalidate(); }
        }

        public bool DrawBorder
        {
            get => _drawBorder;
            set { _drawBorder = value; Invalidate(); }
        }

        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Draw the XP gradient
            var bounds = new Rectangle(0, 0, Width, Height);
            Win32Native.DrawXpGradient(e.Graphics, bounds, _gradientStart, _gradientEnd, _vertical);

            // Draw border if enabled
            if (_drawBorder)
            {
                using (var pen = new Pen(_borderColor))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            }

            // Let child controls paint
            base.OnPaint(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Don't paint background - we'll handle it in OnPaint
        }
    }
}
