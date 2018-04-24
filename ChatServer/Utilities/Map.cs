using System.Collections.Generic;

namespace ChatServer.Utilities
{
    public class Map<T1, T2>
    {
        private readonly Dictionary<T1, T2> forward = new Dictionary<T1, T2>();
        private readonly Dictionary<T2, T1> reverse = new Dictionary<T2, T1>();

        public Map()
        {
            Forward = new Indexer<T1, T2>(forward);
            Reverse = new Indexer<T2, T1>(reverse);
        }

        public class Indexer<T3, T4>
        {
            private readonly Dictionary<T3, T4> dictionary;
            public Indexer(Dictionary<T3, T4> dictionary)
            {
                this.dictionary = dictionary;
            }
            public T4 this[T3 index]
            {
                get => dictionary.TryGetValue(index, out T4 v) ? v : default(T4);
                set => dictionary[index] = value;
            }
        }

        public void Add(T1 t1, T2 t2)
        {
            forward.Add(t1, t2);
            reverse.Add(t2, t1);
        }

        public Indexer<T1, T2> Forward { get; }
        public Indexer<T2, T1> Reverse { get; }

        public bool ContainsKey(T1 key)
        {
            return forward.ContainsKey(key);
        }

        public bool ContainsValue(T2 val)
        {
            return reverse.ContainsKey(val);
        }

        public bool Remove(T1 key)
        {
            var val = forward[key];
            return forward.Remove(key) && reverse.Remove(val);
        }

        public bool Remove(T2 val)
        {
            var key = reverse[val];
            return forward.Remove(key) && reverse.Remove(val);
        }
    }
}
