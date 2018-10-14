using LiteDB;
using Microsoft.Bot.Builder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BinaryFog.Bot.Builder.LiteDb
{
    /// <summary>
    /// A storage layer that uses an in-memory dictionary.
    /// </summary>
    public class LiteDbStorage : IStorage
    {
        private static readonly Newtonsoft.Json.JsonSerializer StateJsonSerializer = new Newtonsoft.Json.JsonSerializer() { TypeNameHandling = TypeNameHandling.All };

        private readonly Dictionary<string, JObject> _memory;
        private readonly object _syncroot = new object();
        private int _eTag = 0;

        private string databaseFileName;
        private const string CollectionName = "storage";
        private const string KeyName = "_id";
        private const string ContentName = "content";

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryStorage"/> class.
        /// </summary>
        /// <param name="databaseFileName">Loaction of LiteDb database file name.</param>
        public LiteDbStorage( string databaseFileName)
        {
            //_memory = dictionary ?? new Dictionary<string, JObject>();
            this.databaseFileName = databaseFileName;
        }

        /// <summary>
        /// Deletes storage items from storage.
        /// </summary>
        /// <param name="keys">keys of the <see cref="IStoreItem"/> objects to delete.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <seealso cref="ReadAsync(string[], CancellationToken)"/>
        /// <seealso cref="WriteAsync(IDictionary{string, object}, CancellationToken)"/>
        public Task DeleteAsync(string[] keys, CancellationToken cancellationToken)
        {
            lock (_syncroot)
            {
                using (var db = new LiteEngine(databaseFileName))
                {
                    foreach (var key in keys)
                    {
                        //_memory.Remove(key);
                        db.Delete(CollectionName, Query.EQ(KeyName, key));
                    }
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Reads storage items from storage.
        /// </summary>
        /// <param name="keys">keys of the <see cref="IStoreItem"/> objects to read.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>If the activities are successfully sent, the task result contains
        /// the items read, indexed by key.</remarks>
        /// <seealso cref="DeleteAsync(string[], CancellationToken)"/>
        /// <seealso cref="WriteAsync(IDictionary{string, object}, CancellationToken)"/>
        public Task<IDictionary<string, object>> ReadAsync(string[] keys, CancellationToken cancellationToken)
        {
            var storeItems = new Dictionary<string, object>(keys.Length);
            lock (_syncroot)
            {
                using (var db = new LiteEngine(databaseFileName))
                {
                    foreach (var key in keys)
                    {
                        var value = db.Find(CollectionName, Query.EQ(KeyName, key)).FirstOrDefault();
                        if (value != null)
                        {
                            var state = JsonConvert.DeserializeObject(value[ContentName]);

                            if (state != null)
                            {
                                storeItems.Add(key, JObject.FromObject(state, StateJsonSerializer));
                            }
                        }
                    }
                }
            }

            return Task.FromResult<IDictionary<string, object>>(storeItems);
        }

        /// <summary>
        /// Writes storage items to storage.
        /// </summary>
        /// <param name="changes">The items to write, indexed by key.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <seealso cref="DeleteAsync(string[], CancellationToken)"/>
        /// <seealso cref="ReadAsync(string[], CancellationToken)"/>
        public Task WriteAsync(IDictionary<string, object> changes, CancellationToken cancellationToken)
        {
            lock (_syncroot)
            {
                using (var db = new LiteEngine(databaseFileName))
                {

                    foreach (var change in changes)
                    {
                        var newValue = change.Value;

                        var oldStateETag = default(string);
                        var oldValue = db.Find(CollectionName, Query.EQ(KeyName, change.Key)).FirstOrDefault();
                        if (oldValue != null)
                        {
                            var oldState = JObject.FromObject(JsonConvert.DeserializeObject(oldValue[ContentName]), StateJsonSerializer);

                            //if (oldState.GetType().GetProperty("eTag") != null)
                            //{
                            //    oldStateETag = (string)oldState.GetType().GetProperty("eTag").GetValue(oldState);
                            //}
                            if (oldState.TryGetValue("eTag", out var etag))
                            {
                                oldStateETag = etag.Value<string>();
                            }
                        }

                        //if (_memory.TryGetValue(change.Key, out var oldState))
                        //{
                        //    if (oldState.TryGetValue("eTag", out var etag))
                        //    {
                        //        oldStateETag = etag.Value<string>();
                        //    }
                        //}

                        var newState = JObject.FromObject(newValue, StateJsonSerializer);

                        // Set ETag if applicable
                        if (newValue is IStoreItem newStoreItem)
                        {
                            if (oldStateETag != null
                                    &&
                               newStoreItem.ETag != "*"
                                    &&
                               newStoreItem.ETag != oldStateETag)
                            {
                                throw new Exception($"Etag conflict.\r\n\r\nOriginal: {newStoreItem.ETag}\r\nCurrent: {oldStateETag}");
                            }

                            newState["eTag"] = (_eTag++).ToString();
                        }

                        string json = JsonConvert.SerializeObject(newState, Formatting.None);
                        db.Upsert(CollectionName, new BsonDocument { { KeyName, change.Key }, { ContentName, json } } ); // false (update)

                        //_memory[change.Key] = newState;
                    }

                }//end using
            }

            return Task.CompletedTask;
        }
    }
}
