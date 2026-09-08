namespace Vaultbreakers.UI
{
    // Session presentation preferences never change damage, timing, or collision.
    public static class FeedbackSettings
    {
        public static bool Shake = true;
        public static bool Flashes = true;
        public static bool Rumble = true;
        public static bool Numbers = true;
        public static bool Bloom = true;
        public static bool Grayscale;
        public static float HitStopScale = 1;
    }
}
