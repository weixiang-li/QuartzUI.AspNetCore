using Quartz.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuartzUI.AspNetCore.Data
{
    internal class MemoryQuartzData : IQuartzData
    {
        private static readonly List<TaskMaster> taskList = new List<TaskMaster>();
        private static readonly List<TaskLog> logList = new List<TaskLog>();

        public bool AddTask(TaskMaster master)
        {
            taskList.Add(master);
            return true;
        }

        public bool AddTaskLog(TaskLog log)
        {
            logList.Add(log);
            return true;
        }

        public bool DeleteTask(string key)
        {
            return taskList.RemoveAll(x => x.Key == key || string.IsNullOrEmpty(key)) > 0;
        }

        public bool DeleteTaskLogById(string id)
        {
            return logList.RemoveAll(x => x.Id == id) > 0;
        }

        public int DeleteTaskLogByKey(string key)
        {
            return logList.RemoveAll(x => x.Key == key || string.IsNullOrEmpty(key));
        }

        public PageList<TaskLog> GetTaskLogByKey(string key, int pageIndex, int pageSize)
        {
            return new PageList<TaskLog>()
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = logList.Where(x => x.Key == key || string.IsNullOrEmpty(key)).Count(),
                Data = logList.Where(x => x.Key == key || string.IsNullOrEmpty(key)).OrderBy(x => x.StartTime).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList(),
            };
        }

        public List<TaskMaster> GetTasks()
        {
            return taskList;
        }

        public bool UpdateTask(TaskMaster master, UpdateTaskEnum update = UpdateTaskEnum.RunStatus)
        {
            var tmp = taskList.FirstOrDefault(x => x.Key == master.Key);
            if (tmp != null)
            {
                switch (update)
                {
                    case UpdateTaskEnum.Status:
                        tmp.Status = master.Status;
                        break;

                    case UpdateTaskEnum.RunStatus:
                        tmp.RunStatus = master.RunStatus;
                        tmp.LastRunTime = master.LastRunTime;
                        tmp.ErrorMsg = master.ErrorMsg;
                        break;

                    case UpdateTaskEnum.Config:
                        tmp.Name = master.Name;
                        tmp.Description = master.Description;
                        tmp.CronExpression = master.CronExpression;
                        break;

                    default:
                        break;
                }
                return true;
            }
            return false;
        }
    }
}