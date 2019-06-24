using Microsoft.Bot.Builder;

namespace BinaryFog.Bot.Builder.LiteDb.Tests.Models
{
    public class PocoStoreItem : IStoreItem
    {
        public string ETag { get; set; }

        public string Id { get; set; }

        public int Count { get; set; }
    }
}
