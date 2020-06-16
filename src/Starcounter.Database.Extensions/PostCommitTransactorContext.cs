using System;
using System.Collections.Generic;
using Starcounter.Database.ChangeTracking;

namespace Starcounter.Database.Extensions
{
    public class PostCommitTransactorContext
    {
        public IEnumerable<(Type Type, Change Change)> Changes { get; set; }
    }
}
