using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficTown2D.Level5
{
    public enum ApproachDirection
    {
        North,
        South,
        East,
        West
    }

    public enum TurnType
    {
        Straight,
        RightTurn,
        LeftTurn
    }

    public class TrafficRoute
    {
        public ApproachDirection EntryDirection { get; }
        public TurnType Turn { get; }
        public Vector3[] Waypoints { get; }
        public float StopLineCoordinate { get; }
        public Vector3 SpawnPosition => Waypoints[0];

        public TrafficRoute(ApproachDirection entry, TurnType turn, Vector3[] waypoints, float stopCoord)
        {
            EntryDirection = entry;
            Turn = turn;
            Waypoints = waypoints;
            StopLineCoordinate = stopCoord;
        }
    }

    public static class TrafficRouteManager
    {
        public const float StopLineDist = 7.6f;

        public static TrafficRoute GetRoute(ApproachDirection entry, TurnType turn)
        {
            switch (entry)
            {
                case ApproachDirection.North:
                    return GetNorthRoute(turn);
                case ApproachDirection.South:
                    return GetSouthRoute(turn);
                case ApproachDirection.East:
                    return GetEastRoute(turn);
                case ApproachDirection.West:
                    return GetWestRoute(turn);
                default:
                    return GetNorthRoute(TurnType.Straight);
            }
        }

        private static TrafficRoute GetNorthRoute(TurnType turn)
        {
            switch (turn)
            {
                case TurnType.Straight:
                    return new TrafficRoute(ApproachDirection.North, TurnType.Straight, new Vector3[]
                    {
                        new Vector3(-2.4f, 24f, 0f),
                        new Vector3(-2.4f, 7.6f, 0f),
                        new Vector3(-2.4f, 4.0f, 0f),
                        new Vector3(-2.4f, 0f, 0f),
                        new Vector3(-2.4f, -4.0f, 0f),
                        new Vector3(-2.4f, -7.6f, 0f),
                        new Vector3(-2.4f, -26f, 0f)
                    }, 7.6f);

                case TurnType.RightTurn:
                    return new TrafficRoute(ApproachDirection.North, TurnType.RightTurn, new Vector3[]
                    {
                        new Vector3(-2.4f, 24f, 0f),
                        new Vector3(-2.4f, 7.6f, 0f),
                        new Vector3(-2.4f, 4.5f, 0f),
                        new Vector3(-2.8f, 3.4f, 0f),
                        new Vector3(-3.4f, 2.8f, 0f),
                        new Vector3(-4.5f, 2.4f, 0f),
                        new Vector3(-7.6f, 2.4f, 0f),
                        new Vector3(-40f, 2.4f, 0f)
                    }, 7.6f);

                case TurnType.LeftTurn:
                    return new TrafficRoute(ApproachDirection.North, TurnType.LeftTurn, new Vector3[]
                    {
                        new Vector3(-2.4f, 24f, 0f),
                        new Vector3(-2.4f, 7.6f, 0f),
                        new Vector3(-2.4f, 2.0f, 0f),
                        new Vector3(-1.8f, -0.6f, 0f),
                        new Vector3(-0.6f, -1.8f, 0f),
                        new Vector3(1.2f, -2.4f, 0f),
                        new Vector3(4.0f, -2.4f, 0f),
                        new Vector3(7.6f, -2.4f, 0f),
                        new Vector3(40f, -2.4f, 0f)
                    }, 7.6f);
            }
            return null;
        }

        private static TrafficRoute GetSouthRoute(TurnType turn)
        {
            switch (turn)
            {
                case TurnType.Straight:
                    return new TrafficRoute(ApproachDirection.South, TurnType.Straight, new Vector3[]
                    {
                        new Vector3(2.4f, -24f, 0f),
                        new Vector3(2.4f, -7.6f, 0f),
                        new Vector3(2.4f, -4.0f, 0f),
                        new Vector3(2.4f, 0f, 0f),
                        new Vector3(2.4f, 4.0f, 0f),
                        new Vector3(2.4f, 7.6f, 0f),
                        new Vector3(2.4f, 26f, 0f)
                    }, -7.6f);

                case TurnType.RightTurn:
                    return new TrafficRoute(ApproachDirection.South, TurnType.RightTurn, new Vector3[]
                    {
                        new Vector3(2.4f, -24f, 0f),
                        new Vector3(2.4f, -7.6f, 0f),
                        new Vector3(2.4f, -4.5f, 0f),
                        new Vector3(2.8f, -3.4f, 0f),
                        new Vector3(3.4f, -2.8f, 0f),
                        new Vector3(4.5f, -2.4f, 0f),
                        new Vector3(7.6f, -2.4f, 0f),
                        new Vector3(40f, -2.4f, 0f)
                    }, -7.6f);

                case TurnType.LeftTurn:
                    return new TrafficRoute(ApproachDirection.South, TurnType.LeftTurn, new Vector3[]
                    {
                        new Vector3(2.4f, -24f, 0f),
                        new Vector3(2.4f, -7.6f, 0f),
                        new Vector3(2.4f, -2.0f, 0f),
                        new Vector3(1.8f, 0.6f, 0f),
                        new Vector3(0.6f, 1.8f, 0f),
                        new Vector3(-1.2f, 2.4f, 0f),
                        new Vector3(-4.0f, 2.4f, 0f),
                        new Vector3(-7.6f, 2.4f, 0f),
                        new Vector3(-40f, 2.4f, 0f)
                    }, -7.6f);
            }
            return null;
        }

        private static TrafficRoute GetEastRoute(TurnType turn)
        {
            switch (turn)
            {
                case TurnType.Straight:
                    return new TrafficRoute(ApproachDirection.East, TurnType.Straight, new Vector3[]
                    {
                        new Vector3(38f, 2.4f, 0f),
                        new Vector3(7.6f, 2.4f, 0f),
                        new Vector3(4.0f, 2.4f, 0f),
                        new Vector3(0f, 2.4f, 0f),
                        new Vector3(-4.0f, 2.4f, 0f),
                        new Vector3(-7.6f, 2.4f, 0f),
                        new Vector3(-40f, 2.4f, 0f)
                    }, 7.6f);

                case TurnType.RightTurn:
                    return new TrafficRoute(ApproachDirection.East, TurnType.RightTurn, new Vector3[]
                    {
                        new Vector3(38f, 2.4f, 0f),
                        new Vector3(7.6f, 2.4f, 0f),
                        new Vector3(4.5f, 2.4f, 0f),
                        new Vector3(3.4f, 2.8f, 0f),
                        new Vector3(2.8f, 3.4f, 0f),
                        new Vector3(2.4f, 4.5f, 0f),
                        new Vector3(2.4f, 7.6f, 0f),
                        new Vector3(2.4f, 26f, 0f)
                    }, 7.6f);

                case TurnType.LeftTurn:
                    return new TrafficRoute(ApproachDirection.East, TurnType.LeftTurn, new Vector3[]
                    {
                        new Vector3(38f, 2.4f, 0f),
                        new Vector3(7.6f, 2.4f, 0f),
                        new Vector3(2.0f, 2.4f, 0f),
                        new Vector3(-0.6f, 1.8f, 0f),
                        new Vector3(-1.8f, 0.6f, 0f),
                        new Vector3(-2.4f, -1.2f, 0f),
                        new Vector3(-2.4f, -4.0f, 0f),
                        new Vector3(-2.4f, -7.6f, 0f),
                        new Vector3(-2.4f, -26f, 0f)
                    }, 7.6f);
            }
            return null;
        }

        private static TrafficRoute GetWestRoute(TurnType turn)
        {
            switch (turn)
            {
                case TurnType.Straight:
                    return new TrafficRoute(ApproachDirection.West, TurnType.Straight, new Vector3[]
                    {
                        new Vector3(-38f, -2.4f, 0f),
                        new Vector3(-7.6f, -2.4f, 0f),
                        new Vector3(-4.0f, -2.4f, 0f),
                        new Vector3(0f, -2.4f, 0f),
                        new Vector3(4.0f, -2.4f, 0f),
                        new Vector3(7.6f, -2.4f, 0f),
                        new Vector3(40f, -2.4f, 0f)
                    }, -7.6f);

                case TurnType.RightTurn:
                    return new TrafficRoute(ApproachDirection.West, TurnType.RightTurn, new Vector3[]
                    {
                        new Vector3(-38f, -2.4f, 0f),
                        new Vector3(-7.6f, -2.4f, 0f),
                        new Vector3(-4.5f, -2.4f, 0f),
                        new Vector3(-3.4f, -2.8f, 0f),
                        new Vector3(-2.8f, -3.4f, 0f),
                        new Vector3(-2.4f, -4.5f, 0f),
                        new Vector3(-2.4f, -7.6f, 0f),
                        new Vector3(-2.4f, -26f, 0f)
                    }, -7.6f);

                case TurnType.LeftTurn:
                    return new TrafficRoute(ApproachDirection.West, TurnType.LeftTurn, new Vector3[]
                    {
                        new Vector3(-38f, -2.4f, 0f),
                        new Vector3(-7.6f, -2.4f, 0f),
                        new Vector3(-2.0f, -2.4f, 0f),
                        new Vector3(0.6f, -1.8f, 0f),
                        new Vector3(1.8f, -0.6f, 0f),
                        new Vector3(2.4f, 1.2f, 0f),
                        new Vector3(2.4f, 4.0f, 0f),
                        new Vector3(2.4f, 7.6f, 0f),
                        new Vector3(2.4f, 26f, 0f)
                    }, -7.6f);
            }
            return null;
        }
    }
}
