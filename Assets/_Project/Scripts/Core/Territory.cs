using System.Collections.Generic;

namespace GameJam.Core
{
    /// <summary>
    /// One space on the board. Presentation only reads it; the <see cref="Match"/> changes
    /// owner and troops through the internal setters.
    /// </summary>
    public sealed class Territory
    {
        private readonly List<Territory> _neighbors = new List<Territory>();

        internal Territory(int id, string name, TerritoryKind kind, God? owner, int troops)
        {
            Id = id;
            Name = name;
            Kind = kind;
            Owner = owner;
            Troops = troops;
        }

        public int Id { get; }
        public string Name { get; }
        public TerritoryKind Kind { get; }

        /// <summary>Null while nobody has claimed it. Unowned territories always have 0 troops.</summary>
        public God? Owner { get; internal set; }

        public int Troops { get; internal set; }

        public IReadOnlyList<Territory> Neighbors => _neighbors;

        public bool IsAdjacentTo(Territory other) => _neighbors.Contains(other);

        public override string ToString() => Name;

        // Adjacency is always two-way, so both lists are updated together.
        internal void Connect(Territory other)
        {
            _neighbors.Add(other);
            other._neighbors.Add(this);
        }
    }
}
