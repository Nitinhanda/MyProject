using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace OpenXMLPowerTools
{
    public static class PtUtil
    {

        public static string StringConcatenate(this IEnumerable<string> source)
        {
            return source.Aggregate(
                new StringBuilder(),
                (sb, s) => sb.Append(s),
                sb => sb.ToString());
        }

        public static string StringConcatenate<T>(this IEnumerable<T> source, Func<T, string> projectionFunc)
        {
            return source.Aggregate(
                new StringBuilder(),
                (sb, i) => sb.Append(projectionFunc(i)),
                sb => sb.ToString());
        }

        public static IEnumerable<XElement> DescendantsTrimmed(this XElement element,
            XName trimName)
        {
            return DescendantsTrimmed(element, e => e.Name == trimName);
        }

        public static IEnumerable<XElement> DescendantsTrimmed(this XElement element,
          Func<XElement, bool> predicate)
        {
            Stack<IEnumerator<XElement>> iteratorStack = new Stack<IEnumerator<XElement>>();
            iteratorStack.Push(element.Elements().GetEnumerator());
            while (iteratorStack.Count > 0)
            {
                while (iteratorStack.Peek().MoveNext())
                {
                    XElement currentXElement = iteratorStack.Peek().Current;
                    if (predicate(currentXElement))
                    {
                        yield return currentXElement;
                        continue;
                    }
                    yield return currentXElement;
                    iteratorStack.Push(currentXElement.Elements().GetEnumerator());
                }
                iteratorStack.Pop();
            }
        }

        public static IEnumerable<IGrouping<TKey, TSource>> GroupAdjacent<TSource, TKey>(
         this IEnumerable<TSource> source,
         Func<TSource, TKey> keySelector)
        {
            TKey last = default(TKey);
            var haveLast = false;
            var list = new List<TSource>();

            foreach (TSource s in source)
            {
                TKey k = keySelector(s);
                if (haveLast)
                {
                    if (!k.Equals(last))
                    {
                        yield return new GroupOfAdjacent<TSource, TKey>(list, last);

                        list = new List<TSource> { s };
                        last = k;
                    }
                    else
                    {
                        list.Add(s);
                        last = k;
                    }
                }
                else
                {
                    list.Add(s);
                    last = k;
                    haveLast = true;
                }
            }
            if (haveLast)
                yield return new GroupOfAdjacent<TSource, TKey>(list, last);
        }

    }

    public class GroupOfAdjacent<TSource, TKey> : IGrouping<TKey, TSource>
    {
        public GroupOfAdjacent(List<TSource> source, TKey key)
        {
            GroupList = source;
            Key = key;
        }

        public TKey Key { get; set; }
        private List<TSource> GroupList { get; set; }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<TSource>)this).GetEnumerator();
        }

        IEnumerator<TSource> IEnumerable<TSource>.GetEnumerator()
        {
            return ((IEnumerable<TSource>)GroupList).GetEnumerator();
        }
    }
}
