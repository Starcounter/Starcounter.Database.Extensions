using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Starcounter.Database;
using Starcounter.Database.Extensions;
using Starcounter.Database.Linq;

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

            transactor.Transact(db =>
            {
                var jane = db.Insert<Person>();
                jane.Name = "Jane Doe";
                
                transactor.Transact(db =>
                {
                    // Wihout NestedTransactor, this would not be allowed.
                    // Starcounter natively don't support nested transactions and
                    // will raise an exception for any Transact done as part of an
                    // already running transaction

                    var john = db.Insert<Person>();
                    john.Name = "John Doe";

                    // Here, we can find "Jane", simply because the NestedTransactor
                    // execute the current delegate as part of the outer transaction.

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
    }
}
