using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuartzService
{
    class JobRecordUpdater
    {
        QuartzEntities db;

        /// <summary>
        /// Constructor
        /// </summary>
        public JobRecordUpdater()
        {
            db = new QuartzEntities();
        }

        public int addNewRecord(int JobID, String JobName, DateTime startTime)
        {
            int iRecordID = 0;
            JobRecord jobRecord = new JobRecord();
            try
            {
                jobRecord.JobId = JobID;
                jobRecord.JobName = JobName;
                jobRecord.BeginTime = startTime;
                db.JobRecords.Add(jobRecord);
                db.SaveChanges();
                iRecordID = jobRecord.RecordId;
            }
            catch(Exception ex)
            {
                QuartzService.log("Error adding job record for JobID " + JobID + " to the JobRecords DB\n" + ex);
            }
            return iRecordID;
        }

        public bool setRecordEnding(int RecordID, DateTime endTime, int timedOut)
        {
            bool bSuccessful = false;
            try
            {
                var jobRecordQuery = from jobRecord in db.JobRecords
                               where jobRecord.RecordId == RecordID
                               select jobRecord;
                JobRecord record = jobRecordQuery.Single();
                record.EndTime = endTime;
                record.TimedOut = timedOut;
                db.SaveChanges();
                bSuccessful = true;
            }
            catch(Exception ex)
            {
                QuartzService.log("Error adding job record ending time for RecordID " + RecordID + " to the JobRecords DB\n" + ex);
            }
            return bSuccessful;
        }
    }
}
