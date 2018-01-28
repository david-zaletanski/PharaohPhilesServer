using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PharaohPhilesServer.PhilesProtocol.PhilesJobs;

namespace PharaohPhilesServer.PhilesProtocol
{
    class PhilesJobManager
    {
        private int JobNumberCounter;
        private Dictionary<int, PhilesJob> Jobs;

        public PhilesJobManager()
        {
            JobNumberCounter = 1;
            Jobs = new Dictionary<int, PhilesJob>();
        }

        public int AddJob(PhilesJobType jType)
        {
            int jNumber = GetNextJobNumber();
            PhilesJob nJob = null;
            if (jType == PhilesJobType.JOB_PHILES)
                nJob = new PhilesJob();
            else if (jType == PhilesJobType.JOB_CLIENT_DIRECTORY_LISTING)
                nJob = new ClientDirectoryRequestJob(null);
            else if (jType == PhilesJobType.JOB_CLIENT_FILE_UPLOAD)
                nJob = new ClientFileUploadJob(null);
            else if (jType == PhilesJobType.JOB_CLIENT_PHILES)
                nJob = new ClientPhilesJob();
            else if (jType == PhilesJobType.JOB_SERVER_DIRECTORY_LISTING)
                nJob = new ServerDirectoryRequestJob();
            else if (jType == PhilesJobType.JOB_SERVER_FILE_UPLOAD)
                nJob = new ServerFileUploadJob();
            nJob.OnJobComplete += new PhilesJob.JobCompleteDelegate(nJob_OnJobComplete);
            nJob.JobNumber = jNumber;
            Jobs.Add(jNumber,nJob);
            return jNumber;
        }

        public int AddJob(PhilesJobType jType, object[] param)
        {
            int jNumber = GetNextJobNumber();
            PhilesJob nJob = null;
            if (jType == PhilesJobType.JOB_PHILES)
                nJob = new PhilesJob();
            else if (jType == PhilesJobType.JOB_CLIENT_DIRECTORY_LISTING)
                nJob = new ClientDirectoryRequestJob(param);
            else if (jType == PhilesJobType.JOB_CLIENT_FILE_UPLOAD)
                nJob = new ClientFileUploadJob(param);
            else if (jType == PhilesJobType.JOB_CLIENT_PHILES)
                nJob = new ClientPhilesJob();
            else if (jType == PhilesJobType.JOB_SERVER_DIRECTORY_LISTING)
                nJob = new ServerDirectoryRequestJob();
            else if (jType == PhilesJobType.JOB_SERVER_FILE_UPLOAD)
                nJob = new ServerFileUploadJob();
            nJob.OnJobComplete += new PhilesJob.JobCompleteDelegate(nJob_OnJobComplete);
            nJob.JobNumber = jNumber;
            Jobs.Add(jNumber, nJob);
            return jNumber;
        }

        private int GetNextJobNumber()
        {
            int nJobNumber = JobNumberCounter;
            JobNumberCounter++;
            return nJobNumber;
        }

        private void nJob_OnJobComplete(int jNumber)
        {
            // Remove completed job from dictionary.
            Jobs.Remove(jNumber);
        }

        private PhilesJob getJob(int jNumber)
        {
            try
            {
                PhilesJob j = Jobs[jNumber];
                return j;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        // Overload index operator
        public PhilesJob this[int jNumber]
        {
            get
            {
                return getJob(jNumber);
            }
        }
    }

    public enum PhilesJobType
    {
        JOB_PHILES,
        JOB_SERVER_DIRECTORY_LISTING,
        JOB_SERVER_FILE_UPLOAD,
        JOB_CLIENT_PHILES,
        JOB_CLIENT_DIRECTORY_LISTING,
        JOB_CLIENT_FILE_UPLOAD
    }
}
