using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Timers;
using System.Runtime.InteropServices;
using System;

namespace GetManuals
{
    public partial class GetManualsService : ServiceBase
    {
        // Define remote URL to where the source is.
        private string remoteUrl = @"\\xxx.xxx.xxx.xxx\Offshore_Manuals\Manuals";
        // Local folder to use as destination and sync folder.
        private string localUrl = @"D:\Manuals";
        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDINGD = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

        public GetManualsService()
        {
            InitializeComponent();
            // Setup logging
            AutoLog = false;

            ((ISupportInitialize)EventLog).BeginInit();
            if (!EventLog.SourceExists(ServiceName))
            {
                EventLog.CreateEventSource(ServiceName, "Application");
            }
            ((ISupportInitialize)EventLog).EndInit();

            EventLog.Source = ServiceName;
            EventLog.Log = "Application";
        }

        // Check and copy folders and files in folders
        // sourceFolder = remote folder (for ex a file server)
        // destFolder = local folder (local PC storage)
        private void CopyFolder(string sourceFolder, string destFolder)
        {
            // Check current destFolder exixts, if not make it
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            // Check files in current sourceFolder
            // Copy if not in destFodler
            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files)
            {
                DateTime modifiedSource = File.GetLastWriteTime(file);
                string name = Path.GetFileName(file);
                string dest = Path.Combine(destFolder, name);
                if (!File.Exists(dest))
                {
                    try
                    {
                        File.Copy(file, dest);
                    }
                    catch
                    {

                    }
                    continue;
                }
                // Check modified date
                var modifiedDest = File.GetLastWriteTime(dest);
                if (modifiedSource != modifiedDest)
                {
                    File.Delete(dest);
                    File.Copy(file, destFolder + "\\" + name);
                }
            }
            // Run for next folder in sourceFolder
            string[] folders = Directory.GetDirectories(sourceFolder);
            foreach(string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(destFolder, name);
                // Recurring function for all folders
                CopyFolder(folder, dest);
            }
        }

        // Check and delete files in destination that does not exist in source anymore.
        // sourceFolder = remote folder (for ex a file server), used to check local files
        // destFolder = local folder (local PC storage)
        private void CheckFiles(string sourceFolder, string destFolder)
        {
            // Check files in current destFolder folder
            // Delete file that is not present in sourceFolder
            string[] files = Directory.GetFiles(destFolder);
            foreach(string file in files)
            {
                string name = Path.GetFileName(file);
                string source = Path.Combine(sourceFolder, name);
                if (!File.Exists(source))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {

                    }
                }
            }
            // Check folders in current destFolder folder
            // Delete folder if not present in sourceFolder
            string[] foldersDel = Directory.GetDirectories(destFolder);
            foreach (string folder in foldersDel)
            {
                string name = Path.GetFileName(folder);
                string source = Path.Combine(sourceFolder, name);
                if (!Directory.Exists(source))
                {
                    try
                    {
                        Directory.Delete(folder, true);
                    }
                    catch
                    {

                    }
                }
            }
            // Run for next folder in sourceFolder
            string[] folders = Directory.GetDirectories(sourceFolder);
            foreach(string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(destFolder, name);
                // Recurring function for all folders
                CheckFiles(folder, dest);
            }
        }

        protected override void OnStart(string[] args)
        {
            // Update the service state to Start Pending
            ServiceStatus serviceStatus = new ServiceStatus
            {
                dwCurrentState = ServiceState.SERVICE_START_PENDING,
                dwWaitHint = 60000
            };
            SetServiceStatus(ServiceHandle, ref serviceStatus);

            if (Directory.Exists(remoteUrl))
            {
                CopyFolder(remoteUrl, localUrl);
                CheckFiles(remoteUrl, localUrl);
                EventLog.WriteEntry("Startup sync done", EventLogEntryType.Information);
            }
            else
            {
                EventLog.WriteEntry("Error connecting to source.\nSource: " + remoteUrl, EventLogEntryType.Error);
            }

            // Timer for running OnTimer method
            Timer timer = new Timer
            {
                Interval = 60000 * 60 * 6 // 6 hours
            };
            timer.Elapsed += new ElapsedEventHandler(OnTimer);
            timer.Start();

            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(ServiceHandle, ref serviceStatus);
            EventLog.WriteEntry("Service started", EventLogEntryType.Information);
        }

        private void OnTimer(object sender, ElapsedEventArgs args)
        {
            if(Directory.Exists(remoteUrl))
            {
                CopyFolder(remoteUrl, localUrl);
                CheckFiles(remoteUrl, localUrl);
            }
            else
            {
                EventLog.WriteEntry("Error connecting to source.\nSource: " + remoteUrl, EventLogEntryType.Error);
            }
        }

        protected override void OnStop()
        {

        }
    }
}
