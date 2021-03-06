﻿#region LICENSE

// Copyright 2014 - 2014 SpellDetector
// SkillshotDetector.cs is part of SpellDetector.
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
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

#endregion

namespace SpellDetector
{
    internal static class SkillshotDetector
    {
        public static event OnDetectSkillshotH OnDetectSkillshot;

        public static event OnDeleteMissileH OnDeleteMissile;

        public delegate void OnDeleteMissileH(Skillshot skillshot, Obj_SpellMissile missile);

        public delegate void OnDetectSkillshotH(Skillshot skillshot);

        public static SpellList<Skillshot> ActiveSkillshots = new SpellList<Skillshot>();

        static SkillshotDetector()
        {
            //Detect when the skillshots are created.
            Game.OnGameProcessPacket += GameOnOnGameProcessPacket; // Used only for viktor's Laser :^)
            Obj_AI_Base.OnProcessSpellCast += HeroOnProcessSpellCast;

            //Detect when projectiles collide.
            GameObject.OnCreate += SpellMissileOnCreate;
            GameObject.OnDelete += SpellMissileOnDelete;

            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;

            // Debug
            if (ObjectManager.Get<Obj_AI_Hero>().Count() == 1)
            {
                GameObject.OnCreate += DebugSpellMissileOnCreate;
                GameObject.OnDelete += DebugSpellMissileOnDelete;
            }
        }

        private static void TriggerOnDetectSkillshot(DetectionType detectionType, SpellData spellData, int startT,
            Vector2 start, Vector2 end, Obj_AI_Base unit)
        {
            var skillshot = new Skillshot(detectionType, spellData, startT, start, end, unit);

            if (OnDetectSkillshot != null)
            {
                OnDetectSkillshot(skillshot);
            }
        }

        #region Debug
        private static void DebugSpellMissileOnCreate(GameObject sender, EventArgs args)
        {
            if (!sender.IsValid<Obj_SpellMissile>())
            {
                return;
            }

            var missile = (Obj_SpellMissile) sender;

            if (missile.SpellCaster.IsValid<Obj_AI_Hero>())
            {
                Console.WriteLine("{0} Missile Created:{1} Distance:{2} Radius:{3} Speed:{4}",
                    Environment.TickCount,
                    missile.SData.Name,
                    missile.StartPosition.Distance(missile.EndPosition),
                    missile.SData.CastRadiusSecondary[0],
                    missile.SData.MissileSpeed);
            }
        }

        private static void DebugSpellMissileOnDelete(GameObject sender, EventArgs args)
        {
            if (!sender.IsValid<Obj_SpellMissile>())
            {
                return;
            }

            var missile = (Obj_SpellMissile) sender;

            if (missile.SpellCaster.IsValid<Obj_AI_Hero>())
            {
                Console.WriteLine("{0} Missile Deleted:{1} Distance:{2}",
                    Environment.TickCount,
                    missile.SData.Name,
                    missile.EndPosition.Distance(missile.Position));
            }
        }
        #endregion

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            //TODO: Detect lux R and other large skillshots.
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (!sender.IsValid)
            {
                return;
            }

            for (var i = ActiveSkillshots.Count - 1; i >= 0; i--)
            {
                var skillshot = ActiveSkillshots[i];
                if (skillshot.SpellData.ToggleParticleName != "" &&
                    sender.Name.Contains(skillshot.SpellData.ToggleParticleName))
                {
                    ActiveSkillshots.RemoveAt(i);
                }
            }
        }

        private static void SpellMissileOnCreate(GameObject sender, EventArgs args)
        {
            if (!sender.IsValid<Obj_SpellMissile>())
            {
                return; // only valid missile
            }

            var missile = (Obj_SpellMissile) sender;
            var unit = missile.SpellCaster;

            if (!unit.IsValid<Obj_AI_Hero>())
            {
                return; // only valid hero
            }

            var spellData = SpellDatabase.GetByMissileName(missile.SData.Name);

            if (spellData == null)
            {
                return; // only if database contains skillshot
            }

            var missilePosition = missile.Position.To2D();
            var unitPosition = missile.StartPosition.To2D();
            var endPos = missile.EndPosition.To2D();

            //Calculate the real end Point:
            var direction = (endPos - unitPosition).Normalized();
            if (unitPosition.Distance(endPos) > spellData.Range || spellData.FixedRange)
            {
                endPos = unitPosition + direction*spellData.Range;
            }

            if (spellData.ExtraRange != -1)
            {
                endPos = endPos +
                         Math.Min(spellData.ExtraRange, spellData.Range - endPos.Distance(unitPosition))*direction;
            }

            var castTime = Environment.TickCount - Game.Ping/2 - (spellData.MissileDelayed ? 0 : spellData.Delay) -
                           (int) (1000*missilePosition.Distance(unitPosition)/spellData.MissileSpeed);

            //Trigger the skillshot detection callbacks.
            TriggerOnDetectSkillshot(DetectionType.RecvPacket, spellData, castTime, unitPosition, endPos, unit);
        }

