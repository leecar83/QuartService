using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuartzService
{
    public class JobTemplate
    {
        #region Properties
        public int ID { get; set; }
        public String Name { get; set; }
        public String Group { get; set; }
        public String Process { get; set; }
        public String WorkingDirectory { get; set; }
        public String Arguments { get; set; }
        public String CronSchedule { get; set; }
        public String Timeout { get; set; }   
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public JobTemplate()
        {

        }

        /// <summary>
        /// Processes the path strings in DB (Replaces "\\" with "\")
        /// </summary>
        /// <param name="Process"></param>
        /// <param name="WorkingDirectory"></param>
        public void processPathStringsFromDB(string Process, string WorkingDirectory)
        {
            StringBuilder strBuilder = new StringBuilder(Process);
            strBuilder.Replace(@"\\", @"\");
            this.Process = strBuilder.ToString();

            strBuilder = new StringBuilder(WorkingDirectory);
            strBuilder.Replace(@"\\", @"\");
            this.WorkingDirectory = strBuilder.ToString();
        }
    }
}
