using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.AdoJobStore;
using Quartz.Impl.AdoJobStore.Common;
using Quartz.Impl.Matchers;
using Quartz.Impl.Triggers;
using Quartz.Simpl;
using Quartz.Util;
using Lampon.QuartzWeb.Common;
using Lampon.QuartzWeb.Models;

namespace Lampon.QuartzWeb.QuartzCore
{
    public class SchedulerCenter
    {
        /// <summary>
        /// 任务调度对象
        /// </summary>
        public static readonly SchedulerCenter Instance;
        static SchedulerCenter()
        {
            Instance = new SchedulerCenter();
        }
        private IScheduler _scheduler;
        private IScheduler Scheduler
        {
            get
            {
                if (_scheduler != null)
                {
                    return _scheduler;
                }

                //如果不存在sqlite数据库，则创建
                if (!File.Exists("DAL/sqliteScheduler.db"))
                {
                    using (var connection = new SqliteConnection("Data Source=DAL/sqliteScheduler.db"))
                    {
                        connection.OpenAsync().Wait();
                        string sql = File.ReadAllTextAsync("DAL/tables_sqlite.sql").Result;
                        var command = new SqliteCommand(sql, connection);
                        command.ExecuteNonQuery();
                        connection.Close();
                    }
                }

                //MySql存储
                //DBConnectionManager.Instance.AddConnectionProvider("default", new DbProvider("MySql", "server=192.168.10.133;user id=root;password=pass;persistsecurityinfo=True;database=quartz"));
                DBConnectionManager.Instance.AddConnectionProvider("default", new DbProvider("SQLite-Microsoft", "Data Source=DAL/sqliteScheduler.db"));
                var serializer = new JsonObjectSerializer();
                serializer.Initialize();
                var jobStore = new JobStoreTX
                {
                    DataSource = "default",
                    TablePrefix = "QRTZ_",
                    InstanceId = "AUTO",
                    //DriverDelegateType = typeof(MySQLDelegate).AssemblyQualifiedName, //MySql存储
                    DriverDelegateType = typeof(SQLiteDelegate).AssemblyQualifiedName,  //SQLite存储
                    ObjectSerializer = serializer
                };
                DirectSchedulerFactory.Instance.CreateScheduler("benny" + "Scheduler", "AUTO", new DefaultThreadPool(), jobStore);
                _scheduler = SchedulerRepository.Instance.Lookup("benny" + "Scheduler").Result;

                _scheduler.Start();//默认开始调度器
                return _scheduler;
            }
        }
        /// <summary>
        /// 添加一个工作调度（映射程序集指定IJob实现类）
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public async Task<BaseResult> AddScheduleJobAsync(ScheduleEntity entity)
        {
            var result = new BaseResult();
            try
            {
                //检查任务是否已存在
                var jk = new JobKey(entity.JobName, entity.JobGroup);
                if (await this.Scheduler.CheckExists(jk))
                {
                    //删除已经存在任务
                    await this.Scheduler.DeleteJob(jk);
                }
                //反射获取任务执行类
                var jobType = FileHelper.GetAbsolutePath(entity.AssemblyName, entity.AssemblyName + "." + entity.ClassName);
                // 定义这个工作，并将其绑定到我们的IJob实现类
                var jobData = new JobDataMap();
                jobData.Add("AssemblyName", entity.AssemblyName);
                jobData.Add("ClassName", entity.ClassName);
                var jobDetailImpl = new JobDetailImpl(entity.JobName, entity.JobGroup, jobType);
                jobDetailImpl.Description = entity.Description;
                jobDetailImpl.JobDataMap = jobData;
                IJobDetail job = jobDetailImpl;
                // 创建触发器
                ITrigger trigger;
                //校验是否正确的执行周期表达式
                if (!string.IsNullOrEmpty(entity.Cron) && CronExpression.IsValidExpression(entity.Cron))
                {
                    trigger = CreateCronTrigger(entity);
                }
                else
                {
                    trigger = CreateSimpleTrigger(entity);
                }

                // 告诉Quartz使用我们的触发器来安排作业
                await this.Scheduler.ScheduleJob(job, trigger);

                result.Code = MsgCode.Success;
                result.Msg = "计划任务添加成功";
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(string.Format("添加任务出错{0}", ex.Message));
                result.Code = MsgCode.Unknow;
                result.Msg = ex.Message;
            }
            return result;
        }
        /// <summary>
        /// 添加任务调度（指定IJob实现类）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="m"></param>
        /// <returns></returns>
        public async Task<BaseResult> AddScheduleJobAsync<T>(ScheduleEntity entity) where T : IJob
        {
            var result = new BaseResult();
            try
            {
                //检查任务是否已存在
                var jobKey = new JobKey(entity.JobName, entity.JobGroup);
                if (await Scheduler.CheckExists(jobKey))
                {
                    //删除已经存在任务
                    await this.Scheduler.DeleteJob(jobKey);
                }
                //http请求配置
                var jobData = new JobDataMap();
                jobData.Add("RequestUrl", entity.RequestUrl);
                // 定义这个工作，并将其绑定到我们的IJob实现类                
                IJobDetail job = JobBuilder.CreateForAsync<T>()
                    .SetJobData(jobData)
                    .WithDescription(entity.Description)
                    .WithIdentity(entity.JobName, entity.JobGroup)
                    .Build();
                // 创建触发器
                ITrigger trigger;
                //校验是否正确的执行周期表达式
                if (!string.IsNullOrEmpty(entity.Cron) && CronExpression.IsValidExpression(entity.Cron))
                {
                    trigger = CreateCronTrigger(entity);
                }
                else
                {
                    trigger = CreateSimpleTrigger(entity);
                }

                // 告诉Quartz使用我们的触发器来安排作业
                await Scheduler.ScheduleJob(job, trigger);
                result.Code = MsgCode.Success;
                result.Msg = "计划任务添加成功";
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(string.Format("添加任务出错", ex.Message));
                result.Code = MsgCode.Unknow;
                result.Msg = ex.Message;
            }
            return result;
        }
        /// <summary>
        /// 暂停/删除 指定的计划
        /// </summary>
        /// <param name="jobGroup">任务分组</param>
        /// <param name="jobName">任务名称</param>
        /// <param name="isDelete">停止并删除任务</param>
        /// <returns></returns>
        public async Task<BaseResult> StopOrDelScheduleJobAsync(string jobGroup, string jobName, bool isDelete = false)
        {
            var result = new BaseResult();
            try
            {
                await Scheduler.PauseJob(new JobKey(jobName, jobGroup));
                if (isDelete)
                {
                    await Scheduler.DeleteJob(new JobKey(jobName, jobGroup));
                }
                result = new BaseResult
                {
                    Code = MsgCode.Success,
                    Msg = "停止任务计划成功"
                };
            }
            catch (Exception ex)
            {
                result.Code = MsgCode.Unknow;
                result.Msg = ex.Message;
            }
            return result;
        }
        /// <summary>
        /// 恢复运行暂停的任务
        /// </summary>
        /// <param name="jobName">任务名称</param>
        /// <param name="jobGroup">任务分组</param>
        public async Task<BaseResult> ResumeJobAsync(string jobGroup, string jobName)
        {
            var result = new BaseResult();
            try
            {
                //检查任务是否存在
                var jobKey = new JobKey(jobName, jobGroup);
                if (await Scheduler.CheckExists(jobKey))
                {
                    //任务已经存在则暂停任务
                    await Scheduler.ResumeJob(jobKey);
                    result.Msg = "恢复任务计划成功";
                    result.Code = MsgCode.Success;

                }
                else
                {
                    result.Msg = "任务不存在";
                    result.Code = MsgCode.IsExist;
                }
            }
            catch (Exception ex)
            {
                result.Msg = "恢复任务计划失败";
                result.Code = MsgCode.Unknow;
            }
            return result;
        }
        /// <summary>
        /// 获取所有Job（详情信息 - 初始化页面调用）
        /// </summary>
        /// <returns></returns>
        public async Task<List<JobInfoEntity>> GetAllJobAsync()
        {
            List<JobKey> jboKeyList = new List<JobKey>();
            List<JobInfoEntity> jobInfoList = new List<JobInfoEntity>();
            var groupNames = await Scheduler.GetJobGroupNames();
            foreach (var groupName in groupNames.OrderBy(t => t))
            {
                jboKeyList.AddRange(await Scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(groupName)));
                jobInfoList.Add(new JobInfoEntity() { GroupName = groupName });
            }
            foreach (var jobKey in jboKeyList.OrderBy(t => t.Name))
            {
                var jobDetail = await Scheduler.GetJobDetail(jobKey);
                var triggersList = await Scheduler.GetTriggersOfJob(jobKey);
                var triggers = triggersList.AsEnumerable().FirstOrDefault();

                var interval = string.Empty;
                if (triggers is SimpleTriggerImpl)
                    interval = (triggers as SimpleTriggerImpl)?.RepeatInterval.ToString();
                else
                    interval = (triggers as CronTriggerImpl)?.CronExpressionString;

                foreach (var jobInfo in jobInfoList)
                {
                    if (jobInfo.GroupName == jobKey.Group)
                    {
                        jobInfo.JobInfoList.Add(new JobInfo()
                        {
                            Name = jobKey.Name,
                            LastErrMsg = jobDetail.JobDataMap.GetString(Constant.EXCEPTION),
                            RequestUrl = jobDetail.JobDataMap.GetString(Constant.REQUESTURL),
                            JobType = jobDetail.JobDataMap.GetString(Constant.REQUESTURL) == null ? 1 : 0,
                            AssemblyName = jobDetail.JobDataMap.GetString(Constant.ASSEMBLYNAME),
                            ClassName = jobDetail.JobDataMap.GetString(Constant.CLASSNAME),
                            TriggerState = await Scheduler.GetTriggerState(triggers.Key),
                            PreviousFireTime = triggers.GetPreviousFireTimeUtc()?.LocalDateTime,
                            NextFireTime = triggers.GetNextFireTimeUtc()?.LocalDateTime,
                            BeginTime = triggers.StartTimeUtc.LocalDateTime,
                            Interval = interval,
                            EndTime = triggers.EndTimeUtc?.LocalDateTime,
                            Description = jobDetail.Description
                        });
                        continue;
                    }
                }
            }
            return jobInfoList;
        }
        /// <summary>
        /// 查询任务
        /// </summary>
        /// <param name="jobGroup"></param>
        /// <param name="jobName"></param>
        /// <returns></returns>
        public async Task<ScheduleEntity> QueryJobAsync(string jobGroup, string jobName)
        {
            var entity = new ScheduleEntity();
            var jobKey = new JobKey(jobName, jobGroup);
            var jobDetail = await Scheduler.GetJobDetail(jobKey);
            var triggersList = await Scheduler.GetTriggersOfJob(jobKey);
            var triggers = triggersList.AsEnumerable().FirstOrDefault();
            var intervalSeconds = (triggers as SimpleTriggerImpl)?.RepeatInterval.TotalSeconds;
            entity.RequestUrl = jobDetail.JobDataMap.GetString(Constant.REQUESTURL);
            entity.BeginTime = triggers.StartTimeUtc.LocalDateTime;
            entity.EndTime = triggers.EndTimeUtc?.LocalDateTime;
            entity.IntervalSecond = intervalSeconds.HasValue ? Convert.ToInt32(intervalSeconds.Value) : 0;
            entity.JobGroup = jobGroup;
            entity.JobName = jobName;
            entity.JobType = jobDetail.JobDataMap.GetString(Constant.REQUESTURL) == null ? 1 : 0;
            entity.AssemblyName = jobDetail.JobDataMap.GetString(Constant.ASSEMBLYNAME);
            entity.ClassName = jobDetail.JobDataMap.GetString(Constant.CLASSNAME);
            entity.Cron = (triggers as CronTriggerImpl)?.CronExpressionString;
            entity.RunTimes = (triggers as SimpleTriggerImpl)?.RepeatCount;
            entity.TriggerType = triggers is SimpleTriggerImpl ? 0 : 1;
            //entity.RequestType = (RequestTypeEnum)int.Parse(jobDetail.JobDataMap.GetString(Constant.REQUESTTYPE));
            //entity.RequestParameters = jobDetail.JobDataMap.GetString(Constant.REQUESTPARAMETERS);
            entity.Description = jobDetail.Description;
            return entity;
        }
        /// <summary>
        /// 立即执行
        /// </summary>
        /// <param name="jobGroup"></param>
        /// <param name="jobName"></param>
        /// <returns></returns>
        public async Task<bool> TriggerJobAsync(JobKey jobKey)
        {
            await Scheduler.TriggerJob(jobKey);
            return true;
        }
        /// <summary>
        /// 开启调度器
        /// </summary>
        /// <returns></returns>
        public async Task<bool> StartScheduleAsync()
        {
            //开启调度器
            if (Scheduler.InStandbyMode)
            {
                await Scheduler.Start();

            }
            return Scheduler.InStandbyMode;
        }
        /// <summary>
        /// 停止任务调度
        /// </summary>
        public async Task<bool> StopScheduleAsync()
        {
            //判断调度是否已经关闭
            if (!Scheduler.InStandbyMode)
            {
                //等待任务运行完成
                await Scheduler.Standby(); //TODO  注意：Shutdown后Start会报错，所以这里使用暂停。
            }
            return !Scheduler.InStandbyMode;
        }
        /// <summary>
        /// 创建类型Simple的触发器
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        private ITrigger CreateSimpleTrigger(ScheduleEntity m)
        {
            //作业触发器
            if (m.RunTimes.HasValue && m.RunTimes > 0)
            {
                return TriggerBuilder.Create()
                    .WithIdentity(m.JobName, m.JobGroup)
                    .StartAt(m.BeginTime)//开始时间
                    .EndAt(m.EndTime)//结束数据
                    .WithSimpleSchedule(x => x
                        .WithIntervalInSeconds(m.IntervalSecond)//执行时间间隔，单位秒
                        .WithRepeatCount(m.RunTimes.Value))//执行次数、默认从0开始
                    .ForJob(m.JobName, m.JobGroup)//作业名称
                    .Build();
            }
            else
            {
                return TriggerBuilder.Create()
                    .WithIdentity(m.JobName, m.JobGroup)
                    .StartAt(m.BeginTime)//开始时间
                    .EndAt(m.EndTime)//结束数据
                    .WithSimpleSchedule(x => x
                        .WithIntervalInSeconds(m.IntervalSecond)//执行时间间隔，单位秒
                        .RepeatForever())//无限循环
                    .ForJob(m.JobName, m.JobGroup)//作业名称
                    .Build();
            }

        }
        /// <summary>
        /// 创建类型Cron的触发器
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        private ITrigger CreateCronTrigger(ScheduleEntity m)
        {
            // 作业触发器
            return TriggerBuilder.Create()
                .WithIdentity(m.JobName, m.JobGroup)
                .StartAt(m.BeginTime)//开始时间
                .EndAt(m.EndTime)//结束数据
                .WithCronSchedule(m.Cron)//指定cron表达式
                .ForJob(m.JobName, m.JobGroup)//作业名称
                .Build();
        }
    }
}
