using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using HzQxj.Quartz.Entity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;
using SqlSugar;
using WeChat.SDK;

namespace HzQxj.Quartz
{
    [DisallowConcurrentExecution]
    [PersistJobDataAfterExecution]
    public class HzQxjJob : IJob
    {
        private DbContext dbContext = new DbContext();
        public async Task Execute(IJobExecutionContext context)
        {
        
            WebClient webClient = new WebClient();
            string content = webClient.DownloadString("http://www.hzqx.com/hztq/data/QxyjxxInfo.xml");
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(content);
            XmlNodeList xnList = doc.SelectNodes("//WeiXin");
            for (int i = 0; i < xnList.Count; i++)
            {
                if (xnList[i].SelectSingleNode("YjName").InnerText.Contains("杭州市"))
                {
                    string w_text = xnList[i].SelectSingleNode("YjNr").InnerText;
                    //类别
                    string category = "";
                    if (xnList[i].SelectSingleNode("YjName").InnerText.Contains("发布"))
                    {
                        Match mcat = Regex.Match(xnList[i].SelectSingleNode("YjName").InnerText, @"发布\S+色");
                        category = mcat.Groups[0].Value.Substring(2, mcat.Groups[0].Value.Length - 4);
                    }
                    else
                    {
                        Match mcat = Regex.Match(xnList[i].SelectSingleNode("YjName").InnerText, @"解除\S+色");
                        category = mcat.Groups[0].Value.Substring(2, mcat.Groups[0].Value.Length - 4);
                    }
                    //获取预警级别
                    Match mlevel = Regex.Match(xnList[i].SelectSingleNode("YjName").InnerText, @"\S色");
                    string level = mlevel.Groups[0].Value;
                    //发布日期
                    Match m = Regex.Match(xnList[i].SelectSingleNode("YjName").InnerText, @"[\d]{4}\S+[\d]分");
                    string w_ldatetime = m.Groups[0].Value;
                    string w_title = xnList[i].SelectSingleNode("YjName").InnerText.Replace("\r\n", "").Replace("\"", "'");
                    if (IsAllowExcute(m.Groups[0].Value))
                    {
                        string appid = "wx201b58af0d19763b";
                        string secret = "fc6dd99763c170b7937515f220ebda42";
                        var getToken = string.Format("https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={0}&secret={1}", appid, secret);
                        WebClient webGetToken = new WebClient();
                        var tokenObject = JsonConvert.DeserializeObject<AccessTokenJson>(webGetToken.DownloadString(getToken));
                        string token = tokenObject.access_token;
                        string getOpenidListUrl = "https://api.weixin.qq.com/cgi-bin/user/get?access_token=" + token + "&next_openid=";
                        WebClient webClientOpenId = new WebClient();
                        string resOpenidList = webClientOpenId.DownloadString(getOpenidListUrl);
                        WeChatUsers weChatUsers = (WeChatUsers)JsonConvert.DeserializeObject(resOpenidList, typeof(WeChatUsers));
                        weChatUsers.total = weChatUsers.total - 1;
                        weChatUsers = GetOpenidList(weChatUsers, token);
                        List<string> list = weChatUsers.data.openid;
                        string templateId = "";
                        string newData;
                        if (!w_title.Contains("解除"))
                        {
                            //气象灾害预警提醒
                            templateId = "yQaoXyMTjeY1uNVdVa0d4qQ_qJWdK16d9RfKbFeJIsQ";
                            var data = new WarningNoticeTemplate()
                            {
                                first = new TemplateDataItem(w_title),
                                alarm_unit = new TemplateDataItem("杭州市气象台"),
                                alarm_type = new TemplateDataItem(category),
                                alarm_level = new TemplateDataItem(level),
                                alarm_time = new TemplateDataItem(w_ldatetime),
                                remark = new TemplateDataItem(w_text)
                            };
                            newData = JsonConvert.SerializeObject(data);
                        }
                        else
                        {
                            //气象灾害预警解除提醒
                            templateId = "dltXNCOacQXAKf5F5oDaz9wuSJ8vAzFBXeDg6vRckRM";
                            var data = new RemoveWarningTemplate()
                            {
                                first = new TemplateDataItem(w_title),
                                keyword1 = new TemplateDataItem("杭州市气象台"),
                                keyword2 = new TemplateDataItem(category),
                                keyword3 = new TemplateDataItem(level),
                                keyword4 = new TemplateDataItem(w_ldatetime),
                                remark = new TemplateDataItem(w_text)
                            };

                            newData = JsonConvert.SerializeObject(data);
                        }
                        DateTime stTime = DateTime.Now;
                        foreach (var item in list)
                        {
                            var msgData = new TempleteModel()
                            {
                                touser = item,
                                template_id = templateId,
                                topcolor = "#FF0000",
                                url = "",
                                data = newData
                            };
                            string jsonString = JsonConvert.SerializeObject(msgData);
                            string urlFormat = string.Format("https://api.weixin.qq.com/cgi-bin/message/template/send?access_token={0}", token);
                            WebClient webClientNotify = new WebClient();
                            webClientNotify.UploadStringCompleted += new UploadStringCompletedEventHandler(
                                delegate (object sender, UploadStringCompletedEventArgs args)
                                {
                                    DbContext db = new DbContext();
                                    HzQxj_SendLog hzQxjSendLog = new HzQxj_SendLog();
                                    hzQxjSendLog.Content = jsonString;
                                    hzQxjSendLog.Openid = urlFormat;
                                    hzQxjSendLog.ErrMsg = "";
                                    hzQxjSendLog.Result = "";
                                    if (args.Error != null)
                                    {
                                        hzQxjSendLog.ErrMsg = args.Error.Message;
                                    }
                                    else
                                    {
                                        hzQxjSendLog.Result = args.Result;
                                    }
                                    db.SugarDatabase.Insertable(hzQxjSendLog).ExecuteCommand();
                                });
                            webClientNotify.UploadStringAsync(new Uri(urlFormat), jsonString);
                        }
                        DateTime endTime = DateTime.Now;
                        Console.WriteLine(endTime.Subtract(stTime).TotalMilliseconds);
                        HzQxj_ExcuteLog excuteLog = new HzQxj_ExcuteLog();
                        excuteLog.ExcuteTime = m.Groups[0].Value;
                        dbContext.SugarDatabase.Insertable(excuteLog).ExecuteCommand();
                    }
                    else
                    {

                    }
                }
            }
        }
        /// <summary>
        /// 是否允许执行
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public bool IsAllowExcute(string time)
        {
            var model = dbContext.SugarDatabase.Queryable<HzQxj_ExcuteLog>().First(a => a.ExcuteTime == time);
            if (model == null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public WeChatUsers GetOpenidList(WeChatUsers weChatUsers, string access_token)
        {
            if (weChatUsers.total > weChatUsers.count)
            {
                string getOpenidListUrl = "https://api.weixin.qq.com/cgi-bin/user/get?access_token=" + access_token +
                                          "&next_openid=" + weChatUsers.next_openid;
                WebClient webClient = new WebClient();
                string res = webClient.DownloadString(getOpenidListUrl);
                WeChatUsers weChatUsers2 = (WeChatUsers)JsonConvert.DeserializeObject(res, typeof(WeChatUsers));
                weChatUsers.count = weChatUsers.count + weChatUsers2.count;
                weChatUsers.data.openid.AddRange(weChatUsers2.data.openid);
                weChatUsers.next_openid = weChatUsers2.next_openid;
                GetOpenidList(weChatUsers, access_token);
            }
            return weChatUsers;
        }
        public class Data
        {
            public List<string> openid;
        }
        public class WeChatUsers
        {
            public int total;
            public int count;
            public Data data;
            public string next_openid;
        }
        public class WarningNoticeTemplate
        {
            public TemplateDataItem first { get; set; }
            public TemplateDataItem alarm_unit { get; set; }
            public TemplateDataItem alarm_type { get; set; }
            public TemplateDataItem alarm_level { get; set; }
            public TemplateDataItem alarm_time { get; set; }
            public TemplateDataItem remark { get; set; }
        }
        public class RemoveWarningTemplate
        {
            public TemplateDataItem first { get; set; }
            public TemplateDataItem keyword1 { get; set; }
            public TemplateDataItem keyword2 { get; set; }
            public TemplateDataItem keyword3 { get; set; }
            public TemplateDataItem keyword4 { get; set; }
            public TemplateDataItem remark { get; set; }
        }
    }
}
