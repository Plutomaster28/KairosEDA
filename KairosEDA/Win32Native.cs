using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KairosEDA
{
    /// <summary>
    /// Win32 API calls for Windows 7 styling and DWM effects
    /// </summary>
        public static class Win32Native
        {
            /// <summary>
            /// Recursively apply Classic Windows theme to all controls in a container
            /// </summary>
            public static void ApplyClassicThemeRecursive(Control? control)
            {
                if (control == null) return;
                
                // Apply classic theme (empty string disables visual styles for control)
                try
                {
                    if (control.IsHandleCreated)
                    {
                        SetWindowTheme(control.Handle, " ", " ");
                    }
                }
                catch { }

                // Set all buttons to System style for classic Windows look
                if (control is Button btn)
                {
                    btn.FlatStyle = FlatStyle.System;
                    btn.BackColor = SystemColors.Control;
                }

                // Set text controls to system colors
                if (control is TextBox || control is RichTextBox || control is ListBox)
                {
                    control.BackColor = SystemColors.Window;
                    control.ForeColor = SystemColors.WindowText;
                }

                // Recurse for children
                foreach (Control child in control.Controls)
                {
                    ApplyClassicThemeRecursive(child);
                }
            }
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        private static extern int DwmIsCompositionEnabled(ref int pfEnabled);

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int leftWidth;
            public int rightWidth;
            public int topHeight;
            public int bottomHeight;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        private enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_NCRENDERING_POLICY = 2;
        private const int DWMNCRP_ENABLED = 2;

        /// <summary>
        /// Apply Windows 7 Aero theme to the entire application
        /// </summary>
        public static void SetWindowTheme()
        {
            // This forces the classic/Aero visual style
        }

        /// <summary>
        /// Disable Aero glass effect for classic Windows look
        /// </summary>
        public static void DisableAeroGlass(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                return;

            try
            {
                // Disable DWM rendering for classic look
                int policy = 1; // DWMNCRP_DISABLED
                DwmSetWindowAttribute(handle, DWMWA_NCRENDERING_POLICY, ref policy, sizeof(int));
            }
            catch { }
        }

        /// <summary>
        /// Apply explorer theme to control (for proper Windows 7 styling)
        /// </summary>
        public static void SetExplorerTheme(Control? control)
        {
            if (control?.Handle != IntPtr.Zero)
            {
                SetWindowTheme(control.Handle, "explorer", null!);
            }
        }

        /// <summary>
        /// Apply Windows 7 theme to ListView
        /// </summary>
        public static void ApplyListViewTheme(ListView? listView)
        {
            if (listView?.Handle != IntPtr.Zero)
            {
                SetWindowTheme(listView.Handle, "explorer", null!);
                
                // Enable extended styles for better appearance
                const int LVM_SETEXTENDEDLISTVIEWSTYLE = 0x1000 + 54;
                const int LVS_EX_DOUBLEBUFFER = 0x00010000;
                const int LVS_EX_BORDERSELECT = 0x00008000;
                
                SendMessage(listView.Handle, LVM_SETEXTENDEDLISTVIEWSTYLE, 0, 
                    LVS_EX_DOUBLEBUFFER | LVS_EX_BORDERSELECT);
            }
        }

        /// <summary>
        /// Apply Windows 7 theme to TreeView
        /// </summary>
        public static void ApplyTreeViewTheme(TreeView? treeView)
        {
            if (treeView?.Handle != IntPtr.Zero)
            {
                SetWindowTheme(treeView.Handle, "explorer", null!);
                
                // Enable double buffering
                const int TVS_EX_DOUBLEBUFFER = 0x0004;
                const int TVM_SETEXTENDEDSTYLE = 0x1100 + 44;
                
                SendMessage(treeView.Handle, TVM_SETEXTENDEDSTYLE, TVS_EX_DOUBLEBUFFER, TVS_EX_DOUBLEBUFFER);
            }
        }

        /// <summary>
        /// Scroll a TreeView control by the specified number of steps, emulating line-based panning.
        /// </summary>
        public static void ScrollTreeView(TreeView? treeView, int horizontalSteps, int verticalSteps)
        {
            if (treeView == null)
            {
                return;
            }

            var handle = treeView.Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            const int WM_HSCROLL = 0x114;
            const int WM_VSCROLL = 0x115;
            const int SB_LINELEFT = 0;
            const int SB_LINERIGHT = 1;
            const int SB_LINEUP = 0;
            const int SB_LINEDOWN = 1;

            void ScrollMessage(int message, int command, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    SendMessage(handle, message, command, 0);
                }
            }

            if (horizontalSteps > 0)
            {
                ScrollMessage(WM_HSCROLL, SB_LINELEFT, horizontalSteps);
            }
            else if (horizontalSteps < 0)
            {
                ScrollMessage(WM_HSCROLL, SB_LINERIGHT, -horizontalSteps);
            }

            if (verticalSteps > 0)
            {
                ScrollMessage(WM_VSCROLL, SB_LINEUP, verticalSteps);
            }
            else if (verticalSteps < 0)
            {
                ScrollMessage(WM_VSCROLL, SB_LINEDOWN, -verticalSteps);
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
    }
}
