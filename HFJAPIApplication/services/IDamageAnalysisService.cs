using HFJAPIApplication.BO;
using HFJAPIApplication.VO;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.Services
{
    public interface IDamageAnalysisService
    {
        List<DamageVO> GetDamageResult();
        List<AttackVO> Attack(AttackBO bo);
        List<CounterVO> GetCounterResult();
        List<SelectVO> Select(JObject bo);

        int InfoChanged();

        //2020-07-27
        List<DamageAreaMergeVO> Merge();
        string Area();
        List<DamageMultiVO> Multi();
        List<DamageResultVO> MissileMulti(MissileBO bo);
        MissileAreaVO MissileArea(MissileBO bo);


    }
}
