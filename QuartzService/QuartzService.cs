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
using System.Threading;

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
            EventLog.WriteEntry("Quartz Scheduler is starting");
            log("QuartzScheduler is starting");
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
            this.EventLog.Log = "";
        }

        /// <summary>
        /// Sets up FileSystemWatcher to watch for update FLGs
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
            if(String.Equals(e.Name, "updatesql.flg", StringComparison.CurrentCultureIgnoreCase))
            {
                if(Settings.Default.MSSQLEnabled)
                {
                    runSQLUpdate();
                } 
            }
        }
        
        /// <summary>
        /// Update SQL DB and Job scheduler from incoming folder
        /// </summary>
        private void runSQLUpdate()
        {
            log("UPDATESQL has been initiated");
            deleteUpdateSQLFLG();
            DBUpdater updater = new DBUpdater();
            Dictionary<JobTemplate,String> jobIDs = updater.updateFromIncoming();
            if (jobIDs != null)
            {
               foreach(var key in jobIDs)
                {
                    if(key.Value == "1")
                    {
                        scheduler.setUpJob(scheduler.loadJobFromDB(key.Key.ID.ToString()));
                    }
                    else if(key.Value == "2")
                    {
                        scheduler.removeJobFromScheduler(key.Key.ID.ToString());
                        scheduler.setUpJob(key.Key);
                    }
                    else if (key.Value == "3")
                    {
                        scheduler.removeJobFromScheduler(key.Key.ID.ToString());
                    }
                }
            }
        }
        
        /// <summary>
        /// Deletes the UPDATESQL.FLG safely
        /// </summary>
        private void deleteUpdateSQLFLG()
        {
            try
            {
                if(File.Exists(strFlagsFolder + @"\UpdateSQL.flg"))
                File.Delete(strFlagsFolder + @"\UpdateSQL.flg");
            }
            catch (Exception ex)
            {
                log("Error deleting Update.FLG Exception at \n" + ex);
            }
        }

        /// <summary>
        /// Adds to the logs
        /// </summary>
        /// <param name="LogEntry">String to add to log file</param>
        /// <param name="LogFile">Set automatically</param>
        public static void log(String LogEntry, String LogFile = LOGFILEDIRECTORY)
        {
            String strLogFileName = DateTime.Now.ToString("yyyyMMdd") + @".log";
            LogFile += strLogFileName;
            try
            {
                lock (LogFile)
                {
                    using (StreamWriter streamWriter = new StreamWriter(LogFile, true))
                    {
                        streamWriter.WriteLine(DateTime.Now.ToString() + " " + LogEntry + "...");
                    }
                }
            }
            catch(Exception ex)
            {
                using (StreamWriter streamWriter = new StreamWriter(@"C:\Temp\QuartzError.log", true))
                {
                    streamWriter.WriteLine(DateTime.Now.ToString() + " Exception: " + ex.Message);
                }
            }
        
        }
        
        /// <summary>
        /// Checks for required directories and clears flags folder
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
                EventLog.WriteEntry("Error checking " + LOGFILEDIRECTORY + "\n" + ex.StackTrace);
            }
            try
            {
                if (!Directory.Exists(Settings.Default.IncomingDirectory))
                {
                    Directory.CreateDirectory(Settings.Default.IncomingDirectory);
                }
            }
            catch (Exception ex)
            {
                QuartzService.log("Error checking for incoming folder " + "\n" + ex.StackTrace);
            }
            try
            {
                if (!Directory.Exists(Settings.Default.JobsDirectory))
                {
                    Directory.CreateDirectory(Settings.Default.JobsDirectory);
                }
            }
            catch(Exception ex)
            {
                EventLog.WriteEntry("Error checking " + LOGFILEDIRECTORY + "\n" + ex.StackTrace);
            }

            try
            {
                if(!Directory.Exists(strFlagsFolder))
                {
                    Directory.CreateDirectory(strFlagsFolder);
                }
                else
                {
                    try
                    {
                        String[] flagFiles = Directory.GetFiles(strFlagsFolder);
                        if(flagFiles.Length > 0)
                        {
                            foreach(String file in flagFiles)
                            {
                                try
                                {
                                    File.Delete(file);
                                }
                                catch(Exception ex)
                                {
                                    EventLog.WriteEntry("Error deleting flag " + file + " in " + strFlagsFolder + "\n" + ex.StackTrace);
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        EventLog.WriteEntry("Error checking for flags in " + strFlagsFolder + "\n" + ex.StackTrace);
                    }
                }
            }
            catch(Exception ex)
            {
                EventLog.WriteEntry("Error checking " + strFlagsFolder + "\n" + ex.StackTrace);
            }
        }
    }
}
