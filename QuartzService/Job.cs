using Quartz;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace QuartzService
{
    public class Job : IJob
    {
        #region Properties and Variables
        
        public int iID { get; set; }
        public String strName { get; set; }
        public String strProcess { get; set; }
        public String strArguments { get; set; }
        public String strWorkingDirectory { get; set; }
        public int iTimeOut { get; set; }
        private int iProcessID = 0;
        private int iRecordID = 0;
        private int iTimedOut = 0;
        Process process = new Process();
        JobRecordUpdater recordUpdater = new JobRecordUpdater();
     
        #endregion

        /// <summary>
        /// Executes the JOB
        /// </summary>
        /// <param name="context"></param>
        public void Execute(IJobExecutionContext context)
        {
            JobDataMap datamap = context.JobDetail.JobDataMap;

            iID = datamap.GetInt("ID");
            strName = datamap.GetString("Name");
            strProcess = datamap.GetString("Process");
            strArguments = datamap.GetString("Arguments");
            strWorkingDirectory = datamap.GetString("WorkingDirectory");
            iTimeOut = datamap.GetInt("TimeOut");

            QuartzService.log("Job ID " + iID + " is starting...");
          
            iRecordID = recordUpdater.addNewRecord(iID, strName, DateTime.Now);

            try
            {
                process.StartInfo.FileName = strProcess;
                process.StartInfo.Arguments = strArguments;
                setWorkingDirectory();
                process.Start();
                iProcessID = process.Id;
                process.WaitForExit(iTimeOut);
                if (processStillRunning(iProcessID))
                {
                    iTimedOut = 1;
                    process.Kill();
                    QuartzService.log("Job ID " + iID + " was stopped; Process TimeOut of " + iTimeOut + "ms Exceeded..."); 
                }
                else
                {
                    QuartzService.log("Job ID " + iID + " has completed...");
                }
                recordUpdater.setRecordEnding(iRecordID, DateTime.Now, iTimedOut);
            }
            catch (Exception ex)
            {
                    QuartzService.log(@"Exception When Running " + strName +  "\n" +
                                             ex.StackTrace);
            }
        }

        /// <summary>
        /// Checks to see if process is still running
        /// </summary>
        /// <param name="ID">Process ID</param>
        /// <returns>Bool indicating if process is running</returns>
        private bool processStillRunning(int ID)
        {
            bool bRunning = false;

            try
            {
                Process process = Process.GetProcessById(ID);
                bRunning = true;
            }
            catch
            {
                
            }
            return bRunning;
        }

        /// <summary>
        /// Sets working directory to process directory if set to NULL in DB
        /// </summary>
        private void setWorkingDirectory()
        {
            if(String.IsNullOrWhiteSpace(strWorkingDirectory))
            {
                string temp = strProcess;
                strWorkingDirectory = temp.Remove(temp.LastIndexOf(@"\"));
            }
            process.StartInfo.WorkingDirectory = strWorkingDirectory;
        }
    }
}
