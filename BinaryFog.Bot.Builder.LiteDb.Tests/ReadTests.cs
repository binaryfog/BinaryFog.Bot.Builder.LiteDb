using BinaryFog.Bot.Builder.LiteDb.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BinaryFog.Bot.Builder.LiteDb.Tests
{
    [TestClass]
    [TestCategory("Read Tests")]
    public class ReadTests
    {
        private LiteDbStorage storage;
        private string readItemKey = "read1";

        [TestInitialize]
        public async Task Initialize()
        {
            storage = new LiteDbStorage();

            var dict = new Dictionary<string, object>()
                {
                    { readItemKey, new PocoStoreItem() { Id = "1", Count = 1 } },
                };

            await storage.WriteAsync(dict);

        }

        [TestMethod]
        public async Task ReadItemShouldBeNotNull()
        {
            //ARRANGE

            //ACT
            var result = await storage.ReadAsync(new[] { readItemKey });

            //ASSERT
            Assert.IsNotNull(result);            
        }

        [TestMethod]
        public async Task ReadItemShouldBeSameAsTypeAsWrittenOne()
        {
            //ARRANGE

            //ACT
            var result = await storage.ReadAsync(new[] { readItemKey });

            //ASSERT
            Assert.IsInstanceOfType(result[readItemKey], typeof(PocoStoreItem));


        }

        [TestMethod]
        public async Task ReadItemShouldHaveSameValuesAsWrittenOne()
        {
            //ARRANGE

            //ACT
            var result = await storage.ReadAsync(new[] { readItemKey });

            //ASSERT
            Assert.IsNull((result[readItemKey] as PocoStoreItem).ETag);
            Assert.AreEqual((result[readItemKey] as PocoStoreItem).Id, "1");
            Assert.AreEqual((result[readItemKey] as PocoStoreItem).Count, 1);

        }
        [TestMethod]
        public async Task UnknownItemShouldHaveNoValue()
        {
            //ARRANGE
            string unknownKey = "unknown1";

            //ACT
            var result = await storage.ReadAsync(new[] { unknownKey });

            //ASSERT
            Assert.IsNotNull(result, "result should not be null");
            Assert.IsNull(result.FirstOrDefault(e => e.Key == unknownKey).Value, "\"unknown\" key should have returned no value");


        }
    }
}
