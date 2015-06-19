using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Document;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            IDocumentStore store = new DocumentStore
                {
                    Url = "http://localhost:8080/",
                    DefaultDatabase = "SampleDatabase"
                }.Initialize();

            using (var session = store.OpenSession())
            {
                var document = new
                    {
                        Id = "documents/1",
                        Property = "Value1"
                    };
                session.Store(document);
                session.SaveChanges();
            }

            var cache = MemoryCache.Default;

            CacheItemPolicy policy = new CacheItemPolicy();

            var changes = store.Changes().ForDocument("documents/1");
            policy.ChangeMonitors.Add(new RavenDbChangeMonitor.RavenDbChangeMonitor<DocumentChangeNotification>(changes));
            policy.UpdateCallback = arguments =>
                {
                    var key = arguments.Key;
                    Console.WriteLine("Cache object (with key {0}) was updated", key);
                };

            cache.Set("cacheKey", "cacheValue", policy);


            Console.ReadLine();
        }
    }
}
