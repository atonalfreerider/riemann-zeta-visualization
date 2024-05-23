using System;
using UnityEngine;

namespace VRTKLite
{
    static class DirectionExtensions
    {
        public static Vector2 AsVector(this Direction direction)
        {
            switch (direction) {
                case Direction.Up:
                    return new Vector2(0, 1);
                case Direction.Right:
                    return new Vector2(1, 0);
                case Direction.Down:
                    return new Vector2(0, -1);
                case Direction.Left:
                    return new Vector2(-1, 0);
                case Direction.None:
                    return new Vector2(0, 0);
                default:
                    throw new ArgumentOutOfRangeException(
                        $"Invalid Direction enum value: '{direction}'. " +
                        "Cannot return a Vector2.");
            }
        }

        const float Tolerance = 0.0001f;

        public static Direction AsDirection(this Vector2 position)
        {
            if (Math.Abs(position.x) <= Tolerance &&
                Math.Abs(position.y) <= Tolerance)
            {
                return Direction.None;
            }

            if (Mathf.Abs(position.y) > Mathf.Abs(position.x))
            {
                return position.y >= 0 ? Direction.Up : Direction.Down;
            }

            return position.x >= 0 ? Direction.Right : Direction.Left;
        }
    }

    public enum Direction { None = 0, Up = 1, Right = 2, Down = 3, Left = 4 }
}
