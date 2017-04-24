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
        public String strName { get; set; }
        public String strProcess { get; set; }
        public String strArguments { get; set; }
        public String strWorkingDirectory { get; set; }
        public String strWindowStyle { get; set; }
        public int intTimeOut { get; set; }
        private int intProcessID = 0;
        Process process = new Process();
        #endregion

        public void Execute(IJobExecutionContext context)
        {
            JobDataMap datamap = context.JobDetail.JobDataMap;

            strName = datamap.GetString("Name");
            strProcess = datamap.GetString("Process");
            strArguments = datamap.GetString("Arguments");
            strWorkingDirectory = datamap.GetString("WorkingDirectory");
            strWindowStyle = datamap.GetString("WindowStyle");
            intTimeOut = datamap.GetInt("TimeOut");

            QuartzService.log("Running " + strName + "...");
 
            try
            {
                process.StartInfo.FileName = strProcess;
                process.StartInfo.Arguments = strArguments;
                setWorkingDirectory();
                setWindowStyle(strWindowStyle);
                process.Start();
                intProcessID = process.Id;
                process.WaitForExit(intTimeOut);
                if (processStillRunning(intProcessID))
                {
                    process.Kill();
                    using (StreamWriter streamWriter = new StreamWriter(@"C:\TestServiceLog.txt", true))
                    {
                        QuartzService.log(strName + @" Process TimeOut of " + intTimeOut + "ms Exceeded... Process Killed");
                    }
                }
                else
                {
                    QuartzService.log(strName + @" Job Ended...");
                }
            }
            catch (Exception ex)
            {
                using (StreamWriter streamWriter = new StreamWriter(@"C:\TestServiceLog.txt", true))
                {
                    QuartzService.log(@"Exception When Running " + strName +  "\n" +
                                             ex.StackTrace);
                }
            }
        }

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

        private void setWorkingDirectory()
        {
            if(String.IsNullOrWhiteSpace(strWorkingDirectory))
            {
                string temp = strProcess;
                strWorkingDirectory = temp.Remove(temp.LastIndexOf(@"\"));
            }
            process.StartInfo.WorkingDirectory = strWorkingDirectory;
        }

        private void setWindowStyle(String style)
        {
            if(!String.IsNullOrWhiteSpace(style))
            {
                ProcessWindowStyle winStyle = ProcessWindowStyle.Hidden;

                if (String.Equals("Maximixed", style, StringComparison.CurrentCultureIgnoreCase))
                {
                    winStyle = ProcessWindowStyle.Maximized;
                    process.StartInfo.WindowStyle = winStyle;
                }
                else if (String.Equals("Minimized", style, StringComparison.CurrentCultureIgnoreCase))
                {
                    winStyle = ProcessWindowStyle.Minimized;
                    process.StartInfo.WindowStyle = winStyle;
                }
                else if (String.Equals("Normal", style, StringComparison.CurrentCultureIgnoreCase))
                {
                    winStyle = ProcessWindowStyle.Normal;
                    process.StartInfo.WindowStyle = winStyle;
                }
                else if(String.Equals("Hidden", style, StringComparison.CurrentCultureIgnoreCase))
                {
                    winStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.WindowStyle = winStyle;
                }
  
            }
        }
    }
}
