using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqlSugar;

namespace HzQxj.Quartz.Entity
{
    public class HzQxj_SendLog
    {
        public HzQxj_SendLog()
        {
            AddTime = DateTime.Now;
        }
        [SugarColumn(IsNullable = false, IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        [SugarColumn(IsNullable = false, Length = 99999)]
        public string Openid { get; set; }
        [SugarColumn(IsNullable = false, Length = 99999)]
        public string Content { get; set; }
        /// <summary>
        /// 错误信息
        /// </summary>
        [SugarColumn(IsNullable = false, Length = 99999)]
        public string ErrMsg { get; set; }
        /// <summary>
        /// 执行结果
        /// </summary>
        [SugarColumn(IsNullable = false, Length = 99999)]
        public string Result { get; set; }
        /// <summary>
        /// 添加时间
        /// </summary>
        [SugarColumn(IsNullable = false)]
        public DateTime AddTime { get; set; }
    }
}
