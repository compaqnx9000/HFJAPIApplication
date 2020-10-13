using HFJAPIApplication.BO;
using HFJAPIApplication.Mock;
using HFJAPIApplication.Services;
using HFJAPIApplication.VO;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace HFJAPIApplication.Controllers
{
    [ApiController]
    public class HFJController : ControllerBase
    {
        private readonly ILogger<HFJController> _logger;
        private static int count = 0;


        private readonly IDamageAnalysisService _analysisService;

        public HFJController(IDamageAnalysisService analysisService, ILogger<HFJController> logger)
        {
            _analysisService = analysisService ??
                throw new ArgumentNullException(nameof(analysisService));

            _logger = logger;
        }

        [HttpGet("infochanged")]
        public int InfoChanged()
        {
            return _analysisService.InfoChanged();
        }

        /// <summary>
        /// 不間斷調用（1秒）。
        /// </summary>
        /// <returns></returns>
        [HttpGet("damage")]
        public ActionResult Damage()
        {
            _logger.LogDebug("Damage被调用");

            var r = _analysisService.GetDamageResult();
            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = r
            });
        }
        [HttpPost("damage/select")]
        public ActionResult SelectEx([FromBody] dynamic bo)
        {
            try
            {
                //_logger.LogDebug("[HttpPost(damage / select)]" + ((JObject)bo).ToString());
                //count++;
                //Console.WriteLine($"Select被调用{count}次");
                return new JsonResult(new
                {
                    return_status = 0,
                    return_msg = "",
                    return_data = _analysisService.Select(bo)
                });
            }
            catch (Exception e)
            {
                //_logger.LogDebug("[HttpPost(damage / select)]" + e.ToString());

            }
            return new JsonResult(new
            {
                return_status = 1,
                return_msg = "",
                return_data = ""
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
