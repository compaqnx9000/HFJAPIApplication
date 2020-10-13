
using HFJAPIApplication.BO;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.Services
{
    public interface IMongoService
    {

        /* Info表操作 */
        List<InfoBO> GetInfos();
        RuleBo QueryRule(string name);

        /* overlay表操作 */
        List<OverlayBO> GetOverlays();


        // test
        List<InfoBO> FindTargetByTag(Dictionary<string, List<string>> tagGroups);

        // Config表
        List<ConfigBO> GetConfigs();


    }
}
