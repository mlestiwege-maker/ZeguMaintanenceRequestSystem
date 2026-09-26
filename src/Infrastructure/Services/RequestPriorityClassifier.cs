using ZEGU.Core.Enums;

namespace ZEGU.Infrastructure.Services
{
    /// <summary>
    /// Determines a request's priority from the category's configured default plus
    /// urgency signals in the reported text, so requesters can't just self-report
    /// "Emergency" to jump the queue. Escalates upward only, never below the
    /// category's default.
    /// </summary>
    public static class RequestPriorityClassifier
    {
        private static readonly string[] EmergencyKeywords =
        {
            "fire", "smoke", "gas leak", "gas smell", "smell of gas", "spark", "sparking",
            "exposed wire", "live wire", "electrocut", "electric shock", "shock hazard",
            "flooding", "flood", "burst pipe", "collapsed", "collapsing", "structural",
            "injury", "injured", "bleeding", "trapped", "carbon monoxide", "explosion"
        };

        private static readonly string[] HighKeywords =
        {
            "no power", "no electricity", "power outage", "no water", "no hot water",
            "leaking", "leak", "burning smell", "overheating", "smoking", "door won't lock",
            "can't lock", "security risk", "ceiling falling", "water damage", "not working at all"
        };

        public static RequestPriority Determine(RequestPriority categoryDefault, string? title, string? description)
        {
            var text = $"{title} {description}".ToLowerInvariant();

            if (EmergencyKeywords.Any(k => text.Contains(k)))
                return Max(categoryDefault, RequestPriority.Emergency);

            if (HighKeywords.Any(k => text.Contains(k)))
                return Max(categoryDefault, RequestPriority.High);

            return categoryDefault;
        }

        private static RequestPriority Max(RequestPriority a, RequestPriority b) => a > b ? a : b;
    }
}
