# BinaryFog.Bot.Builder.LiteDb
Microsoft Bot Builder Storage provider based on LiteDb database

## Usage
In Startup.cs , replace MemoryStorage with BinaryFog.Bot.Builder.LiteDb.LiteDbStorage
```C#
 var storage = new BinaryFog.Bot.Builder.LiteDb.LiteDbStorage();
 
  // Create the User state passing in the storage layer.
 var userState = new UserState(storage);
 services.AddSingleton(userState);

 // Create the Conversation state passing in the storage layer.
 var conversationState = new ConversationState(storage);
 services.AddSingleton(conversationState);
```
