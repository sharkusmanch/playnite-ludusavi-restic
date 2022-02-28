using System;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace LudusaviRestic
{
    public class ResticSnapshot : GenericItemOption
    {
        private string short_id;
        public string ID { get { return short_id; } } 
        private string hostname;
        public string Hostname { get { return hostname; } } 
        private DateTime? time;
        public DateTime? Time { get { return time; } } 

        public ResticSnapshot(string id, string hostname, DateTime? time) : base()
        {
            this.short_id = id;
            this.hostname = hostname;
            this.time = time;

            this.Name = this.short_id;
            this.Description = ToString();
        }

        public override string ToString()
        {
            return $"{this.short_id} {this.hostname} {this.time}";
        }
    }


}