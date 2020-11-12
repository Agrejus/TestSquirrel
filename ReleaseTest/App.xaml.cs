using Squirrel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;

namespace ReleaseTest
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            var mainWindow = new MainWindow
            {
                Args = e.Args
            };
            mainWindow.Show();
            //Log("app start");
            //Log("Args");
            //Log(e.Args);

            //var updatePath = ConfigurationManager.AppSettings["UpdatePathFolder"];
            //var packageId = ConfigurationManager.AppSettings["PackageID"];

            //using (var manager = await UpdateManager.GitHubUpdateManager(updatePath, packageId))
            //{

            //    if (e.Args.Contains("--squirrel-install") && IsRunningAsAdministrator() == false)
            //    {
            //        // installing
            //        Log("restarting as admin");
            //        var args = e.Args.ToList();
            //        args.Add("--resolve-registry");

            //        var processStartInfo = new ProcessStartInfo(Assembly.GetEntryAssembly().CodeBase)
            //        {
            //            // Using operating shell and setting the ProcessStartInfo.Verb to “runas” will let it run as admin
            //            UseShellExecute = true,
            //            Verb = "runas",
            //            Arguments = string.Join(" ", args)
            //        };

            //        // Start the application as new process
            //        Process.Start(processStartInfo);

            //        // Shut down the current (old) process
            //        Log("app closing");
            //        Shutdown();
            //        return;
            //    }

            //    if (e.Args.Contains("--squirrel-install"))
            //    {
            //        mainWindow.Loaded += async (s, args) =>
            //        {
            //            await mainWindow.Install(e.Args[1], manager);
            //        };
            //    }
            //    else if (e.Args.Contains("--squirrel-uninstall"))
            //    {
            //        mainWindow.Loaded += async (s, args) =>
            //        {
            //            await mainWindow.Uninstall(e.Args[1], manager);
            //        };
            //    }
            //    else if (e.Args.Contains("--squirrel-firstrun"))
            //    {
            //        // should be up to date
            //    }
            //    else
            //    {
            //        var updates = await manager.CheckForUpdate();

            //        if (updates.ReleasesToApply.Any())
            //        {
            //            var lastVersion = updates.ReleasesToApply.OrderBy(x => x.Version).Last();

            //            mainWindow.Loaded += async (s, args) =>
            //            {

            //                await mainWindow.Update(e.Args[1], manager, lastVersion, updates);
            //            };
            //        }
            //    }
            //}

            //mainWindow.Show();
        }

        private void Log(params string[] lines)
        {
            File.AppendAllLines(@"C:\Users\JamesDeMuse\Desktop\squirrel.txt", lines);
        }


        /// <summary>
        /// Function that check's if current user is in Aministrator role
        /// </summary>
        /// <returns></returns>
    }
}
