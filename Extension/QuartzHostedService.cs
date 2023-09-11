using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Spi;
using QuartzUI.AspNetCore.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuartzUI.AspNetCore.Extension
{
    internal class QuartzHostedService : BackgroundService
    {
        private readonly ISchedulerFactory SchedulerFactory;
        private readonly IJobFactory JobFactory;
        private readonly IQuartzData DataAccess;
        private readonly IServiceProvider Services;
        private readonly ILogger<QuartzHostedService> Logger;

        private IScheduler Scheduler { get; set; }

        private static readonly IDictionary<string, Type> TypeDict = new Dictionary<string, Type>();

        public QuartzHostedService(
            ISchedulerFactory schedulerFactory,
            IJobFactory jobFactory,
            IQuartzData dataAccess,
            IServiceProvider services,
            ILogger<QuartzHostedService> logger)
        {
            SchedulerFactory = schedulerFactory;
            JobFactory = jobFactory;
            DataAccess = dataAccess;
            Services = services;
            Logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogDebug($"Quartz服务已经启动");
                InitTask();

                Scheduler = await SchedulerFactory.GetScheduler(cancellationToken);
                Scheduler.JobFactory = JobFactory;

                var taskList = DataAccess.GetTasks();
                foreach (var task in taskList)
                {
                    try
                    {
                        var job = QuartzHostedService.CreateJob(task);
                        await Scheduler.AddJob(job, true, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Quartz启动任务时错误");
                        try
                        {
                            task.RunStatus = RunStatusEnum.Errored;
                            task.LastRunTime = DateTime.Now;
                            task.ErrorMsg = $"{ex.Message}\r\n{ex.StackTrace}";
                            DataAccess.UpdateTask(task, UpdateTaskEnum.RunStatus);
                        }
                        catch { }
                    }
                }

                foreach (var task in taskList.Where(x => x.Status == StatusEnum.Enabled))
                {
                    try
                    {
                        var trigger = QuartzHostedService.CreateTrigger(task);
                        await Scheduler.ScheduleJob(trigger, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Quartz启动任务时错误");
                        try
                        {
                            task.RunStatus = RunStatusEnum.Errored;
                            task.LastRunTime = DateTime.Now;
                            task.ErrorMsg = $"{ex.Message}\r\n{ex.StackTrace}";
                            DataAccess.UpdateTask(task, UpdateTaskEnum.RunStatus);
                        }
                        catch { }
                    }
                }

                Scheduler.ListenerManager.AddJobListener(new JobLogListener(Services));

                _ = Scheduler.Start(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Quartz服务错误");
            }

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(30, stoppingToken);
                    while (TaskCache.Count > 0)
                    {
                        var act = TaskCache.Pop();
                        if (act != null)
                        {
                            try
                            {
                                #region 重启

                                if (act.Status == TaskCacheStatusEnum.ResetAll)
                                {
                                    InitTask();

                                    foreach (var reTask in DataAccess.GetTasks().Where(x => x.Status == StatusEnum.Enabled))
                                    {
                                        try
                                        {
                                            var trigger = QuartzHostedService.CreateTrigger(reTask);
                                            await Scheduler.ScheduleJob(trigger, stoppingToken);
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.LogError(ex, "Quartz启动任务时错误");
                                            try
                                            {
                                                reTask.RunStatus = RunStatusEnum.Errored;
                                                reTask.LastRunTime = DateTime.Now;
                                                reTask.ErrorMsg = $"{ex.Message}\r\n{ex.StackTrace}";
                                                DataAccess.UpdateTask(reTask, UpdateTaskEnum.RunStatus);
                                            }
                                            catch { }
                                        }
                                    }

                                    continue;
                                }

                                #endregion 重启

                                var taskList = DataAccess.GetTasks();

                                var task = taskList.FirstOrDefault(x => x.Key == act.Key);
                                if (task == null)
                                {
                                    act.SetResult(TaskCacheResultEnum.Failed, "没有找到对应的任务类型");
                                    Logger.LogError($"Quartz服务，没有找到对应的job:{act.Key}");
                                }
                                else
                                {
                                    switch (act.Status)
                                    {
                                        case TaskCacheStatusEnum.Disabled:
                                            {
                                                var triggerKey = new TriggerKey(act.Key);
                                                if (await Scheduler.CheckExists(triggerKey, stoppingToken))
                                                {
                                                    await Scheduler.PauseTrigger(triggerKey, stoppingToken);
                                                    await Scheduler.UnscheduleJob(triggerKey, stoppingToken);
                                                }
                                                if (task.Status != StatusEnum.Disabled)
                                                {
                                                    act.SetResult(TaskCacheResultEnum.Success);
                                                }
                                                else
                                                {
                                                    act.SetResult(TaskCacheResultEnum.Failed, "当前任务已停止");
                                                }
                                            }
                                            break;

                                        case TaskCacheStatusEnum.Enabled:
                                            {
                                                var triggerKey = new TriggerKey(act.Key);
                                                if (!await Scheduler.CheckExists(triggerKey, stoppingToken))
                                                {
                                                    var trigger = QuartzHostedService.CreateTrigger(task);
                                                    await Scheduler.ScheduleJob(trigger, stoppingToken);
                                                }
                                                if (task.Status != StatusEnum.Enabled)
                                                {
                                                    act.SetResult(TaskCacheResultEnum.Success);
                                                }
                                                else
                                                {
                                                    act.SetResult(TaskCacheResultEnum.Failed, "当前任务正常");
                                                }
                                            }
                                            break;

                                        case TaskCacheStatusEnum.Start:
                                            {
                                                var jobKey = JobKey.Create(act.Key);
                                                await Scheduler.TriggerJob(jobKey, stoppingToken);

                                                act.SetResult(TaskCacheResultEnum.Success);
                                            }
                                            break;

                                        case TaskCacheStatusEnum.Stop:
                                            {
                                                var jobKey = JobKey.Create(act.Key);
                                                await Scheduler.Interrupt(jobKey, stoppingToken);

                                                act.SetResult(TaskCacheResultEnum.Success);
                                            }
                                            break;

                                        case TaskCacheStatusEnum.Config:
                                            {
                                                if (task.Status == StatusEnum.Enabled)
                                                {
                                                    var triggerKey = new TriggerKey(act.Key);
                                                    if (await Scheduler.CheckExists(triggerKey, stoppingToken))
                                                    {
                                                        await Scheduler.PauseTrigger(triggerKey, stoppingToken);
                                                        await Scheduler.UnscheduleJob(triggerKey, stoppingToken);
                                                    }

                                                    if (!await Scheduler.CheckExists(triggerKey, stoppingToken))
                                                    {
                                                        var trigger = QuartzHostedService.CreateTrigger(task);
                                                        await Scheduler.ScheduleJob(trigger, stoppingToken);
                                                        act.SetResult(TaskCacheResultEnum.Success);
                                                    }
                                                    else
                                                    {
                                                        act.SetResult(TaskCacheResultEnum.Failed, "任务已存在，无法删除");
                                                    }
                                                }
                                                else
                                                {
                                                    act.SetResult(TaskCacheResultEnum.Success, "禁用，不需要改任务");
                                                }
                                            }
                                            break;

                                        default:
                                            act.SetResult(TaskCacheResultEnum.Failed);
                                            break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                act.SetResult(TaskCacheResultEnum.Failed, "执行错误，" + ex.Message);
                                Logger.LogError(ex, $"Quartz服务错误，job:{act.Key}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Quartz服务错误");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await Scheduler?.Shutdown(cancellationToken);
            Logger.LogDebug($"Quartz服务已经停止");
            await base.StopAsync(cancellationToken);
        }

        private void InitTask()
        {
            var tasks = DataAccess.GetTasks();
            var js = Services.GetServices<IJobService>();
            foreach (var item in js)
            {
                var type = item.GetType();
                if (!TypeDict.ContainsKey(type.FullName))
                {
                    TypeDict.Add(type.FullName, type);
                }

                if (!tasks.Exists(x => x.Key == type.FullName))
                {
                    DataAccess.AddTask(new TaskMaster()
                    {
                        Key = type.FullName,
                        Name = item.Name,
                        Description = item.Description,
                        CronExpression = item.CronExpression,
                    });
                }
            }
        }

        private static IJobDetail CreateJob(TaskMaster master)
        {
            if (TypeDict.TryGetValue(master.Key, out var jobType))
            {
                return JobBuilder
                    .Create(jobType)
                    .WithIdentity(master.Key)
                    .WithDescription(master.Name)
                    .StoreDurably(true)
                    .Build();
            }
            else
            {
                throw new InvalidOperationException("没有找到任务对应的类型");
            }
        }

        private static ITrigger CreateTrigger(TaskMaster master)
        {
            return TriggerBuilder
                .Create()
                .ForJob(JobKey.Create(master.Key))
                .WithIdentity(master.Key)
                .StartNow()
                .WithCronSchedule(master.CronExpression)
                .WithDescription(master.CronExpression)
                .UsingJobData("Type", "定时任务")
                .Build();
        }

        public class JobLogListener : IJobListener
        {
            private readonly IServiceProvider Services;

            public JobLogListener(IServiceProvider services)
            {
                Services = services;
            }

            public string Name => "JobLogListener";

            public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
            {
                Console.WriteLine($"此次任务{context.JobDetail.Key.Name}被忽略...");
                return Task.CompletedTask;
            }

            public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
            {
                var key = context.JobDetail.Key.Name;

                using (var scope = Services.CreateScope())
                {
                    var dataAccess = scope.ServiceProvider.GetService<IQuartzData>();
                    dataAccess.UpdateTask(new TaskMaster() { Key = key, RunStatus = RunStatusEnum.Running, LastRunTime = DateTime.Now, ErrorMsg = null }, UpdateTaskEnum.RunStatus);

                    var log = new TaskLog()
                    {
                        Id = Guid.NewGuid().ToString().Replace("-", ""),
                        Key = key,
                        StartTime = DateTime.Now,
                    };
                    dataAccess.AddTaskLog(log);

                    context.Put("log", log);
                }

                return Task.CompletedTask;
            }

            public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default)
            {
                var key = context.JobDetail.Key.Name;

                var task = new TaskMaster() { Key = key };

                var log = context.Get("log") as TaskLog;

                log.EndTime = DateTime.Now;

                if (jobException != null)
                {
                    log.Status = ResultStatusEnum.Exception;
                    log.Msg = $"{jobException.Message}\r\n{jobException.StackTrace}";

                    task.RunStatus = RunStatusEnum.Errored;
                    task.ErrorMsg = log.Msg;
                }
                else
                {
                    var msg = Convert.ToString(context.Get("Result"));
                    log.Status = bool.TryParse(msg, out var f) && f == true ? ResultStatusEnum.Success : ResultStatusEnum.Failed;
                    log.Msg = $"{context.Trigger.JobDataMap.Get("Type")}：{Convert.ToString(context.Get("Msg"))}".Trim('：');

                    task.RunStatus = log.Status == ResultStatusEnum.Success ? RunStatusEnum.Default : RunStatusEnum.Errored;
                    task.ErrorMsg = log.Msg;
                }

                using (var scope = Services.CreateScope())
                {
                    var dataAccess = scope.ServiceProvider.GetService<IQuartzData>();

                    dataAccess.DeleteTaskLogById(log.Id);
                    dataAccess.AddTaskLog(log);
                    dataAccess.UpdateTask(task, UpdateTaskEnum.RunStatus);
                }

                return Task.CompletedTask;
            }
        }
    }
}