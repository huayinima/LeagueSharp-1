﻿#region LICENSE

// Copyright 2014 - 2014 SpellDetector
// Skillshot.cs is part of SpellDetector.
// SpellDetector is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// SpellDetector is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// You should have received a copy of the GNU General Public License
// along with SpellDetector. If not, see <http://www.gnu.org/licenses/>.

#endregion

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

#endregion

namespace SpellDetector
{
    public enum SkillShotType
    {
        SkillshotCircle,
        SkillshotLine,
        SkillshotMissileLine,
        SkillshotCone,
        SkillshotMissileCone,
        SkillshotRing,
    }

    public enum DetectionType
    {
        RecvPacket,
        ProcessSpell
    }

    public struct SafePathResult
    {
        public FoundIntersection Intersection;
        public bool IsSafe;

        public SafePathResult(bool isSafe, FoundIntersection intersection)
        {
            IsSafe = isSafe;
            Intersection = intersection;
        }
    }

    public struct FoundIntersection
    {
        public Vector2 ComingFrom;
        public float Distance;
        public Vector2 Point;
        public int Time;
        public bool Valid;

        public FoundIntersection(float distance, int time, Vector2 point, Vector2 comingFrom)
        {
            Distance = distance;
            ComingFrom = comingFrom;
            Valid = (point.X != 0) && (point.Y != 0);
            Point = point + 10 * (ComingFrom - point).Normalized();
            Time = time;
        }
    }

    public class Skillshot
    {
        public DetectionType DetectionType { get; set; }
        public Geometry.Circle Circle { get; set; }
        public Geometry.Polygon Polygon { get; set; }
        public Geometry.Rectangle Rectangle { get; set; }
        public Geometry.Ring Ring { get; set; }
        public Geometry.Sector Sector { get; set; }


        public Vector2 Start { get; set; }
        public Vector2 End { get; set; }
        public Vector2 Direction { get; set; }

        public bool ForceDisabled { get; set; }
        public Vector2 MissilePosition { get; set; }
        public SpellData SpellData { get; set; }
        public int StartTick { get; set; }
        public Obj_AI_Base Unit { get; set; }

        public Vector2 CollisionEnd
        {
            get
            {
                if (_collisionEnd.IsValid())
                {
                    return _collisionEnd;
                }

                if (IsGlobal)
                {
                    return GlobalGetMissilePosition(0) +
                           Direction * SpellData.MissileSpeed *
                           (0.5f + SpellData.Radius * 2 / ObjectManager.Player.MoveSpeed);
                }

                return End;
            }
        }

        public bool IsGlobal
        {
            get { return SpellData.RawRange == 20000; }
        }

        private Vector2 _collisionEnd;
        private int _lastCollisionCalc;

        public Skillshot(DetectionType detectionType, SpellData spellData, int startT, Vector2 start, Vector2 end, Obj_AI_Base unit)
        {
            DetectionType = detectionType;
            SpellData = spellData;
            StartTick = startT;
            Start = start;
            End = end;
            MissilePosition = start;
            Direction = (end - start).Normalized();
            Unit = unit;

            //Create the spatial object for each type of skillshot.
            switch (spellData.Type)
            {
                case SkillShotType.SkillshotCircle:
                    Circle = new Geometry.Circle(CollisionEnd, spellData.Radius);
                    break;
                case SkillShotType.SkillshotLine:
                    Rectangle = new Geometry.Rectangle(Start, CollisionEnd, spellData.Radius);
                    break;
                case SkillShotType.SkillshotMissileLine:
                    Rectangle = new Geometry.Rectangle(Start, CollisionEnd, spellData.Radius);
                    break;
                case SkillShotType.SkillshotCone:
                    Sector = new Geometry.Sector(start, CollisionEnd - start, spellData.Radius * (float)Math.PI / 180, spellData.Range);
                    break;
                case SkillShotType.SkillshotRing:
                    Ring = new Geometry.Ring(CollisionEnd, spellData.Radius, spellData.RingRadius);
                    break;
            }

            UpdatePolygon(); // Create the polygon
        }

        public bool IsActive()
        {
            if (SpellData.MissileAccel != 0)
            {
                return Environment.TickCount <= StartTick + 5000;
            }

            return Environment.TickCount <=
                   StartTick + SpellData.Delay + SpellData.ExtraDuration +
                   1000 * (Start.Distance(End) / SpellData.MissileSpeed);
        }

