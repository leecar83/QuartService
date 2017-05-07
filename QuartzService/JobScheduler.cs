using Newtonsoft.Json;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using QuartzService.Properties;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace QuartzService
{
    class JobScheduler
    {
        #region Constants, Variables, DataStructures
        private String strJobsDirectory = Settings.Default.JobsDirectory;
        private String strJobsFile = Settings.Default.JobsFile;
        List<JobTemplate> templates;  
        IScheduler scheduler = StdSchedulerFactory.GetDefaultScheduler();
        QuartzEntities1 db = new QuartzEntities1();
        #endregion

        /// <summary>
        /// Start Scheduler
        /// </summary>
        public void Start()
        {
            scheduler.Start();
            #region Job/Trigger example
            //IJobDetail job1 = JobBuilder.Create<Job>().Build();

            //ITrigger trigger1 = TriggerBuilder.Create()
            //           .WithIdentity("LunchMealReadingsJob", "JOB")
            //           .WithCronSchedule("0 0 14 * * ?")
            //           .StartAt(DateTime.UtcNow)
            //           .WithPriority(1)
            //           .Build();

            //scheduler.ScheduleJob(job1, trigger1);
            #endregion
            if(Settings.Default.JobsDirectoryEnabled)
            {
                loadAndScheduleJobsFromFiles();
            }
            if(Settings.Default.JobsFileEnabled)
            {
                loadAndScheduleJobsFromFile();
            }
            if(Settings.Default.MSSQLEnabled)
            {
                loadJobsFromDB();
                setUpJobs();
            }
        }

        /// <summary>
        /// Shutdown Scheduler
        /// </summary>
        public void Stop()
        {
            IScheduler scheduler = StdSchedulerFactory.GetDefaultScheduler();
            scheduler.Shutdown();
        }

        /// <summary>
        /// Load a single job from SQLDB by JobID
        /// </summary>
        /// <param name="JobID"></param>
        /// <returns>Template</returns>
        public JobTemplate loadJobFromDB(String JobID)
        {
            
            JobTemplate template = new JobTemplate();
            try
            {
                int iJob = Int32.Parse(JobID);
                DBJob dbJob = db.Jobs.FirstOrDefault(j => j.JobId == iJob);
                template.ID = dbJob.JobId;
                template.Name = dbJob.JobName;
                template.Group = dbJob.JobGroup;
                template.processPathStringsFromDB(dbJob.Process, dbJob.WorkingDirectory);
                template.Arguments = dbJob.Arguments;
                template.WorkingDirectory = dbJob.WorkingDirectory;
                template.CronSchedule = dbJob.CronSchedule;
                template.Timeout = dbJob.TimeOut.ToString();
            }
            catch (Exception ex)
            {
                QuartzService.log("Error loading Job from DB: " + "Exception\n" + ex.StackTrace);
            }
            return template;
        }

        /// <summary>
        /// Schedules a single job in the scheduler
        /// </summary>
        /// <param name="Template"></param>
        public void setUpJob(JobTemplate Template)
        {
            try
            {
                IJobDetail job = JobBuilder.Create<Job>().Build();
                job.JobDataMap["ID"] = Template.ID.ToString();
                job.JobDataMap["Name"] = Template.Name;
                //job.JobDataMap["Group"] = template.Group;
                //Defaulting all to "Default" group for now
                job.JobDataMap["Group"] = "Default";
                job.JobDataMap["Process"] = Template.Process;
                job.JobDataMap["Arguments"] = Template.Arguments;
                job.JobDataMap["CronSchedule"] = Template.CronSchedule;
                job.JobDataMap["TimeOut"] = Template.Timeout;

                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity(job.JobDataMap["ID"].ToString(), job.JobDataMap["Group"].ToString())
                    .WithCronSchedule(job.JobDataMap["CronSchedule"].ToString())
                    .StartAt(DateTime.UtcNow)
                    .WithPriority(1)
                    .Build();

                scheduler.ScheduleJob(job, trigger);
                QuartzService.log("Job ID " + Template.ID + " has been scheduled");
            }
            catch (Exception ex)
            {
                QuartzService.log("Error scheduling " + Template.Name + "\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// Removes a single job from scheduler by JobID
        /// </summary>
        /// <param name="jobID">Job ID to be removed</param>
        public void removeJobFromScheduler(String jobID)
        {
            IList<string> jobGroups = scheduler.GetJobGroupNames();
            var groupMatcher = GroupMatcher<JobKey>.GroupContains(jobGroups[0]);
            var jobKeys = scheduler.GetJobKeys(groupMatcher);
            foreach (JobKey jobKey in jobKeys)
            {
                var detail = scheduler.GetJobDetail(jobKey);
                String strID = detail.JobDataMap.GetString("ID");
                if (strID == jobID)
                {
                    try
                    {
                        scheduler.DeleteJob(jobKey);
                        QuartzService.log("Job ID " + jobID + " has been unscheduled");
                    }
                    catch (Exception ex)
                    {
                        QuartzService.log("Error removing job " + jobID + " from scheduler \n" + ex);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Loads and schedules jobs from a single JSON file in Jobs directory
        /// </summary>
        private void loadAndScheduleJobsFromFile()
        {
            try
            {
                if (File.Exists(strJobsFile))
                {
                    List<JobTemplate> fileTemplates = JsonConvert.DeserializeObject<List<JobTemplate>>(File.ReadAllText(strJobsFile));
                    QuartzService.log(strJobsFile + " file loaded successfully...");
                    foreach (JobTemplate template in fileTemplates)
                    {
                        try
                        {
                            IJobDetail job = JobBuilder.Create<Job>().Build();
                            job.JobDataMap["ID"] = template.ID.ToString();
                            job.JobDataMap["Name"] = template.Name;
                            //job.JobDataMap["Group"] = template.Group;
                            //Defaulting all to "Default" group for now
                            job.JobDataMap["Group"] = "Default";
                            job.JobDataMap["Process"] = template.Process;
                            job.JobDataMap["Arguments"] = template.Arguments;
                            job.JobDataMap["CronSchedule"] = template.CronSchedule;
                            job.JobDataMap["TimeOut"] = template.Timeout;

                            ITrigger trigger = TriggerBuilder.Create()
                                .WithIdentity(job.JobDataMap["ID"].ToString(), job.JobDataMap["Group"].ToString())
                                .WithCronSchedule(job.JobDataMap["CronSchedule"].ToString())
                                .StartAt(DateTime.UtcNow)
                                .WithPriority(1)
                                .Build();

                            scheduler.ScheduleJob(job, trigger);
                            QuartzService.log("Job ID " + template.ID + " has been scheduled");
                        }
                        catch (Exception ex)
                        {
                            QuartzService.log("Error scheduling " + template.Name + "\n" + ex.StackTrace);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                QuartzService.log("Error loading jobs from " + strJobsFile + "\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// Loads and schedules jobs from JSON files in Jobs directory
        /// </summary>
        private void loadAndScheduleJobsFromFiles()
        {
            List<JobTemplate> fileTemplates = new List<JobTemplate>();
            String[] filepaths = Directory.GetFiles(strJobsDirectory);

            foreach (String file in filepaths)
            {
                try
                {
                    JobTemplate template = JsonConvert.DeserializeObject<JobTemplate>(File.ReadAllText(file));
                    fileTemplates.Add(template);
                    QuartzService.log(file + " file loaded successfully...");
                }
                catch (Exception ex)
                {
                    QuartzService.log("Error loading " + file + "\n" + ex.StackTrace);
                }
            }

            foreach (JobTemplate template in fileTemplates)
            {
                try
                {
                    IJobDetail job = JobBuilder.Create<Job>().Build();
                    job.JobDataMap["ID"] = template.ID.ToString();
                    job.JobDataMap["Name"] = template.Name;
                    //job.JobDataMap["Group"] = template.Group;
                    //Defaulting all to "Default" group for now
                    job.JobDataMap["Group"] = "Default";
                    job.JobDataMap["Process"] = template.Process;
                    job.JobDataMap["Arguments"] = template.Arguments;
                    job.JobDataMap["CronSchedule"] = template.CronSchedule;
                    job.JobDataMap["TimeOut"] = template.Timeout;

                    ITrigger trigger = TriggerBuilder.Create()
                        .WithIdentity(job.JobDataMap["ID"].ToString(), job.JobDataMap["Group"].ToString())
                        .WithCronSchedule(job.JobDataMap["CronSchedule"].ToString())
                        .StartAt(DateTime.UtcNow)
                        .WithPriority(1)
                        .Build();

                    scheduler.ScheduleJob(job, trigger);
                    QuartzService.log("Job ID " + template.ID + " has been scheduled");
                }
                catch (Exception ex)
                {
                    QuartzService.log("Error scheduling " + template.Name + "\n" + ex.StackTrace);
                }
            }
        }

        /// <summary>
        /// Loads all jobs from SQLDB into a template List
        /// </summary>
        private void loadJobsFromDB()
        {
            templates = new List<JobTemplate>();
            try
            {
                DBJob dbJob = new DBJob();
                var dbJobQuery = from job in db.Jobs select job;
                List<DBJob> dbJobs = dbJobQuery.ToList();
                if (dbJobs != null)
                {
                    foreach (DBJob job in dbJobs)
                    {
                        JobTemplate template = new JobTemplate();
                        template.ID = job.JobId;
                        template.Name = job.JobName;
                        template.Group = job.JobGroup;
                        template.processPathStringsFromDB(job.Process, job.WorkingDirectory);
                        template.Arguments = job.Arguments;
                        template.WorkingDirectory = job.WorkingDirectory;
                        template.CronSchedule = job.CronSchedule;
                        template.Timeout = job.TimeOut.ToString();
                        templates.Add(template);
                    }
                }
            }
            catch(Exception ex)
            {
                QuartzService.log("Error loading Jobs from DB: " + "Exception\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// Schedules the jobs in the template list
        /// </summary>
        private void setUpJobs()
        {
            foreach (JobTemplate template in templates)
            {
                try
                {
                    IJobDetail job = JobBuilder.Create<Job>().Build();
                    job.JobDataMap["ID"] = template.ID.ToString();
                    job.JobDataMap["Name"] = template.Name;
                    //job.JobDataMap["Group"] = template.Group;
                    //Defaulting all to "Default" group for now
                    job.JobDataMap["Group"] = "Default";
                    job.JobDataMap["Process"] = template.Process;
                    job.JobDataMap["Arguments"] = template.Arguments;
                    job.JobDataMap["CronSchedule"] = template.CronSchedule;
                    job.JobDataMap["TimeOut"] = template.Timeout;

                    ITrigger trigger = TriggerBuilder.Create()
                        .WithIdentity(job.JobDataMap["ID"].ToString(), job.JobDataMap["Group"].ToString())
                        .WithCronSchedule(job.JobDataMap["CronSchedule"].ToString())
                        .StartAt(DateTime.UtcNow)
                        .WithPriority(1)
                        .Build();

                    scheduler.ScheduleJob(job, trigger);
                    QuartzService.log("Job ID " + template.ID + " has been scheduled");
                }
                catch(Exception ex)
                {
                    QuartzService.log("Error scheduling " + template.Name + "\n" + ex.StackTrace);
                }
            }
        }

        /// <summary>
        /// Rename json files to err
        /// </summary>
        /// <param name="InputString">File and path to be named to .err</param>
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
            catch (Exception ex)
            {
                QuartzService.log("Error renaming  " + InputString + " to .err Exception\n" + ex.StackTrace);
            }
        }
    }
}
