using System;
using System.Collections.Generic;
using QuartzUI.AspNetCore.Data;

namespace QuartzUI.AspNetCore
{
    public interface IQuartzData
    {
        List<TaskMaster> GetTasks();

        bool AddTask(TaskMaster master);

        bool UpdateTask(TaskMaster master, UpdateTaskEnum update = UpdateTaskEnum.RunStatus);

        bool DeleteTask(string key);

        bool AddTaskLog(TaskLog log);

        /// <summary>
        /// 获取日志
        /// </summary>
        /// <param name="key">任务主键</param>
        /// <param name="pageIndex">页码，从1开始</param>
        /// <param name="pageSize">分页数</param>
        /// <returns></returns>
        PageList<TaskLog> GetTaskLogByKey(string key, int pageIndex, int pageSize);

        bool DeleteTaskLogById(string id);

        int DeleteTaskLogByKey(string key);
    }

    public class PageList<T> where T : class, new()
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 1000;
        public int PageCount => (int)Math.Ceiling(TotalCount / PageSize);
        public decimal TotalCount { get; set; } = 0;
        public List<T> Data { get; set; }
    }

    public enum UpdateTaskEnum
    {
        Status,
        RunStatus,
        Config
    }
}