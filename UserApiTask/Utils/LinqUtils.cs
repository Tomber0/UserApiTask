namespace UserApiTask.Utils
{
    public static class LinqUtils
    {
        public static IEnumerable<T> Union<T>(this IEnumerable<T> source, params T[] items) 
        {
            return source.Union((IEnumerable<T>)items);
        }
    }
}
