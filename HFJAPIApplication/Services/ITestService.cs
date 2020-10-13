using HFJAPIApplication.VO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.Services
{
    public interface ITestService
    {
        List<TestResultVO> TestStart(ref double damageR);

    }
}
