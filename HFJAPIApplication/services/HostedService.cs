using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HFJAPIApplication.Services
{
    public class HostedService : IHostedService
    {
        public HostedService(
            IMongoService mongoservice,
            IDamageAnalysisService damageService
            )
        {

        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
