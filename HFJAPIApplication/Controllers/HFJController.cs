using HFJAPIApplication.BO;
using HFJAPIApplication.Mock;
using HFJAPIApplication.services;
using HFJAPIApplication.Services;
using HFJAPIApplication.VO;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace HFJAPIApplication.Controllers
{
    [EnableCors("AllowSameDomain")]
    [ApiController]
    public class HFJController : ControllerBase
    {
        private readonly IDamageAnalysisService _analysisService;
        private static int count = 0;

        public HFJController(IDamageAnalysisService analysisService)
        {
            _analysisService = analysisService ??
                throw new ArgumentNullException(nameof(analysisService));
        }

        [HttpGet("infochanged")]
        public int InfoChanged()
        {
            return _analysisService.InfoChanged();
        }

        /// <summary>
        /// 模拟接口。
        /// </summary>
        /// <returns></returns>
        [HttpGet("nuclearthreatanalysis/missileinfo")]
        public ActionResult Missileinfo()
        {
            count++;

            List<MissileVO> missiles = new List<MissileVO>();

            missiles.Add(new MissileVO("6001", "W78", 0.01, 110.79119, 33.345433, 10.11, 60, 12.12, 1000.11, "111-111-111-111"));//Safe
            //missiles.Add(new MissileVO("1001", "W78", 0.01, 110.79119, 33.345433, 10.11, 40, 12.12, 1000.11, "111-111-111-111"));//Safe
            //missiles.Add(new MissileVO("2001", "W78", 10, 110.79119, 33.345433, 10.11, 30, 12.12, 1000.11));//Light
            //missiles.Add(new MissileVO("3001", "W88", 10, 110.79119, 33.345433, 10.11, 50, 12.12, 1000.11));//Light
            //missiles.Add(new MissileVO("4001", "W98", 10, 110.79119, 33.345433, 10.11, 80, 12.12, 1000.11));//Light
            //missiles.Add(new MissileVO("5001", "W98", 10, 110.79119, 33.345433, 10.11, 90, 12.12, 1000.11));//Light

            //missiles.Add(new MissileVO("1001", "W78", 10, 110.79119, 33.345433, 10.11, 60, 12.12, 1000.11));//Light
            //missiles.Add(new MissileVO("2001", "W88", 100, 110.79119, 33.345433, 20.22, 30, 13.13, 2000.22));//Heavy

            missiles.Add(new MissileVO("3001", "W98", 50000, 110.79119, 33.345433, 60.96, 30, 13.13, 2000.22, "222-222-222-222"));//Light    
                                                                                                                                  //missiles.Add(new MissileVO("3001", "W98", 500000, 110.79119, 33.345433, 60.96, 30, 13.13, 2000.22));//Heavy    
                                                                                                                                  //missiles.Add(new MissileVO("3001", "W98", 100000000, 110.79119, 33.345433, 60.96, 60, 13.13, 2000.22));//Heavy，空爆200英尺

            if (count > 5)
                missiles.Add(new MissileVO("3001", "W98", 100000000, 110.79119, 33.345433, 0, 90, 13.13, 2000.22, "5435-2345-2345-44"));//Destory，地爆

            //{
            //    "missileID": "1001",
            //    "warHeadNo": "W78",
            //    "yield": 10000,
            //    "lon": 110.23,
            //    "lat": 34.23,
            //    "alt": 10.12,
            //    "impactTimeUTC": 1589270740.123,
            //    "measurement": 12.12,
            //    "attackAccuracy": 1000.12
            //}
            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "查询成功",
                return_data = missiles
            });
        }
        /// <summary>
        /// 不間斷調用（1秒）。
        /// </summary>
        /// <returns></returns>
        [HttpGet("damage")]
        public ActionResult Damage()
        {
            var r = _analysisService.GetDamageResult();
            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = r
            });
        }


        [HttpPost("damage/select")]
        public ActionResult Select([FromBody] FilterBO bo)
        {
            // 因為上一個接口是不停的調用，所以，這個接口就不用單獨算了，直接篩選上一個接口的結果就可以了

            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = _analysisService.Select(bo)
            });
        }

        // TODO: add 0701
        [HttpPost("attack")]
        public ActionResult Attack([FromBody] AttackBO bo)
        {
            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "计算成功",
                return_data = _analysisService.Attack(bo)
            });
        }

        // TODO: add 0702
        [HttpGet("damage/counter")]
        public ActionResult Counter()
        {
            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "计算成功",
                return_data = _analysisService.GetCounterResult()
            });
        }

    }
}
