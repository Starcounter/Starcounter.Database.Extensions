using System;
using System.Collections.Generic;
using Starcounter.Database.ChangeTracking;

namespace Starcounter.Database.Extensions
{
    public class PostCommitTransactorContext
    {
        public List<KeyValuePair<Type, Change>> Changes { get; set; }
    }
}
