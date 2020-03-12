using System;
using Microsoft.Extensions.DependencyInjection;
using Starcounter.Database;
using Starcounter.Database.Extensions;

namespace Hooks
{
    [Database]
    public abstract class Person : IDeleteAware
    {
        public abstract string Name { get; set; }

        public void OnDelete(IDatabaseContext db) => Console.WriteLine($"{Name} is about to be deleted.");
    }
    
    class Program
    {
        static void Main(string[] args)
        {
            using var services = new ServiceCollection()
                .AddStarcounter($"Database=./.database/Hooks")
                .Decorate<ITransactor, OnDeleteTransactor>()
                .Decorate<ITransactor, PreCommitTransactor>()
                .Configure<PreCommitOptions>(o => o.Hook<Person>((db, change) 
                    => Console.WriteLine($"{change.ChangeType} of object with id {change.Id}")))
                .BuildServiceProvider();

            var transactor = services.GetRequiredService<ITransactor>();

            var id = transactor.Transact(db =>
            {
                var per = db.Insert<Person>();
                per.Name = "Per";
                return db.GetOid(per);
            });

            transactor.Transact(db =>
            {
                var per = db.Get<Person>(id);
                per.Name = "Per Samuelsson";
            });

            transactor.Transact(db =>
            {
                var per = db.Get<Person>(id);
                db.Delete(per);
            });

            // Output:
            // Insert of object with id <n>
            // Update of object with id <n>
            // Per Samuelsson is about to be deleted.
        }
    }
}
