using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Database.ChangeTracking;

namespace Starcounter.Database.Extensions
{
    internal static class TransactionChangesFilter
    {
        /// <summary>
        /// Filters <see cref="IChangeTracker.Changes"/> by object type using specified types dictionary.
        /// Entires with <see cref="Change.Type"/> equal to <see cref="ChangeType.Delete"/> are ignored.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="db"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        internal static IEnumerable<(Change, TValue)> SelectHooks<TValue>(IDatabaseContext db, IDictionary<Type, TValue> types)
        {
            foreach (var change in db.ChangeTracker.Changes)
            {
                if (change.Type == ChangeType.Delete)
                {
                    continue;
                }

                var type = db.GetRealType(change.Oid);

                if (types.TryGetValue(type, out TValue value))
                {
                    yield return (change, value);
                }
            }
        }
    }
}
