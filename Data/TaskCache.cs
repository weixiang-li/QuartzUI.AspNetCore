using Quartz;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuartzUI.AspNetCore.Data
{
    internal class TaskCache
    {
        private static readonly Stack<TaskCacheItem> taskCaches = new Stack<TaskCacheItem>();

        public static async Task<(TaskCacheResultEnum, string)> Emit(string key, TaskCacheStatusEnum status)
        {
            var item = new TaskCacheItem()
            {
                Key = key,
                Status = status,
            };

            taskCaches.Push(item);

            while (taskCaches.Contains(item) || item.Result == TaskCacheResultEnum.Default)
            {
                await Task.Delay(10);
            }

            return (item.Result, item.Msg);
        }

        public static int Count => taskCaches.Count;

        public static TaskCacheItem Pop()
        {
            if (taskCaches.Count == 0)
            {
                return null;
            }
            var item = taskCaches.Pop();
            return item;
        }
    }

    internal class TaskCacheItem
    {
        public string Key { get; set; }
        public TaskCacheStatusEnum Status { get; set; }

        public TaskCacheResultEnum Result { get; protected set; } = TaskCacheResultEnum.Default;
        public string Msg { get; protected set; }

        public void SetResult(TaskCacheResultEnum result, string msg = null)
        {
            Result = result;
            Msg = msg;
        }
    }

    public enum TaskCacheStatusEnum
    {
        Disabled,
        Enabled,
        Start,
        Stop,
        Config,
        ResetAll
    }

    public enum TaskCacheResultEnum
    {
        Default,
        Success,
        Failed
    }
}