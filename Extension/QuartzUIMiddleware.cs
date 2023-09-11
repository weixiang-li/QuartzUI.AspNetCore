using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#if NETSTANDARD2_0

using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

#endif

namespace QuartzUI.AspNetCore.Extension
{
    internal class QuartzUIMiddleware
    {
        private const string EmbeddedFileNamespace = "QuartzUI.AspNetCore.quartz_ui";

        private readonly QuartzUIOption _options;
        private readonly StaticFileMiddleware _staticFileMiddleware;

        public QuartzUIMiddleware(
            RequestDelegate next,
            IWebHostEnvironment hostingEnv,
            ILoggerFactory loggerFactory,
            QuartzUIOption options)
        {
            _options = options ?? new QuartzUIOption();
            _staticFileMiddleware = QuartzUIMiddleware.CreateStaticFileMiddleware(next, hostingEnv, loggerFactory, options);
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var httpMethod = httpContext.Request.Method;
            var path = httpContext.Request.Path.Value;

            // If the RoutePrefix is requested (with or without trailing slash), redirect to index URL
            if (httpMethod == "GET" && Regex.IsMatch(path, $"^/?{Regex.Escape(_options.RoutePrefix)}/?$", RegexOptions.IgnoreCase))
            {
                // Use relative redirect to support proxy environments
                var relativeIndexUrl = string.IsNullOrEmpty(path) || path.EndsWith("/")
                    ? "index.html"
                    : $"{path.Split('/').Last()}/index.html";

                // 判断是否登录
                if (relativeIndexUrl.EndsWith("index.html") && !httpContext.Request.Cookies.ContainsKey(_options.CookieKey)
                    && !_options.IsContainCookie(httpContext.Request.Cookies))
                {
                    QuartzUIMiddleware.RespondWithRedirect(httpContext.Response, relativeIndexUrl.Replace("/index.html", "/login.html"));
                    return;
                }

                QuartzUIMiddleware.RespondWithRedirect(httpContext.Response, relativeIndexUrl);
                return;
            }

            if (httpMethod == "GET" && Regex.IsMatch(path, $"^/{Regex.Escape(_options.RoutePrefix)}/?index.html$", RegexOptions.IgnoreCase))
            {
                await RespondWithIndexHtml(httpContext.Response);
                return;
            }

            await _staticFileMiddleware.Invoke(httpContext);
        }

        private static StaticFileMiddleware CreateStaticFileMiddleware(
            RequestDelegate next,
            IWebHostEnvironment hostingEnv,
            ILoggerFactory loggerFactory,
            QuartzUIOption options)
        {
            var staticFileOptions = new StaticFileOptions
            {
                RequestPath = string.IsNullOrEmpty(options.RoutePrefix) ? string.Empty : $"/{options.RoutePrefix}",
                FileProvider = new EmbeddedFileProvider(typeof(QuartzUIMiddleware).GetTypeInfo().Assembly, EmbeddedFileNamespace),
            };

            return new StaticFileMiddleware(next, hostingEnv, Options.Create(staticFileOptions), loggerFactory);
        }

        private static void RespondWithRedirect(HttpResponse response, string location)
        {
            response.StatusCode = 301;
            response.Headers["Location"] = location;
        }

        private async Task RespondWithIndexHtml(HttpResponse response)
        {
            response.StatusCode = 200;
            response.ContentType = "text/html;charset=utf-8";

            using (var stream = _options.IndexStream())
            {
                using (var reader = new StreamReader(stream))
                {
                    await response.WriteAsync(await reader.ReadToEndAsync(), Encoding.UTF8);
                }
            }
        }
    }
}