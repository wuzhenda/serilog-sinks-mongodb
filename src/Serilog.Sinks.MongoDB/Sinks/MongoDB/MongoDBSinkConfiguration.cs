﻿// Copyright 2014-2022 Serilog Contributors
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

using MongoDB.Driver;

namespace Serilog.Sinks.MongoDB
{
    public class MongoDBSinkConfiguration
    {
        public string CollectionName { get; private set; } = MongoDBSinkDefaults.CollectionName;

        public int BatchPostingLimit { get; private set; } = MongoDBSinkDefaults.BatchPostingLimit;

        public CreateCollectionOptions CollectionCreationOptions { get; private set; }

        public TimeSpan? ExpireTTL { get; private set; }

        public TimeSpan BatchPeriod { get; private set; } = MongoDBSinkDefaults.BatchPeriod;

        public MongoUrl MongoUrl { get; private set; }

        public IMongoDatabase MongoDatabase { get; private set; }

        public bool Legacy { get; internal set; }

        public void Validate()
        {
            if (MongoDatabase == null && MongoUrl == null)
            {
                throw new ArgumentOutOfRangeException(
                    "MongoDatabase or MongoUrl",
                    "Invalid Configuration: MongoDatabase or Mongo Connection String must be specified.");
            }

            if (MongoUrl != null && string.IsNullOrWhiteSpace(MongoUrl.DatabaseName))
            {
                throw new ArgumentNullException(
                    nameof(MongoUrl.DatabaseName),
                    "Database name is required in the MongoDb connection string. Use: mongodb://mongoDbServer/databaseName");
            }

            if (ExpireTTL.HasValue && Legacy)
            {
                throw new ArgumentNullException(
                    nameof(ExpireTTL),
                    "Expiration TTL is only supported on the MongoDBBson Sink");
            }
        }

        /// <summary>
        ///     Set the periodic batch timeout period. (Default: 2 seconds)
        /// </summary>
        /// <param name="period"></param>
        public void SetBatchPeriod(TimeSpan period)
        {
            this.BatchPeriod = period;
        }

        /// <summary>
        /// Sets the expiration time on all log documents: https://docs.mongodb.com/manual/tutorial/expire-data/
        /// Only supported for the MongoDBBson sink.
        /// </summary>
        /// <param name="timeToLive"></param>
        public void SetExpireTTL(TimeSpan? timeToLive)
        {
            this.ExpireTTL = timeToLive;
        }

        /// <summary>
        ///     Setup capped collections during collection creation
        /// </summary>
        /// <param name="cappedMaxSizeMb">(Optional) Max Size in Mb of the Capped Collection. Default is 50mb.</param>
        /// <param name="cappedMaxDocuments">(Optional) Max Number of Documents in the Capped Collection. Default is none.</param>
        public void SetCreateCappedCollection(
            long cappedMaxSizeMb = MongoDBSinkDefaults.CappedCollectionMaxSizeMb,
            long? cappedMaxDocuments = null)
        {
            this.CollectionCreationOptions = new CreateCollectionOptions
            {
                Capped = true,
                MaxSize = cappedMaxSizeMb * 1024 * 1024
            };

            if (cappedMaxDocuments.HasValue)
                this.CollectionCreationOptions.MaxDocuments = cappedMaxDocuments.Value;
        }

        /// <summary>
        ///     Set the mongo database instance directly
        /// </summary>
        /// <param name="database"></param>
        public void SetMongoDatabase(IMongoDatabase database)
        {
            this.MongoDatabase = database ?? throw new ArgumentNullException(nameof(database));
            this.MongoUrl = null;
        }

        /// <summary>
        ///     Set the mongo url (connection string) -- e.g. mongodb://localhost/databaseName
        /// </summary>
        /// <param name="mongoUrl"></param>
        public void SetMongoUrl(string mongoUrl)
        {
            if (string.IsNullOrWhiteSpace(mongoUrl))
                throw new ArgumentNullException(nameof(mongoUrl));

            this.MongoUrl = MongoUrl.Create(mongoUrl);
            this.MongoDatabase = null;
        }

        /// <summary>
        ///     Set the MongoDB collection name (Default: 'logs')
        /// </summary>
        /// <param name="collectionName"></param>
        public void SetCollectionName(string collectionName)
        {
            if (collectionName == string.Empty)
            {
                throw new ArgumentOutOfRangeException(nameof(collectionName), "Must not be string.empty");
            }

            this.CollectionName = collectionName ?? MongoDBSinkDefaults.CollectionName;
        }

        /// <summary>
        ///     Set the batch posting limit (Default: 50)
        /// </summary>
        /// <param name="batchPostingLimit"></param>
        public void SetBatchPostingLimit(int batchPostingLimit)
        {
            this.BatchPostingLimit = batchPostingLimit;
        }

#if NET452
        /// <summary>
        ///     Tries to set the Mongo url from a connection string in the .config file.
        /// </summary>
        /// <returns>false if not found</returns>
        /// <param name="connectionStringName"></param>
        public bool TrySetMongoUrlFromConnectionStringNamed(string connectionStringName)
        {
            if (string.IsNullOrWhiteSpace(connectionStringName))
                throw new ArgumentNullException(nameof(connectionStringName));

            var connectionString =
                System.Configuration.ConfigurationManager.ConnectionStrings[connectionStringName];

            if (connectionString == null)
                return false;

            this.SetMongoUrl(connectionString.ConnectionString);

            return true;
        }

        /// <summary>
        ///     Set the Mongo url (connection string) or Connection String Name -- e.g. mongodb://localhost
        /// </summary>
        /// <param name="connectionString"></param>
        public void SetConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            if (!this.TrySetMongoUrlFromConnectionStringNamed(connectionString))
                this.SetMongoUrl(connectionString);
        }
#else
        /// <summary>
        ///     Set the Mongo url (connection string)
        /// </summary>
        /// <param name="connectionString"></param>
        public void SetConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            this.SetMongoUrl(connectionString);
        }
#endif
    }
}