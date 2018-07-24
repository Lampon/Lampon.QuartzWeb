using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqlSugar;

namespace HzQxj.Quartz.Entity
{
    public class HzQxj_ExcuteLog
    {
        public HzQxj_ExcuteLog()
        {
            AddTime=DateTime.Now;
        }
        [SugarColumn(IsNullable = false, IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        /// <summary>
        /// 上次执行时间
        /// </summary>
        [SugarColumn(IsNullable = false, Length = 50)]
        public string ExcuteTime { get; set; }
        /// <summary>
        /// 添加时间
        /// </summary>
        [SugarColumn(IsNullable = false)]
        public DateTime AddTime { get; set; }
    }
}
