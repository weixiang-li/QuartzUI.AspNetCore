using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzUI.AspNetCore;
using QuartzUI.AspNetCore.Data;
using QuartzUI.AspNetCore.Extension;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class QuartzUIExtension
    {
        /// <summary>
        /// 添加QuartzUI配置
        /// </summary>
        public static IServiceCollection AddQuartzUI(this IServiceCollection services)
        {
            services.AddScoped<IQuartzData, MemoryQuartzData>();
            return services.AddQuartzHost();
        }

        /// <summary>
        /// 添加QuartzUI配置
        /// </summary>
        public static IServiceCollection AddQuartzUI<TImplementation>(this IServiceCollection services) where TImplementation : class, IQuartzData
        {
            services.AddScoped<IQuartzData, TImplementation>();
            return services.AddQuartzHost();
        }

        private static IServiceCollection AddQuartzHost(this IServiceCollection services)
        {
            AppDomain.CurrentDomain.GetAssemblies().Where(asm => asm != Assembly.GetExecutingAssembly())
                .SelectMany(t => t.DefinedTypes)
                .Where(a => a.IsClass && !a.IsAbstract && a.IsSubclassOf(typeof(JobService)))
                .ToList()
                .ForEach(type =>
                {
                    services.AddScoped(type);
                    services.AddScoped(typeof(IJobService), type);
                });

            return services.AddSingleton<IJobFactory, SingletonJobFactory>()
                .AddSingleton<ISchedulerFactory, StdSchedulerFactory>()
                .AddHostedService<QuartzHostedService>();
        }
    }
}

namespace Microsoft.AspNetCore.Builder
{
    public static class QuartzUIExtension
    {
        /// <summary>
        /// Register the QuartzUI middleware with provided options
        /// </summary>
        public static IApplicationBuilder UseQuartzUI(this IApplicationBuilder app, QuartzUIOption options)
        {
            return app.UseMiddleware<QuartzUIMiddleware>(options);
        }

        /// <summary>
        /// Register the QuartzUI middleware with optional setup action for DI-injected options
        /// </summary>
        public static IApplicationBuilder UseQuartzUI(
            this IApplicationBuilder app,
            Action<QuartzUIOption> setupAction = null)
        {
            QuartzUIOption options;
            using (var scope = app.ApplicationServices.CreateScope())
            {
                options = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<QuartzUIOption>>().Value;
                setupAction?.Invoke(options);
            }

            return app.UseQuartzUI(options);
        }
    }
}