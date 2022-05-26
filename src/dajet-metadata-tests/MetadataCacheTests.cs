using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DaJet.Metadata.Cache.Tests
{
    internal sealed class Actor
    {
        internal Actor(int id, string key)
        {
            Id = id;
            Key = key;
        }
        internal int Id { set; get; }
        internal string Key { set; get; }
    }
    [TestClass] public sealed class MetadataCacheTests
    {
        private readonly IMetadataCache cache = new MetadataCache();
        public MetadataCacheTests()
        {
            cache.Add("test_node_1", new MetadataCacheOptions()
            {
                Expiration = 5,
                DatabaseProvider = DatabaseProvider.SQLServer,
                ConnectionString = "Data Source=ZHICHKIN;Initial Catalog=test_node_1;Integrated Security=True;Encrypt=False;"
            });

            cache.Add("test_node_2", new MetadataCacheOptions()
            {
                Expiration = 5,
                DatabaseProvider = DatabaseProvider.PostgreSQL,
                ConnectionString = "Host=127.0.0.1;Port=5432;Database=test_node_2;Username=postgres;Password=postgres;"
            });

            cache.Add("test_node_3", new MetadataCacheOptions() // database unavailable test
            {
                Expiration = 5,
                DatabaseProvider = DatabaseProvider.PostgreSQL,
                ConnectionString = "Host=127.0.0.1;Port=5432;Database=test_node_X;Username=postgres;Password=postgres;"
            });
        }
        [TestMethod] public void TestMetadataCache()
        {
            List<Actor> actors = new List<Actor>()
            {
                new Actor(1, "test_node_1"),
                new Actor(2, "test_node_1"),
                new Actor(3, "test_node_1"),
                new Actor(4, "test_node_X"), // cache key is not found test
                new Actor(5, "test_node_3")  // database unavailable test
                //new Actor(1, "test_node_2"),
                //new Actor(2, "test_node_2"),
                //new Actor(3, "test_node_2")
            };

            List<Task> tasks = new List<Task>();
            foreach (var actor in actors)
            {
                tasks.Add(Task.Factory.StartNew(ReadCacheValue, actor));
                Task.Delay(10).Wait();
            }
            Task.WaitAll(tasks.ToArray());

            Console.WriteLine();
            Console.WriteLine("10 seconds delay to test cache expiration ...");
            Task.Delay(TimeSpan.FromSeconds(10)).Wait();

            Console.WriteLine();

            tasks = new List<Task>();
            foreach (var actor in actors)
            {
                tasks.Add(Task.Factory.StartNew(ReadCacheValue, actor));
                Task.Delay(10).Wait();
            }
            Task.WaitAll(tasks.ToArray());

            cache.Dispose();
        }
        private void ReadCacheValue(object parameter)
        {
            if (!(parameter is Actor actor))
            {
                return;
            }

            Console.WriteLine($"[{actor.Id}] entr [{actor.Key}] {Environment.TickCount}");

            InfoBase infoBase = cache.TryGet(actor.Key, out string error);

            if (infoBase == null)
            {
                Console.WriteLine($"[{actor.Id}] failed to get [{actor.Key}] value: {error}");
            }
            else
            {
                Console.WriteLine($"[{actor.Id}] value of [{actor.Key}] is \"{infoBase.Name}\"");
            }

            Console.WriteLine($"[{actor.Id}] exit [{actor.Key}] {Environment.TickCount64}");
        }
    }
}