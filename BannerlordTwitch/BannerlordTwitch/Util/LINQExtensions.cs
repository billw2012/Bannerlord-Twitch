using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BannerlordTwitch.Util;
using TaleWorlds.Core;

public static class LINQExtensions
{
    public static T SelectWeighted<T>(this IEnumerable<T> @this, float rnd, Func<T, float> weightFn)
    {
        var prob = @this.Select(o => (obj: o, P: weightFn(o)));
        float totalProb = prob.Select(o => o.P).Sum();

        float randomP = rnd * totalProb;
        float sum = 0;
        foreach ((var obj, float p) in prob)
        {
            sum += p;
            if (sum >= randomP)
            {
                return obj;
            }
        }

        return prob.LastOrDefault().obj;
    }

    public static T SelectRandom<T>(this IEnumerable<T> @this) => @this.Shuffle().FirstOrDefault();

    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> @this)
    {
        return @this.OrderBy(e => StaticRandom.Next());
    }

    /// <summary>
    /// Wraps this object instance into an IEnumerable&lt;T&gt;
    /// consisting of a single item.
    /// </summary>
    /// <typeparam name="T"> Type of the object. </typeparam>
    /// <param name="item"> The instance that will be wrapped. </param>
    /// <returns> An IEnumerable&lt;T&gt; consisting of a single item. </returns>
    public static IEnumerable<T> Yield<T>(this T item)
    {
        yield return item;
    }
    
    public static IEnumerable<T> Append<T>(this IEnumerable<T> @this, T item) => @this.Concat(item.Yield());

    public static IEnumerable<(EquipmentElement element, EquipmentIndex index)> YieldEquipmentSlots(this Equipment equipment)
    {
        for (int i = 0; i < (int) EquipmentIndex.NumEquipmentSetSlots; i++)
        {
            yield return (equipment[i], (EquipmentIndex) i);
        }
    }
        
    public static IEnumerable<(EquipmentElement element, EquipmentIndex index)> YieldWeaponSlots(this Equipment equipment)
    {
        for (int i = (int) EquipmentIndex.WeaponItemBeginSlot;
            i < (int) EquipmentIndex.ExtraWeaponSlot; 
            i++)
        {
            yield return (equipment[i], (EquipmentIndex) i);
        }
    }
            
    public static IEnumerable<(EquipmentElement element, EquipmentIndex index)> YieldArmorSlots(this Equipment equipment)
    {
        for (int i = (int) EquipmentIndex.ArmorItemBeginSlot; i < (int) EquipmentIndex.ArmorItemEndSlot; i++)
        {
            yield return (equipment[i], (EquipmentIndex) i);
        }
    }
    
    // public static SerializableDict<TKey, TElement> ToSerializableDict<TSource, TKey, TElement>(
    //     this IEnumerable<TSource> source,
    //     Func<TSource, TKey> keySelector,
    //     Func<TSource, TElement> elementSelector) =>
    //     source.ToSerializableDict(keySelector, elementSelector, (IEqualityComparer<TKey>) null);
    //
    // public static SerializableDict<TKey, TElement> ToSerializableDict<TSource, TKey, TElement>(
    //     this IEnumerable<TSource> source,
    //     Func<TSource, TKey> keySelector,
    //     Func<TSource, TElement> elementSelector,
    //     IEqualityComparer<TKey> comparer)
    // {
    //     if (source == null)
    //         throw new ArgumentNullException(nameof (source));
    //     if (keySelector == null)
    //         throw new ArgumentNullException(nameof (keySelector));
    //     if (elementSelector == null)
    //         throw new ArgumentNullException(nameof (elementSelector));
    //     int capacity = 0;
    //     if (source is ICollection<TSource> sources)
    //     {
    //         capacity = sources.Count;
    //         if (capacity == 0)
    //             return new SerializableDict<TKey, TElement>(comparer);
    //         switch (sources)
    //         {
    //             case TSource[] source1:
    //                 return ToSerializableDict(source1, keySelector, elementSelector, comparer);
    //             case List<TSource> source1:
    //                 return ToSerializableDict(source1, keySelector, elementSelector, comparer);
    //         }
    //     }
    //     var dictionary = new SerializableDict<TKey, TElement>(capacity, comparer);
    //     foreach (var source1 in source)
    //         dictionary.Add(keySelector(source1), elementSelector(source1));
    //     return dictionary;
    // }
    //
    //
    // private static SerializableDict<TKey, TElement> ToSerializableDict<TSource, TKey, TElement>(
    //     TSource[] source,
    //     Func<TSource, TKey> keySelector,
    //     Func<TSource, TElement> elementSelector,
    //     IEqualityComparer<TKey> comparer)
    // {
    //     var dictionary = new SerializableDict<TKey, TElement>(source.Length, comparer);
    //     for (int index = 0; index < source.Length; ++index)
    //         dictionary.Add(keySelector(source[index]), elementSelector(source[index]));
    //     return dictionary;
    // }
    //
    // private static SerializableDict<TKey, TElement> ToSerializableDict<TSource, TKey, TElement>(
    //     List<TSource> source,
    //     Func<TSource, TKey> keySelector,
    //     Func<TSource, TElement> elementSelector,
    //     IEqualityComparer<TKey> comparer)
    // {
    //     var dictionary = new SerializableDict<TKey, TElement>(source.Count, comparer);
    //     foreach (var source1 in source)
    //         dictionary.Add(keySelector(source1), elementSelector(source1));
    //     return dictionary;
    // }
}
