using Quartz;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuartzUI.AspNetCore.Data
{
    public class TaskLog
    {
        public string Id { get; set; }
        public string Key { get; set; }

        /// <summary>
        /// 任务开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 任务结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        public ResultStatusEnum Status { get; set; }
        public string Msg { get; set; }
    }

    public enum ResultStatusEnum
    {
        Exception,
        Failed,
        Success
    }
}