﻿using QuartzService.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace QuartzService
{
    public partial class QuartzService : ServiceBase
    {
        #region Variables & Constants
        JobScheduler scheduler;
        FileSystemWatcher updateFlagWatcher;
        private String strFlagsFolder = Settings.Default.FlagsDirectory;
        const String LOGFILEDIRECTORY = @"C:\Quartz\Logs\";
 
        #endregion

        public QuartzService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            onStartMethod();
        }

        protected override void OnStop()
        {
            log("Stopping Quartz Scheduler...");
            if (scheduler != null)
            {
                scheduler.Stop();
            }
        }

        public void onStartMethod()
        {
            setupEventLog();
            checkDirectories();
            log("QuartzService is starting");
            //Process any incoming updates at startup
            DBUpdater updater = new DBUpdater();
            updater.updateFromIncoming();

            //Start the job scheduler
            scheduler = new JobScheduler();
            scheduler.Start();

            watchForUpdates();
        }

        /// <summary>
        /// Setup event logging
        /// </summary>
        private void setupEventLog()
        {
            this.ServiceName = "Quartz Scheduler";
            this.CanStop = true;
            this.CanPauseAndContinue = true;

            //Setup logging
            this.AutoLog = false;

            ((ISupportInitialize)this.EventLog).BeginInit();
            if (!EventLog.SourceExists(this.ServiceName))
            {
                EventLog.CreateEventSource(this.ServiceName, "Quartz Scheduler");
            }
        ((ISupportInitialize)this.EventLog).EndInit();

            this.EventLog.Source = this.ServiceName;
            this.EventLog.Log = "Quartz Scheduler";
        }

        /// <summary>
        /// Sets up FileSystemWatcher to watch for UPDATE.FLG
        /// </summary>
        private void watchForUpdates()
        {
            updateFlagWatcher = new FileSystemWatcher(strFlagsFolder);
            updateFlagWatcher.NotifyFilter = NotifyFilters.LastWrite;
            updateFlagWatcher.Created += new FileSystemEventHandler(OnChanged);
            updateFlagWatcher.Changed += new FileSystemEventHandler(OnChanged);
            updateFlagWatcher.EnableRaisingEvents = true;
        }
        
        /// <summary>
        /// Event handler for change to flags directory
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnChanged(Object source, FileSystemEventArgs e)
        {
            if(String.Equals(e.Name, "update.flg", StringComparison.CurrentCultureIgnoreCase))
            {
                try
                {
                    log("UPDATE has been initiated");
                    File.Delete(strFlagsFolder + @"\Update.flg");
                }
                catch(Exception ex)
                {
                    log("Error deleting Update.FLG Exception at \n" + ex);
                }
                DBUpdater updater = new DBUpdater();
                String[,] jobIDs = updater.updateFromIncoming();
                for(int i = 0;  i < jobIDs.Length / 2; i++)
                {
                    if(jobIDs[i, 1] == "1")
                    {
                        scheduler.setUpJob(scheduler.loadJobFromDB(jobIDs[i, 0]));
                    }
                    else if(jobIDs[i, 1] == "2")
                    {
                        scheduler.removeJobFromScheduler(jobIDs[i, 0]);
                        scheduler.setUpJob(scheduler.loadJobFromDB(jobIDs[i, 0]));
                    }
                    else if(jobIDs[i, 1] == "3")
                    {
                        scheduler.removeJobFromScheduler(jobIDs[i, 0]);
                    }
                }
            }
        }
        
        /// <summary>
        /// Deletes the UPDATE.FLG safely
        /// </summary>
        private void deleteUpdateFLG()
        {
            try
            {
                if(File.Exists(strFlagsFolder + @"\Update.flg"))
                File.Delete(strFlagsFolder + @"\Update.flg");
            }
            catch (Exception ex)
            {
                log("Error deleting Update.FLG Exception at \n" + ex);
            }
        }

        /// <summary>
        /// Adds to the logs
        /// </summary>
        /// <param name="LogEntry"></param>
        /// <param name="LogFile"></param>
        public static void log(String LogEntry, String LogFile = LOGFILEDIRECTORY)
        {
            logInEventViewer(LogEntry);
            String strLogFileName = DateTime.Now.ToString("yyyyMMdd") + @".log";
            LogFile += strLogFileName;
            lock(LogFile)
            {
                using (StreamWriter streamWriter = new StreamWriter(LogFile, true))
                {
                    streamWriter.WriteLine(DateTime.Now.ToString() + " " + LogEntry + "...");
                }
            }
        }

        private static void logInEventViewer(String Entry)
        {
            try
            {
                EventLog.WriteEntry(Entry);
            }
            catch(Exception ex)
            {

            }
        }
        
        /// <summary>
        /// Check for required directories and adds them if missing
        /// </summary>
        private void checkDirectories()
        {
            try
            {
                if(!Directory.Exists(LOGFILEDIRECTORY))
                {
                    Directory.CreateDirectory(LOGFILEDIRECTORY);
                }
            }
            catch(Exception ex)
            {

            }

            try
            {
                if(!Directory.Exists(strFlagsFolder))
                {
                    Directory.CreateDirectory(strFlagsFolder);
                }
            }
            catch(Exception ex)
            {

            }
        }
    }
}
