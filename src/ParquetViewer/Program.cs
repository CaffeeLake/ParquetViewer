﻿using ParquetViewer.Analytics;
using ParquetViewer.Exceptions;
using System;
using System.IO;
using System.Windows.Forms;

namespace ParquetViewer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static int Main(string[] args)
        {
            string? fileToOpen = null;
            try
            {
                if (args?.Length > 0)
                {
                    if (AboutBox.PERFORM_FILE_ASSOCIATION.Equals(args[0]))
                    {
                        try
                        {
                            if (args.Length > 1 && bool.TryParse(args[1], out bool associate))
                            {
                                return AboutBox.ToggleFileAssociation(associate) ? 0 : 1;
                            }
                            else
                            {
                                return 2; //no true/false flag passed
                            }
                        }
                        catch (Exception)
                        {
                            return 3;
                        }
                    }
                    else if (File.Exists(args[0]))
                    {
                        fileToOpen = args[0];
                    }
                }
            }
            catch (Exception) { /*Swallow Exception*/ }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //Form must be created after calling SetCompatibleTextRenderingDefault();
            Form mainForm;
            bool isOpeningFile = !string.IsNullOrWhiteSpace(fileToOpen);
            if (isOpeningFile)
                mainForm = new MainForm(fileToOpen!);
            else
                mainForm = new MainForm();

            RouteUnhandledExceptions();

            Application.Run(mainForm);
            return 0;
        }

        /// <summary>
        /// When called, all unhandled exceptions within the runtime and winforms UI thread
        /// will be routed to the <see cref="ExceptionHandler"/> handler. 
        /// </summary>
        /// <remarks>Side effect: The application will never quit when an unhandled exception happens.</remarks>
        private static void RouteUnhandledExceptions()
        {
            //If we're not debugging, route all unhandled exceptions to our top level exception handler
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                // Add the event handler for handling non-UI thread exceptions to the event. 
                AppDomain.CurrentDomain.UnhandledException += new((sender, e) => ExceptionHandler((Exception)e.ExceptionObject));

                // Add the event handler for handling UI thread exceptions to the event.
                Application.ThreadException += new((sender, e) => ExceptionHandler(e.Exception));
            }
        }

        private static void ExceptionHandler(Exception ex)
        {
            ExceptionEvent.FireAndForget(ex);
            MessageBox.Show($"Something went wrong (CTRL+C to copy):{Environment.NewLine}{ex}", ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// We only ask for consent if the user launched the app at least twice, 1 day apart.
        /// </summary>
        public static void GetUserConsentToGatherAnalytics()
        {
            if (AppSettings.AnalyticsDataGatheringConsent)
            {
                //Keep user's consent asked version up to date with the current assembly version
                if (AssemblyVersionToInt(AppSettings.ConsentLastAskedOnVersion) < AssemblyVersionToInt(AboutBox.AssemblyVersion))
                {
                    AppSettings.ConsentLastAskedOnVersion = AboutBox.AssemblyVersion;
                }
            }
            else if (AssemblyVersionToInt(AppSettings.ConsentLastAskedOnVersion) < AssemblyVersionToInt(AboutBox.AssemblyVersion))
            {
                bool isFirstLaunch = AppSettings.ConsentLastAskedOnVersion is null;
                if (isFirstLaunch)
                {
                    //Don't ask for consent on the first launch. Record the day of the month instead so we can ask tomorrow. 
                    AppSettings.ConsentLastAskedOnVersion = DateTime.Now.Day.ToString();
                }
                else if (AppSettings.ConsentLastAskedOnVersion != DateTime.Now.Day.ToString())
                {
                    AppSettings.ConsentLastAskedOnVersion = AboutBox.AssemblyVersion;
                    if (MessageBox.Show($"Would you like to share anonymous usage data to help make ParquetViewer better?{Environment.NewLine}{Environment.NewLine}" +
                        $"You can always change this setting later from the Help menu.", "Share Anonymous Usage Data?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        //We got consent! Start gathering some data..
                        AppSettings.AnalyticsDataGatheringConsent = true;
                    }
                }
            }

            static int AssemblyVersionToInt(string? version)
            {
                try
                {
                    return int.Parse(version?.Replace(".", string.Empty) ?? "0");
                }
                catch (Exception ex)
                {
                    ExceptionEvent.FireAndForget(new UnsupportedAssemblyVersionException(ex));
                    return 0;
                }
            }
        }

        /// <summary>
        /// We only ask for file extension association if the user opened at least 8 parquet files
        /// </summary>
        public static void AskUserForFileExtensionAssociation()
        {
            //if (AppSettings.OpenedFileCount == 8 && !AboutBox.IsDefaultViewerForParquetFiles)
            //{
            //    if (MessageBox.Show($"Would you like to associate ParquetViewer with .parquet files?{Environment.NewLine}{Environment.NewLine}" +
            //            $"This will allow you to double-click to open your parquet files. You can also toggle this setting from the Help -> About page.", "Make ParquetViewer Default for .parquet Files?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            //    {
            //        AboutBox.ToggleFileAssociation(true);
            //    }
            //}
        }
    }
}
