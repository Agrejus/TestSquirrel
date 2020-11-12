using Microsoft.Win32;
using Squirrel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
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
        public string[] Args { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void StartApp(string baseDirectory, params string[] args)
        {
            var resolvedArgs = args == null ? "" : $" {string.Join(" ", args)}";
            var fileName = System.IO.Path.Combine(baseDirectory, "task-start.bat");
            var baseAppExe = GetAppBaseExe();

            using (var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = $"\"{baseAppExe}\"{resolvedArgs}",
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

        private string GetAppBaseExe()
        {
            var currentLocation = Assembly.GetEntryAssembly().Location;
            var exeName = System.IO.Path.GetFileName(currentLocation);
            var directory = System.IO.Path.GetDirectoryName(currentLocation);
            var baseAppPath = System.IO.Path.Combine(directory, "..\\");
            return System.IO.Path.Combine(baseAppPath, exeName);
        }

        private void StartAppAsAdministrator(string baseDirectory, string[] args = null)
        {
            var resolvedArgs = args == null ? "" : $" {string.Join(" ", args)}";
            var baseAppExe = GetAppBaseExe();

            var proc = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = baseAppExe,
                Arguments = resolvedArgs,
                Verb = "runas"
            };

            Process.Start(proc);
        }

        private void ShutdownCurrent(string baseDirectory)
        {
            Shutdown(baseDirectory, Process.GetCurrentProcess().Id);
        }

        private void Shutdown(string baseDirectory, int processId)
        {
            var fileName = System.IO.Path.Combine(baseDirectory, "task-kill.bat");
            using (var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = $"{processId}",
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

        public async Task<bool> Install(string version, UpdateManager manager)
        {
            try
            {
                this.Dispatcher.Invoke(() => { lblContent.Content = $"Installing Release Test v{version}"; });

                ResolveRegistry();

                Thread.Sleep(3000);

                return true;
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                return false;
            }
        }

        public async Task<bool> Uninstall(string version, UpdateManager manager)
        {
            try
            {
                this.Dispatcher.Invoke(() => { lblContent.Content = $"Application is uninstalling"; });

                return false;
            }
            catch (Exception)
            {
                this.Dispatcher.Invoke(() => { lblContent.Content = $"An error occurred, please try again"; });

                return false;
            }
        }

        public async Task<bool> OnLaunch()
        {
            var baseDirectory = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var updatePath = ConfigurationManager.AppSettings["UpdatePathFolder"];
            var packageId = ConfigurationManager.AppSettings["PackageID"];

            using (var manager = await UpdateManager.GitHubUpdateManager(updatePath, packageId))
            {

                if (Args.Contains("--squirrel-install") && IsRunningAsAdministrator() == false)
                {
                    this.Dispatcher.Invoke(() => { lblContent.Content = "Installing - Restarting application as administrator..."; });

                    Thread.Sleep(1000);

                    // installing
                    var args = Args.ToList();
                    args.Add("--resolve-registry");

                    Thread.Sleep(1000);

                    StartAppAsAdministrator(baseDirectory, args.ToArray());

                    // Shut down the current (old) process
                    ShutdownCurrent(baseDirectory);
                    return false;
                }

                if (Args.Contains("--squirrel-install"))
                {
                    try
                    {
                        Log("Resolving Registry");
                        ResolveRegistry();
                        Log("Done Resolving Registry");
                    }
                    catch (Exception ex)
                    {
                        Log(ex.Message);
                    }
                    this.Dispatcher.Invoke(() => { lblContent.Content = "Installing..."; });

                    return false;
                }
                else if (Args.Contains("--squirrel-uninstall"))
                {
                    this.Dispatcher.Invoke(() => { lblContent.Content = $"Application is uninstalling"; });

                    return false;
                }
                else if (Args.Contains("--squirrel-firstrun"))
                {
                    var process = Process.GetProcessesByName(packageId);
                    var currentProcess = Process.GetCurrentProcess();

                    var installProcesses = process.Where(w => w.Id != currentProcess.Id);

                    foreach (var installProcess in installProcesses)
                    {
                        installProcess.Kill();
                    }
                }
                else if (Args.Contains("--after-update"))
                {
                    return true;
                }
                else if (Args.Contains("--squirrel-obsolete"))
                {
                    this.Dispatcher.Invoke(() => { this.Visibility = Visibility.Hidden; ; });
                    return false;
                }
                else if (Args.Contains("--squirrel-updated"))
                {
                    this.Dispatcher.Invoke(() => { lblContent.Content = "Updating to newest version"; });
                    this.Dispatcher.Invoke(() => { this.Visibility = Visibility.Hidden; });
                    return false;
                }
                else
                {
                    this.Dispatcher.Invoke(() => { lblContent.Content = "Checking for updates, please wait..."; });
                    var updates = await manager.CheckForUpdate();

                    if (updates.ReleasesToApply.Any())
                    {
                        this.Dispatcher.Invoke(() => { lblContent.Content = "Updates Found, Downloading..."; });
                        var lastVersion = updates.ReleasesToApply.OrderBy(x => x.Version).Last();

                        await manager.DownloadReleases(new[] { lastVersion });

                        this.Dispatcher.Invoke(() => { lblContent.Content = "Applying Releases..."; });
                        var releasesApplied = await manager.ApplyReleases(updates);

                        this.Dispatcher.Invoke(() => { lblContent.Content = "Updating, the app will automatically restart when finished..."; });
                        var releaseEntries = await manager.UpdateApp();

                        Thread.Sleep(2000);

                        StartApp(baseDirectory, "--after-update");

                        Thread.Sleep(500);

                        ShutdownCurrent(baseDirectory);

                        return false;
                    }

                    this.Dispatcher.Invoke(() => { lblContent.Content = "No updates found, app starting"; });
                }

                return true;
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Debugger.IsAttached == true)
            {
                var home = new Home();
                home.Show();
                home.Closing += Home_Closing;
                this.Close();
                return;
            }

            var result = await Task.Run(OnLaunch);

            if (result)
            {
                var home = new Home();
                home.Show();
                home.Closing += Home_Closing;
                this.Visibility = Visibility.Hidden;
            }
        }

        private void Home_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.Current.Shutdown();
        }

        private void ResolveRegistry()
        {
            Registry.SetValue($"{Registry.ClassesRoot}\\armorsecurity", "", "URL:armorsecurity");
            Registry.SetValue($"{Registry.ClassesRoot}\\armorsecurity", "URL Protocol", "");

            Registry.SetValue($"{Registry.ClassesRoot}\\armorsecurity\\shell", "", "");

            Registry.SetValue($"{Registry.ClassesRoot}\\armorsecurity\\shell\\open", "", "");

            Registry.SetValue($"{Registry.ClassesRoot}\\armorsecurity\\shell\\open\\command", "", $"\"{GetAppBaseExe()}\" \"%1\"");
        }

        public static bool IsRunningAsAdministrator()
        {
            // Get current Windows user
            WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();

            // Get current Windows user principal
            WindowsPrincipal windowsPrincipal = new WindowsPrincipal(windowsIdentity);

            // Return TRUE if user is in role "Administrator"
            return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void Log(params string[] lines)
        {
            File.AppendAllLines(@"C:\Users\JamesDeMuse\Desktop\squirrel.txt", lines);
        }
    }
}
