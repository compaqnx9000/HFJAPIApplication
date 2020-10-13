using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.BO
{
    public class OverlayBO
    {
        public ObjectId _id { get; set; }
        public string addend { get; set; }
        public string augend { get; set; }
        public string result { get; set; }
    }
}
