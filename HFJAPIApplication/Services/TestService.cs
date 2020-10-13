using HFJAPIApplication.BO;
using HFJAPIApplication.VO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.Services
{
    public class TestService : ITestService
    {
        public IConfiguration Configuration { get; }
        public ILogger<TestService> _logger = null;
        private readonly IMongoService _mongoService;

        public TestService(IMongoService mongoService,
                                ILogger<TestService> logger,
                                IConfiguration configuration)
        {
            Configuration = configuration;

            _mongoService = mongoService ??
               throw new ArgumentNullException(nameof(mongoService));


            this._logger = logger;

        }

        
            
        public List<TestResultVO> TestStart(ref double damageR)
        {
            List<TestResultVO> result = new List<TestResultVO>();
            DD dd = null;
            // 1. 访问DD接口，拿到数据
            var url = Configuration["ServiceUrls:MissileInfo"];//http://localhost:5000/nuclearthreatanalysis/missileinfo
            try
            {
                Task<string> s = MyCore.Utils.HttpCli.GetAsyncJson(url);
                s.Wait();

                //JObject jo = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(s.Result);
                dd = Newtonsoft.Json.JsonConvert.DeserializeObject<DD>(s.Result);

            }
            catch (Exception e)
            {
                _logger.LogInformation("DD访问接口出错" + e.ToString());
                //Console.WriteLine(e.Message);
            }
            finally
            {

            }
            // 2. 读取info表里的井和车
            // 3. 计算井和车距离爆点的距离

            var infos = _mongoService.GetInfos();
            foreach (var info in infos)
            {
                // 只要井和车
                if (info.platform != "发射井" && info.platform != "发射车")
                    continue;

                double dis = MyCore.Utils.Translate.GetDistance(dd.return_data[0].lat, dd.return_data[0].lon, info.lat, info.lon);
                result.Add(new TestResultVO(info.platform, info.lon, info.lat, dis));

            }


            double r1 = MyCore.NuclearAlgorithm.GetShockWaveRadius(dd.return_data[0].yield, dd.return_data[0].alt, 1);
            double r2 = MyCore.NuclearAlgorithm.GetNuclearRadiationRadius(dd.return_data[0].yield, dd.return_data[0].alt, 100);
            double r3 = MyCore.NuclearAlgorithm.GetThermalRadiationRadius(dd.return_data[0].yield, dd.return_data[0].alt, 1.9);
            double r4 = MyCore.NuclearAlgorithm.GetNuclearPulseRadius(dd.return_data[0].yield, dd.return_data[0].alt, 200);

            // 4. 返回距离和综合损伤半径
            damageR = r4;


            return result;
        }
    }
}
