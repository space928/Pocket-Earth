using UnityEngine;
using System.Collections.Generic;

namespace Assets.Scripts
{
    public static class ExtensionMethods
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
        }
    }
}
