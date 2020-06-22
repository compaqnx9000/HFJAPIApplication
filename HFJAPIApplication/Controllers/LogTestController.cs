using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HFJAPIApplication.Controllers
{
    [EnableCors("AllowSameDomain")]
    [ApiController]
    public class LogTestController : ControllerBase
    {
        //ILoggerFactory和ILogger都是系统内置的接口，它们两个都可以写日志，随便你用哪个都行
        public ILoggerFactory _Factory = null;
        public ILogger<LogTestController> _logger = null;

        //注意：ILoggerFactory的命名空间是Microsoft.Extensions.Logging;
        public LogTestController(ILoggerFactory factory, ILogger<LogTestController> logger)
        {
            this._Factory = factory;
            this._logger = logger;
        }
        [HttpGet("log")]
        public IActionResult Index()
        {
            this._Factory.CreateLogger<LogTestController>().LogError("这里出现了一个错误");
            this._logger.LogError("出现了严重的错误！");

            return Content("OK");
        }       

    }
}
