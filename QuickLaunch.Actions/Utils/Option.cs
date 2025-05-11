using System;

namespace QuickLaunch.Core.Utils
{
    public static class Option
    {
        /// <summary>
        /// Map an non-null value to a function returning a transformed object (akin to Select).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="obj"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        public static R? Map<T, R>(this T? obj, Func<T, R> map)
        {
            if (obj is T t)
            {
                return map(t);
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Map an non-null value to a function without return value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="obj"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        public static void Map<T>(this T? obj, Action<T> map)
        {
            if (obj is T t)
            {
                map(t);
            }
        }


        public static R? Map<T, R>(this object? obj, Func<T, R> map, Func<object?, R?>? typeError = null)
        {
            if (obj is T t)
            {
                return map(t);
            }
            else if (obj is null)
            {
                return default;
            }
            else
            {
                if (typeError is Func<object?, R?> func)
                {
                    return func(obj);
                }
                else
                {
                    throw new ArgumentException($"Unexpected type '{obj.GetType().FullName}', expected '{nameof(T)}.", nameof(obj));
                }
            }
        }
    }
}
