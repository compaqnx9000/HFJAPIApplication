using HFJAPIApplication.BO;
using HFJAPIApplication.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.Controllers
{
    [ApiController]
    public class DamageAreaController : ControllerBase
    {
        private readonly IDamageAnalysisService _analysisService;
        public DamageAreaController(IDamageAnalysisService analysisService)
        {
            _analysisService = analysisService ??
                throw new ArgumentNullException(nameof(analysisService));
        }

        [HttpGet("damagearea/merge")]
        public IActionResult Merge()
        {
            // 根据DD接口和rule表
            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = _analysisService.Merge()
            });
        }

        [HttpGet("damagearea/area")]
        public IActionResult Area()
        {
            // 根据DD接口和rule表
            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = new
                {
                    damageGeometry=_analysisService.Area()
                }
            });
        }

        [HttpGet("damagearea/multi")]
        public IActionResult Multi()
        {
            // 基于DD态势，针对每枚导弹，返回不同类型的损伤范围。
            // 根据DD接口和rule表,
            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = _analysisService.Multi()
            });
        }

        [HttpPost("damagearea/missile/merge")]
        public IActionResult MissileMulti([FromBody] MissileBO bo)
        {
            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = _analysisService.MissileMulti(bo)
            });
        }

        [HttpPost("damagearea/missile/area")]
        public IActionResult MissileArea([FromBody] MissileBO bo)
        {
            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = _analysisService.MissileArea(bo)
            });
        }
    }
}
