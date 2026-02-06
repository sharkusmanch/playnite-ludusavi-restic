using Newtonsoft.Json;

namespace LudusaviRestic
{
    public class GameIntervalOverride
    {
        [JsonIgnore]
        public string GameId { get; set; }
        public string GameName { get; set; }
        public int IntervalMinutes { get; set; }

        public GameIntervalOverride() { }

        public GameIntervalOverride(string gameName, int intervalMinutes)
        {
            GameName = gameName;
            IntervalMinutes = intervalMinutes;
        }
    }
}
