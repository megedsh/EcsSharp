using System.Reflection;
using System.Text;
using EcsSharp.Logging;

namespace EcsSharp
{
    public static class AssemblyLoadLogger
    {
        private static readonly ICommonLog s_log = CommonLogManager.GetLogger(typeof(AssemblyLoadLogger));

        private static          bool   s_loaded;
        private static readonly object s_sync = new object();

        public static void Init()
        {
            if (!s_loaded)
            {
                lock(s_sync)
                {
                    if (!s_loaded)
                    {
                        writeLog();
                        s_loaded = true;
                    }
                }
            }
        }

        private static void writeLog()
        {
            Assembly assembly1 = Assembly.GetAssembly(typeof(EcsRepo));
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("Ecs:");
            sb.AppendLine("---------------------------");
            sb.AppendLine($"{assembly1.GetName().Name}  Ver : {assembly1.GetName().Version}");
            sb.AppendLine("---------------------------");
            s_log.Info(sb.ToString());
        }
    }
}