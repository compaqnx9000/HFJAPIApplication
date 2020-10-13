using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.Helper
{
    public static class Kafka
    {
        public static void Write(String json)
        {
            //try
            //{
            //    var config = new ProducerConfig { BootstrapServers = "localhost:9092" };
            //    using var producer = new ProducerBuilder<Null, string>(config).Build();

            //    producer.Produce("MNXLMul", new Message<Null, string> { Value = json });
            //    producer.Flush(TimeSpan.FromSeconds(3));
            //    Console.WriteLine(json);
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e.ToString());
            //}
        }
    }
}