        public void Game_OnGameUpdate()
        {
            //Even if it doesnt consume a lot of resources with 20 updatest second works k
            if (SpellData.CollisionObjects.Count() > 0 && SpellData.CollisionObjects != null &&
                Environment.TickCount - _lastCollisionCalc > 50)
            {
                _lastCollisionCalc = Environment.TickCount;
                _collisionEnd = Collision.GetCollisionPoint(this);
            }

            //Update the missile position each time the game updates.
            if (SpellData.Type == SkillShotType.SkillshotMissileLine)
            {
                Rectangle = new Geometry.Rectangle(GetMissilePosition(0), CollisionEnd, SpellData.Radius);
                UpdatePolygon();
            }

            //Spells that update to the unit position.
            if (SpellData.MissileFollowsUnit)
            {
                if (Unit.IsVisible)
                {
                    End = Unit.ServerPosition.To2D();
                    Direction = (End - Start).Normalized();
                    UpdatePolygon();
                }
            }
        }

        public void UpdatePolygon()
        {
            switch (SpellData.Type)
            {
                case SkillShotType.SkillshotCircle:
                    Polygon = Circle.ToPolygon();
                    break;
                case SkillShotType.SkillshotLine:
                    Polygon = Rectangle.ToPolygon();
                    break;
                case SkillShotType.SkillshotMissileLine:
                    Polygon = Rectangle.ToPolygon();
                    break;
                case SkillShotType.SkillshotCone:
                    Polygon = Sector.ToPolygon();
                    break;
                case SkillShotType.SkillshotRing:
                    Polygon = Ring.ToPolygon();
                    break;
            }
        }

        /// <summary>
        ///     Returns the missile position after time time.
        /// </summary>
        public Vector2 GlobalGetMissilePosition(int time)
        {
            var t = Math.Max(0, Environment.TickCount + time - StartTick - SpellData.Delay);
            t = (int)Math.Max(0, Math.Min(End.Distance(Start), t * SpellData.MissileSpeed / 1000));
            return Start + Direction * t;
        }

        /// <summary>
        ///     Returns the missile position after time time.
        /// </summary>
        public Vector2 GetMissilePosition(int time)
        {
            var t = Math.Max(0, Environment.TickCount + time - StartTick - SpellData.Delay);


            var x = 0;

            //Missile with acceleration = 0.
            if (SpellData.MissileAccel == 0)
            {
                x = t * SpellData.MissileSpeed / 1000;
            }

                //Missile with constant acceleration.
            else
            {
                var t1 = (SpellData.MissileAccel > 0
                    ? SpellData.MissileMaxSpeed
                    : SpellData.MissileMinSpeed - SpellData.MissileSpeed) * 1000f / SpellData.MissileAccel;

                if (t <= t1)
                {
                    x =
                        (int)
                            (t * SpellData.MissileSpeed / 1000d + 0.5d * SpellData.MissileAccel * Math.Pow(t / 1000d, 2));
                }
                else
                {
                    x =
                        (int)
                            (t1 * SpellData.MissileSpeed / 1000d +
                             0.5d * SpellData.MissileAccel * Math.Pow(t1 / 1000d, 2) +
                             (t - t1) / 1000d *
                             (SpellData.MissileAccel < 0 ? SpellData.MissileMaxSpeed : SpellData.MissileMinSpeed));
                }
            }

            t = (int)Math.Max(0, Math.Min(CollisionEnd.Distance(Start), x));
            return Start + Direction * t;
        }

