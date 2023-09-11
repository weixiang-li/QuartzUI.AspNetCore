using Quartz;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuartzUI.AspNetCore.Data
{
    [Serializable]
    public class TaskMaster
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CronExpression { get; set; }
        public StatusEnum Status { get; set; }
        public RunStatusEnum RunStatus { get; set; }
        public DateTime? LastRunTime { get; set; }
        public string ErrorMsg { get; set; }
    }

    [Serializable]
    public enum StatusEnum
    {
        Disabled,
        Enabled
    }

    [Serializable]
    public enum RunStatusEnum
    {
        Default,
        Errored,
        Running
    }
}