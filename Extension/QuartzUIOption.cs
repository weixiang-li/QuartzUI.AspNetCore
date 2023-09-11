using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace QuartzUI.AspNetCore.Extension
{
    public class QuartzUIOption
    {
        private static readonly List<string> cookieValues = new List<string>();

        /// <summary>
        /// cookie key
        /// </summary>
        public string CookieKey { get; set; } = "qzcookie";

        public static string GetCookieValue()
        {
            var guid = Guid.NewGuid().ToString().Replace("-", "");
            cookieValues.Add(guid);
            return guid;
        }

        public bool IsContainCookie(IRequestCookieCollection cookies)
        {
            var val = cookies[CookieKey];
            return cookieValues.Contains(val);
        }

        /// <summary>
        /// Gets or sets a route prefix for accessing the quartz-ui
        /// </summary>
        public string RoutePrefix { get; set; } = "quartz";

        /// <summary>
        /// Gets or sets a Stream function for retrieving the quartz-ui page
        /// </summary>
        public Func<Stream> IndexStream { get; set; } = () => typeof(QuartzUIOption).GetTypeInfo().Assembly
            .GetManifestResourceStream("QuartzUI.AspNetCore.quartz_ui.index.html");

        /// <summary>
        /// 过期时间（分钟）
        /// </summary>
        public int CookieExpiredMinute { get; set; } = 60;
    }
}