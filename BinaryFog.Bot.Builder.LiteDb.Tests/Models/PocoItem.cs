using System;
using System.Collections.Generic;
using System.Text;

namespace BinaryFog.Bot.Builder.LiteDb.Tests.Models
{
    public class PocoItem
    {
        public string Id { get; set; }

        public int Count { get; set; }

        public string[] ExtraBytes { get; set; }
    }
}
