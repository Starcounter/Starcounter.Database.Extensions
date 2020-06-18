using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Starcounter.Database;
using Starcounter.Database.ChangeTracking;
using Starcounter.Database.Extensions;

namespace Hooks
{
    [Database]
    public abstract class Person : IDeleteAware
    {
        public abstract string Name { get; set; }

        public void OnDelete(IDatabaseContext db) => Console.WriteLine($"{Name} is about to be deleted.");
    }

    public class RelevantChanges
    {
        public List<(ChangeType ChangeType, Type ObjectType, ulong Oid)> Changes { get; protected set; } = new List<(ChangeType ChangeType, Type ObjectType, ulong Oid)>();
    }

    class Program
    {
        static void Main(string[] args)
        {
            using var services = new ServiceCollection()
                .AddStarcounter($"Database=./.database/Hooks")
                .Decorate<ITransactor, OnDeleteTransactor>()
                .Decorate<ITransactor, OnCommitTransactor<RelevantChanges>>()
                .Configure<OnCommitTransactorOptions<RelevantChanges>>(o =>
                {
                    o.OnBeforeCommit = db =>
                    {
                        var context = new RelevantChanges();

                        foreach (var change in db.ChangeTracker.Changes)
                        {
                            context.Changes.Add((change.Type, db.GetUserDefinedType(change.Oid), change.Oid));
                            Console.WriteLine($"{change.Type} of an object with id {change.Oid} is about to be committed.");
                        }

                        return context;
                    };

                    o.OnAfterCommit = context =>
                    {
                        foreach (var change in context.Changes)
                        {
                            Console.WriteLine($"{change.ChangeType} of an object with id {change.Oid} has just been committed.");
                        }
                    };
                })
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
            // Insert of an object with id 1 is about to be committed.
            // Insert of an object with id 1 has just been committed.
            // Update of an object with id 1 is about to be committed.
            // Update of an object with id 1 has just been committed.
            // Per Samuelsson is about to be deleted.
            // Delete of an object with id 1 is about to be committed.
            // Delete of an object with id 1 has just been committed.
        }
    }
}
