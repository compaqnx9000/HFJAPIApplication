using HFJAPIApplication.services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.Controllers
{
    public class AsyncTestController : ControllerBase
    {
        private readonly IDamageAnalysisService _analysisService;
        public AsyncTestController(IDamageAnalysisService analysisService)
        {
            _analysisService = analysisService;
        }
        [HttpGet("async")]
        public async Task<IActionResult> AsyncTest()
        {
            var stu = await _analysisService.GetStuInfoAsync("10000");
            return new JsonResult( "ok");
        }
    }
}
