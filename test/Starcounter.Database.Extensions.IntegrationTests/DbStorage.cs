using System;
using System.Collections.Generic;
using System.Linq;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    class DbStorage
    {
        object _lock = new object();
        Dictionary<int, object> _objects = new Dictionary<int, object>();

        volatile int _counter = 0;

        public ulong Insert(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            int id = ++_counter;
            _objects.Add(id, obj);

            return (ulong) id;
        }

        public ulong Delete(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            int id = _objects.FirstOrDefault(t => object.ReferenceEquals(t.Value, obj)).Key;
            if (id == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(obj));
            }

            _objects.Remove(id);

            return (ulong)id;
        }

        public object Get(ulong oid)
        {
            if (_objects.TryGetValue((int)oid, out object result))
            {
                return result;
            }

            throw new ArgumentOutOfRangeException(nameof(oid));
        }

        public ulong GetOid(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            int id = _objects.FirstOrDefault(t => object.ReferenceEquals(t.Value, obj)).Key;

            if (id == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(obj));
            }

            return (ulong)id;
        }
    }
}
