using System;
using System.Collections.Generic;
using System.Linq;

public static class ListExtensions
{
    static Random rng;

    //Shuffles using the Durstenfeld implementation of Fisher-Yates algorithm
    // <param name="list">The list to be shuffled.</param>
    // <typeparam name="T">The type of the elements in the list.</typeparam>
    // <returns>Shuffled list</returns>

    public static IList<T> Shuffle<T>(this IList<T> list) {
        if (rng == null) rng = new Random();
        int count = list.Count;
        while (count > 1) {
            --count;
            int index = rng.Next(count + 1);
            (list[index], list[count]) = (list[count], list[index]);
        }

        return list;
    }
}
