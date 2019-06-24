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
        private static readonly JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };

        private string databaseFileName;
        private const string CollectionName = "storage";
        private const string KeyName = "_id";
        private const string ContentName = "content";
        private const string DefaultDbName = "mioData.db";

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteDbStorage"/> class.
        /// </summary>
        /// <param name="databaseFileName">Loaction of LiteDb database file name.</param>
        public LiteDbStorage(string databaseFileName)
        {
            this.databaseFileName = databaseFileName;
        }

        public LiteDbStorage()
        {
            //create a new LiteDb named mioData.db on current directory

            this.databaseFileName = DefaultDbName;
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
        public Task DeleteAsync(string[] keys, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            using (var db = new LiteEngine(databaseFileName))
            {
                foreach (var key in keys)
                {
                    //_memory.Remove(key);
                    db.Delete(CollectionName, Query.EQ(KeyName, key));
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
        public Task<IDictionary<string, object>> ReadAsync(string[] keys, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }


            var storeItems = new Dictionary<string, object>(keys.Length);
            using (var db = new LiteEngine(databaseFileName))
            {
                foreach (var key in keys)
                {
                    var value = db.Find(CollectionName, Query.EQ(KeyName, key)).FirstOrDefault();
                    if (value != null)
                    {
                        var state = JsonConvert.DeserializeObject(value[ContentName], settings);

                        if (state != null)
                        {
                            storeItems.Add(key, state);
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
        public Task WriteAsync(IDictionary<string, object> changes, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (changes == null)
            {
                throw new ArgumentNullException(nameof(changes));
            }

                using (var db = new LiteEngine(databaseFileName))
                {

                    foreach (var change in changes)
                    {
                        var newValue = change.Value;

                        var json = JsonConvert.SerializeObject(newValue, typeof(object), settings);

                    db.Upsert(CollectionName, new BsonDocument { { KeyName, change.Key }, { ContentName, json } }); // false (update)        

                }//end using
            }

            return Task.CompletedTask;
        }
    }
}
