using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Lampon.QuartzWeb.Models;
using Lampon.QuartzWeb.QuartzCore;
using Lampon.QuartzWeb.QuartzCore.Jobs;

namespace Lampon.QuartzWeb.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        /// 查询任务
        /// </summary>
        /// <param name="jobGroup"></param>
        /// <param name="jobName"></param>
        /// <returns></returns>
        public IActionResult QueryJob(string jobGroup, string jobName)
        {
            var model = SchedulerCenter.Instance.QueryJobAsync(jobGroup, jobName);
            return Json(new BaseResult { Code = MsgCode.Success, Data = model.Result }, new Newtonsoft.Json.JsonSerializerSettings() { DateFormatString = "yyyy-MM-dd HH:mm:ss" });
        }
        /// <summary>
        /// 添加任务
        /// </summary>
        /// <param name="schedule"></param>
        /// <returns></returns>
        public IActionResult AddJob(ScheduleEntity schedule)
        {

            var result = new BaseResult();
            if (schedule.JobType == 0)
            {
                result = SchedulerCenter.Instance.AddScheduleJobAsync<HttpJobTask>(schedule).Result;
            }
            else
            {
                result = SchedulerCenter.Instance.AddScheduleJobAsync(schedule).Result;
            }
            return Json(result, new Newtonsoft.Json.JsonSerializerSettings() { DateFormatString = "yyyy-MM-dd HH:mm:ss" });
        }
        /// <summary>
        /// 获取所有任务信息
        /// </summary>
        /// <returns></returns>
        public IActionResult GetAllJob()
        {
            var list = SchedulerCenter.Instance.GetAllJobAsync();
            return Json(new BaseResult { Code = MsgCode.Success, Data = list.Result }, new Newtonsoft.Json.JsonSerializerSettings() { DateFormatString = "yyyy-MM-dd HH:mm:ss" });
        }
        /// <summary>
        /// 暂停任务
        /// </summary>
        /// <param name="jobGroup"></param>
        /// <param name="jobName"></param>
        /// <returns></returns>
        public IActionResult PasuseJob(string jobGroup, string jobName)
        {
            var result = SchedulerCenter.Instance.StopOrDelScheduleJobAsync(jobGroup, jobName);
            return Json(result.Result, new Newtonsoft.Json.JsonSerializerSettings() { DateFormatString = "yyyy-MM-dd HH:mm:ss" });
        }
        /// <summary>
        /// 恢复任务
        /// </summary>
        /// <param name="jobGroup"></param>
        /// <param name="jobName"></param>
        /// <returns></returns>
        public IActionResult ResumeJob(string jobGroup, string jobName)
        {
            var result = SchedulerCenter.Instance.ResumeJobAsync(jobGroup, jobName);
            return Json(result.Result, new Newtonsoft.Json.JsonSerializerSettings() { DateFormatString = "yyyy-MM-dd HH:mm:ss" });
        }
        /// <summary>
        /// 删除任务
        /// </summary>
        /// <param name="jobGroup"></param>
        /// <param name="jobName"></param>
        /// <returns></returns>
        public IActionResult DelJob(string jobGroup, string jobName)
        {
            var result = SchedulerCenter.Instance.StopOrDelScheduleJobAsync(jobGroup, jobName, true);
            return Json(result.Result, new Newtonsoft.Json.JsonSerializerSettings() { DateFormatString = "yyyy-MM-dd HH:mm:ss" });
        }
        /// <summary>
        /// 修改任务
        /// </summary>
        /// <param name="schedule"></param>
        /// <returns></returns>
        public IActionResult ModifyJob(ScheduleEntity schedule)
        {
            var res = SchedulerCenter.Instance.StopOrDelScheduleJobAsync(schedule.JobGroup, schedule.JobName, true);
            var result = new BaseResult();
            if (schedule.JobType == 0)
            {
                result = SchedulerCenter.Instance.AddScheduleJobAsync<HttpJobTask>(schedule).Result;
            }
            else
            {
                result = SchedulerCenter.Instance.AddScheduleJobAsync(schedule).Result;
            }
            if (result.Code == MsgCode.Success)
            {
                return Json(new BaseResult()
                {
                    Code = MsgCode.Success,
                    Msg = "修改计划任务成功"
                }, new Newtonsoft.Json.JsonSerializerSettings() { DateFormatString = "yyyy-MM-dd HH:mm:ss" });
            }
            else
            {
                return Json(result, new Newtonsoft.Json.JsonSerializerSettings() { DateFormatString = "yyyy-MM-dd HH:mm:ss" });
            }
        }
    }
}
