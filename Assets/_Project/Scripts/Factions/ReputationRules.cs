namespace TheDeep.Factions
{
    // How a faction feels about the player, from worst to best. Content (barks, gates) checks
    // bands rather than exact numbers, so many different histories collapse into a few states.
    public enum ReputationBand
    {
        Hostile = 0,
        Unfriendly = 1,
        Neutral = 2,
        Friendly = 3,
    }

    public static class ReputationRules
    {
        public const int Min = -100;
        public const int Max = 100;
        public const int HostileAtOrBelow = -40;
        public const int UnfriendlyBelow = -10;
        public const int FriendlyAtOrAbove = 20;

        public static int Clamp(int value) => value < Min ? Min : (value > Max ? Max : value);

        public static ReputationBand BandFor(int value)
        {
            if (value <= HostileAtOrBelow) return ReputationBand.Hostile;
            if (value < UnfriendlyBelow) return ReputationBand.Unfriendly;
            if (value >= FriendlyAtOrAbove) return ReputationBand.Friendly;
            return ReputationBand.Neutral;
        }
    }
}
