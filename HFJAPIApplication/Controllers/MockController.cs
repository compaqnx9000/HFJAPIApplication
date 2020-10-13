using HFJAPIApplication.Mock;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.Controllers
{
    [ApiController]
    public class MockController : ControllerBase
    {
        private static int count = 0;
        private ILogger<MockController> _logger;

        public MockController(ILogger<MockController> logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// 模拟接口。
        /// </summary>
        /// <returns></returns>
        [HttpGet("nuclearthreatanalysis/missilesnapshot/missileinfo")]
        public ActionResult Missileinfo()
        {
            count++;

            List<MissileVO> missiles = new List<MissileVO>();
            // 10-08 测试损伤
            missiles.Add(new MissileVO("3001", "W98", 300000, -104.848061, 38.746976, 0, 100, 13.13, 2000.22, "222-222-222-222"));//Heavy，空爆200英尺
            //missiles.Add(new MissileVO("3001", "W99", 335000, 110.625, 33.280, 0, 200, 13.13, 2000.22, "222-222-222-222"));//Heavy，空爆200英尺

            //if (count % 2 == 0)
            //{
            //    //_logger.LogDebug("count % 2");
            //    missiles.Add(new MissileVO("6001", "W78", 100000, 110.79119, 33.345433, 10.11, 60, 12.12, 1000.11, "111-111-111-111"));//Safe
            //    missiles.Add(new MissileVO("3001", "W98", 500000, 110.79119, 33.345433, 60.96, 30, 13.13, 2000.22, "222-222-222-222"));//Light    
            //}
            //else if (count % 3 == 0)
            {
                //_logger.LogDebug("count % 3");

                //missiles.Add(new MissileVO("2001", "W88", 100000, 110.79119, 33.345433, 20.22, 30, 13.13, 2000.22, "222-222-222-222"));//Heavy
            }
            //else if (count % 5 == 0)
            //{
            //    //_logger.LogDebug("count % 5");

            //    missiles.Add(new MissileVO("1001", "W78", 1000000, 110.79119, 33.345433, 10.11, 40, 12.12, 1000.11, "111-111-111-111"));//Safe
            //    missiles.Add(new MissileVO("2001", "W78", 1000000, 110.79119, 33.345433, 10.11, 30, 12.12, 1000.11, "111-111-111-111"));//Light
            //    missiles.Add(new MissileVO("3001", "W88", 1000000, 110.79119, 33.345433, 10.11, 50, 12.12, 1000.11, "111-111-111-111"));//Light
            //    missiles.Add(new MissileVO("4001", "W98", 1000000, 110.79119, 33.345433, 10.11, 80, 12.12, 1000.11, "111-111-111-111"));//Light
            //    missiles.Add(new MissileVO("5001", "W98", 1000000, 110.79119, 33.345433, 10.11, 90, 12.12, 1000.11, "111-111-111-111"));//Light
            //}

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
    }
}
