using Newtonsoft.Json;

namespace LudusaviRestic
{
    public class RetentionValues
    {
        public int KeepLast { get; set; }
        public int KeepDaily { get; set; }
        public int KeepWeekly { get; set; }
        public int KeepMonthly { get; set; }
        public int KeepYearly { get; set; }

        public RetentionValues(int keepLast, int keepDaily, int keepWeekly, int keepMonthly, int keepYearly)
        {
            KeepLast = keepLast;
            KeepDaily = keepDaily;
            KeepWeekly = keepWeekly;
            KeepMonthly = keepMonthly;
            KeepYearly = keepYearly;
        }
    }

    public class GameOverride
    {
        [JsonIgnore]
        public string GameId { get; set; }
        public string GameName { get; set; }
        public int? IntervalMinutes { get; set; }

        public int? KeepLast { get; set; }
        public int? KeepDaily { get; set; }
        public int? KeepWeekly { get; set; }
        public int? KeepMonthly { get; set; }
        public int? KeepYearly { get; set; }

        [JsonIgnore]
        public bool HasRetentionOverride =>
            KeepLast.HasValue || KeepDaily.HasValue || KeepWeekly.HasValue ||
            KeepMonthly.HasValue || KeepYearly.HasValue;

        [JsonIgnore]
        public bool HasIntervalOverride =>
            IntervalMinutes.HasValue && IntervalMinutes.Value > 0;

        public GameOverride() { }

        public GameOverride(string gameName, int intervalMinutes)
        {
            GameName = gameName;
            IntervalMinutes = intervalMinutes > 0 ? intervalMinutes : (int?)null;
        }

        /// <summary>
        /// Get resolved retention values for this game. When a game has any retention
        /// override, unset fields default to 0 (disabled) rather than the global value,
        /// so the override fully replaces the global policy for that game.
        /// </summary>
        public RetentionValues GetEffectiveRetention(LudusaviResticSettings globalSettings)
        {
            return new RetentionValues(
                KeepLast ?? 0,
                KeepDaily ?? 0,
                KeepWeekly ?? 0,
                KeepMonthly ?? 0,
                KeepYearly ?? 0
            );
        }
    }
}
