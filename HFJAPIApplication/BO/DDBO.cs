using HFJAPIApplication.Mock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.BO
{
    /// <summary>
    /// 电子所提供的DD接口对应的类。
    /// </summary>
    public class DD
    {
        public int return_status { get; set; }
        public string return_msg { get; set; }
        public List<MissileVO> return_data { get; set; }
    }


}
