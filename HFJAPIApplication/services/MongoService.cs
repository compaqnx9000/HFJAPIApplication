
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
using Microsoft.Extensions.Configuration;

namespace HFJAPIApplication.Services
{
    public class MongoService : IMongoService
    {
        public IConfiguration Configuration { get; }

        private MongoClient _client = null;
        public MongoService(IConfiguration configuration)
        {
            Configuration = configuration;

            string conn = "mongodb://" + Configuration["MongoSetting:Ip"] + ":" + Configuration["MongoSetting:Port"];

            _client = new MongoClient(conn);
        }

        public List<InfoBO> GetInfos()
        {
            IMongoCollection<InfoBO> collection = _client.GetDatabase(Configuration["MongoSetting:InfoSetting:Database"]).
              GetCollection<InfoBO>(Configuration["MongoSetting:InfoSetting:Collection"]);
            return collection.Find(Builders<InfoBO>.Filter.Empty).ToList();
        }

        public RuleBo QueryRule(string name)
        {
            IMongoCollection<RuleBo> collection = _client.
                                    GetDatabase(Configuration["MongoSetting:RuleSetting:Database"])
                                   .GetCollection<RuleBo>(Configuration["MongoSetting:RuleSetting:Collection"]);
            return collection.Find(Builders<RuleBo>.Filter.Eq("name", name)).FirstOrDefault();
        }

        public List<OverlayBO> GetOverlays()
        {
            IMongoCollection<OverlayBO> collection = _client.
                                    GetDatabase(Configuration["MongoSetting:OverlaySetting:Database"]).
                                    GetCollection<OverlayBO>(Configuration["MongoSetting:OverlaySetting:Collection"]);
            return collection.Find(Builders<OverlayBO>.Filter.Empty).ToList();
        }


        //test
        public List<InfoBO> FindTargetByTag(Dictionary<string, List<string>> tagGroups)
        {
            if (tagGroups.Count() == 0)
            {
                return GetInfos();
            }
            
                
            IMongoCollection<InfoBO> collection = _client.
                                    GetDatabase(Configuration["MongoSetting:InfoSetting:Database"])
                                   .GetCollection<InfoBO>(Configuration["MongoSetting:InfoSetting:Collection"]);

            List<InfoBO> ret = new List<InfoBO>();

            FilterDefinitionBuilder<InfoBO> builder = new FilterDefinitionBuilder<InfoBO>();

            List<FilterDefinition<InfoBO>> filters = new List<FilterDefinition<InfoBO>>();
            foreach (var it in tagGroups)
            {
                string tagGroupName = it.Key;
                List<string> tagsInGroup = it.Value;

                Builders<InfoBO>.Filter.Exists(x => x.tags, true);

                var filter1 = builder.Exists(x => x.tags[tagGroupName], false);
                var filter2 = builder.AnyIn(r => r.tags[tagGroupName], it.Value);
                var filter = filter1 | filter2;

                filters.Add(filter);
            }
            MongoDB.Driver.FilterDefinition<InfoBO> finalFiter = filters.Count == 1 ? filters[0] : builder.And(filters);

            ret = collection.Find(finalFiter).ToList();

            return ret;
        }

        public List<ConfigBO> GetConfigs()
        {
            IMongoCollection<ConfigBO> collection = _client.GetDatabase(Configuration["MongoSetting:ConfigSetting:Database"])
                                    .GetCollection<ConfigBO>(Configuration["MongoSetting:ConfigSetting:Collection"]);

            return collection.Find(Builders<ConfigBO>.Filter.Empty).ToList();
        }

    }
}
