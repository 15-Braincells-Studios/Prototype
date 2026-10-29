using UnityEngine;

namespace PreventTheMurder
{
    public static class ClockFormat
    {
        public static string Format(float seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
