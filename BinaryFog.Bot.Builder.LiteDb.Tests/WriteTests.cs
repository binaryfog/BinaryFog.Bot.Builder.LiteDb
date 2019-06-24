using BinaryFog.Bot.Builder.LiteDb.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BinaryFog.Bot.Builder.LiteDb.Tests
{
    [TestClass]
    [TestCategory("Write Tests")]
    public class WriteTests
    {
        [TestMethod]
        public async Task WriteTwoTypesGetSameTypes()
        {
            //ARRANGE
            LiteDbStorage storage = new LiteDbStorage();

            var storeItems = new Dictionary<string, object>()
            {
                ["createPoco"] = new PocoItem() { Id = "1" },
                ["createPocoStoreItem"] = new PocoStoreItem() { Id = "2" },
            };

            //ACT
            await storage.WriteAsync(storeItems);

            //ASSERT
            var readStoreItems = new Dictionary<string, object>(await storage.ReadAsync(storeItems.Keys.ToArray()));

            Assert.IsInstanceOfType(readStoreItems["createPoco"], typeof(PocoItem));
            Assert.IsInstanceOfType(readStoreItems["createPocoStoreItem"], typeof(PocoStoreItem));
        }

        [TestMethod]
        public async Task WriteTwoTypesGetSameValues()
        {
            //ARRANGE
            LiteDbStorage storage = new LiteDbStorage();

            var storeItems = new Dictionary<string, object>()
            {
                ["createPoco"] = new PocoItem() { Id = "1" },
                ["createPocoStoreItem"] = new PocoStoreItem() { Id = "2" },
            };

            //ACT
            await storage.WriteAsync(storeItems);

            //ASSERT
            var readStoreItems = new Dictionary<string, object>(await storage.ReadAsync(storeItems.Keys.ToArray()));

            var createPoco = readStoreItems["createPoco"] as PocoItem;

            Assert.IsNotNull(createPoco, "createPoco should not be null");
            Assert.AreEqual(createPoco.Id, "1", "createPoco.id should be 1");

            var createPocoStoreItem = readStoreItems["createPocoStoreItem"] as PocoStoreItem;

            Assert.IsNotNull(createPocoStoreItem, "createPocoStoreItem should not be null");
            Assert.AreEqual(createPocoStoreItem.Id, "2", "createPocoStoreItem.id should be 2");
            //Assert.IsNotNull(createPocoStoreItem.ETag, "createPocoStoreItem.eTag  should not be null");
        }

        [TestMethod]
        public async Task HandleCrazyKeys()
        {
            LiteDbStorage storage = new LiteDbStorage();

            var key = "!@#$%^&*()~/\\><,.?';\"`~";
            var storeItem = new PocoStoreItem() { Id = "1" };

            var dict = new Dictionary<string, object>() { { key, storeItem } };

            await storage.WriteAsync(dict);

            var storeItems = await storage.ReadAsync(new[] { key });

            storeItem = storeItems.FirstOrDefault(si => si.Key == key).Value as PocoStoreItem;

            Assert.IsNotNull(storeItem);
            Assert.AreEqual("1", storeItem.Id);
        }


        [TestMethod]
        public async Task UpdateTwoTypesGetUpdatedValues()
        {
            LiteDbStorage storage = new LiteDbStorage();

            var originalPocoItem = new PocoItem() { Id = "1", Count = 1 };
            var originalPocoStoreItem = new PocoStoreItem() { Id = "1", Count = 1 };

            // first write should work
            var dict = new Dictionary<string, object>()
            {
                { "pocoItem", originalPocoItem },
                { "pocoStoreItem", originalPocoStoreItem },
            };

            await storage.WriteAsync(dict);

            var loadedStoreItems = new Dictionary<string, object>(await storage.ReadAsync(new[] { "pocoItem", "pocoStoreItem" }));

            var updatePocoItem = loadedStoreItems["pocoItem"] as PocoItem;
            var updatePocoStoreItem = loadedStoreItems["pocoStoreItem"] as PocoStoreItem;
            //Assert.IsNotNull(updatePocoStoreItem.ETag, "updatePocoItem.eTag  should not be null");

            // 2nd write should work, because we have new etag, or no etag
            updatePocoItem.Count++;
            updatePocoStoreItem.Count++;

            await storage.WriteAsync(loadedStoreItems);

            var reloadedStoreItems = new Dictionary<string, object>(await storage.ReadAsync(new[] { "pocoItem", "pocoStoreItem" }));

            var reloeadedUpdatePocoItem = reloadedStoreItems["pocoItem"] as PocoItem;
            var reloadedUpdatePocoStoreItem = reloadedStoreItems["pocoStoreItem"] as PocoStoreItem;

            //Assert.IsNotNull(reloadedUpdatePocoStoreItem.ETag, "updatePocoItem.eTag  should not be null");
            //Assert.AreNotEqual(updatePocoStoreItem.ETag, reloadedUpdatePocoStoreItem.ETag, "updatePocoItem.eTag  should not be different");
            Assert.AreEqual(2, reloeadedUpdatePocoItem.Count, "updatePocoItem.Count should be 2");
            Assert.AreEqual(2, reloadedUpdatePocoStoreItem.Count, "updatePocoStoreItem.Count should be 2");



        }

        [TestMethod]
        public async Task BatchCreateBigObjectsShouldSucceed()
        {
            LiteDbStorage storage = new LiteDbStorage();
            string[] stringArray = GenerateExtraBytes(23);

            var storeItemsList = new List<Dictionary<string, object>>(new[]
                {
                new Dictionary<string, object> { ["createPoco"] = new PocoItem() { Id = "1", Count = 0, ExtraBytes = stringArray } },
                new Dictionary<string, object> { ["createPoco"] = new PocoItem() { Id = "1", Count = 1, ExtraBytes = stringArray } },
                new Dictionary<string, object> { ["createPoco"] = new PocoItem() { Id = "1", Count = 2, ExtraBytes = stringArray } },
            });

            // TODO: this code as a generic test doesn't make much sense - for now just eliminating the custom exception
            // Writing large objects in parallel might raise an InvalidOperationException

            //await Task.WhenAll(
            //    storeItemsList.Select(storeItems =>
            //        Task.Run(async () => await storage.WriteAsync(storeItems))));

            Parallel.Invoke(
                async() =>  { await storage.WriteAsync(storeItemsList[0]); },
                async () => { await storage.WriteAsync(storeItemsList[1]); },
                async () => { await storage.WriteAsync(storeItemsList[2]); }
                ); //close parallel.invoke


            var readStoreItems = new Dictionary<string, object>(await storage.ReadAsync(new[] { "createPoco" }));
            Assert.IsInstanceOfType(readStoreItems["createPoco"], typeof(PocoItem));
            var createPoco = readStoreItems["createPoco"] as PocoItem;
            Assert.AreEqual(createPoco.Id, "1", "createPoco.id should be 1");
        }

        private string[] GenerateExtraBytes(long minimumExtraBytes = 0)
        {
            string[] stringArray = null;

            if (minimumExtraBytes > 0)
            {
                // chunks of maximum string size to fill the extra bytes request
                var extraStringCount = (int)(minimumExtraBytes / int.MaxValue);
                stringArray = Enumerable.Range(0, extraStringCount).Select(i => new string('X', int.MaxValue / 2)).ToArray();

                // Append the remaining string size
                stringArray = stringArray.Append(new string('X', (int)(minimumExtraBytes % int.MaxValue) / 2)).ToArray();
            }

            return stringArray;
        }

    }
}
