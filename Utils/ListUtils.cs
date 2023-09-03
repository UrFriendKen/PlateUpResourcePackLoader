using System.Collections.Generic;
using System.Linq;

namespace KitchenResourcePackLoader.Utils
{
    public static class ListUtils
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> list)
        {
            return list == null || list.Count() == 0;
        }
    }
}
