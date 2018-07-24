using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HzQxj.Quartz.Entity;
using Quartz;

namespace HzQxj.Quartz
{
    [DisallowConcurrentExecution]
    [PersistJobDataAfterExecution]
    public class HzQxjSendFailJob : IJob
    {
        private DbContext dbContext = new DbContext();

        public async Task Execute(IJobExecutionContext context)
        {
            var list = dbContext.SugarDatabase.Queryable<HzQxj_SendLog>().Where(a => a.ErrMsg != "").ToList();
            foreach (var item in list)
            {
                WebClient webClientNotify = new WebClient();
                webClientNotify.UploadStringCompleted += new UploadStringCompletedEventHandler(
                    delegate (object sender, UploadStringCompletedEventArgs args)
                    {
                        DbContext db = new DbContext();
                        item.Content = item.Content;
                        item.Openid = item.Openid;
                        item.ErrMsg = "";
                        item.Result = "";
                        if (args.Error != null)
                        {
                            item.ErrMsg = args.Error.Message;
                        }
                        else
                        {
                            item.Result = args.Result;
                        }
                        //else
                        //{
                        //    var result = JObject.Parse(e.Result).Properties().Select(item => item.Value.ToString()).ToArray();
                        //    if (result[0] != "0")
                        //    {
                        //        hzQxjSendLog.ErrMsg = e.Result;
                        //    }
                        //}
                        db.SugarDatabase.Updateable(item).ExecuteCommand();
                    });
                webClientNotify.UploadStringAsync(new Uri(item.Openid), item.Content);
            }
        }
    }
}
