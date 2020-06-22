
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using HFJAPIApplication;
using HFJAPIApplication.BO;

namespace HFJAPIApplication.Services
{
    public class MongoService : IMongoService
    {
        private MongoSetting _config;
        private MongoClient _client = null;
        public MongoService(IOptions<MongoSetting> setting)
        {
            _config = setting.Value;
            string conn = "mongodb://" + _config.IP + ":" + _config.Port;
            _client = new MongoClient(conn);
        }

        public List<InfoBO> GetInfos()
        {
            List<InfoBO> infos = new List<InfoBO>();
            var collection = _client.GetDatabase(_config.InfoSetting.Database).
              GetCollection<BsonDocument>(_config.InfoSetting.Collection);

            var list = collection.Find(Builders<BsonDocument>.Filter.Empty).ToList();
            foreach (var doc in list)
            {
                var info = BsonSerializer.Deserialize<InfoBO>(doc);
                infos.Add(info);
            }
            return infos;
        }

    }
}
