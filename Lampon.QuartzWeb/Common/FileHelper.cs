using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Lampon.QuartzWeb.Common
{
    public class FileHelper
    {
        /// <summary>
        /// 反射获取类信息
        /// </summary>
        /// <param name="assemblyName">程序集名称</param>
        /// <param name="className">类名</param>
        /// <returns></returns>
        public static Type GetAbsolutePath(string assemblyName, string className)
        {
            Assembly assembly = Assembly.Load(new AssemblyName(assemblyName));

            Type type = assembly.GetType(className);
            return type;
        }

    }
}
