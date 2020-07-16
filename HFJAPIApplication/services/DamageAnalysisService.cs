using HFJAPIApplication.BO;
using HFJAPIApplication.Mock;
using HFJAPIApplication.Services;
using HFJAPIApplication.VO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace HFJAPIApplication.services
{
    public class DamageAnalysisService:IDamageAnalysisService
    {
        public static object _damageLocker = new object();//添加一个对象作为锁
        public static object _counterLocker = new object();//添加一个对象作为锁
        public static object _infoLocker = new object();//添加一个对象作为锁

        private readonly IMongoService _mongoService;
        private ServiceUrls _config;
        private static List<BO.InfoBO> _infos;
        //private static ConcurrentBag<DamageVO> _damageVOs = new ConcurrentBag<DamageVO>();
        //private static ConcurrentBag<DamageVO> _returnDamageVOs = new ConcurrentBag<DamageVO>();

        private static List<DamageVO> _damageVOs = new List<DamageVO>();
        private static List<DamageVO> _returnDamageVOs = new List<DamageVO>();

        // 0715现在damage接口要保留一个8种类型的list，因为select接口现在需要8种类型了
        private static List<DamageVO> _noFilterDamageVOs = new List<DamageVO>();


        private static volatile List<CounterVO> _counterVOs = new List<CounterVO>();
        private static volatile List<CounterVO> _returnCounterVOs = new List<CounterVO>();


        public ILogger<DamageAnalysisService> _logger = null;
        private static bool _firstRun = true;


        public int InfoChanged()
        {
            lock (_infoLocker)
            {
                _infos = _mongoService.GetInfos();
            }
            return 1;
        }

        public List<AttackVO> Attack(AttackBO bo)
        {
            List<AttackVO> attacks = new List<AttackVO>();
            lock (_infoLocker)
            {
                foreach (var info in _infos)
                {
                    double dis = MyCore.Utils.Translate.GetDistance(info.lat, info.lon, bo.Lat, bo.Lon);
                    if (dis <= bo.Range && (info.platform.Equals("发射车") || info.platform.Equals("发射井")))
                    {
                        attacks.Add(new AttackVO(info.launchUnit, info.platform,
                                                    info.warZone, dis,
                                                    info.warBase, info.brigade));
                    }
                }
            }
                

            return attacks;
        }

        public List<CounterVO> GetCounterResult()
        {
            return _returnCounterVOs;
        }

        private void Counter()
        {
            _counterVOs.Clear();

            // 1. 获取Damage计算结果，找到时间片>1的第一个时间点
            foreach (DamageVO damageVO in _returnDamageVOs)
            {
                // 只要井和车
                if (damageVO.platform != "发射井" && damageVO.platform != "发射车")
                    continue;

                StatusTimeRangesVO statusTime =  damageVO.statusTimeRanges.Where(it => it.Status > 1).FirstOrDefault();
                if (statusTime != null)
                {
                    long startTimeUtc = (long)statusTime.StartTimeUtc;
                    DateTime? dt = MyCore.Utils.DataTimeUtil.ToDateTime(startTimeUtc);
                    DateTime? dt0 =null;
                    double prepareTimeUtc = 0;

                    if (dt != null)
                    {
                        dt0 = dt.Value.AddSeconds(-damageVO.prepareTime);

                        //prepareTimeUtc = DataTimeUtil.ToTimestamp(dt0.Value,false);
                        // 不要基于毫秒算，用秒
                        prepareTimeUtc = startTimeUtc - damageVO.prepareTime;

                        CounterVO counter = new CounterVO();
                        counter.launchUnitInfo = damageVO.launchUnitInfo;
                        counter.timeRanges.Add(new TimeRange(0, prepareTimeUtc, 0));
                        counter.timeRanges.Add(new TimeRange(prepareTimeUtc, startTimeUtc, 1));
                        counter.timeRanges.Add(new TimeRange(startTimeUtc, MyCore.Utils.Const.TimestampMax, 2));
                        counter.nonce = Guid.NewGuid().ToString();

                        _counterVOs.Add(counter);
                    }
                }
                else
                {
                    // 没有收到打击
                    CounterVO counter = new CounterVO();
                    counter.launchUnitInfo = damageVO.launchUnitInfo;
                    counter.timeRanges.Add(new TimeRange(0, MyCore.Utils.Const.TimestampMax, 0));
                    counter.nonce = Guid.NewGuid().ToString();
                    _counterVOs.Add(counter);
                }
            }
            // 判断“counters”与“_reallyCounters”的“timeRanges”是否有变化？
            if (_counterVOs.Count() == _returnCounterVOs.Count())
            {
                for (int i = 0; i < _counterVOs.Count(); i++)
                {
                    CounterVO previousCounterVO = _returnCounterVOs[i];
                    CounterVO currentCounterVO = _counterVOs[i];

                    // 如果没有变化，把上一次的nonce值赋给这次的
                    if (previousCounterVO.Equals(currentCounterVO))
                        currentCounterVO.nonce = previousCounterVO.nonce;
                }
            }

            lock (_counterLocker)//锁
            {
                _returnCounterVOs = Clone(_counterVOs);
            }
        }
        public List<DamageVO> GetDamageResult()
        {
            //Thread.Sleep(5000);
            if(_firstRun)
            {
                _firstRun = false;
                Thread.Sleep(1000);
            }
            
            return _returnDamageVOs;
            
        }
        public virtual async Task<string> GetStuInfoAsync(string stuNo)
        {
            return await GetStuInfoAsync2(stuNo);
        }

        public virtual async Task<string> GetStuInfoAsync2(string stuNo)
        {
            return await Task.Run(() => this.GetDamageResult().ToString());
        }
        //public static ConcurrentBag<T> Clone<T>(object List)
        //{
        //    using (Stream objectStream = new MemoryStream())
        //    {
        //        IFormatter formatter = new BinaryFormatter();
        //        formatter.Serialize(objectStream, List);
        //        objectStream.Seek(0, SeekOrigin.Begin);
        //        return formatter.Deserialize(objectStream) as ConcurrentBag<T>;
        //    }
        //}

        public DamageAnalysisService(IMongoService mongoService, ILogger<DamageAnalysisService> logger,
                                IOptions<ServiceUrls> options)
        {
            _mongoService = mongoService ??
               throw new ArgumentNullException(nameof(mongoService));

            _config = options.Value;

            //this._logger = logger;

            _infos = _mongoService.GetInfos();

            //Damage();

            //创建线程，并启动
            Thread th = new Thread(new ThreadStart(ThreadMethod));                      
            th.Start(); 
        }

        private void Damage()
        {
            _damageVOs.Clear();
            List<MissileVO> dds = new List<MissileVO>();


            /*************************
             * 1. 调用接口获取导弹信息
             *************************/


            //导弹接口
            string url = _config.MissileInfo;//http://localhost:5000/nuclearthreatanalysis/missileinfo
           // _logger.LogInformation("URL:{0}", url);
            try
            {
                Task<string> s = GetAsyncJson(url);
                s.Wait();
                JObject jo = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(s.Result);

                dds = new List<MissileVO>();
                foreach (var obj in jo["return_data"])
                {
                    string missileID = obj["missileID"].ToString();
                    string warHeadNo = obj["warHeadNo"].ToString();
                    double yield = Double.Parse(obj["yield"].ToString());
                    double lon = Double.Parse(obj["lon"].ToString());
                    double lat = Double.Parse(obj["lat"].ToString());
                    double alt = Double.Parse(obj["alt"].ToString());
                    double impactTimeUTC = Double.Parse(obj["impactTimeUtc"].ToString());//报错了
                                                             
                    double measurement = Double.Parse(obj["measurement"].ToString());
                    double attackAccuracy = Double.Parse(obj["attackAccuracy"].ToString());
                    string nonce = obj["nonce"].ToString();

                    //_logger.LogInformation(string.Format("【missileID】：{0},【warHeadNo】：{1}," +
                    //    "【yield】：{2},【lon】：{3},【lat】：{4},【alt】：{5},,【impactTimeUTC】：{6}",

                   //     missileID, warHeadNo,yield,lon, lat, alt, impactTimeUTC));

                    dds.Add(new MissileVO(missileID, warHeadNo, yield, lon, lat, alt, impactTimeUTC, measurement, attackAccuracy, nonce));
                }

                // 按時間戳排序
                dds.Sort((a, b) => a.impactTimeUTc.CompareTo(b.impactTimeUTc));


            }
            catch (Exception e)
            {
                //_logger.LogInformation("DD访问接口出错");
                Console.WriteLine(e.Message);

            }
            finally
            {
               // Console.WriteLine("DD访问接口出错");
            }

            /****************************************
            * 2.循環計算Info表中的每一條記錄的損傷level
            ****************************************/
            //List<DamageVO> damageVOs = new List<DamageVO>();
            lock (_infoLocker)
            {
                foreach (var info in _infos)
                {
                    // 只要井和车
                    //if (info.platform != "发射井" && info.platform != "发射车")
                    //    continue;

                    DamageVO damageVO = new DamageVO();
                    damageVO.launchUnitInfo = new LaunchUnitInfoVO(info.launchUnit, info.warBase, info.brigade, info.missileNo);
                    damageVO.nonce = Guid.NewGuid().ToString();
                    damageVO.warBase = info.warBase;
                    damageVO.platform = info.platform;
                    damageVO.missileNum = info.missileNum;
                    damageVO.warZone = info.warZone;
                    damageVO.combatZone = info.combatZone;
                    damageVO.platoon = info.platoon;
                    damageVO.lon = info.lon;
                    damageVO.lat = info.lat;
                    damageVO.alt = info.alt;
                    damageVO.prepareTime = info.prepareTime;

                    if (info.platform.Equals("发射井"))
                    {
                        foreach (var dd in dds)
                        {
                            // GetDistance返回单位是：米。
                            double dis = MyCore.Utils.Translate.GetDistance(dd.lat, dd.lon, info.lat, info.lon);
                            // 对《发射井》有影响的是[冲击波]
                            dd.alt = 0;// 全部按地爆处理
                            var result = Airblast(dis, dd.yield / 1000, dd.alt * 3.2808399, info.shock_wave_01, info.shock_wave_02, info.shock_wave_03);
                            if (result != MyCore.enums.DamageEnumeration.Safe)
                            {
                                // 只记录照成损伤的DD
                                damageVO.missileList.Add(new MissileListVO(dd.missileID, dd.impactTimeUTc, (int)result));
                            }
                        }
                        if (damageVO.missileList.Count == 0)
                        {
                            damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(0, MyCore.Utils.Const.TimestampMax, 0));
                        }
                        else
                        {
                            int preDamageLevel = 0;
                            foreach (var missile in damageVO.missileList)
                            {
                                if (preDamageLevel >= 3) break;

                                int index = damageVO.missileList.IndexOf(missile); //index 为索引值

                                if (index == 0)
                                {
                                    // 第一枚导弹
                                    damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(0, missile.ImpactTimeUtc, 0));
                                    damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(missile.ImpactTimeUtc, MyCore.Utils.Const.TimestampMax, missile.DamageLevel));
                                    preDamageLevel = missile.DamageLevel;
                                }
                                else
                                {
                                    int currentDamageLevel = preDamageLevel + missile.DamageLevel;
                                    if (currentDamageLevel > 3) currentDamageLevel = 3;
                                    damageVO.statusTimeRanges[index].EndTimeUtc = missile.ImpactTimeUtc;
                                    damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(missile.ImpactTimeUtc, MyCore.Utils.Const.TimestampMax, currentDamageLevel));
                                    preDamageLevel = currentDamageLevel;
                                }
                            }
                        }
                    }
                    else if (info.platform.Equals("发射车"))
                    {

                        foreach (var dd in dds)
                        {
                            // GetDistance返回单位是：米。
                            double dis = MyCore.Utils.Translate.GetDistance(dd.lat, dd.lon, info.lat, info.lon);

                            dd.alt = 0;// 全部按地爆处理

                            // 对《发射车》有影响的是[ 冲击波 & 光辐射 & 核辐射 & 核电磁脉冲 ] ，取4种损伤最大的
                            var result1 = Airblast(dis, dd.yield / 1000, dd.alt * 3.2808399, info.shock_wave_01, info.shock_wave_02, info.shock_wave_03);
                            var result2 = ThermalRadiation(dis, dd.yield / 1000, dd.alt * 3.2808399, info.thermal_radiation_01,
                                                            info.thermal_radiation_02, info.thermal_radiation_03);
                            var result3 = NuclearRadiation(dis, dd.yield / 1000, info.alt * 3.2808399, info.nuclear_radiation_01,
                                                            info.nuclear_radiation_02, info.nuclear_radiation_03);
                            var result4 = NuclearPulse(dis / 1000, dd.yield, info.alt / 1000, info.nuclear_pulse_01,
                                                         info.nuclear_pulse_02, info.nuclear_pulse_03);
                            var result12 = (MyCore.enums.DamageEnumeration)Math.Max(result1.GetHashCode(), result2.GetHashCode());
                            var result34 = (MyCore.enums.DamageEnumeration)Math.Max(result3.GetHashCode(), result4.GetHashCode());

                            var result = (MyCore.enums.DamageEnumeration)Math.Max(result12.GetHashCode(), result34.GetHashCode());

                            if (result != MyCore.enums.DamageEnumeration.Safe)
                            {
                                // 只记录照成损伤的DD
                                damageVO.missileList.Add(new MissileListVO(dd.missileID, dd.impactTimeUTc, (int)result));
                            }
                        }
                        if (damageVO.missileList.Count == 0)
                        {
                            damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(0, MyCore.Utils.Const.TimestampMax, 0));
                        }
                        else
                        {
                            int preDamageLevel = 0;
                            foreach (var missile in damageVO.missileList)
                            {
                                if (preDamageLevel >= 3) break;

                                int index = damageVO.missileList.IndexOf(missile); //index 为索引值

                                if (index == 0)
                                {
                                    // 第一枚导弹
                                    damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(0, missile.ImpactTimeUtc, 0));
                                    damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(missile.ImpactTimeUtc, MyCore.Utils.Const.TimestampMax, missile.DamageLevel));
                                    preDamageLevel = missile.DamageLevel;
                                }
                                else
                                {
                                    int currentDamageLevel = preDamageLevel + missile.DamageLevel;
                                    if (currentDamageLevel > 3) currentDamageLevel = 3;
                                    damageVO.statusTimeRanges[index].EndTimeUtc = missile.ImpactTimeUtc;
                                    damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(missile.ImpactTimeUtc, MyCore.Utils.Const.TimestampMax, currentDamageLevel));
                                    preDamageLevel = currentDamageLevel;
                                }
                            }
                        }
                    }
                    else
                    {
                        // add 0715 什么类型都要，只不过不是井和车的不参与计算
                        damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(0, MyCore.Utils.Const.TimestampMax, 0));
                    }

                    // 时间片去重
                    for (int i = damageVO.statusTimeRanges.Count - 1; i >= 0; i--)
                    {
                        if (damageVO.statusTimeRanges[i].StartTimeUtc == damageVO.statusTimeRanges[i].EndTimeUtc)
                            damageVO.statusTimeRanges.RemoveAt(i);
                    }

                    _damageVOs.Add(damageVO);
                }
            }

                


            // 判断“_damageVOs”与“_returnDamageVOs”的“missileList”和“statusTimeRanges”是否有变化？
            if (_damageVOs.Count() == _returnDamageVOs.Count())
            {
                for (int i = 0; i < _damageVOs.Count(); i++)
                {
                    DamageVO previousDamageVO = _returnDamageVOs[i];
                    DamageVO currentDamageVO = _damageVOs[i];

                    if (previousDamageVO.Equals(currentDamageVO))
                        currentDamageVO.nonce = previousDamageVO.nonce;

                }
            }

            lock (_damageLocker)//锁
            {
                //0715 把8种类型的给_noFilterDamageVOs
                _noFilterDamageVOs = Clone(_damageVOs);

                //0715 把井和车类型的给_returnDamageVOs
                _returnDamageVOs.Clear();
                foreach(var vo in _damageVOs)
                {
                    if(vo.platform.Equals("发射井") || vo.platform.Equals("发射车"))
                    {
                        _returnDamageVOs.Add(vo);
                    }
                }
            }


            //反击时间片
            Counter();
               
        }
        public List<SelectVO> Select(FilterBO bo)
        {
            /*************************************
            * 在_damageVOs中查找
            * {
                 "基地":["11","12"],用info里的《warBase》
                 "发射平台":["井","车"],用info里的《platform》
                 "弹型":["DF-5C"]  用info里的《missileNo》。
              }
            *************************************/
            var r = _noFilterDamageVOs;

            //_damageVOs
            List<SelectVO> selectVOs = new List<SelectVO>();

            for (int i = 0; i < r.Count; i++)
            {
                int index1 = 0, index2 = 0, index3 = 0;
                var damageVO = r.ElementAt(i);
                //判断当前warBase是否在body的基地数组内
                //index1 = Array.IndexOf(bo.基地, damageVO.warBase);
                //index2 = Array.IndexOf(bo.发射平台, damageVO.platform);

                if (bo.基地 == null) index1 = 0;
                else index1 = Array.IndexOf(bo.基地, damageVO.warBase);

                if (bo.发射平台 == null) index2 = 0;
                else index2 = Array.IndexOf(bo.发射平台, damageVO.platform);

                if (bo.弹型 == null) index3 = 0;
                else index3 = Array.IndexOf(bo.弹型, damageVO.launchUnitInfo.MissileNo);


                if (index1 >= 0 && index2 >= 0 && index3 >= 0)
                {
                    SelectVO selectVO = new SelectVO();

                    selectVO.launchUnit = damageVO.launchUnitInfo.LaunchUnit;
                    selectVO.platform = damageVO.platform;
                    selectVO.warZone = damageVO.warZone;
                    selectVO.combatZone = damageVO.combatZone;
                    selectVO.brigade = damageVO.launchUnitInfo.Brigade;
                    selectVO.platoon = damageVO.platoon;
                    selectVO.missileNo = damageVO.launchUnitInfo.MissileNo;
                    selectVO.missileNum = damageVO.missileNum;
                    selectVO.warBase = damageVO.warBase;
                    selectVO.lon = damageVO.lon;
                    selectVO.lat = damageVO.lat;
                    selectVO.alt = damageVO.alt;

                    selectVO.missileList = damageVO.missileList;
                    selectVO.statusTimeRanges = damageVO.statusTimeRanges;

                    selectVOs.Add(selectVO);
                }

            }

            return selectVOs;
        }


        private double GetShockWaveRadius(double yield, double ft,double psi)
        {
            MyCore.MyAnalyse myAnalyse = new MyCore.MyAnalyse();
            return myAnalyse.CalcShockWaveRadius(yield,ft, psi);
        }
        private double GetNuclearRadiationRadius(double yield, double ft, double rem)
        {
            MyCore.MyAnalyse myAnalyse = new MyCore.MyAnalyse();
            return myAnalyse.CalcNuclearRadiationRadius(yield, ft,rem);
        }
        private double GetThermalRadiationRadius(double yield, double ft, double threm)
        {
            MyCore.MyAnalyse myAnalyse = new MyCore.MyAnalyse();
            return myAnalyse.GetThermalRadiationR(yield,ft,threm);
        }
        private double GetNuclearPulseRadius(double yield, double ft, double vm)
        {
            MyCore.MyAnalyse myAnalyse = new MyCore.MyAnalyse();
            return myAnalyse.CalcNuclearPulseRadius(yield, ft, vm);
        }
        void ThreadMethod()
        {
            while (true)
            {
                Damage();
                //Thread.Sleep(5);//如果不延时，将占用CPU过高  
                Thread.Sleep(_config.Interval);//如果不延时，将占用CPU过高  
            }
        }

        private static async Task<string> GetAsyncJson(string url)
        {
            HttpClient client = new HttpClient();
            //HttpContent content = new StringContent();
            //content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        #region 计算
        private MyCore.enums.DamageEnumeration Airblast(double dis, double yield, double ft, double psi01, double psi02, double psi03)
        {
            // 冲击波
            double r1 = GetShockWaveRadius(yield, ft, psi01);
            double r2 = GetShockWaveRadius(yield, ft, psi02);
            double r3 = GetShockWaveRadius(yield, ft, psi03);

            if (dis <= r3) return MyCore.enums.DamageEnumeration.Destroy;
            if (dis <= r2) return MyCore.enums.DamageEnumeration.Heavy;
            if (dis <= r1) return MyCore.enums.DamageEnumeration.Light;

            return MyCore.enums.DamageEnumeration.Safe;
        }
        private MyCore.enums.DamageEnumeration ThermalRadiation(double dis, double yield, double ft, double cal01, double cal02, double cal03)
        {
            // 光辐射 =》营区、发射车、人员

            double r1 = GetThermalRadiationRadius(yield, ft, cal01);
            double r2 = GetThermalRadiationRadius(yield, ft, cal02);
            double r3 = GetThermalRadiationRadius(yield, ft, cal03);

            if (dis <= r3) return MyCore.enums.DamageEnumeration.Destroy;
            if (dis <= r2) return MyCore.enums.DamageEnumeration.Heavy;
            if (dis <= r1) return MyCore.enums.DamageEnumeration.Light;

            return MyCore.enums.DamageEnumeration.Safe;
        }
        private MyCore.enums.DamageEnumeration NuclearRadiation(double dis, double yield, double ft,
                                                    double rem01, double rem02, double rem03)
        {
            // 核辐射 =》发射场、发射车、人员

            double r1 = GetNuclearRadiationRadius(yield, ft, rem01);
            double r2 = GetNuclearRadiationRadius(yield, ft, rem02);
            double r3 = GetNuclearRadiationRadius(yield, ft, rem03);

            if (dis <= r3) return MyCore.enums.DamageEnumeration.Destroy;
            if (dis <= r2) return MyCore.enums.DamageEnumeration.Heavy;
            if (dis <= r1) return MyCore.enums.DamageEnumeration.Light;

            return MyCore.enums.DamageEnumeration.Safe;
        }
        private MyCore.enums.DamageEnumeration NuclearPulse(double dis, double yield, double km, double vm01, double vm02, double vm03)
        {
            // 核电磁脉冲 =》中心库、待机库、通信站、发射车

            double r1 = GetNuclearPulseRadius(yield, km, vm01);
            double r2 = GetNuclearPulseRadius(yield, km, vm02);
            double r3 = GetNuclearPulseRadius(yield, km, vm03);

            if (dis <= r3) return MyCore.enums.DamageEnumeration.Destroy;
            if (dis <= r2) return MyCore.enums.DamageEnumeration.Heavy;
            if (dis <= r1) return MyCore.enums.DamageEnumeration.Light;

            return MyCore.enums.DamageEnumeration.Safe;
        }
        #endregion

        public List<T> Clone<T>(List<T> inputList)
        {
            BinaryFormatter Formatter = new BinaryFormatter(null, new StreamingContext(StreamingContextStates.Clone));
            System.IO.MemoryStream stream = new System.IO.MemoryStream();
            Formatter.Serialize(stream, inputList);
            stream.Position = 0;
            var outList = Formatter.Deserialize(stream) as List<T>;
            stream.Close();
            return outList;
        }
    }
}
