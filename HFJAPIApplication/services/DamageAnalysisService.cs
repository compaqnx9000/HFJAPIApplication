using HFJAPIApplication.core;
using HFJAPIApplication.enums;
using HFJAPIApplication.Mock;
using HFJAPIApplication.Services;
using HFJAPIApplication.VO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
        private readonly IMongoService _mongoService;
        private ServiceUrls _config;
        private static List<BO.InfoBO> _infos;
        //private static ConcurrentBag<DamageVO> _damageVOs = new ConcurrentBag<DamageVO>();
        //private static ConcurrentBag<DamageVO> _reallyDamageResult = new ConcurrentBag<DamageVO>();

        private static List<DamageVO> _damageVOs = new List<DamageVO>();
        private static List<DamageVO> _reallyDamageResult = new List<DamageVO>();
        static private ReaderWriterLockSlim rwl = new ReaderWriterLockSlim();

        public ILogger<DamageAnalysisService> _logger = null;
        private static bool _firstRun = true;

        public List<DamageVO> GetDamageResult()
        {
            //Thread.Sleep(5000);
            if(_firstRun)
            {
                _firstRun = false;
                 Damage();
            }
            
            
               return _reallyDamageResult;
            
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

            this._logger = logger;

            _infos = _mongoService.GetInfos();

            //Damage();

            //创建线程，并启动
            Thread th = new Thread(new ThreadStart(ThreadMethod));                      
            th.Start(); 
        }

        public List<DamageVO> Damage()
        {
            _damageVOs.Clear();
            List<MissileVO> dds = new List<MissileVO>();


            /*************************
             * 1. 调用接口获取导弹信息
             *************************/


            //导弹接口
            string url = _config.MissileInfo;//http://localhost:5000/nuclearthreatanalysis/missileinfo
            try
            {
                Task<string> s = GetAsyncJson(url);
                s.Wait(1000);
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
                    double impactTimeUTC = Double.Parse(obj["impactTimeUTC"].ToString());
                    double measurement = Double.Parse(obj["measurement"].ToString());
                    double attackAccuracy = Double.Parse(obj["attackAccuracy"].ToString());
                    string nonce = obj["nonce"].ToString();

                    _logger.LogInformation(string.Format("【missileID】：{0},【warHeadNo】：{1}," +
                        "【yield】：{2},【lon】：{3},【lat】：{4},【alt】：{5},,【impactTimeUTC】：{6}",

                        missileID, warHeadNo,yield,lon, lat, alt, impactTimeUTC));

                    dds.Add(new MissileVO(missileID, warHeadNo, yield, lon, lat, alt, impactTimeUTC, measurement, attackAccuracy, nonce));
                }

                // 按時間戳排序
                dds.Sort((a, b) => a.impactTimeUTC.CompareTo(b.impactTimeUTC));


            }
            catch (Exception)
            {

            }
            finally
            {

            }

            /****************************************
          * 2.循環計算Info表中的每一條記錄的損傷level
          ****************************************/
            //List<DamageVO> damageVOs = new List<DamageVO>();

            foreach (var info in _infos)
            {
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

                if (info.platform.Equals("发射井"))
                {
                    foreach (var dd in dds)
                    {
                        // GetDistance返回单位是：米。
                        double dis = Utils.Translate.GetDistance(dd.lat, dd.lon, info.lat, info.lon);
                        // 对《发射井》有影响的是[冲击波]
                        var result = Airblast(dis, dd.yield / 1000, dd.alt * 3.2808399, info.shock_wave_01, info.shock_wave_02, info.shock_wave_03);
                        if (result != DamageEnumeration.Safe)
                        {
                            // 只记录照成损伤的DD
                            damageVO.missileList.Add(new MissileListVO(dd.missileID, dd.impactTimeUTC, (int)result));
                        }
                    }
                    if (damageVO.missileList.Count == 0)
                    {
                        damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(0, 9999999999, 0));
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
                                damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(missile.ImpactTimeUtc, 9999999999, missile.DamageLevel));
                                preDamageLevel = missile.DamageLevel;
                            }
                            else
                            {
                                int currentDamageLevel = preDamageLevel + missile.DamageLevel;
                                if (currentDamageLevel > 3) currentDamageLevel = 3;
                                damageVO.statusTimeRanges[index].EndTimeUtc = missile.ImpactTimeUtc;
                                damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(missile.ImpactTimeUtc, 9999999999, currentDamageLevel));
                                preDamageLevel = currentDamageLevel;
                            }
                        }
                    }
                }
                if (info.platform.Equals("发射车"))
                {
                    foreach (var dd in dds)
                    {
                        // GetDistance返回单位是：米。
                        double dis = Utils.Translate.GetDistance(dd.lat, dd.lon, info.lat, info.lon);

                        // 对《发射车》有影响的是[ 冲击波 & 光辐射 & 核辐射 & 核电磁脉冲 ] ，取4种损伤最大的
                        var result1 = Airblast(dis, dd.yield / 1000, dd.alt * 3.2808399, info.shock_wave_01, info.shock_wave_02, info.shock_wave_03);
                        var result2 = ThermalRadiation(dis, dd.yield / 1000, dd.alt * 3.2808399, info.thermal_radiation_01,
                                                        info.thermal_radiation_02, info.thermal_radiation_03);
                        var result3 = NuclearRadiation(dis, dd.yield / 1000, info.alt * 3.2808399, info.nuclear_radiation_01,
                                                        info.nuclear_radiation_02, info.nuclear_radiation_03);
                        var result4 = NuclearPulse(dis, dd.yield, info.alt/1000, info.nuclear_pulse_01,
                                                     info.nuclear_pulse_02, info.nuclear_pulse_03);
                        var result12 = (DamageEnumeration)Math.Max(result1.GetHashCode(), result2.GetHashCode());
                        var result34 = (DamageEnumeration)Math.Max(result3.GetHashCode(), result4.GetHashCode());

                        var result = (DamageEnumeration)Math.Max(result12.GetHashCode(), result34.GetHashCode());

                        if (result != DamageEnumeration.Safe)
                        {
                            // 只记录照成损伤的DD
                            damageVO.missileList.Add(new MissileListVO(dd.missileID, dd.impactTimeUTC, (int)result));
                        }
                    }
                    if (damageVO.missileList.Count == 0)
                    {
                        damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(0, 9999999999, 0));
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
                                damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(missile.ImpactTimeUtc, 9999999999, missile.DamageLevel));
                                preDamageLevel = missile.DamageLevel;
                            }
                            else
                            {
                                int currentDamageLevel = preDamageLevel + missile.DamageLevel;
                                if (currentDamageLevel > 3) currentDamageLevel = 3;
                                damageVO.statusTimeRanges[index].EndTimeUtc = missile.ImpactTimeUtc;
                                damageVO.statusTimeRanges.Add(new StatusTimeRangesVO(missile.ImpactTimeUtc, 9999999999, currentDamageLevel));
                                preDamageLevel = currentDamageLevel;
                            }
                        }
                    }
                }
                _damageVOs.Add(damageVO);
            }

            rwl.EnterWriteLock();
            _reallyDamageResult.Clear();
            for (int i = 0; i < _damageVOs.Count; i++)
            {
                DamageVO vo = _damageVOs.ElementAt(i);
                _reallyDamageResult.Add((DamageVO)vo.Clone());

            }
            rwl.ExitWriteLock();
            return null;
        }


        public double GetShockWaveRadius(double yield, double ft,double psi)
        {
            MyAnalyse myAnalyse = new MyAnalyse();
            return myAnalyse.CalcShockWaveRadius(yield,ft, psi);
        }
        public double GetNuclearRadiationRadius(double yield, double ft, double rem)
        {
            MyAnalyse myAnalyse = new MyAnalyse();
            return myAnalyse.CalcNuclearRadiationRadius(yield, ft,rem);
        }
        public double GetThermalRadiationRadius(double yield, double ft, double threm)
        {
            MyAnalyse myAnalyse = new MyAnalyse();
            return myAnalyse.GetThermalRadiationR(yield,ft,threm);
        }
        public double GetNuclearPulseRadius(double yield, double ft, double vm)
        {
            MyAnalyse myAnalyse = new MyAnalyse();
            return myAnalyse.CalcNuclearPulseRadius(yield, ft, vm);
        }
        void ThreadMethod()
        {
            while (true)
            {
                Damage();
                //Thread.Sleep(5);//如果不延时，将占用CPU过高  
                Thread.Sleep(1000);//如果不延时，将占用CPU过高  
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
        private DamageEnumeration Airblast(double dis, double yield, double ft, double psi01, double psi02, double psi03)
        {
            // 冲击波
            double r1 = GetShockWaveRadius(yield, ft, psi01);
            double r2 = GetShockWaveRadius(yield, ft, psi02);
            double r3 = GetShockWaveRadius(yield, ft, psi03);

            if (dis <= r3) return DamageEnumeration.Destroy;
            if (dis <= r2) return DamageEnumeration.Heavy;
            if (dis <= r1) return DamageEnumeration.Light;

            return DamageEnumeration.Safe;
        }
        private DamageEnumeration ThermalRadiation(double dis, double yield, double ft, double cal01, double cal02, double cal03)
        {
            // 光辐射 =》营区、发射车、人员

            double r1 = GetThermalRadiationRadius(yield, ft, cal01);
            double r2 = GetThermalRadiationRadius(yield, ft, cal02);
            double r3 = GetThermalRadiationRadius(yield, ft, cal03);

            if (dis <= r3) return DamageEnumeration.Destroy;
            if (dis <= r2) return DamageEnumeration.Heavy;
            if (dis <= r1) return DamageEnumeration.Light;

            return DamageEnumeration.Safe;
        }
        private DamageEnumeration NuclearRadiation(double dis, double yield, double ft,
                                                    double rem01, double rem02, double rem03)
        {
            // 核辐射 =》发射场、发射车、人员

            double r1 = GetNuclearRadiationRadius(yield, ft, rem01);
            double r2 = GetNuclearRadiationRadius(yield, ft, rem02);
            double r3 = GetNuclearRadiationRadius(yield, ft, rem03);

            if (dis <= r3) return DamageEnumeration.Destroy;
            if (dis <= r2) return DamageEnumeration.Heavy;
            if (dis <= r1) return DamageEnumeration.Light;

            return DamageEnumeration.Safe;
        }
        private DamageEnumeration NuclearPulse(double dis, double yield, double km, double vm01, double vm02, double vm03)
        {
            // 核电磁脉冲 =》中心库、待机库、通信站、发射车

            double r1 = GetNuclearPulseRadius(yield, km, vm01);
            double r2 = GetNuclearPulseRadius(yield, km, vm02);
            double r3 = GetNuclearPulseRadius(yield, km, vm03);

            r1 *= 1000;
            r2 *= 1000;
            r3 *= 1000;

            if (dis <= r3) return DamageEnumeration.Destroy;
            if (dis <= r2) return DamageEnumeration.Heavy;
            if (dis <= r1) return DamageEnumeration.Light;

            return DamageEnumeration.Safe;
        }
        #endregion
    }
}
