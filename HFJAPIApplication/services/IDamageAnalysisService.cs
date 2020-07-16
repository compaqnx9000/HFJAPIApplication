using HFJAPIApplication.BO;
using HFJAPIApplication.VO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.services
{
    public interface IDamageAnalysisService
    {
        List<DamageVO> GetDamageResult();
        Task<string> GetStuInfoAsync(string stuNo);
       // List<DamageVO> Damage();
        List<AttackVO> Attack(AttackBO bo);
        List<CounterVO> GetCounterResult();
        List<SelectVO> Select(FilterBO bo);
        int InfoChanged();

    }
}
