using System;

namespace Starcounter.Database.Extensions
{
    public class OnCommitTransactorContext<TUserContextType>
    {
        public TUserContextType UserContext { get; set; }
    }
}
