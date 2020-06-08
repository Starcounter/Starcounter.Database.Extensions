using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Starcounter.Database;
using Starcounter.Database.Extensions;

namespace NestedTransactions
{
    [Database]
    public abstract class Person
    {
        public abstract string Name { get; set; }

        public abstract Person BestFriend { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            using var services = new ServiceCollection()
                .AddStarcounter($"Database=./.database/NestedTransactions")
                .Decorate<ITransactor, NestedTransactor>()
                .BuildServiceProvider();

            var transactor = services.GetRequiredService<ITransactor>();

            NestedTransactionWithinReadWriteTransaction(transactor);
            Console.WriteLine("----");
            NestedTransactionWithinReadOnlyTransaction(transactor);
        }

        /// <summary>
        /// Within a read-write database transaction,
        /// inner transactor.Transact runs as a part of the parent transaction.
        /// </summary>
        /// <param name="transactor"></param>
        static void NestedTransactionWithinReadWriteTransaction(ITransactor transactor)
        {
            transactor.Transact(db =>
            {
                var jane = db.Insert<Person>();
                jane.Name = "Jane Doe";

                transactor.Transact(db =>
                {
                    // Without NestedTransactor, this would execute as a new, independent
                    // transaction. Now, it instead join the outer one.

                    var john = db.Insert<Person>();
                    john.Name = "John Doe";

                    // Here, we can find "Jane", simply because the NestedTransactor
                    // executes the current delegate as a part of the outer transaction.

                    var jane = db.Sql<Person>("SELECT p FROM Person p WHERE p.Name = ?", "Jane Doe").First();
                    john.BestFriend = jane;

                    // You can use extension IsNested() if you need to check if the
                    // current scope is a nested transaction.

                    Console.WriteLine(db.IsNested());
                    Console.WriteLine(john.BestFriend.Name);
                });

                // This will return false, because we are executing a top-level
                // transaction.

                Console.WriteLine(db.IsNested());
            });

            // Output:
            // True
            // Jane Doe
            // False
        }

        /// <summary>
        /// Within a read-only database transaction,
        /// inner transactor.Transact runs as a separate transaction.
        /// The parent read-only transaction is rebased after each inner trasaction.
        /// </summary>
        /// <param name="transactor"></param>
        static void NestedTransactionWithinReadOnlyTransaction(ITransactor transactor)
        {
            transactor.Transact(db =>
            {
                Person p = null;

                transactor.Transact(idb =>
                {
                    p = db.Insert<Person>();
                });

                Console.WriteLine($"Oid: {db.GetOid(p)}, Name: {p.Name}");

                transactor.Transact(idb =>
                {
                    p.Name = "Test";
                });

                Console.WriteLine($"Oid: {db.GetOid(p)}, Name: {p.Name}");
            }, new TransactOptions(TransactionFlags.ReadOnly));
        }
    }
}
