using Quartz;
using System.Threading.Tasks;

namespace QuartzUI.AspNetCore
{
    internal interface IJobService : IJob
    {
        string Name { get; }
        string Description { get; }

        /// <summary>
        /// 执行周期， 默认每5秒执行一次 = "0/5 * * * * ?"
        /// </summary>
        string CronExpression { get; }
    }

    public abstract class JobService : IJobService
    {
        public abstract string Name { get; }

        public abstract string Description { get; }

        public abstract string CronExpression { get; }

        public Task Execute(IJobExecutionContext context)
        {
            return ExecuteTask(context).ContinueWith(async task =>
            {
                var result = await task;
                context.Put("Result", result.Result);
                context.Put("Msg", result.Msg);
            });
        }

        public abstract Task<JobExecuteResult> ExecuteTask(IJobExecutionContext context);

        public static Task<JobExecuteResult> Success(string msg)
        {
            return Task.FromResult(new JobExecuteResult()
            {
                Result = true,
                Msg = msg
            });
        }

        public static Task<JobExecuteResult> Failed(string msg)
        {
            return Task.FromResult(new JobExecuteResult()
            {
                Result = false,
                Msg = msg
            });
        }
    }

    public class JobExecuteResult
    {
        public bool Result { get; set; }
        public string Msg { get; set; }
    }
}