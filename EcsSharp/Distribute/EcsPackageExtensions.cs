using System;
using System.Linq;
using System.Text;

namespace EcsSharp.Distribute;

public static class EcsPackageExtensions
{
    [ThreadStatic]
    private static StringBuilder ts_cacheStringBuilder;

    public static string ToLogString(this EcsPackage ecsPackage)
    {
        StringBuilder sb = getStringBuilder();
        if (ecsPackage.Updated.Count > 0)
        {
            string updated = string.Join(",", ecsPackage.Updated.Select(a => Tuple.Create(a.Key, string.Join(",", a.Value.Select(s => s.Value)))));
            sb.Append($"Updated: [{updated}] ");
        }

        if (ecsPackage.Deleted.Count > 0)
        {
            string deleted = string.Join(",", ecsPackage.Deleted);
            sb.Append($"Deleted: [{deleted}] ");
        }

        return sb.ToString();
    }

    private static StringBuilder getStringBuilder()
    {
        StringBuilder sb = ts_cacheStringBuilder;
        if (sb == null)
        {
            sb = ts_cacheStringBuilder = new StringBuilder();
        }
        else
        {
            sb.Clear();
        }

        return sb;
    }
}