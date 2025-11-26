using System;
using System.Windows;
using KairosEDA.Models;

namespace KairosEDA.Controls
{
    public partial class SetupWizard : Window
    {
        private readonly WSLManager wslManager;
        private readonly ToolchainInstaller installer;
        private int currentPage = 0;
        private bool installationInProgress = false;
        private bool setupSuccessful = false;

        public SetupWizard(WSLManager wslManager)
        {
            InitializeComponent();
            this.wslManager = wslManager;
            this.installer = new ToolchainInstaller(wslManager);

            // Hook up installer events
            installer.ProgressUpdate += Installer_ProgressUpdate;
            installer.ProgressPercentage += Installer_ProgressPercentage;
            installer.InstallationComplete += Installer_InstallationComplete;

            ShowPage(0);
        }

        private void ShowPage(int page)
        {
            // Hide all pages
            welcomePage.Visibility = Visibility.Collapsed;
            installPage.Visibility = Visibility.Collapsed;
            completePage.Visibility = Visibility.Collapsed;
            errorPage.Visibility = Visibility.Collapsed;

            currentPage = page;

            switch (page)
            {
                case 0: // Welcome
                    welcomePage.Visibility = Visibility.Visible;
                    nextButton.Content = "Begin Setup";
                    cancelButton.IsEnabled = true;
                    statusText.Text = "Ready to begin setup";
                    break;

                case 1: // Installation
                    installPage.Visibility = Visibility.Visible;
                    nextButton.IsEnabled = false;
                    nextButton.Content = "Installing...";
                    cancelButton.IsEnabled = false;
                    statusText.Text = "Installation in progress...";
                    break;

                case 2: // Complete
                    completePage.Visibility = Visibility.Visible;
                    nextButton.Content = "Finish";
                    nextButton.IsEnabled = true;
                    cancelButton.Content = "Close";
                    cancelButton.IsEnabled = true;
                    statusText.Text = "Setup completed successfully!";
                    break;

                case 3: // Error
                    errorPage.Visibility = Visibility.Visible;
                    nextButton.Content = "Retry";
                    nextButton.IsEnabled = true;
                    cancelButton.Content = "Close";
                    cancelButton.IsEnabled = true;
                    statusText.Text = "Setup encountered issues";
                    break;
            }
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage == 0)
            {
                // Start installation
                ShowPage(1);
                installationInProgress = true;

                try
                {
                    setupSuccessful = await installer.PerformCompleteSetupAsync();
                }
                catch (Exception ex)
                {
                    setupSuccessful = false;
                    LogMessage($"❌ Error: {ex.Message}");
                }

                installationInProgress = false;
            }
            else if (currentPage == 2)
            {
                // Finish - close dialog
                DialogResult = true;
                Close();
            }
            else if (currentPage == 3)
            {
                // Retry
                ShowPage(0);
                installLog.Clear();
                progressBar.Value = 0;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 0 && !installationInProgress)
            {
                ShowPage(currentPage - 1);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (installationInProgress)
            {
                var result = MessageBox.Show(
                    "Installation is in progress. Are you sure you want to cancel?",
                    "Cancel Setup",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            DialogResult = false;
            Close();
        }

        private void Installer_ProgressUpdate(object? sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogMessage(message);
                progressText.Text = message;
            });
        }

        private void Installer_ProgressPercentage(object? sender, int percent)
        {
            Dispatcher.Invoke(() =>
            {
                progressBar.Value = percent;
                statusText.Text = $"Installation progress: {percent}%";
            });
        }

        private void Installer_InstallationComplete(object? sender, bool success)
        {
            Dispatcher.Invoke(() =>
            {
                if (success)
                {
                    installedComponents.Text = "• WSL2\n• Docker\n• OpenLane (Docker image)\n• Yosys (if available)";
                    ShowPage(2);
                }
                else
                {
                    errorMessage.Text = "Some components could not be installed automatically. " +
                        "Please check the installation log for details and try manual installation if needed.";
                    ShowPage(3);
                }
            });
        }

        private void LogMessage(string message)
        {
            installLog.AppendText(message + "\n");
            logScrollViewer.ScrollToEnd();
        }
    }
}
