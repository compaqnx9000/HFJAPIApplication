using HFJAPIApplication.BO;
using HFJAPIApplication.Mock;
using HFJAPIApplication.VO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson.IO;
using NetTopologySuite.Geometries;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace HFJAPIApplication.Services
{
    public class DamageAnalysisService : IDamageAnalysisService, IDisposable
    {
        public IConfiguration Configuration { get; }

        private Task task;
        private CancellationTokenSource cts = new CancellationTokenSource();

        public static object _damageLocker = new object();//添加一个对象作为锁
        public static object _counterLocker = new object();//添加一个对象作为锁
        public static object _infoLocker = new object();//添加一个对象作为锁

        private readonly IMongoService _mongoService;
        private static List<InfoBO> _infos;
        private static List<OverlayBO> _overlays;
        //private static List<ConfigBO> _configs;
        private static Dictionary<string, int> additionTable = new Dictionary<string, int>();//加法表

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

        private static string _returnArea = "";//缓存damagearea/area接口

        public DamageAnalysisService(IMongoService mongoService,
                                ILogger<DamageAnalysisService> logger,
                                IConfiguration configuration)
        {
            Configuration = configuration;

            _mongoService = mongoService ??
               throw new ArgumentNullException(nameof(mongoService));


            this._logger = logger;

            _infos = _mongoService.GetInfos();
            //_configs = _mongoService.GetConfigs();


            _overlays = _mongoService.GetOverlays();
            foreach (var overlay in _overlays)
            {

                additionTable.Add(Damage2String(overlay.addend) + Damage2String(overlay.augend),
                    Int32.Parse(Damage2String(overlay.result)));

            }

            //创建线程，并启动
            //Thread th = new Thread(new ThreadStart(ThreadMethod));
            //th.Start();

            task = new Task(Run, cts.Token, TaskCreationOptions.LongRunning);
            task.Start();
        }
        public void Dispose()
        {
            cts.Cancel();
        }
        private string Damage2String(string s)
        {
            if (s == "轻微") return "1";
            else if (s == "中度") return "2";
            else if (s == "重度") return "3";
            return "0";
        }


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

                StatusTimeRangesVO statusTime = damageVO.statusTimeRanges.Where(it => it.Status > 1).FirstOrDefault();
                if (statusTime != null)
                {
                    long startTimeUtc = (long)statusTime.StartTimeUtc;
                    DateTime? dt = MyCore.Utils.DataTimeUtil.ToDateTime(startTimeUtc);
                    DateTime? dt0 = null;
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
                        counter.name = damageVO.name;//2020-10-11
                        counter.useState = damageVO.useState == null ? "未知" : damageVO.useState;//2020-10-13


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
                    counter.name = damageVO.name;//2020-10-11
                    counter.useState = damageVO.useState == null ? "未知" : damageVO.useState;//2020-10-13

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
            if (_firstRun)
            {
                _firstRun = false;
                Thread.Sleep(1000);
            }

            return _returnDamageVOs;

        }



        private void Damage()
        {
            _damageVOs.Clear();
            List<MissileVO> dds = new List<MissileVO>();


            /*************************
             * 1. 调用接口获取导弹信息
             *************************/

            //导弹接口
            var url = Configuration["ServiceUrls:MissileInfo"];//http://localhost:5000/nuclearthreatanalysis/missileinfo
            try
            {
                Task<string> s = MyCore.Utils.HttpCli.GetAsyncJson(url);
                s.Wait();

                //JObject jo = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(s.Result);
                DD dd = Newtonsoft.Json.JsonConvert.DeserializeObject<DD>(s.Result);
                dds = Clone(dd.return_data);

                // 按時間戳排序
                dds.Sort((a, b) => a.impactTimeUtc.CompareTo(b.impactTimeUtc));
            }
            catch (Exception e)
            {
                _logger.LogInformation("DD访问接口出错" + e.ToString());
                //Console.WriteLine(e.Message);
            }
            finally
            {
                // Console.WriteLine("DD访问接口出错");
            }

            try
            {
                /****************************************
            * 2.循環計算Info表中的每一條記錄的損傷level
            ****************************************/
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
                        damageVO.id = info._id.ToString();
                        damageVO.name = info.name;
                        damageVO.useState = info.useState==null?"未知" : info.useState;
                        //damageVO.tags = info.tags;

                        if (info.platform.Equals("发射井"))
                        {
                            foreach (var dd in dds)
                            {
                                dd.alt = 0;// 全部按地爆处理

                                // GetDistance返回单位是：米。
                                double dis = MyCore.Utils.Translate.GetDistance(dd.lat, dd.lon, info.lat, info.lon);
                                MyCore.enums.DamageEnumeration result1 = MyCore.enums.DamageEnumeration.Safe;
                                MyCore.enums.DamageEnumeration result2 = MyCore.enums.DamageEnumeration.Safe;
                                MyCore.enums.DamageEnumeration result3 = MyCore.enums.DamageEnumeration.Safe;
                                MyCore.enums.DamageEnumeration result4 = MyCore.enums.DamageEnumeration.Safe;
                                MyCore.enums.DamageEnumeration result = MyCore.enums.DamageEnumeration.Safe;

                                
                                // 1.受到冲击波影响？
                                if (info.shock_wave_01 > 0 && info.shock_wave_02 > 0 && info.shock_wave_03 > 0)
                                    result1 = MyCore.NuclearAlgorithm.Airblast(dis, dd.yield, dd.alt, info.shock_wave_01, info.shock_wave_02, info.shock_wave_03);
                                    
                                // 2.受到热辐射影响？
                                if (info.thermal_radiation_01 > 0 && info.thermal_radiation_02 > 0 && info.thermal_radiation_03 > 0)
                                    result2 = MyCore.NuclearAlgorithm.ThermalRadiation(dis, dd.yield, dd.alt, info.thermal_radiation_01, info.thermal_radiation_02, info.thermal_radiation_03);
                                 
                                // 3.受到核辐射影响？
                                if (info.nuclear_radiation_01 > 0 && info.nuclear_radiation_02 > 0 && info.nuclear_radiation_03 > 0)
                                    result3 = MyCore.NuclearAlgorithm.NuclearRadiation(dis, dd.yield, dd.alt, info.nuclear_radiation_01, info.nuclear_radiation_02, info.nuclear_radiation_03);
                                    
                                // 4.受到核电磁脉冲影响？
                                if (info.nuclear_pulse_01 > 0 && info.nuclear_pulse_02 > 0 && info.nuclear_pulse_03 > 0)
                                    result4 = MyCore.NuclearAlgorithm.NuclearPulse(dis, dd.yield, dd.alt, info.nuclear_pulse_01, info.nuclear_pulse_02, info.nuclear_pulse_03);
                                    

                                var result12 = (MyCore.enums.DamageEnumeration)Math.Max(result1.GetHashCode(), result2.GetHashCode());
                                var result34 = (MyCore.enums.DamageEnumeration)Math.Max(result3.GetHashCode(), result4.GetHashCode());
                                result = (MyCore.enums.DamageEnumeration)Math.Max(result12.GetHashCode(), result34.GetHashCode());

                                if (result != MyCore.enums.DamageEnumeration.Safe)
                                {
                                    // 只记录照成损伤的DD
                                    damageVO.missileList.Add(new MissileListVO(dd.missileID, dd.impactTimeUtc, (int)result));
                                }
                            }
                            if (damageVO.missileList.Count == 0)
                            {
                                damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(0, MyCore.Utils.Const.TimestampMax, 0));
                            }
                            else
                            {
                                int preDamageLevel = 0;
                                int index = 0;
                                foreach (var missile in damageVO.missileList)
                                {
                                    if (preDamageLevel >= 3) break;

                                    

                                    if (index == 0)
                                    {
                                        // 第一枚导弹
                                        damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(0, missile.ImpactTimeUtc, 0));
                                        damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(missile.ImpactTimeUtc, MyCore.Utils.Const.TimestampMax, missile.DamageLevel));
                                        preDamageLevel = missile.DamageLevel;
                                    }
                                    else
                                    {
                                        int currentDamageLevel = additionTable[preDamageLevel.ToString() + missile.DamageLevel.ToString()];
                                        //int currentDamageLevel2 = preDamageLevel + missile.DamageLevel;
                                        //if (currentDamageLevel > 3) currentDamageLevel = 3;
                                        damageVO.statusTimeRanges[index].EndTimeUtc = missile.ImpactTimeUtc;
                                        damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(missile.ImpactTimeUtc, MyCore.Utils.Const.TimestampMax, currentDamageLevel));
                                        preDamageLevel = currentDamageLevel;
                                    }
                                    index++;
                                }
                            }
                        }
                        else if (info.platform.Equals("发射车"))
                        {
                            foreach (var dd in dds)
                            {
                                dd.alt = 0;// 全部按地爆处理

                                // GetDistance返回单位是：米。
                                double dis = MyCore.Utils.Translate.GetDistance(dd.lat, dd.lon, info.lat, info.lon);

                                MyCore.enums.DamageEnumeration result1 = MyCore.enums.DamageEnumeration.Safe;
                                MyCore.enums.DamageEnumeration result2 = MyCore.enums.DamageEnumeration.Safe;
                                MyCore.enums.DamageEnumeration result3 = MyCore.enums.DamageEnumeration.Safe;
                                MyCore.enums.DamageEnumeration result4 = MyCore.enums.DamageEnumeration.Safe;
                                MyCore.enums.DamageEnumeration result = MyCore.enums.DamageEnumeration.Safe;

                                
                                // 1.受到冲击波影响？
                                if (info.shock_wave_01 > 0 && info.shock_wave_02 > 0 && info.shock_wave_03 > 0)
                                    result1 = MyCore.NuclearAlgorithm.Airblast(dis, dd.yield, dd.alt, info.shock_wave_01, info.shock_wave_02, info.shock_wave_03);
                                    
                                // 2.受到热辐射影响？
                                if (info.thermal_radiation_01 > 0 && info.thermal_radiation_02 > 0 && info.thermal_radiation_03 > 0)
                                    result2 = MyCore.NuclearAlgorithm.ThermalRadiation(dis, dd.yield, dd.alt, info.thermal_radiation_01, info.thermal_radiation_02, info.thermal_radiation_03);
                                    
                                // 3.受到核辐射影响？
                                if (info.nuclear_radiation_01 > 0 && info.nuclear_radiation_02 > 0 && info.nuclear_radiation_03 > 0)
                                    result3 = MyCore.NuclearAlgorithm.NuclearRadiation(dis, dd.yield, dd.alt, info.nuclear_radiation_01, info.nuclear_radiation_02, info.nuclear_radiation_03);
                                   
                                // 4.受到核电磁脉冲影响？
                                if (info.nuclear_pulse_01 > 0 && info.nuclear_pulse_02 > 0 && info.nuclear_pulse_03 > 0)
                                    result3 = MyCore.NuclearAlgorithm.NuclearPulse(dis, dd.yield, dd.alt, info.nuclear_pulse_01, info.nuclear_pulse_02, info.nuclear_pulse_03);
                                    
                                var result12 = (MyCore.enums.DamageEnumeration)Math.Max(result1.GetHashCode(), result2.GetHashCode());
                                var result34 = (MyCore.enums.DamageEnumeration)Math.Max(result3.GetHashCode(), result4.GetHashCode());
                                result = (MyCore.enums.DamageEnumeration)Math.Max(result12.GetHashCode(), result34.GetHashCode());

                                if (result != MyCore.enums.DamageEnumeration.Safe)
                                {
                                    // 只记录照成损伤的DD
                                    damageVO.missileList.Add(new MissileListVO(dd.missileID, dd.impactTimeUtc, (int)result));
                                }
                            }
                            if (damageVO.missileList.Count == 0)
                            {
                                damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(0, MyCore.Utils.Const.TimestampMax, 0));
                            }
                            else
                            {
                                int preDamageLevel = 0;
                                int index = 0;
                                foreach (var missile in damageVO.missileList)
                                {
                                    if (preDamageLevel >= 3) break;

                                    if (index == 0)
                                    {
                                        // 第一枚导弹
                                        damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(0, missile.ImpactTimeUtc, 0));
                                        damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(missile.ImpactTimeUtc, MyCore.Utils.Const.TimestampMax, missile.DamageLevel));
                                        preDamageLevel = missile.DamageLevel;
                                    }
                                    else
                                    {
                                        int currentDamageLevel = additionTable[preDamageLevel.ToString() + missile.DamageLevel.ToString()];
                                        //int currentDamageLevel = preDamageLevel + missile.DamageLevel;
                                        //if (currentDamageLevel > 3) currentDamageLevel = 3;
                                        damageVO.statusTimeRanges[index].EndTimeUtc = missile.ImpactTimeUtc;
                                        damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(missile.ImpactTimeUtc, MyCore.Utils.Const.TimestampMax, currentDamageLevel));
                                        preDamageLevel = currentDamageLevel;
                                    }
                                    index++;
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
                //if (_damageVOs.Count() == _returnDamageVOs.Count())
                //{
                //    for (int i = 0; i < _damageVOs.Count(); i++)
                //    {
                //        DamageVO previousDamageVO = _returnDamageVOs[i];
                //        DamageVO currentDamageVO = _damageVOs[i];

                //        if (previousDamageVO.Equals(currentDamageVO))
                //            currentDamageVO.nonce = previousDamageVO.nonce;

                //    }
                //}

                foreach (var vo in _returnDamageVOs)
                {
                    DamageVO previousDamageVO = vo;
                    DamageVO currentDamageVO = _damageVOs.Find((DamageVO damage) => damage == vo);
                    if (currentDamageVO != null)
                        currentDamageVO.nonce = previousDamageVO.nonce;
                }


                lock (_damageLocker)//锁
                {
                    //0715 把8种类型的给_noFilterDamageVOs
                    _noFilterDamageVOs = Clone(_damageVOs);

                    //0715 把井和车类型的给_returnDamageVOs
                    _returnDamageVOs.Clear();
                    foreach (var vo in _damageVOs)
                    {
                        if (vo.platform.Equals("发射井") || vo.platform.Equals("发射车"))
                        {
                            _returnDamageVOs.Add(vo);
                        }
                    }
                }


                //反击时间片
                Counter();

            }
            catch (Exception e)
            {

                _logger.LogInformation("循環計算Info表中的每一條記錄的損傷level:" + e.ToString());

            }


            // 往7078发
            try
            {
                string json1 = Newtonsoft.Json.JsonConvert.SerializeObject(_returnDamageVOs);
                //Configuration["PushUrls:damage"]     "http://localhost:7078/receivce/damage"
                Task<string> s = MyCore.Utils.HttpCli.PostAsyncJson(Configuration["PushUrls:damage"], json1);
                s.Wait();
            }
            catch (Exception)
            {
                Console.WriteLine("检查7078配置");
            }

            // 往7078发
            try
            {
                string json2 = Newtonsoft.Json.JsonConvert.SerializeObject(_returnCounterVOs);
                
                Task<string> s = MyCore.Utils.HttpCli.PostAsyncJson(Configuration["PushUrls:counter"], json2);
                s.Wait();
            }
            catch (Exception)
            {
                Console.WriteLine("检查7078配置");
            }


        }

        public List<SelectVO> Select(JObject bo)
        {
            //int a = 9;
            //int b = 0;
            //double c = a / b;
            /*************************************
            * 在_damageVOs中查找
            * {
                 "基地":["11","12"],用info里的tags
                 "发射平台":["井","车"],用info里的tags
                 "弹型":["DF-5C"]  用info里的tags。
              }
            *************************************/

            try
            {
                Dictionary<string, List<string>> body = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(bo.ToString());
                List<InfoBO> ret = _mongoService.FindTargetByTag(body);

                var r = _noFilterDamageVOs;

                //_damageVOs
                List<SelectVO> selectVOs = new List<SelectVO>();

                for (int i = 0; i < r.Count; i++)
                {
                    var damageVO = r.ElementAt(i);

                    if (ret.Find(s => s._id.ToString() == damageVO.id) == null)
                        continue;

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
                    selectVO.nonce = damageVO.nonce;
                    selectVO.decisionTimeUtc = MyCore.Utils.Const.TimestampMax;
                    selectVO.name = damageVO.name;

                    // 0730 添加了一个decisionTimeUtc，在 _returnCounterVOs里找launchUnit相等的，
                    // 把 _returnCounterVOs的timerange第一个endtime
                    // 赋值回来。如果找不到，就写decisionTimeUtc=999
                    var result = _returnCounterVOs.Find(s => s.launchUnitInfo.LaunchUnit.Equals(selectVO.launchUnit));
                    if (result != null)
                    {
                        selectVO.decisionTimeUtc = result.timeRanges[0].EndTimeUtc;
                    }

                    selectVO.missileList = damageVO.missileList;
                    selectVO.statusTimeRanges = damageVO.statusTimeRanges;

                    selectVOs.Add(selectVO);
                }

                return selectVOs;
            }
            catch (Exception e)
            {

                _logger.LogDebug("ExceptionList<SelectVO> Select(JObject bo):" + e.ToString());

            }

            return null;
        }

        //void ThreadMethod()
        //{
        //    while (true)
        //    {
        //        Damage();
        //        //Thread.Sleep(5);//如果不延时，将占用CPU过高  
        //        Thread.Sleep(_config.Interval);//如果不延时，将占用CPU过高  
        //    }
        //}
        private void Run()
        {
            while (!cts.Token.IsCancellationRequested)
            {
                // Your stuff goes here.
                Damage();
                AreaTemp();
                //Thread.Sleep(_config.Interval);//如果不延时，将占用CPU过高  
                Thread.Sleep(Int32.Parse(Configuration["ServiceUrls:Interval"]));//如果不延时，将占用CPU过高  

            }
        }
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

        // 2020-07-27
        public List<DamageAreaMergeVO> Merge()
        {
            double psi = 1; double rem = 100; double calcm = 1.9; double vm = 200;
            GetLimits(ref psi, ref rem, ref calcm, ref vm);

            // 读取mongo数据库中HB库，用于仿真模拟
            List<DamageAreaMergeVO> damageMergeVOs = new List<DamageAreaMergeVO>();

            List<MissileVO> dds = new List<MissileVO>();
            //导弹接口
            string url = Configuration["ServiceUrls:MissileInfo"];//http://localhost:5000/nuclearthreatanalysis/missileinfo
            // _logger.LogInformation("URL:{0}", url);
            try
            {
                Task<string> s = MyCore.Utils.HttpCli.GetAsyncJson(url);
                s.Wait();

                //JObject jo = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(s.Result);
                DD dd = Newtonsoft.Json.JsonConvert.DeserializeObject<DD>(s.Result);
                dds = Clone(dd.return_data);
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.Message);
            }

            Geometry geom_02 = null;
            Geometry geom_03 = null;
            Geometry geom_04 = null;
            Geometry geom_05 = null;

            foreach (var dd in dds)
            {
                double lon = dd.lon;
                double lat = dd.lat;
                double alt = dd.alt;
                double yield = dd.yield;

                if (geom_02 == null)
                    geom_02 = MyCore.NuclearAlgorithm.GetNuclearRadiationGeometry(lon, lat, yield, alt, rem);
                else
                    geom_02 = geom_02.Union(MyCore.NuclearAlgorithm.GetNuclearRadiationGeometry(lon, lat, yield, alt, rem));

                if (geom_03 == null)
                    geom_03 = MyCore.NuclearAlgorithm.GetShockWaveGeometry(lon, lat, yield, alt, psi);
                else
                    geom_03 = geom_03.Union(MyCore.NuclearAlgorithm.GetShockWaveGeometry(lon, lat, yield, alt, psi));

                if (geom_04 == null)
                    geom_04 = MyCore.NuclearAlgorithm.GetThermalRadiationGeometry(lon, lat, yield, alt, calcm);
                else
                    geom_04 = geom_04.Union(MyCore.NuclearAlgorithm.GetThermalRadiationGeometry(lon, lat, yield, alt, calcm));

                if (geom_05 == null)
                    geom_05 = MyCore.NuclearAlgorithm.GetNuclearPulseGeometry(lon, lat, yield, alt, vm);
                else
                    geom_05 = geom_05.Union(MyCore.NuclearAlgorithm.GetNuclearPulseGeometry(lon, lat, yield, alt, vm));
            }

            damageMergeVOs.Add(new DamageAreaMergeVO("早期核辐射", MyCore.Utils.Translate.Geometry2GeoJson(geom_02), rem, "rem"));
            damageMergeVOs.Add(new DamageAreaMergeVO("冲击波", MyCore.Utils.Translate.Geometry2GeoJson(geom_03), psi, "psi"));
            damageMergeVOs.Add(new DamageAreaMergeVO("光辐射", MyCore.Utils.Translate.Geometry2GeoJson(geom_04), calcm, "cal/cm²"));
            damageMergeVOs.Add(new DamageAreaMergeVO("核电磁脉冲", MyCore.Utils.Translate.Geometry2GeoJson(geom_05), vm, "v/m"));

            return damageMergeVOs;
        }

        /// <summary>
        /// 纯粹是为了测试用的
        /// </summary>
        /// <returns></returns>
        private string AreaTemp()
        {
            double test_need_r = 0;

            double psi = 1; double rem = 100; double calcm = 1.9; double vm = 200;
            GetLimits(ref psi, ref rem, ref calcm, ref vm);

            // 读取mongo数据库中HB库，用于仿真模拟
            List<DamageAreaMergeVO> damageMergeVOs = new List<DamageAreaMergeVO>();

            List<MissileVO> dds = new List<MissileVO>();
            //导弹接口
            string url = Configuration["ServiceUrls:MissileInfo"];//http://localhost:5000/nuclearthreatanalysis/missileinfo
            // _logger.LogInformation("URL:{0}", url);
            try
            {
                Task<string> s = MyCore.Utils.HttpCli.GetAsyncJson(url);
                s.Wait();

                //JObject jo = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(s.Result);
                DD dd = Newtonsoft.Json.JsonConvert.DeserializeObject<DD>(s.Result);

                // 如果dd接口是空，没有dd，返回空
                if (dd.return_data.Count == 0) return "";

                dds = Clone(dd.return_data);
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.Message);
            }

            foreach (var dd in dds)
            {
                double lon = dd.lon;
                double lat = dd.lat;
                double alt = dd.alt;
                double yield = dd.yield;
                test_need_r = MyCore.NuclearAlgorithm.GetNuclearPulseRadius(yield, alt, vm);
            }

            // 往7078发
            try
            {
                string json1 = test_need_r.ToString();
                Task<string> s = MyCore.Utils.HttpCli.PostAsyncJson(Configuration["PushUrls:area"], json1);
                s.Wait();
            }
            catch (Exception)
            {
                Console.WriteLine("检查7078配置");
            }

            return "AreaTemp";
        }

        public string Area()
        {
            double test_need_r = 0;

            double psi = 1; double rem = 100; double calcm = 1.9; double vm = 200;
            GetLimits(ref psi, ref rem, ref calcm, ref vm);

            // 读取mongo数据库中HB库，用于仿真模拟
            List<DamageAreaMergeVO> damageMergeVOs = new List<DamageAreaMergeVO>();

            List<MissileVO> dds = new List<MissileVO>();
            //导弹接口
            string url = Configuration["ServiceUrls:MissileInfo"];//http://localhost:5000/nuclearthreatanalysis/missileinfo
            // _logger.LogInformation("URL:{0}", url);
            try
            {
                Task<string> s = MyCore.Utils.HttpCli.GetAsyncJson(url);
                s.Wait();

                //JObject jo = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(s.Result);
                DD dd = Newtonsoft.Json.JsonConvert.DeserializeObject<DD>(s.Result);

                // 如果dd接口是空，没有dd，返回空
                if (dd.return_data.Count == 0) return "";

                dds = Clone(dd.return_data);
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.Message);
            }

            Geometry geom_02 = null;
            Geometry geom_03 = null;
            Geometry geom_04 = null;
            Geometry geom_05 = null;

            foreach (var dd in dds)
            {
                double lon = dd.lon;
                double lat = dd.lat;
                double alt = dd.alt;
                double yield = dd.yield;
                test_need_r = MyCore.NuclearAlgorithm.GetNuclearPulseRadius(yield, alt, vm);

                if (geom_02 == null)
                    geom_02 = MyCore.NuclearAlgorithm.GetNuclearRadiationGeometry(lon, lat, yield, alt, rem);
                else
                    geom_02 = geom_02.Union(MyCore.NuclearAlgorithm.GetNuclearRadiationGeometry(lon, lat, yield, alt, rem));

                if (geom_03 == null)
                    geom_03 = MyCore.NuclearAlgorithm.GetShockWaveGeometry(lon, lat, yield, alt, psi);
                else
                    geom_03 = geom_03.Union(MyCore.NuclearAlgorithm.GetShockWaveGeometry(lon, lat, yield, alt, psi));

                if (geom_04 == null)
                    geom_04 = MyCore.NuclearAlgorithm.GetThermalRadiationGeometry(lon, lat, yield, alt, calcm);
                else
                    geom_04 = geom_04.Union(MyCore.NuclearAlgorithm.GetThermalRadiationGeometry(lon, lat, yield, alt, calcm));

                if (geom_05 == null)
                    geom_05 = MyCore.NuclearAlgorithm.GetNuclearPulseGeometry(lon, lat, yield, alt, vm);
                else
                    geom_05 = geom_05.Union(MyCore.NuclearAlgorithm.GetNuclearPulseGeometry(lon, lat, yield, alt, vm));
            }

            Geometry newGeom = geom_02.Union(geom_03).Union(geom_04).Union(geom_05);

            _returnArea = MyCore.Utils.Translate.Geometry2GeoJson(newGeom);

            // 往7078发
            try
            {
                string json1 = test_need_r.ToString();
                Task<string> s = MyCore.Utils.HttpCli.PostAsyncJson(Configuration["PushUrls:area"], json1);
                s.Wait();
            }
            catch (Exception)
            {
                Console.WriteLine("检查7078配置");
            }

            return _returnArea;
        }

        public List<DamageMultiVO> Multi()
        {
            double psi = 1; double rem = 100; double calcm = 1.9; double vm = 200;
            GetLimits(ref psi, ref rem, ref calcm, ref vm);

            List<DamageMultiVO> damageMultiVOs = new List<DamageMultiVO>();

            List<MissileVO> dds = new List<MissileVO>();
            //导弹接口
            string url = Configuration["ServiceUrls:MissileInfo"];//http://localhost:5000/nuclearthreatanalysis/missileinfo
            // _logger.LogInformation("URL:{0}", url);
            try
            {
                Task<string> s = MyCore.Utils.HttpCli.GetAsyncJson(url);
                s.Wait();

                //JObject jo = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(s.Result);
                DD dd = Newtonsoft.Json.JsonConvert.DeserializeObject<DD>(s.Result);
                dds = Clone(dd.return_data);
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.Message);
            }
            List<MultiVO> multiVOs_01 = new List<MultiVO>();
            List<MultiVO> multiVOs_02 = new List<MultiVO>();
            List<MultiVO> multiVOs_03 = new List<MultiVO>();
            List<MultiVO> multiVOs_04 = new List<MultiVO>();
            List<MultiVO> multiVOs_05 = new List<MultiVO>();

            foreach (var dd in dds)
            {
                double lon = dd.lon;
                double lat = dd.lat;
                double alt = dd.alt;
                double yield = dd.yield;

                string id = dd.missileID;

                double r = MyCore.NuclearAlgorithm.GetNuclearRadiationRadius(yield, alt, rem);
                multiVOs_02.Add(new MultiVO(id, Math.Round(r, 2), lon, lat, alt, rem, "rem"));

                r = MyCore.NuclearAlgorithm.GetShockWaveRadius(yield, alt, psi);
                multiVOs_03.Add(new MultiVO(id, Math.Round(r, 2), lon, lat, alt, psi, "psi"));

                r = MyCore.NuclearAlgorithm.GetThermalRadiationRadius(yield, alt, calcm);
                multiVOs_04.Add(new MultiVO(id, Math.Round(r, 2), lon, lat, alt, calcm, "cal/cm²"));

                // 吨不变；米变千米
                r = MyCore.NuclearAlgorithm.GetNuclearPulseRadius(yield, alt, vm);

                // 上一步r的返回值是千米，所以要变成米
                multiVOs_05.Add(new MultiVO(id, Math.Round(r * 1000, 2), lon, lat, alt, vm, "v/m"));

            }
            damageMultiVOs.Add(new DamageMultiVO("早期核辐射", multiVOs_02));
            damageMultiVOs.Add(new DamageMultiVO("冲击波", multiVOs_03));
            damageMultiVOs.Add(new DamageMultiVO("光辐射", multiVOs_04));
            damageMultiVOs.Add(new DamageMultiVO("核电磁脉冲", multiVOs_05));

            return damageMultiVOs;
        }
        public List<DamageResultVO> MissileMulti(MissileBO bo)
        {
            double psi = 1; double rem = 100; double calcm = 1.9; double vm = 200;
            GetLimits(ref psi, ref rem, ref calcm, ref vm);

            var nuclearradiation = MyCore.NuclearAlgorithm.GetNuclearRadiationRadius(bo.Yield, bo.Alt, rem);
            var airblast = MyCore.NuclearAlgorithm.GetShockWaveRadius(bo.Yield, bo.Alt, psi);
            var thermalradiation = MyCore.NuclearAlgorithm.GetThermalRadiationRadius(bo.Yield, bo.Alt, calcm);
            var nuclearpulse = MyCore.NuclearAlgorithm.GetNuclearPulseRadius(bo.Yield, bo.Alt, vm);


            List<DamageResultVO> list = new List<DamageResultVO>();
            list.Add(
                new DamageResultVO("早期核辐射", nuclearradiation, bo.Lon, bo.Lat, bo.Alt, rem, "rem"));
            list.Add(
                new DamageResultVO("冲击波", airblast, bo.Lon, bo.Lat, bo.Alt, psi, "psi"));
            list.Add(
                new DamageResultVO("光辐射", thermalradiation, bo.Lon, bo.Lat, bo.Alt, calcm, "cal/cm²"));
            list.Add(
                new DamageResultVO("核电磁脉冲", nuclearpulse * 1000, bo.Lon, bo.Lat, bo.Alt, vm, "v/m"));

            return list;
        }

        public MissileAreaVO MissileArea(MissileBO bo)
        {
            double psi = 1; double rem = 100; double calcm = 1.9; double vm = 200;
            GetLimits(ref psi, ref rem, ref calcm, ref vm);

            var r = MyCore.NuclearAlgorithm.GetNuclearPulseRadius(bo.Yield, bo.Alt, vm);
            return new MissileAreaVO(r, bo.Lon, bo.Lat, bo.Alt);
        }

        private void GetLimits(ref double psi, ref double rem, ref double calcm, ref double vm)
        {
            var rule = _mongoService.QueryRule("冲击波");
            if (rule != null) psi = rule.limits;

            rule = _mongoService.QueryRule("早期核辐射");
            if (rule != null) rem = rule.limits;

            rule = _mongoService.QueryRule("光辐射");
            if (rule != null) calcm = rule.limits;

            rule = _mongoService.QueryRule("核电磁脉冲");
            if (rule != null) vm = rule.limits;
        }
    }
}
