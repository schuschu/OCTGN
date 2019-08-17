﻿using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;

namespace Octgn.Launcher
{
    public partial class App : Application
    {
        private static log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly Version Windows7Version = new Version(6, 1, 0, 0);

        private const string DotNetDownloadUrl = "http://go.microsoft.com/fwlink/?LinkId=2085155";

        protected override void OnStartup(StartupEventArgs e) {
            Log.Info("Startup");

            try {
                this.DispatcherUnhandledException += App_DispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                var computerInfo = new ComputerInfo();

                var ver = Version.Parse(computerInfo.OSVersion);

                Log.Info("OS Version=" + computerInfo.OSVersion);

                if (ver < Windows7Version) {
                    Log.Warn("Windows version is too low to run OCTGN.");

                    MessageBox.Show($"OCTGN is incompatible with {computerInfo.OSFullName} {computerInfo.OSVersion} at this time.{Environment.NewLine}{Environment.NewLine}OCTGN will now exit.", "Incompatible OS", MessageBoxButton.OK, MessageBoxImage.Stop);

                    Shutdown(2);

                    return;
                }

                if (!IsDotNet47OrLaterInstalled()) {
                    Log.Warn(".net 4.7 is not installed.");

                    var result = MessageBox.Show($"OCTGN requires .net 4.7 to be installed.{Environment.NewLine}{Environment.NewLine}Press 'Yes' to download .net", ".Net Missing", MessageBoxButton.YesNo, MessageBoxImage.Stop);

                    if (result == MessageBoxResult.Yes) {
                        if (DownloadDotNetFramework48(out var downloadPath)) {
                            Log.Info("Launching .net installer: " + downloadPath);

                            using (var process = Process.Start(downloadPath)) {
                                process.WaitForExit();

                                if (process.ExitCode != 0) {
                                    Log.Error("Running .net installer failed with exit code " + process.ExitCode);

                                    Shutdown(3);

                                    return;
                                }
                            }
                        } else {
                            Shutdown(4);

                            return;
                        }
                    } else {
                        Shutdown(5);

                        return;
                    }
                }

                if (!LaunchOctgn()) {
                    Log.Warn("Could not launch OCTGN");

                    MessageBox.Show($"OCTGN was unable to be launched. Please try again later", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    Shutdown(6);

                    return;
                }

                Log.Info("Launcher ran successfully");

                Shutdown(0);
            } catch (Exception ex) {
                Log.Error("Error", ex);

                MessageBox.Show($"OCTGN was unable to be launched. Please try again later: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                Shutdown(99);
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            Log.Error("Unhandled Exception", (Exception)e.ExceptionObject);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
            Log.Error("Dispatcher Unhandled Exception", e.Exception);
        }

        private static bool DownloadDotNetFramework48(out string downloadPath) {
            Log.Info("Creating download path");

            downloadPath = Path.Combine(Path.GetTempPath(), "netinstaller" + DateTime.Now.Ticks + ".exe");

            Log.Info($"Downloading .net installer from {DotNetDownloadUrl} to {downloadPath}");

            try {
                using (var wc = new WebClient()) {
                    wc.DownloadFile(DotNetDownloadUrl, downloadPath);
                }

                return true;
            } catch (Exception ex) {
                Log.Error("Error downloading installer " + DotNetDownloadUrl, ex);

                MessageBox.Show($"There was an error downloading the .Net Framework{Environment.NewLine}{Environment.NewLine}{ex.Message}{Environment.NewLine}{Environment.NewLine}Please try again later.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                return false;
            }
        }

        private static bool IsDotNet47OrLaterInstalled() {
            Log.Info("Getting .net from registry");

            try {
                using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\")) {
                    if (ndpKey == null) {
                        Log.Warn("No .net registry key found.");

                        return false;
                    }

                    var releaseKey = Convert.ToInt32(ndpKey.GetValue("Release"));

                    Log.Info(".Net Release: " + releaseKey);

                    if (releaseKey >= 460798) {
                        return true;
                    }

                    return false;
                }
            } catch (Exception ex) {
                Log.Error("Error getting .net from registry", ex);

                return false;
            }
        }

        private static bool LaunchOctgn() {
            try {
                var appFile = new FileInfo(typeof(App).Assembly.Location);

                var rootDirectory = appFile.Directory.FullName;

                var octgnPath = Path.Combine(rootDirectory, "Octgn.exe");

                if (Debugger.IsAttached) {
                    octgnPath = "..\\..\\..\\Octgn\\bin\\Debug\\Octgn.exe";
                }

                Log.Info("Octgn Path: " + octgnPath);

                var psi = new ProcessStartInfo(octgnPath);
                psi.Arguments = string.Join(" ", Environment.GetCommandLineArgs().Skip(1));
                psi.WorkingDirectory = Environment.CurrentDirectory;

                Process.Start(psi);

                return true;
            } catch (Exception ex) {
                Log.Error("Error launching octgn", ex);

                return false;
            }
        }
    }
}
