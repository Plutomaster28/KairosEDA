using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace KairosEDA.Helpers
{
    /// <summary>
    /// Helper for loading embedded resources (works in single-file publish)
    /// </summary>
    public static class ResourceHelper
    {
        /// <summary>
        /// Gets the application icon, trying embedded resource first, then file system
        /// </summary>
        public static Icon? GetApplicationIcon()
        {
            try
            {
                // Try loading from embedded resource first (works in single-file publish)
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "seadrive_icon.ico";
                
                using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        return new Icon(stream);
                    }
                }
            }
            catch
            {
                // Fall through to file system attempt
            }

            try
            {
                // Fallback: try loading from file system (development builds)
                string iconPath = Path.Combine(Application.StartupPath, "app_resources", "icon", "seadrive_icon.ico");
                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }
            }
            catch
            {
                // Fall through to system icon
            }

            // Last resort: use system default
            return SystemIcons.Application;
        }

        /// <summary>
        /// Gets the application icon as a Bitmap (for PictureBox, etc.)
        /// </summary>
        public static Bitmap? GetApplicationIconAsBitmap(int size = 96)
        {
            try
            {
                // Try loading from embedded resource first
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "seadrive_icon.ico";
                
                using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (Icon ico = new Icon(stream, size, size))
                        {
                            return ico.ToBitmap();
                        }
                    }
                }
            }
            catch
            {
                // Fall through to file system attempt
            }

            try
            {
                // Fallback: try loading from file system
                string iconPath = Path.Combine(Application.StartupPath, "app_resources", "icon", "seadrive_icon.ico");
                if (File.Exists(iconPath))
                {
                    using (Icon ico = new Icon(iconPath, size, size))
                    {
                        return ico.ToBitmap();
                    }
                }
            }
            catch
            {
                // Return null if all attempts fail
            }

            return null;
        }
    }
}
