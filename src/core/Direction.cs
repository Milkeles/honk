/* Description: Shared data class for core and presentation layer for directional enums:
 * the four approaches (North, East, South, West) and a car's intended movement (Left, Straight or Right).
 *
 * Dependencies: None
 * Author(s): H. Hristov (Milkeles)
 * Created: 04/06/2026 (dd/mm/yyyy)
 * Updated: N/A
 * Last change: N/A
*/
public enum LaneOrigin
{
    North = 0,
    East = 1,
    South = 2,
    West = 3
}

public enum MovementDirection
{
    Forward = 0,
    Left = 1,
    Right = 2
}