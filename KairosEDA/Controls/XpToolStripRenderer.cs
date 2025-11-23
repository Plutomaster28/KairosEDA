using System;
using System.Drawing;
using System.Windows.Forms;

namespace KairosEDA.Controls
{
    /// <summary>
    /// Custom ToolStrip renderer for Windows XP / VS.NET 2003 style
    /// </summary>
    public class XpToolStripRenderer : ToolStripProfessionalRenderer
    {
        public XpToolStripRenderer() : base(new XpColorTable())
        {
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            // Draw XP gradient for the entire toolbar/menubar
            var bounds = new Rectangle(Point.Empty, e.ToolStrip.Size);
            
            if (e.ToolStrip is MenuStrip)
            {
                // Menu bar gradient
                Win32Native.DrawXpGradient(e.Graphics, bounds, 
                    Win32Native.XpColors.MenuBarStart, 
                    Win32Native.XpColors.MenuBarEnd);
            }
            else if (e.ToolStrip is ToolStrip)
            {
                // Toolbar gradient
                Win32Native.DrawXpGradient(e.Graphics, bounds,
                    Win32Native.XpColors.ToolbarGradientStart,
                    Win32Native.XpColors.ToolbarGradientEnd);
            }
            else if (e.ToolStrip is StatusStrip)
            {
                // Status bar gradient
                Win32Native.DrawXpGradient(e.Graphics, bounds,
                    Win32Native.XpColors.StatusBarStart,
                    Win32Native.XpColors.StatusBarEnd);
            }
            else
            {
                base.OnRenderToolStripBackground(e);
            }
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var button = e.Item as ToolStripButton;
            if (button == null)
            {
                base.OnRenderButtonBackground(e);
                return;
            }

            var bounds = new Rectangle(Point.Empty, e.Item.Size);

            if (button.Pressed || button.Checked)
            {
                // Pressed/Checked state - darker border and selection gradient
                using (var brush = new SolidBrush(Win32Native.XpColors.SelectionEnd))
                {
                    e.Graphics.FillRectangle(brush, bounds);
                }
                using (var pen = new Pen(Win32Native.XpColors.Border))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, bounds.Width - 1, bounds.Height - 1);
                }
            }
            else if (button.Selected)
            {
                // Hover state - XP button hover gradient
                Win32Native.DrawXpGradient(e.Graphics, bounds,
                    Win32Native.XpColors.ButtonHoverStart,
                    Win32Native.XpColors.ButtonHoverEnd);
                using (var pen = new Pen(Win32Native.XpColors.Border))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, bounds.Width - 1, bounds.Height - 1);
                }
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected)
            {
                base.OnRenderMenuItemBackground(e);
                return;
            }

            var bounds = new Rectangle(Point.Empty, e.Item.Size);

            // XP menu item selection gradient
            Win32Native.DrawXpGradient(e.Graphics, bounds,
                Win32Native.XpColors.SelectionStart,
                Win32Native.XpColors.SelectionEnd);
            using (var pen = new Pen(Win32Native.XpColors.Border))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, bounds.Width - 1, bounds.Height - 1);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            // Draw XP-style separator (raised 3D look)
            var bounds = new Rectangle(Point.Empty, e.Item.Size);
            var isVertical = e.Item.Width < e.Item.Height;

            if (isVertical)
            {
                int x = bounds.Width / 2;
                using (var penLight = new Pen(Color.FromArgb(180, 195, 210)))
                using (var penDark = new Pen(Color.FromArgb(120, 140, 160)))
                {
                    e.Graphics.DrawLine(penDark, x, 3, x, bounds.Height - 3);
                    e.Graphics.DrawLine(penLight, x + 1, 3, x + 1, bounds.Height - 3);
                }
            }
            else
            {
                int y = bounds.Height / 2;
                using (var penLight = new Pen(Color.FromArgb(180, 195, 210)))
                using (var penDark = new Pen(Color.FromArgb(120, 140, 160)))
                {
                    e.Graphics.DrawLine(penDark, 3, y, bounds.Width - 3, y);
                    e.Graphics.DrawLine(penLight, 3, y + 1, bounds.Width - 3, y + 1);
                }
            }
        }
    }

    /// <summary>
    /// Color table for XP-style toolstrips
    /// </summary>
    public class XpColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Win32Native.XpColors.SelectionStart;
        public override Color MenuItemSelectedGradientBegin => Win32Native.XpColors.SelectionStart;
        public override Color MenuItemSelectedGradientEnd => Win32Native.XpColors.SelectionEnd;
        public override Color MenuItemBorder => Win32Native.XpColors.Border;
        
        public override Color ButtonSelectedBorder => Win32Native.XpColors.Border;
        public override Color ButtonSelectedGradientBegin => Win32Native.XpColors.ButtonHoverStart;
        public override Color ButtonSelectedGradientEnd => Win32Native.XpColors.ButtonHoverEnd;
        
        public override Color ButtonPressedBorder => Win32Native.XpColors.Border;
        public override Color ButtonPressedGradientBegin => Win32Native.XpColors.SelectionEnd;
        public override Color ButtonPressedGradientEnd => Win32Native.XpColors.SelectionStart;
        
        public override Color ToolStripGradientBegin => Win32Native.XpColors.ToolbarGradientStart;
        public override Color ToolStripGradientMiddle => Win32Native.XpColors.ToolbarGradientStart;
        public override Color ToolStripGradientEnd => Win32Native.XpColors.ToolbarGradientEnd;
        
        public override Color MenuStripGradientBegin => Win32Native.XpColors.MenuBarStart;
        public override Color MenuStripGradientEnd => Win32Native.XpColors.MenuBarEnd;
        
        public override Color StatusStripGradientBegin => Win32Native.XpColors.StatusBarStart;
        public override Color StatusStripGradientEnd => Win32Native.XpColors.StatusBarEnd;
        
        public override Color SeparatorDark => Color.FromArgb(120, 140, 160);
        public override Color SeparatorLight => Color.FromArgb(180, 195, 210);
    }
}
