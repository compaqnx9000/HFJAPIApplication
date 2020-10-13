using HFJAPIApplication.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.Controllers
{
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly ITestService _testService;
        private readonly IDamageAnalysisService _analysisService;


        public TestController(IDamageAnalysisService analysisService, ITestService testService)
        {
            _analysisService = analysisService ??
                throw new ArgumentNullException(nameof(analysisService)); 
            _testService = testService;
        }


        [HttpGet("calc/dis")]
        public IActionResult CalcDis()
        {
            // 1. 访问DD接口，拿到数据
            // 2. 读取info表里的井和车
            // 3. 计算井和车距离爆点的距离
            // 4. 返回距离和综合损伤半径

            double damageR = 0;
            var lst = _testService.TestStart(ref damageR);
            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "综合损伤半径:"+ damageR.ToString(),
                return_data = lst
            });
        }
        [HttpGet("report/data")]
        public IActionResult ReportData()
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();  //开始监视代码运行时间

            // 1. damage接口数据(0.0015m)
            var r1 = _analysisService.GetDamageResult();

            // 2. damage/counter接口数据
            var r2 = _analysisService.GetCounterResult();

            watch.Stop();  //停止监视
            TimeSpan timespan = watch.Elapsed;  //获取当前实例测量得出的总时间
            System.Diagnostics.Debug.WriteLine("打开窗口代码执行时间：{0}(毫秒)", timespan.TotalMilliseconds);  //总毫秒数

            // 3. damagearea/area接口数据
            var r3 = _analysisService.Area();

            // 4. kt/query接口数据
            //var r4 = _analysisService.GetCounterResult();


            return new JsonResult(new
            {
                return_status = 0,
                return_msg = timespan.TotalMilliseconds,
                return_data = new
                {
                    damage = r1,
                    counter = r2,
                    area = r3,
                    //query = r4
                }
            });

        }

    }
}
