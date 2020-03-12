using System;
using System.Collections.Generic;
using Starcounter.Database.ChangeTracking;

namespace Starcounter.Database.Extensions
{
    public class PreCommitOptions
    {
        internal IDictionary<Type, Action<IDatabaseContext, Change>> Delegates { get; }
            = new Dictionary<Type, Action<IDatabaseContext, Change>>();

        public void Hook<T>(Action<IDatabaseContext, Change> action) => Delegates.Add(typeof(T), action);
    }
}