using System.Runtime.CompilerServices;

// Lets the EditMode test assembly reach `internal` members of Runtime -
// used sparingly, only where a hand-written static helper (e.g.
// BuildingPlacer's civ-cost multipliers) is worth testing directly rather
// than driving the full MonoBehaviour/UI flow it lives on.
[assembly: InternalsVisibleTo("KingdomsOfBharat.Tests")]
