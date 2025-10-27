using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MercaQuiz.Helpers;

public static class BackendUtility
{
    public static bool TryGetInt(IDictionary<string, object> query, string key, out int value)
    {
        value = 0;

        if (!query.TryGetValue(key, out var obj) || obj is null)
            return false;

        // Shell può passare "123" (string) oppure 123 (int)
        if (obj is int i)
        {
            value = i;
            return true;
        }
        if (obj is string s && int.TryParse(s, out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }
}
