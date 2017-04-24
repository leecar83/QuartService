using Newtonsoft.Json;
using QuartzService.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QuartzService
{
    class DBUpdater
    {
        #region Variables, Constants
        QuartzEntities1 db;
        private string strIncomingDirectory = Settings.Default.IncomingDirectory;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public DBUpdater()
        {
            db = new QuartzEntities1();
        }

        /// <summary>
        /// Updates SQLDB from incoming files
        /// </summary>
        /// <returns>JobID and value indicating the change type</returns>
        public String[,] updateFromIncoming()
        {
            QuartzService.log("Loading any incoming updates");
            try
            {
                if(!Directory.Exists(strIncomingDirectory))
                {
                    Directory.CreateDirectory(strIncomingDirectory);
                }
            }
            catch(Exception ex)
            {
                QuartzService.log("Error checking for incoming folder " + "\n" + ex.StackTrace);
            }
            List<String> filePaths = new List<String>(Directory.GetFiles(strIncomingDirectory));
            String[,] jobIDs = new String[filePaths.Count, 2];
            try
            { 
                sortIncomingFiles(filePaths);
                for(int i = 0; i < filePaths.Count; i++)
                {
                    if (filePaths[i].EndsWith(".json", StringComparison.CurrentCultureIgnoreCase))
                    {
                        try
                        {
                            JobTemplate template = JsonConvert.DeserializeObject<JobTemplate>(File.ReadAllText(filePaths[i]));
                            try
                            {
                                String iReturn = updateDatabase(template);
                                QuartzService.log("Job ID " + template.ID + " set to update in DB");
                                db.SaveChanges();
                                QuartzService.log("Job ID " + template.ID + " changes saved to the DB");
                                jobIDs[i,0] = template.ID.ToString();
                                jobIDs[i,1] = iReturn.ToString();
                                try
                                {
                                    File.Delete(filePaths[i]);
                                }
                                catch (Exception ex)
                                {
                                    QuartzService.log("Error deleting JSON file after update Exception\n" + ex.StackTrace);
                                }
                            }
                            catch (Exception ex)
                            {
                                QuartzService.log("Error updating the DB Exception\n" + ex.InnerException + "\n" + ex.StackTrace);
                            }
                        }
                        catch (Exception ex)
                        {
                            renameToERR(filePaths[i]);
                            QuartzService.log("Error updating from " + filePaths[i] + "\n" + ex.StackTrace);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                QuartzService.log("Error loading files from " + strIncomingDirectory + "Exception\n" + ex.StackTrace);
            }
            return jobIDs;
        }

        /// <summary>
        /// Updates SQLDB from single template
        /// </summary>
        /// <param name="template"></param>
        private String updateDatabase(JobTemplate template)
        {
            String iReturn = "0";
            //check to see if all null except ID(this means to delete JOB from DB)
            if(template.Name == null &&
                template.Group == null &&
                template.Process == null &
                template.WorkingDirectory == null &&
                template.Arguments == null &&
                template.CronSchedule == null &&
                template.Timeout == null)
            {
                DBJob toDelete = db.Jobs.Where(s => s.JobId == template.ID).FirstOrDefault<DBJob>();
                if (toDelete != null)
                {
                    db.Entry(toDelete).State = System.Data.Entity.EntityState.Deleted;
                    iReturn = "3";
                }
            }
            //job needs to be added or updated
            else
            {
                var jobQuery = from job in db.Jobs
                               where job.JobId == template.ID
                               select job;

                if (jobQuery.Count() == 1)
                {
                    DBJob dbJob = jobQuery.SingleOrDefault();
                    dbJob.JobName = template.Name;
                    dbJob.JobGroup = template.Group;
                    dbJob.Process = template.Process;
                    dbJob.WorkingDirectory = template.WorkingDirectory;
                    dbJob.Arguments = template.Arguments;
                    dbJob.CronSchedule = template.CronSchedule;
                    dbJob.TimeOut = Int32.Parse(template.Timeout);
                    iReturn = "2";
                }
                if (jobQuery.Count() == 0)
                {
                    DBJob dbJob = new DBJob();
                    dbJob.JobId = template.ID;
                    dbJob.JobName = template.Name;
                    dbJob.JobGroup = template.Group;
                    dbJob.Process = template.Process;
                    dbJob.WorkingDirectory = template.WorkingDirectory;
                    dbJob.Arguments = template.Arguments;
                    dbJob.CronSchedule = template.CronSchedule;
                    dbJob.TimeOut = Int32.Parse(template.Timeout);
                    db.Jobs.Add(dbJob);
                    iReturn = "1";
                }
            }
            return iReturn;
        }

        /// <summary>
        /// Sorts list of incoming json files numerically
        /// </summary>
        /// <param name="strings">List of Strings</param>
        private void sortIncomingFiles(List<String> strings)
        {
            for (int iInt = 0; iInt < strings.Count -1; iInt++)
            {
                for (int i = 0; i < strings.Count - 1; i++)
                {
                    String strSub = strings[i].Substring(strings[i].LastIndexOf(@"\") + 1, (strings[i].Length - 5) - strings[i].LastIndexOf(@"\") - 1);
                    String strSub2 = strings[i + 1].Substring(strings[i + 1].LastIndexOf(@"\") + 1, (strings[i + 1].Length - 5) - strings[i + 1].LastIndexOf(@"\") - 1);
                    int iOne = 0;
                    int iTwo = 0;
                    if (Int32.TryParse(strSub, out iOne) && Int32.TryParse(strSub2, out iTwo) == true)
                    {
                        if (iOne > iTwo)
                        {
                            String tmp = strings[i];
                            strings[i] = strings[i + 1];
                            strings[i + 1] = tmp;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Rename json files to err
        /// </summary>
        /// <param name="InputString"></param>
        private void renameToERR(String InputString)
        {
            String strNewFileName = String.Empty;
            if (InputString.Contains("."))
            {
                strNewFileName = InputString.Remove(InputString.LastIndexOf(".") + 1);
            }
            else
            {
                strNewFileName = String.Concat(InputString, ".");
            }
            strNewFileName = String.Concat(strNewFileName, "err");

            try
            {
                File.Move(InputString, strNewFileName);
            }
            catch(Exception ex)
            {
                QuartzService.log("Error renaming  " + InputString + " to .err Exception\n" + ex.StackTrace);
            }  
        }
    }
}