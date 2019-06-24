using BinaryFog.Bot.Builder.LiteDb.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BinaryFog.Bot.Builder.LiteDb.Tests
{
    [TestClass]
    [TestCategory("Delete Tests")]
    public class DeleteTests
    {
        private LiteDbStorage storage;

        [TestInitialize]
        public async Task Initialize()
        {
            storage = new LiteDbStorage();

            var dict = new Dictionary<string, object>()
                {
                    { "delete1", new PocoStoreItem() { Id = "1", Count = 1 } },
                };

            await storage.WriteAsync(dict);

        }

        [TestMethod]
        public async Task DeletedItemShouldNotBefound()
        {
            //ARRANGE

            //ACT
            await storage.DeleteAsync(new[] { "delete1" });

            //ASSERT
            var reloadedStoreItems = await storage.ReadAsync(new[] { "delete1" });

            Assert.IsFalse(reloadedStoreItems.Any(), "no store item should have been found because it was deleted");


        }

        [TestMethod]
        public async Task UnknownItemShouldNotBefound()
        {
            //ARRANGE

            //ACT
            await storage.DeleteAsync(new[] { "unknown1" });

            //ASSERT
            var reloadedStoreItems = await storage.ReadAsync(new[] { "unknown1" });

            Assert.IsFalse(reloadedStoreItems.Any(), "no store item should have been found because it was not in db before");


        }
    }
}
