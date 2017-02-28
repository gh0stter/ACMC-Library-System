using System.Linq;

namespace ACMC_Library_System.Supports
{
    internal static class Array
    {
        public static bool NullSequenceEqual<T>(this T[] source, T[] target)
        {
            if (ReferenceEquals(source, target))
            {
                return true;
            }
            if (source == null || target == null)
            {
                return false;
            }
            return source.Length == target.Length && source.SequenceEqual(target);
        }
    }
}
