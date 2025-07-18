namespace MRSculpture
{
    public struct CellManager
    {
        public uint status;
    }

    public static class CellManagerExtensions
    {
        public static void AddFlag(ref this CellManager cell, CellFlags flags)
        {
            cell.status |= (uint)flags;
        }

        public static void RemoveFlag(ref this CellManager cell, CellFlags flags)
        {
            cell.status &= ~(uint)flags;
        }

        public static bool HasFlag(ref this CellManager cell, CellFlags flags)
        {
            return (cell.status & (uint)flags) != 0;
        }
    }
}
