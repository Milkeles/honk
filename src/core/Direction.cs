/* Shared directional enums for core and presentation. LaneOrigin is ordered so
 * that, for a car's origin, (origin + 1) % 4 is the lane to its right and
 * (origin + 2) % 4 is the oncoming lane directly ahead.
 *
 * Dependencies: None
 * Author(s): H. Hristov (Milkeles)
 * Created: 04/06/2026 (dd/mm/yyyy)
 * Updated: 05/06/2026 (dd/mm/yyyy)
 * Last change: Reordered LaneOrigin (swapped East/West) to fix right/oncoming geometry.
*/

public enum LaneOrigin
{
    North,
    West,
    South,
    East
}

public enum MovementDirection
{
    Left,
    Straight,
    Right
}