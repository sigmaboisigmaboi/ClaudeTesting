using System.Collections.Generic;

namespace TheDeep.Destruction
{
    // Keeps the number of actively simulating debris pieces under a cap.
    // Each broken object adds a "group" of pieces. If a new group would go over the cap,
    // the oldest groups are handed back so the caller can freeze them early.
    // Plain C# (no Unity) so the bookkeeping can be unit-tested.
    public class DebrisBudget<TGroup> where TGroup : class
    {
        struct Entry
        {
            public TGroup Group;
            public int Pieces;
        }

        readonly int maxActivePieces;
        readonly List<Entry> active = new List<Entry>(); // oldest first

        public DebrisBudget(int maxActivePieces)
        {
            this.maxActivePieces = maxActivePieces;
        }

        public int ActivePieces { get; private set; }

        public int ActiveGroups => active.Count;

        // Adds a group and returns the older groups that must be frozen now to stay within the cap.
        // A single group bigger than the cap is still allowed on its own.
        public List<TGroup> Add(TGroup group, int pieceCount)
        {
            var toFreeze = new List<TGroup>();
            while (active.Count > 0 && ActivePieces + pieceCount > maxActivePieces)
            {
                Entry oldest = active[0];
                active.RemoveAt(0);
                ActivePieces -= oldest.Pieces;
                toFreeze.Add(oldest.Group);
            }

            active.Add(new Entry { Group = group, Pieces = pieceCount });
            ActivePieces += pieceCount;
            return toFreeze;
        }

        // Call when a group has settled (frozen) on its own.
        public void Remove(TGroup group)
        {
            int index = active.FindIndex(e => e.Group == group);
            if (index < 0)
                return;
            ActivePieces -= active[index].Pieces;
            active.RemoveAt(index);
        }
    }
}
