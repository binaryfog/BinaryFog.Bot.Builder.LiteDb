# BinaryFog.Bot.Builder.LiteDb
Microsoft Bot Builder Storage provider based on LiteDb database

## Usage
In Startup.cs , replace MemoryStorage with BinaryFog.Bot.Builder.LiteDb.LiteDbStorage
```C#
 services.AddSingleton<IStorage, BinaryFog.Bot.Builder.LiteDb.LiteDbStorage>();
```
