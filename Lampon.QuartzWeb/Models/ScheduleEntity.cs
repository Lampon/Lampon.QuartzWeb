using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lampon.QuartzWeb.QuartzCore;

namespace Lampon.QuartzWeb.Models
{
    /// <summary>
    /// 任务调度实体
    /// </summary>
    public class ScheduleEntity
    {
        public ScheduleEntity()
        {
            RunTimes = 0;
        }
        /// <summary>
        /// 任务分组
        /// </summary>
        public string JobGroup { get; set; }
        /// <summary>
        /// 任务名称
        /// </summary>
        public string JobName { get; set; }
        /// <summary>
        /// 任务类型（0 URL请求 1程序集）
        /// </summary>
        public int JobType { get; set; }
        /// <summary>
        /// job调用外部的url
        /// </summary>
        public string RequestUrl { get; set; }
        /// <summary>
        /// 任务所在DLL对应的程序集名称
        /// </summary>
        public string AssemblyName { get; set; }
        /// <summary>
        /// 任务所在类
        /// </summary>
        public string ClassName { get; set; }
        /// <summary>
        /// 触发器类型（0、simple 1、cron）
        /// </summary>
        public int TriggerType { get; set; }
        /// <summary>
        /// 执行间隔时间, 秒为单位
        /// </summary>
        public int IntervalSecond { get; set; }
        /// <summary>
        /// 任务运行时间表达式
        /// </summary>
        public string Cron { get; set; }
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime BeginTime { get; set; }
        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }
        /// <summary>
        /// 执行次数
        /// </summary>
        public int? RunTimes { get; set; }
        /// <summary>
        /// 任务描述
        /// </summary>
        public string Description { get; set; }
    }
}
