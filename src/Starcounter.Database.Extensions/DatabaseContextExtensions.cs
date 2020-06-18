using System;

namespace Starcounter.Database.Extensions
{
    public static class DatabaseContextExtensions
    {
        /// <summary>
        /// Returns user declared <see cref="Type"/> of the database object identified by the provided `oid`.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="oid"></param>
        /// <returns></returns>
        public static Type GetUserDefinedType(this IDatabaseContext db, ulong oid)
        {
            var proxy = db.Get<object>(oid);
            var type = proxy?.GetType()?.BaseType;

            return type;
        }
    }
}
