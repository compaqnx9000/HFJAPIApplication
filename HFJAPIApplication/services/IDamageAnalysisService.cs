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
        double GetShockWaveRadius(double yield, double ft, double psi);
        double GetNuclearRadiationRadius(double yield, double ft, double rem);
        double GetThermalRadiationRadius(double yield, double ft, double threm);
        double GetNuclearPulseRadius(double equivalent, double ft, double vm);

        List<DamageVO> GetDamageResult();

        Task<string> GetStuInfoAsync(string stuNo);

    }
}
