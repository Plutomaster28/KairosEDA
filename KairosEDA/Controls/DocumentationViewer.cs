using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace KairosEDA.Controls
{
    public class DocumentationViewer : Form
    {
        private ListBox docList = null!;
        private Button btnOpen = null!;
        private Label lblInfo = null!;

        private List<string> documentPaths = new();

        public DocumentationViewer()
        {
            InitializeUI();
            DiscoverDocuments();
            LoadDocumentList();
        }

        private void InitializeUI()
        {
            Text = "KairosEDA Documentation";
            Size = new Size(600, 400);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = SystemColors.Control; // Classic Windows gray
            
            try
            {
                Icon = Helpers.ResourceHelper.GetApplicationIcon();
            }
            catch
            {
                // Ignore icon errors
            }

            // Info label
            lblInfo = new Label
            {
                Text = "Select a document to open it in your default editor:",
                Location = new Point(10, 10),
                Size = new Size(560, 30),
                Font = new Font("Segoe UI", 9F),
                ForeColor = SystemColors.ControlText,
                BackColor = SystemColors.Control,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Document list
            docList = new ListBox
            {
                Location = new Point(10, 50),
                Size = new Size(560, 260),
                Font = new Font("Segoe UI", 10F),
                BackColor = SystemColors.Window,
                ForeColor = SystemColors.WindowText,
                BorderStyle = BorderStyle.Fixed3D
            };
            docList.DoubleClick += (s, e) => OpenSelectedDocument();

            // Open button
            btnOpen = new Button
            {
                Text = "Open Document",
                Location = new Point(225, 320),
                Size = new Size(150, 35),
                Font = new Font("Segoe UI", 9F),
                BackColor = SystemColors.Control,
                ForeColor = SystemColors.ControlText,
                FlatStyle = FlatStyle.System,
                UseVisualStyleBackColor = true
            };
            btnOpen.Click += (s, e) => OpenSelectedDocument();

            Controls.Add(lblInfo);
            Controls.Add(docList);
            Controls.Add(btnOpen);

            // Apply classic Windows styling
            ApplyClassicStyle();
        }

        private void ApplyClassicStyle()
        {
            // Must be called after window handle is created
            if (!this.IsHandleCreated)
            {
                this.HandleCreated += (s, e) => ApplyClassicStyle();
                return;
            }

            // Apply classic Windows theme
            Win32Native.DisableAeroGlass(this.Handle);
            Win32Native.ApplyClassicThemeRecursive(this);
        }

        private void DiscoverDocuments()
        {
            // Find docs directory
            string exePath = Application.ExecutablePath;
            string? exeDir = Path.GetDirectoryName(exePath);
            
            if (string.IsNullOrEmpty(exeDir))
                return;

            // Try different locations
            string[] possiblePaths = new[]
            {
                Path.Combine(exeDir, "..", "..", "..", "..", "docs"),  // Debug mode
                Path.Combine(exeDir, "docs"),                           // Release mode (if we copy docs)
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "KairosEDA", "docs")
            };

            foreach (var path in possiblePaths)
            {
                string fullPath = Path.GetFullPath(path);
                if (Directory.Exists(fullPath))
                {
                    documentPaths = Directory.GetFiles(fullPath, "*.*")
                        .Where(f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                                   f.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(f => Path.GetFileName(f))
                        .ToList();
                    return;
                }
            }
        }

        private void LoadDocumentList()
        {
            docList.Items.Clear();

            if (!documentPaths.Any())
            {
                docList.Items.Add("❌ No documentation found");
                docList.Items.Add("   (Check the docs/ folder)");
                btnOpen.Enabled = false;
                return;
            }

            foreach (var docPath in documentPaths)
            {
                string fileName = Path.GetFileName(docPath);
                string displayName = fileName;

                // Add emoji based on filename
                if (fileName.Contains("SETUP", StringComparison.OrdinalIgnoreCase) ||
                    fileName.Contains("INSTALL", StringComparison.OrdinalIgnoreCase))
                    displayName = "🔧 " + fileName;
                else if (fileName.Contains("QUICK", StringComparison.OrdinalIgnoreCase) ||
                         fileName.Contains("START", StringComparison.OrdinalIgnoreCase))
                    displayName = "🚀 " + fileName;
                else if (fileName.Contains("FEATURE", StringComparison.OrdinalIgnoreCase))
                    displayName = "✨ " + fileName;
                else if (fileName.Contains("TROUBLE", StringComparison.OrdinalIgnoreCase))
                    displayName = "🔧 " + fileName;
                else if (fileName.Contains("GUIDE", StringComparison.OrdinalIgnoreCase))
                    displayName = "📖 " + fileName;
                else
                    displayName = "📄 " + fileName;

                docList.Items.Add(displayName);
            }

            // Select first item
            if (docList.Items.Count > 0)
            {
                docList.SelectedIndex = 0;
            }
        }

        private void OpenSelectedDocument()
        {
            if (docList.SelectedIndex < 0 || docList.SelectedIndex >= documentPaths.Count)
                return;

            string docPath = documentPaths[docList.SelectedIndex];

            try
            {
                // Open with default application (VS Code, Notepad, etc.)
                Process.Start(new ProcessStartInfo
                {
                    FileName = docPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open document:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