        /// <summary>
        ///     Delete the missiles that collide.
        /// </summary>
        private static void SpellMissileOnDelete(GameObject sender, EventArgs args)
        {
            if (OnDeleteMissile == null)
            {
                return; // no subscriptions
            }

            if (!sender.IsValid<Obj_SpellMissile>())
            {
                return; // only valid missile
            }

            var missile = (Obj_SpellMissile) sender;
            var unit = missile.SpellCaster;

            if (!unit.IsValid<Obj_AI_Hero>())
            {
                return; // only valid hero
            }

            var spellName = missile.SData.Name;

            foreach (var skillshot in ActiveSkillshots)
            {
                if (skillshot.SpellData.MissileSpellName == spellName &&
                    (skillshot.Unit.NetworkId == unit.NetworkId &&
                     (missile.EndPosition.To2D() - missile.StartPosition.To2D()).AngleBetween(skillshot.Direction) < 10) &&
                    skillshot.SpellData.CanBeRemoved)
                {
                    OnDeleteMissile(skillshot, missile);
                    break;
                }
            }

            ActiveSkillshots.RemoveAll(
                skillshot =>
                    (skillshot.SpellData.MissileSpellName == spellName ||
                     skillshot.SpellData.ExtraMissileNames.Contains(spellName)) &&
                    (skillshot.Unit.NetworkId == unit.NetworkId &&
                     ((missile.EndPosition.To2D() - missile.StartPosition.To2D()).AngleBetween(skillshot.Direction) < 10) &&
                     skillshot.SpellData.CanBeRemoved || skillshot.SpellData.ForceRemove)); // 
        }

        /// <summary>
        ///     Gets triggered when a unit casts a spell and the unit is visible.
        /// </summary>
        private static void HeroOnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (args.SData.Name == "dravenrdoublecast")
            {
                ActiveSkillshots.RemoveAll(
                    s => s.Unit.NetworkId == sender.NetworkId && s.SpellData.SpellName == "DravenRCast");
            }

            if (!sender.IsValid<Obj_AI_Hero>())
            {
                return; // only valid hero
            }

            var spellData = SpellDatabase.GetByName(args.SData.Name);

            if (spellData == null)
            {
                return; // only if database contains skillshot
            }

            var startPos = new Vector2();

            if (spellData.FromObject != "")
            {
                foreach (var obj in ObjectManager.Get<GameObject>())
                {
                    if (obj.Name.Contains(spellData.FromObject))
                    {
                        startPos = obj.Position.To2D();
                    }
                }
            }
            else
            {
                startPos = sender.ServerPosition.To2D();
            }

            //For now only zed support.
            if (spellData.FromObjects != null && spellData.FromObjects.Length > 0)
            {
                foreach (var obj in ObjectManager.Get<GameObject>().Where(o => o.IsEnemy))
                {
                    if (spellData.FromObjects.Contains(obj.Name))
                    {
                        var start = obj.Position.To2D();
                        var end = start + spellData.Range*(args.End.To2D() - obj.Position.To2D()).Normalized();
                        TriggerOnDetectSkillshot(DetectionType.ProcessSpell, spellData,
                            Environment.TickCount - Game.Ping/2, start, end, sender);
                    }
                }
            }

            if (!startPos.IsValid())
            {
                return;
            }

            var endPos = args.End.To2D();

            //Calculate the real end Point:
            var direction = (endPos - startPos).Normalized();
            if (startPos.Distance(endPos) > spellData.Range || spellData.FixedRange)
            {
                endPos = startPos + direction*spellData.Range;
            }

            if (spellData.ExtraRange != -1)
            {
                endPos = endPos + Math.Min(spellData.ExtraRange, spellData.Range - endPos.Distance(startPos))*direction;
            }

            //Trigger the skillshot detection callbacks.
            TriggerOnDetectSkillshot(DetectionType.ProcessSpell, spellData, Environment.TickCount - Game.Ping/2,
                startPos, endPos, sender);
        }

        /// <summary>
        ///     Detects the spells that have missile and are casted from fow.
        /// </summary>
        public static void GameOnOnGameProcessPacket(GamePacketEventArgs args)
        {
            //Gets received when a projectile is created.
            if (args.PacketData[0] == 0x3B)
            {
                var packet = new GamePacket(args.PacketData) {Position = 1};

                packet.ReadFloat(); //Missile network ID

                var missilePosition = new Vector3(packet.ReadFloat(), packet.ReadFloat(), packet.ReadFloat());
                var unitPosition = new Vector3(packet.ReadFloat(), packet.ReadFloat(), packet.ReadFloat());

                packet.Position = packet.Size() - 119;
                var missileSpeed = packet.ReadFloat();

                packet.Position = 65;
                var endPos = new Vector3(packet.ReadFloat(), packet.ReadFloat(), packet.ReadFloat());

                packet.Position = 112;
                var id = packet.ReadByte();

                packet.Position = packet.Size() - 83;

                var unit = ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(packet.ReadInteger());

                if (!unit.IsValid<Obj_AI_Hero>())
                {
                    return; // only valid hero
                }

                var spellData = SpellDatabase.GetBySpeed(unit.ChampionName, (int) missileSpeed, id);

                if (spellData == null)
                {
                    return; // only if database contains skillshot
                }

                if (spellData.SpellName != "Laser")
                {
                    return; // ingore lasers
                }

                var castTime = Environment.TickCount - Game.Ping/2 - spellData.Delay -
                               (int)
                                   (1000*missilePosition.SwitchYZ().To2D().Distance(unitPosition.SwitchYZ())/
                                    spellData.MissileSpeed);

                //Trigger the skillshot detection callbacks.
                TriggerOnDetectSkillshot(DetectionType.RecvPacket, spellData, castTime, unitPosition.SwitchYZ().To2D(),
                    endPos.SwitchYZ().To2D(), unit);
            }
        }
    }
}