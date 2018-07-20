using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Quartz;
using Lampon.QuartzWeb.Models;

namespace Lampon.QuartzWeb.QuartzCore.Jobs
{
    [DisallowConcurrentExecution]
    [PersistJobDataAfterExecution]
    public class HttpJobTask : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var requestUrl = context.JobDetail.JobDataMap.GetString(Constant.REQUESTURL);
            requestUrl = requestUrl.IndexOf("http") == 0 ? requestUrl : "http://" + requestUrl;
            try
            {
                WebClient webClient = new WebClient();
                var result = webClient.DownloadString(requestUrl);
                context.JobDetail.JobDataMap[Constant.EXCEPTION] = null;
            }
            catch (Exception ex)
            {
                context.JobDetail.JobDataMap[Constant.EXCEPTION] = $"Time:{DateTime.Now} Url:{requestUrl} Err:{ex.Message}";
            }
        }
    }
}