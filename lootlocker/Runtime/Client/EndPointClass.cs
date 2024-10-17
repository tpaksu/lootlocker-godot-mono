using System;

namespace LootLocker
{
    [Serializable]
    public class EndPointClass
    {
        public string endPoint { get; set; }
        public LootLockerHTTPMethod httpMethod { get; set; }

        public EndPointClass() { }

        public EndPointClass(string endPoint, LootLockerHTTPMethod httpMethod)
        {
            this.endPoint = endPoint;
            this.httpMethod = httpMethod;
        }
    }
}
