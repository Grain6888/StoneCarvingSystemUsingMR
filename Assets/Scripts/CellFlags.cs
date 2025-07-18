namespace MRSculpture
{
    [System.Flags]
    public enum CellFlags
    {
        None = 0,
        IsFilled = 1 << 0, // 1
        IsMeshGenerated = 1 << 1, // 2
    }
}
