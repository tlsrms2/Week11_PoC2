namespace DeepSea.Environment
{
    /// <summary>
    /// Represents the different water layer zones in the deep sea.
    /// Used to transition color schemes, hazards, and enemy behaviors.
    /// </summary>
    public enum DepthZone
    {
        Shallow, // 0m ~ 30m (Bright blue, peaceful)
        Mid,     // 30m ~ 80m (Teal, rising turbidity, undercurrents)
        Deep     // 80m+ (Navy/Black, pitch black, bioluminescent creatures)
    }
}