        /// <summary>
        ///     Returns if the skillshot will hit the unit if the unit follows the path.
        /// </summary>
        public SafePathResult IsSafePath(List<Vector2> path,
            int timeOffset,
            int speed = -1,
            int delay = 0,
            Obj_AI_Base unit = null)
        {
            var Distance = 0f;
            timeOffset += Game.Ping / 2;

            speed = (speed == -1) ? (int)ObjectManager.Player.MoveSpeed : speed;

            if (unit == null)
            {
                unit = ObjectManager.Player;
            }

            var allIntersections = new List<FoundIntersection>();
            for (var i = 0; i <= path.Count - 2; i++)
            {
                var from = path[i];
                var to = path[i + 1];
                var segmentIntersections = new List<FoundIntersection>();

                for (var j = 0; j <= Polygon.Points.Count - 1; j++)
                {
                    var sideStart = Polygon.Points[j];
                    var sideEnd = Polygon.Points[j == (Polygon.Points.Count - 1) ? 0 : j + 1];

                    var intersection = from.Intersection(to, sideStart,
                        sideEnd);

                    if (intersection.Intersects)
                    {
                        segmentIntersections.Add(
                            new FoundIntersection(
                                Distance + intersection.Point.Distance(from),
                                (int)((Distance + intersection.Point.Distance(from)) * 1000 / speed),
                                intersection.Point, from));
                    }
                }

                var sortedList = segmentIntersections.OrderBy(o => o.Distance).ToList();
                allIntersections.AddRange(sortedList);

                Distance += from.Distance(to);
            }

            //Skillshot with missile.
            if (SpellData.Type == SkillShotType.SkillshotMissileLine ||
                SpellData.Type == SkillShotType.SkillshotMissileCone)
            {
                //Outside the skillshot
                if (IsSafe(ObjectManager.Player.ServerPosition.To2D()))
                {
                    //No intersections -> Safe
                    if (allIntersections.Count == 0)
                    {
                        return new SafePathResult(true, new FoundIntersection());
                    }

                    for (var i = 0; i <= allIntersections.Count - 1; i = i + 2)
                    {
                        var enterIntersection = allIntersections[i];
                        var enterIntersectionProjection = enterIntersection.Point.ProjectOn(Start, End).SegmentPoint;

                        //Intersection with no exit point.
                        if (i == allIntersections.Count - 1)
                        {
                            var missilePositionOnIntersection =
                                GetMissilePosition(enterIntersection.Time - timeOffset);
                            return
                                new SafePathResult(
                                    (End.Distance(missilePositionOnIntersection) + 50 <=
                                     End.Distance(enterIntersectionProjection)) &&
                                    ObjectManager.Player.MoveSpeed < SpellData.MissileSpeed, allIntersections[0]);
                        }


                        var exitIntersection = allIntersections[i + 1];
                        var exitIntersectionProjection = exitIntersection.Point.ProjectOn(Start, End).SegmentPoint;

                        var missilePosOnEnter = GetMissilePosition(enterIntersection.Time - timeOffset);
                        var missilePosOnExit = GetMissilePosition(exitIntersection.Time + timeOffset);

                        //Missile didnt pass.
                        if (missilePosOnEnter.Distance(End) + 50 > enterIntersectionProjection.Distance(End))
                        {
                            if (missilePosOnExit.Distance(End) <= exitIntersectionProjection.Distance(End))
                            {
                                return new SafePathResult(false, allIntersections[0]);
                            }
                        }
                    }

                    return new SafePathResult(true, allIntersections[0]);
                }
                //Inside the skillshot.
                if (allIntersections.Count == 0)
                {
                    return new SafePathResult(false, new FoundIntersection());
                }

                if (allIntersections.Count > 0)
                {
                    //Check only for the exit point
                    var exitIntersection = allIntersections[0];
                    var exitIntersectionProjection = exitIntersection.Point.ProjectOn(Start, End).SegmentPoint;

                    var missilePosOnExit = GetMissilePosition(exitIntersection.Time + timeOffset);
                    if (missilePosOnExit.Distance(End) <= exitIntersectionProjection.Distance(End))
                    {
                        return new SafePathResult(false, allIntersections[0]);
                    }
                }
            }


            if (IsSafe(ObjectManager.Player.ServerPosition.To2D()))
            {
                if (allIntersections.Count == 0)
                {
                    return new SafePathResult(true, new FoundIntersection());
                }

                if (SpellData.DontCross)
                {
                    return new SafePathResult(false, allIntersections[0]);
                }
            }
            else
            {
                if (allIntersections.Count == 0)
                {
                    return new SafePathResult(false, new FoundIntersection());
                }
            }

            var timeToExplode = (SpellData.DontAddExtraDuration ? 0 : SpellData.ExtraDuration) + SpellData.Delay +
                                (int)(1000 * Start.Distance(End) / SpellData.MissileSpeed) -
                                (Environment.TickCount - StartTick);


            var myPositionWhenExplodes = path.PositionAfter(timeToExplode, speed, delay);

            if (!IsSafe(myPositionWhenExplodes))
            {
                return new SafePathResult(false, allIntersections[0]);
            }

            var myPositionWhenExplodesWithOffset = path.PositionAfter(timeToExplode, speed, timeOffset);

            return new SafePathResult(IsSafe(myPositionWhenExplodesWithOffset), allIntersections[0]);
        }

        public bool IsSafe(Vector2 point)
        {
            return Polygon.IsOutside(point);
        }

        public bool IsDanger(Vector2 point)
        {
            return !IsSafe(point);
        }

        //Returns if the skillshot is about to hit the unit in the next time seconds.
        public bool IsAboutToHit(int time, Obj_AI_Base unit)
        {
            if (SpellData.Type == SkillShotType.SkillshotMissileLine)
            {
                var missilePos = GetMissilePosition(0);
                var missilePosAfterT = GetMissilePosition(time);

                //TODO: Check for minion collision etc.. in the future.
                var projection = unit.ServerPosition.To2D()
                    .ProjectOn(missilePos, missilePosAfterT);

                if (projection.IsOnSegment && projection.SegmentPoint.Distance(unit.ServerPosition) < SpellData.Radius)
                {
                    return true;
                }

                return false;
            }

            if (!IsSafe(unit.ServerPosition.To2D()))
            {
                var timeToExplode = SpellData.ExtraDuration + SpellData.Delay +
                                    (int)((1000 * Start.Distance(End)) / SpellData.MissileSpeed) -
                                    (Environment.TickCount - StartTick);
                if (timeToExplode <= time)
                {
                    return true;
                }
            }

            return false;
        }
    }
}