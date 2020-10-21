using Squirrel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ReleaseTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static bool ShowTheWelcomeWizard;

        public MainWindow()
        {
            InitializeComponent();
        }

        public async Task<bool> UpdateApp()
        {
            /*
             
            Steps
                Build Solution
                Open last nuget package
                    Increment Version
                    Remove all files (delete .net folder)
                    Add in newly built files
                Save Package
                Release Package 
                    Squirrel --releasify .\ReleaseTest\Nuget\ReleaseTest.1.0.11.nupkg
             
             
             */

            this.Dispatcher.Invoke(() => { lblContent.Content = "Checking For Updates"; });
            var updatePath = ConfigurationManager.AppSettings["UpdatePathFolder"];
            var packageId = ConfigurationManager.AppSettings["PackageID"];

            using (var mgr = await UpdateManager.GitHubUpdateManager(updatePath, packageId))
            {
                SquirrelAwareApp.HandleEvents(
                  onInitialInstall: v =>
                  {
                      mgr.CreateShortcutForThisExe();
                  },
                  onAppUpdate: v =>
                  {
                      mgr.CreateShortcutForThisExe();
                  },
                  onAppUninstall: v =>
                  {
                      mgr.RemoveShortcutForThisExe();
                  },
                  onFirstRun: () => ShowTheWelcomeWizard = true);

                var updates = await mgr.CheckForUpdate();

                if (updates.ReleasesToApply.Any())
                {
                    this.Dispatcher.Invoke(() => { lblContent.Content = "Updates Found, Downloading..."; });
                    var lastVersion = updates.ReleasesToApply.OrderBy(x => x.Version).Last();

                    await mgr.DownloadReleases(new[] { lastVersion });

                    this.Dispatcher.Invoke(() => { lblContent.Content = "Applying Releases..."; });
                    var releasesApplied = await mgr.ApplyReleases(updates);

                    this.Dispatcher.Invoke(() => { lblContent.Content = "Updating, the app will automatically restart when finished..."; });
                    var releaseEntries = await mgr.UpdateApp();

                    Thread.Sleep(5000);


                    UpdateManager.RestartApp("C:\\Users\\JamesDeMuse\\source\\repos\\ReleaseTest\\ReleaseTest\\ReleaseTest\\ReleaseTest.exe");
                    return false;
                }
                else
                {
                    this.Dispatcher.Invoke(() => { lblContent.Content = "No Updates Found... App Starting"; });
                    this.Dispatcher.Invoke(() => { this.Visibility = Visibility.Hidden; });

                    Thread.Sleep(3000);
                    return true;
                }
            }
        }

        private void RestartApp()
        {
            using (var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"C:\Users\JamesDeMuse\source\repos\ReleaseTest\ReleaseTest\app-restart.bat",
                    Arguments = $"\"C:\\Users\\JamesDeMuse\\source\\repos\\ReleaseTest\\ReleaseTest\\ReleaseTest\\\" \"ReleaseTest.exe\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            })
            {
                process.Start();
                process.WaitForExit();
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var result = await Task.Run(UpdateApp);

            if (result)
            {
                var home = new Home();
                home.Show();
                home.Closing += Home_Closing;
            }
        }

        private void Home_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.Current.Shutdown();
        }
    }
}
