using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QuartzUI.AspNetCore.Extension;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuartzUI.AspNetCore.Data
{
    [ApiController]
    [Route("quartz/api/[action]")]
    public class QuartzController : ControllerBase
    {
        private readonly IQuartzData data;

        public QuartzController(IQuartzData quartzData)
        {
            data = quartzData;
        }

        [HttpPost]
        public string Login([FromForm] string loginUsername, [FromForm] string loginPassword)
        {
            IConfiguration configuration = GetService<IConfiguration>();

            var userName = configuration.GetValue<string>("Quart:UserName") ?? "quartz";
            var password = configuration.GetValue<string>("Quart:Password") ?? "123";

            var option = GetService<IOptionsSnapshot<QuartzUIOption>>().Value;
            if (userName == loginUsername && password == loginPassword)
            {
                Response.Cookies.Append(option.CookieKey, QuartzUIOption.GetCookieValue());
                return "success";
            }

            return "failed";
        }

        [HttpGet]
        public IEnumerable<TaskMaster> GetTasks() => data.GetTasks();

        /// <summary>
        /// 立即执行
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<string> StartJob(string jobId)
        {
            var task = data.GetTasks().FirstOrDefault(x => x.Key == jobId);
            if (task == null)
            {
                return "没有找到该任务";
            }

            var (status, msg) = await TaskCache.Emit(task.Key, TaskCacheStatusEnum.Start);
            return status == TaskCacheResultEnum.Success ? "立即执行成功" : msg;
        }

        /// <summary>
        /// 立即停止
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<string> StopJob(string jobId)
        {
            var task = data.GetTasks().FirstOrDefault(x => x.Key == jobId);
            if (task == null)
            {
                return "没有找到该任务";
            }

            var (status, msg) = await TaskCache.Emit(task.Key, TaskCacheStatusEnum.Stop);
            return status == TaskCacheResultEnum.Success ? "立即停止成功" : msg;
        }

        /// <summary>
        /// 开启任务
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<string> EnableTask(string jobId)
        {
            var task = data.GetTasks().FirstOrDefault(x => x.Key == jobId);
            if (task == null)
            {
                return "没有找到该任务";
            }
            task.Status = StatusEnum.Enabled;
            if (data.UpdateTask(task, UpdateTaskEnum.Status))
            {
                var (status, msg) = await TaskCache.Emit(task.Key, TaskCacheStatusEnum.Enabled);
                return status == TaskCacheResultEnum.Success ? "开启任务成功" : msg;
            }
            return "开启任务失败";
        }

        /// <summary>
        /// 禁用任务
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<string> DisableTask(string jobId)
        {
            var task = data.GetTasks().FirstOrDefault(x => x.Key == jobId);
            if (task == null)
            {
                return "没有找到该任务";
            }
            task.Status = StatusEnum.Disabled;
            if (data.UpdateTask(task, UpdateTaskEnum.Status))
            {
                var (status, msg) = await TaskCache.Emit(task.Key, TaskCacheStatusEnum.Disabled);
                return status == TaskCacheResultEnum.Success ? "停止任务成功" : msg;
            }
            return "停止任务失败";
        }

        [HttpGet]
        public TaskMaster GetTaskMasterByKey(string key)
        {
            return data.GetTasks().FirstOrDefault(x => x.Key == key);
        }

        [HttpPost]
        public async Task<string> SaveTaskMasterConfig([FromForm] TaskMaster master)
        {
            if (master == null)
            {
                return "参数不能为空";
            }

            master.Name = master.Name.Trim();
            master.Description = master.Description.Trim();
            master.CronExpression = master.CronExpression.Trim();

            var flag = data.UpdateTask(master, UpdateTaskEnum.Config);
            if (flag)
            {
                var (status, msg) = await TaskCache.Emit(master.Key, TaskCacheStatusEnum.Config);

                return status == TaskCacheResultEnum.Success ? "success" : msg;
            }

            return "保存失败";
        }

        [HttpGet]
        public PageList<TaskLog> GetTaskLogByKey(string key, int pageIndex, int pageSize)
        {
            return data.GetTaskLogByKey(key, pageIndex, pageSize);
        }

        [HttpGet]
        public string DeleteTaskLogById(string id)
        {
            return data.DeleteTaskLogById(id) ? "success" : "删除失败";
        }

        [HttpGet]
        public async Task<string> ResetAll()
        {
            var flag = data.DeleteTask(null);
            if (flag)
            {
                var (status, msg) = await TaskCache.Emit(null, TaskCacheStatusEnum.ResetAll);
                if (status == TaskCacheResultEnum.Success)
                {
                    data.DeleteTaskLogByKey(null);
                    return "success";
                }
                return msg;
            }
            return "重置失败";
        }

        [HttpGet]
        public string ResetLog(string key)
        {
            return data.DeleteTaskLogByKey(key) > 0 ? "success" : "删除失败";
        }

        protected T GetService<T>() => HttpContext.RequestServices.GetRequiredService<T>();
    }
}