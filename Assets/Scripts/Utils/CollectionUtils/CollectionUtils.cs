using System;
using System.Collections.Generic;
using System.Linq;

public static class CollectionUtils 
{
    public static bool IsNullOrEmplty<T>(this IEnumerable<T> source)
    {
        if (source == null)
            return true;        
       return source.Count() == 0;
    }
    /// <summary>
    /// Trộn List
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void Shuffle<T>(this List<T> source)
    {
        if (source.IsNullOrEmplty())
        {
            throw new ArgumentNullException(nameof(source));
        }      
        int count = source.Count();
        while (count > 1)
        {
            count--;
            int k = UnityEngine.Random.Range(0, count + 1); // Chọn một chỉ số ngẫu nhiên từ 0 đến n 
            // Hoán vị (Swap) phần tử thứ n và thứ k
            T value = source[k];
            source[k] = source[count];
            source[count] = value;
        }
    }
    /// <summary>
    /// Trộn mảng
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="array"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void Shuffle<T>(this T[] array)
    {
        if (array.IsNullOrEmplty())
        {
            throw new ArgumentNullException(nameof(array));
        }

        int count = array.Length;
        while (count > 1)
        {
            count--;
            int k = UnityEngine.Random.Range(0, count + 1);

            // Swap
            T value = array[k];
            array[k] = array[count];
            array[count] = value;
        }
    }
    public static T RandomElement<T>(this T[] array)
    {
        if (array.IsNullOrEmplty())
        {
            return default(T);
        }
        int index = UnityEngine.Random.Range(0, array.Length); // chọn số từ 0 đến array.Length - 1
        return array[index];
    }   

    public static T RandomElement<T>(this IEnumerable<T> source)
    {
        if (source.IsNullOrEmplty())
        {
            return default(T);
        }
        int index = UnityEngine.Random.Range(0, source.Count());
        return source.ElementAt(index);
    }
}
