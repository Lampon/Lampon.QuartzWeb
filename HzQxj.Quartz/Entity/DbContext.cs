using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqlSugar;

namespace HzQxj.Quartz.Entity
{
    public class DbContext
    {
        public SqlSugarClient SugarDatabase { get { return GetInstance(); } }
        public SqlSugarClient GetInstance()
        {
            SqlSugarClient db = new SqlSugarClient(
                new ConnectionConfig()
                {
                    ConnectionString = "Data Source=rdsv317vuz4on3rajefzn.sqlserver.rds.aliyuncs.com,3433;Initial Catalog=log_database;Persist Security Info=True;User ID=log;Password=joinheadqazxcv;max pool size=32767;",//主数据库
                    DbType = SqlSugar.DbType.SqlServer,
                    IsAutoCloseConnection = true,
                    InitKeyType = InitKeyType.Attribute,
                });
            db.Ado.CommandTimeOut = 0;
            db.Aop.OnLogExecuting = (sql, pars) =>
            {
                if (db.TempItems == null) db.TempItems = new Dictionary<string, object>();
                db.TempItems.Add("logTime", DateTime.Now);
            };
            db.Aop.OnLogExecuted = (sql, pars) =>
            {
                var startingTime = db.TempItems["logTime"];
                db.TempItems.Remove("logTime");
                var completedTime = DateTime.Now;

            };
            return db;
        }
    }
}
