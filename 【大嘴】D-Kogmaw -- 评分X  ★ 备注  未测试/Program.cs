﻿using System;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using LeagueSharp;
using LeagueSharp.Common;
using System.Collections.Generic;

namespace D_Kogmaw
{
    public static class Program
    {
        private const string ChampionName = "KogMaw";

        private static Orbwalking.Orbwalker _orbwalker;

        private static Spell _q, _w, _e, _r;

        private static Menu _config;

        private static Obj_AI_Hero _player;

        private static int _champSkin;

        private static bool _initialSkin = true;

        private static SpellSlot _igniteSlot;

        private static Items.Item _youmuu, _dfg, _blade, _bilge;

        private static readonly List<string> Skins = new List<string>();

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void CreateSkins()
        {
            Skins.Add("Kog'Maw");
            Skins.Add("Caterpillar Kog'Maw");
            Skins.Add("Sonoran Kog'Maw");
            Skins.Add("Monarch Kog'Maw");
            Skins.Add("Reindeer Kog'Maw");
            Skins.Add("Lion Dance Kog'Maw");
            Skins.Add("Deep Sea Kog'Maw");
            Skins.Add("Jurassic Kog'Maw");
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            _player = ObjectManager.Player;
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;

            _q = new Spell(SpellSlot.Q, 1100f);
            _w = new Spell(SpellSlot.W, float.MaxValue);
            _e = new Spell(SpellSlot.E, 1300f);
            _r = new Spell(SpellSlot.R, float.MaxValue);
           
            _q.SetSkillshot(0.25f, 70f, 1650f, true, SkillshotType.SkillshotLine);
            _e.SetSkillshot(0.25f, 120f, 1400f, false, SkillshotType.SkillshotLine);
            _r.SetSkillshot(1.3f, 120f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            _dfg = Utility.Map.GetMap()._MapType == Utility.Map.MapType.TwistedTreeline ||
                   Utility.Map.GetMap()._MapType == Utility.Map.MapType.CrystalScar
                ? new Items.Item(3188, 750)
                : new Items.Item(3128, 750);
            
            _youmuu = new Items.Item(3142, 10);
            _bilge = new Items.Item(3144, 475f);
            _blade = new Items.Item(3153, 475f);
            _igniteSlot = _player.GetSpellSlot("SummonerDot");

            CreateSkins();

            //D Kogmaw
            _config = new Menu("D-Kogmaw", "D-Kogmaw", true);

            //TargetSelector
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            _config.AddSubMenu(targetSelectorMenu);

            //Orbwalker
            _config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            _orbwalker = new Orbwalking.Orbwalker(_config.SubMenu("Orbwalking"));

            if (Skins.Count > 0)
            {
                _config.AddSubMenu(new Menu("Skin Changer", "Skin Changer"));
                _config.SubMenu("Skin Changer")
                    .AddItem(new MenuItem("Skin_enabled", "Enable skin changer").SetValue(false));
                _config.SubMenu("Skin Changer")
                    .AddItem(new MenuItem("Skin_select", "Skins").SetValue(new StringList(Skins.ToArray())));
                _champSkin = _config.Item("Skin_select").GetValue<StringList>().SelectedIndex;
            }

            //Combo
            _config.AddSubMenu(new Menu("Combo", "Combo"));
            _config.SubMenu("Combo").AddItem(new MenuItem("UseQC", "Use Q")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("UseWC", "Use W")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("UseEC", "Use E")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("UseRC", "Use R")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("UseIgnitecombo", "Use Ignite(rush for it)")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("RlimC", "R Limit").SetValue(new Slider(3, 1, 5)));
            _config.SubMenu("Combo")
                .AddItem(new MenuItem("ActiveCombo", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

            _config.AddSubMenu(new Menu("items", "items"));
            _config.SubMenu("items").AddItem(new MenuItem("Youmuu", "Use Youmuu's")).SetValue(true);
            _config.SubMenu("items").AddItem(new MenuItem("Bilge", "Use Bilge")).SetValue(true);
            _config.SubMenu("items").AddItem(new MenuItem("BilgeEnemyhp", "If Enemy Hp <").SetValue(new Slider(85, 1, 100)));
            _config.SubMenu("items").AddItem(new MenuItem("Bilgemyhp", "Or your Hp < ").SetValue(new Slider(85, 1, 100)));
            _config.SubMenu("items").AddItem(new MenuItem("Blade", "Use Blade")).SetValue(true);
            _config.SubMenu("items").AddItem(new MenuItem("BladeEnemyhp", "If Enemy Hp <").SetValue(new Slider(85, 1, 100)));
            _config.SubMenu("items").AddItem(new MenuItem("Blademyhp", "Or Your  Hp <").SetValue(new Slider(85, 1, 100)));
            _config.SubMenu("items").AddItem(new MenuItem("usedfg", "Use DFG")).SetValue(true);
           

            //Harass
            _config.AddSubMenu(new Menu("Harass", "Harass"));
            _config.SubMenu("Harass").AddItem(new MenuItem("UseQH", "Use Q")).SetValue(true);
            _config.SubMenu("Harass").AddItem(new MenuItem("UseWH", "Use W")).SetValue(true);
            _config.SubMenu("Harass").AddItem(new MenuItem("UseEH", "Use E")).SetValue(true);
            _config.SubMenu("Harass").AddItem(new MenuItem("UseRH", "Use R")).SetValue(true);
            _config.SubMenu("Harass").AddItem(new MenuItem("RlimH", "R Limit").SetValue(new Slider(2, 1, 5)));
            _config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("harasstoggle", "AutoHarass (toggle)").SetValue(new KeyBind("G".ToCharArray()[0],
                        KeyBindType.Toggle)));
            _config.SubMenu("Harass")
                .AddItem(new MenuItem("Harrasmana", "Minimum Mana").SetValue(new Slider(65, 1, 100)));
            _config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("ActiveHarass", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            //Farm
            _config.AddSubMenu(new Menu("Farm", "Farm"));
            _config.SubMenu("Farm").AddSubMenu(new Menu("Lasthit", "Lasthit"));
            _config.SubMenu("Farm").SubMenu("Lasthit").AddItem(new MenuItem("UseQLH", "Q LastHit")).SetValue(true);
            _config.SubMenu("Farm").SubMenu("Lasthit").AddItem(new MenuItem("UseELH", "E LastHit")).SetValue(true);
            _config.SubMenu("Farm")
                .SubMenu("Lasthit")
                .AddItem(new MenuItem("Lastmana", "Minimum Mana").SetValue(new Slider(65, 1, 100)));
            _config.SubMenu("Farm")
                .SubMenu("Lasthit")
                .AddItem(
                    new MenuItem("ActiveLast", "LastHit").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));
            _config.SubMenu("Farm").AddSubMenu(new Menu("Laneclear", "Laneclear"));
            _config.SubMenu("Farm").SubMenu("Laneclear").AddItem(new MenuItem("UseQL", "Q LaneClear")).SetValue(true);
            _config.SubMenu("Farm").SubMenu("Laneclear").AddItem(new MenuItem("UseEL", "E LaneClear")).SetValue(true);
            _config.SubMenu("Farm").SubMenu("Laneclear").AddItem(new MenuItem("UseRL", "R LaneClear")).SetValue(true);
            _config.SubMenu("Farm")
                .SubMenu("Laneclear")
                .AddItem(new MenuItem("RlimL", "R Max Stuck").SetValue(new Slider(1, 1, 5)));
            _config.SubMenu("Farm")
                .SubMenu("Laneclear")
                .AddItem(new MenuItem("Lanemana", "Minimum Mana").SetValue(new Slider(65, 1, 100)));
            _config.SubMenu("Farm")
                .SubMenu("Laneclear")
                .AddItem(
                    new MenuItem("ActiveLane", "Lane Clear").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));
            _config.SubMenu("Farm").AddSubMenu(new Menu("Jungleclear", "Jungleclear"));
            _config.SubMenu("Farm").SubMenu("Jungleclear").AddItem(new MenuItem("UseQJ", "Q Jungle")).SetValue(true);
            _config.SubMenu("Farm").SubMenu("Jungleclear").AddItem(new MenuItem("UseWJ", "W Jungle")).SetValue(true);
            _config.SubMenu("Farm").SubMenu("Jungleclear").AddItem(new MenuItem("UseEJ", "E Jungle")).SetValue(true);
            _config.SubMenu("Farm").SubMenu("Jungleclear").AddItem(new MenuItem("UseRJ", "R Jungle")).SetValue(true);
            _config.SubMenu("Farm")
                .SubMenu("Jungleclear")
                .AddItem(new MenuItem("RlimJ", "R Max Stuck").SetValue(new Slider(2, 1, 5)));
            _config.SubMenu("Farm")
                .SubMenu("Jungleclear")
                .AddItem(new MenuItem("junglemana", "Minimum Mana").SetValue(new Slider(60, 1, 100)));
            _config.SubMenu("Farm")
                .SubMenu("Jungleclear")
                .AddItem(
                    new MenuItem("Activejungle", "Jungle Clear").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));

            //Misc
            _config.AddSubMenu(new Menu("Misc", "Misc"));
            _config.SubMenu("Misc").AddItem(new MenuItem("UseRM", "Use R KillSteal")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("useigniteks", "Use Ignite KillSteal")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("usePackets", "Usepackes")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("Gap_E", "GapClosers E")).SetValue(true);

            //HitChance
            _config.AddSubMenu(new Menu("HitChance", "HitChance"));

            _config.SubMenu("HitChance").AddSubMenu(new Menu("Harass", "Harass"));
            _config.SubMenu("HitChance").SubMenu("Harass").AddItem(new MenuItem("QchangeHar", "Q Hit").SetValue(
                new StringList(new[] {"Low", "Medium", "High", "Very High"})));
            _config.SubMenu("HitChance").SubMenu("Harass").AddItem(new MenuItem("EchangeHar", "E Hit").SetValue(
                new StringList(new[] {"Low", "Medium", "High", "Very High"})));
            _config.SubMenu("HitChance").SubMenu("Harass").AddItem(new MenuItem("RchangeHar", "R Hit").SetValue(
                new StringList(new[] {"Low", "Medium", "High", "Very High"})));
            _config.SubMenu("HitChance").AddSubMenu(new Menu("Combo", "Combo"));
            _config.SubMenu("HitChance").SubMenu("Combo").AddItem(new MenuItem("Qchange", "Q Hit").SetValue(
                new StringList(new[] {"Low", "Medium", "High", "Very High"})));
            _config.SubMenu("HitChance").SubMenu("Combo").AddItem(new MenuItem("Echange", "E Hit").SetValue(
                new StringList(new[] {"Low", "Medium", "High", "Very High"})));
            _config.SubMenu("HitChance").SubMenu("Combo").AddItem(new MenuItem("Rchange", "R Hit").SetValue(
                new StringList(new[] {"Low", "Medium", "High", "Very High"})));

            //Damage after combo:
            MenuItem dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Draw damage after combo").SetValue(true);
            Utility.HpBarDamageIndicator.DamageToUnit = ComboDamage;
            Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
            dmgAfterComboItem.ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs eventArgs)
                {
                    Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                };
            //Drawings
            _config.AddSubMenu(new Menu("Drawings", "Drawings"));
            _config.SubMenu("Drawings").AddItem(new MenuItem("DrawQ", "Draw Q")).SetValue(true);
            _config.SubMenu("Drawings").AddItem(new MenuItem("DrawW", "Draw W")).SetValue(true);
            _config.SubMenu("Drawings").AddItem(new MenuItem("DrawE", "Draw E")).SetValue(true);
            _config.SubMenu("Drawings").AddItem(new MenuItem("DrawR", "Draw R")).SetValue(true);
            _config.SubMenu("Drawings").AddItem(dmgAfterComboItem);
            _config.SubMenu("Drawings").AddItem(new MenuItem("CircleLag", "Lag Free Circles").SetValue(true));
            _config.SubMenu("Drawings")
                .AddItem(new MenuItem("CircleQuality", "Circles Quality").SetValue(new Slider(100, 100, 10)));
            _config.SubMenu("Drawings")
                .AddItem(new MenuItem("CircleThickness", "Circles Thickness").SetValue(new Slider(1, 10, 1)));

            _config.AddToMainMenu();
            Game.PrintChat("<font color='#881df2'>D-Kogmaw by Diabaths</font> Loaded.");
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;

            Game.PrintChat("<font color='#FF0000'>If You like my work and want to support, and keep it always up to date plz donate via paypal in </font> <font color='#FF9900'>ssssssssssmith@hotmail.com</font> (10) S");
        
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (_config.Item("ActiveCombo").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            if ((_config.Item("ActiveHarass").GetValue<KeyBind>().Active ||
                 _config.Item("harasstoggle").GetValue<KeyBind>().Active) &&
                (100*(_player.Mana/_player.MaxMana)) > _config.Item("Harrasmana").GetValue<Slider>().Value)
            {
                Harass();

            }
            if (_config.Item("ActiveLane").GetValue<KeyBind>().Active &&
                (100*(_player.Mana/_player.MaxMana)) > _config.Item("Lanemana").GetValue<Slider>().Value)
            {
                Laneclear();
            }
            if (_config.Item("Activejungle").GetValue<KeyBind>().Active &&
                (100*(_player.Mana/_player.MaxMana)) > _config.Item("junglemana").GetValue<Slider>().Value)
            {
                JungleClear();
            }
            if (_config.Item("ActiveLast").GetValue<KeyBind>().Active &&
                (100*(_player.Mana/_player.MaxMana)) > _config.Item("Lastmana").GetValue<Slider>().Value)
            {
                LastHit();
            }
            _w.Range = 110 + 20*ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level;
            _r.Range = 900 + 300*ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level;

            _player = ObjectManager.Player;

            _orbwalker.SetAttack(true);

            KillSteal();

            UpdateSkin();
        }

        private static void UpdateSkin()
        {
            if (_config.Item("Skin_enabled").GetValue<bool>())
            {
                int skin = _config.Item("Skin_select").GetValue<StringList>().SelectedIndex;
                if (_initialSkin || skin != _champSkin)
                {
                    GenerateSkinPacket(ChampionName, skin);
                    _champSkin = skin;
                    _initialSkin = false;
                }
            }
        }

        private static float ComboDamage(Obj_AI_Hero hero)
        {
            var dmg = 0d;

            if (_q.IsReady())
                dmg += _player.GetSpellDamage(hero, SpellSlot.Q);
            if (_w.IsReady())
                dmg += _player.GetSpellDamage(hero, SpellSlot.W);
            if (_e.IsReady())
                dmg += _player.GetSpellDamage(hero, SpellSlot.E);
            if (_r.IsReady())
                dmg += _player.GetSpellDamage(hero, SpellSlot.R)*3;
            if (Items.HasItem(3153) && Items.CanUseItem(3153))
                dmg += _player.GetItemDamage(hero, Damage.DamageItems.Botrk);
            if (Items.HasItem(3128))
            {
                dmg += _player.GetItemDamage(hero, Damage.DamageItems.Dfg);
                dmg = dmg*1.2;
            }
            if (ObjectManager.Player.GetSpellSlot("SummonerIgnite") != SpellSlot.Unknown)
            {
                dmg += _player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite);
            }
            dmg += _player.GetAutoAttackDamage(hero, true)*2;
            return (float) dmg;
        }

        //By Trelli
        private static void GenerateSkinPacket(string currentChampion, int skinNumber)
        {
            int netid = ObjectManager.Player.NetworkId;
            GamePacket model =
                Packet.S2C.UpdateModel.Encoded(new Packet.S2C.UpdateModel.Struct(ObjectManager.Player.NetworkId,
                    skinNumber, currentChampion));
            model.Process(PacketChannel.S2C);
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (_e.IsReady() && gapcloser.Sender.IsValidTarget(_e.Range) && _config.Item("Gap_E").GetValue<bool>())
            {
                _e.Cast(gapcloser.Sender, Packets());
            }
        }

        private static void Combo()
        {
            if (!Orbwalking.CanMove(100) &&
                !(ObjectManager.Player.BaseAbilityDamage + ObjectManager.Player.FlatMagicDamageMod > 100)) return;
            var etarget = SimpleTs.GetTarget(_e.Range, SimpleTs.DamageType.Physical);
            var useQ = _config.Item("UseQC").GetValue<bool>();
            var useW = _config.Item("UseWC").GetValue<bool>();
            var useE = _config.Item("UseEC").GetValue<bool>();
            var useR = _config.Item("UseRC").GetValue<bool>();
            var ignitecombo = _config.Item("UseIgnitecombo").GetValue<bool>();
            var rLim = _config.Item("RlimC").GetValue<Slider>().Value;
            if (_player.Distance(etarget) <= _dfg.Range && _config.Item("usedfg").GetValue<bool>() &&
                _dfg.IsReady() && etarget.Health <= ComboDamage(etarget))
            {
                _dfg.Cast(etarget);
            }
            if (_igniteSlot != SpellSlot.Unknown && ignitecombo &&
                _player.SummonerSpellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
            {
                if (etarget.Health <= ComboDamage(etarget))
                {
                    _player.SummonerSpellbook.CastSpell(_igniteSlot, etarget);
                }
            }
            if (useW && _w.IsReady() && etarget.Distance(_player.Position) < _e.Range)
            {
                foreach (
                    var hero in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(hero => hero.IsValidTarget(Orbwalking.GetRealAutoAttackRange(hero) + _w.Range)))
                    _w.CastOnUnit(ObjectManager.Player);
            }
            if (useQ && _q.IsReady())
            {
                var t = SimpleTs.GetTarget(_q.Range, SimpleTs.DamageType.Magical);
                var prediction = _q.GetPrediction(t);
                if (t != null && _player.Distance(t) < _q.Range && prediction.Hitchance >= Qchangecombo())
                    _q.Cast(prediction.CastPosition, Packets());
            }
            if (useE && _e.IsReady())
            {
                var t = SimpleTs.GetTarget(_e.Range, SimpleTs.DamageType.Magical);
                var predictione = _e.GetPrediction(t);
                if (t != null && _player.Distance(t) < _e.Range && predictione.Hitchance >= Echangecombo())
                    _e.Cast(predictione.CastPosition, Packets());
            }
            if (useR && _r.IsReady() && GetBuffStacks() < rLim)
            {
                var t = SimpleTs.GetTarget(_r.Range, SimpleTs.DamageType.Magical);
                var predictionr = _r.GetPrediction(t);
                if (t != null && _player.Distance(t) < _r.Range && predictionr.Hitchance >= Rchangecombo())
                    _r.Cast(predictionr.CastPosition, Packets());
            }
            UseItemes(etarget);
        }

        private static void UseItemes(Obj_AI_Hero target)
        {
            var iBilge = _config.Item("Bilge").GetValue<bool>();
            var iBilgeEnemyhp = target.Health <=
                                (target.MaxHealth*(_config.Item("BilgeEnemyhp").GetValue<Slider>().Value)/100);
            var iBilgemyhp = _player.Health <=
                             (_player.MaxHealth*(_config.Item("Bilgemyhp").GetValue<Slider>().Value)/100);
            var iBlade = _config.Item("Blade").GetValue<bool>();
            var iBladeEnemyhp = target.Health <=
                                (target.MaxHealth*(_config.Item("BladeEnemyhp").GetValue<Slider>().Value)/100);
            var iBlademyhp = _player.Health <=
                             (_player.MaxHealth*(_config.Item("Blademyhp").GetValue<Slider>().Value)/100);
            var iYoumuu = _config.Item("Youmuu").GetValue<bool>();

            if (_player.Distance(target) <= 450 && iBilge && (iBilgeEnemyhp || iBilgemyhp) && _bilge.IsReady())
            {
                _bilge.Cast(target);

            }
            if (_player.Distance(target) <= 450 && iBlade && (iBladeEnemyhp || iBlademyhp) && _blade.IsReady())
            {
                _blade.Cast(target);

            }
            if (_player.Distance(target) <= 450 && iYoumuu && _youmuu.IsReady())
            {
                _youmuu.Cast();
            }
        }

        private static void Orbwalking_AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            var useQ = _config.Item("UseQC").GetValue<bool>();
            var useW = _config.Item("UseWC").GetValue<bool>();
            var useE = _config.Item("UseEC").GetValue<bool>();
            var useR = _config.Item("UseRC").GetValue<bool>();
            var combo = _config.Item("ActiveCombo").GetValue<KeyBind>().Active;
            var rLim = _config.Item("RlimC").GetValue<Slider>().Value;
            if (combo && unit.IsMe && (target is Obj_AI_Hero))
            {
                if (useW && _w.IsReady())
                {
                    _w.CastOnUnit(ObjectManager.Player);
                }
                if (useQ && _q.IsReady())
                {
                    var prediction = _q.GetPrediction(target);
                    if (_player.Distance(target) < _q.Range && prediction.Hitchance >= HitChance.Medium)
                        _q.Cast(prediction.CastPosition, Packets());
                }
                if (useE && _e.IsReady())
                {
                    var predictione = _e.GetPrediction(target);
                    if (_player.Distance(target) < _e.Range && predictione.Hitchance >= HitChance.Medium)
                        _e.Cast(predictione.CastPosition, Packets());
                }
                if (useR && _r.IsReady() && GetBuffStacks() < rLim)
                {
                    var predictionr = _r.GetPrediction(target);
                    if (_player.Distance(target) < _r.Range && predictionr.Hitchance >= HitChance.Medium)
                        _r.Cast(predictionr.CastPosition, Packets());
                }
            }
        }

        private static void Harass()
        {
            var eTarget = SimpleTs.GetTarget(_e.Range, SimpleTs.DamageType.Physical);
            var useQ = _config.Item("UseQH").GetValue<bool>();
            var useW = _config.Item("UseWH").GetValue<bool>();
            var useE = _config.Item("UseEH").GetValue<bool>();
            var useR = _config.Item("UseRH").GetValue<bool>();
            var rLimH = _config.Item("RlimH").GetValue<Slider>().Value;
            if (useW && _w.IsReady() && eTarget.Distance(_player.Position) < _e.Range)
            {
                foreach (
                    var hero in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(hero => hero.IsValidTarget(Orbwalking.GetRealAutoAttackRange(hero) + _w.Range)))
                    _w.CastOnUnit(ObjectManager.Player);
            }
            if (useQ && _q.IsReady())
            {
                var t = SimpleTs.GetTarget(_q.Range, SimpleTs.DamageType.Magical);
                if (t != null && _player.Distance(t) < _q.Range && _q.GetPrediction(t).Hitchance >= Qchangehar())
                    _q.Cast(t, Packets());
            }

            if (useE && _e.IsReady())
            {
                var t = SimpleTs.GetTarget(_e.Range, SimpleTs.DamageType.Magical);
                if (t != null && _player.Distance(t) < _e.Range && _e.GetPrediction(t).Hitchance >= Echangehar())
                    _e.Cast(t, Packets(), true);
            }

            if (useR && _r.IsReady() && GetBuffStacks() < rLimH)
            {
                var t = SimpleTs.GetTarget(_r.Range, SimpleTs.DamageType.Magical);
                if (t != null && _player.Distance(t) < _r.Range && _r.GetPrediction(t).Hitchance >= Rchangehar())
                    _r.Cast(t, Packets(), true);
            }
        }

        private static void Laneclear()
        {
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _q.Range, MinionTypes.All);
            var allMinionsR = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _r.Range,
                MinionTypes.All);
            var rangedMinionsR = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _q.Range + _q.Width + 30,
                MinionTypes.Ranged);
            var useQ = _config.Item("UseQL").GetValue<bool>();
            var useE = _config.Item("UseEL").GetValue<bool>();
            var useR = _config.Item("UseRL").GetValue<bool>();
            var rLimL = _config.Item("RlimL").GetValue<Slider>().Value;
            foreach (var minion in allMinionsQ)
                if (_q.IsReady() && useQ)
                {
                    if (allMinionsQ.Count >= 3)
                    {
                        _q.Cast(minion);
                    }
                    else if (!Orbwalking.InAutoAttackRange(minion) &&
                             minion.Health < 0.75*_player.GetSpellDamage(minion, SpellSlot.Q))
                        _q.Cast(minion);
                }
            if (_e.IsReady() && useE)
            {
                var fl2 = _e.GetLineFarmLocation(allMinionsQ, _e.Width);
                if (fl2.MinionsHit >= 3)
                {
                    _e.Cast(fl2.Position);
                }
                else
                    foreach (var minion in allMinionsQ)
                        if (!Orbwalking.InAutoAttackRange(minion) &&
                            minion.Health < 0.75*_player.GetSpellDamage(minion, SpellSlot.E))
                            _e.Cast(minion);
            }
            if (_r.IsReady() && useR && GetBuffStacks() < rLimL)
            {
                var fl1 = _r.GetCircularFarmLocation(rangedMinionsR, _r.Width);
                var fl2 = _r.GetCircularFarmLocation(allMinionsR, _r.Width);

                if (fl1.MinionsHit >= 3)
                {
                    _r.Cast(fl1.Position);
                }
                else if (fl2.MinionsHit >= 2 || allMinionsQ.Count == 1)
                {
                    _r.Cast(fl2.Position);
                }
                else
                    foreach (var minion in allMinionsR)
                        if (!Orbwalking.InAutoAttackRange(minion) &&
                            minion.Health < 0.75*_player.GetSpellDamage(minion, SpellSlot.R))
                            _r.Cast(minion);
            }
        }

        private static void LastHit()
        {
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _e.Range, MinionTypes.All);
            var useQ = _config.Item("UseQLH").GetValue<bool>();
            var useE = _config.Item("UseELH").GetValue<bool>();
            if (allMinions.Count < 2) return;

            foreach (var minion in allMinions)
            {
                if (useQ && _q.IsReady() && minion.Distance(_player.Position) < _q.Range &&
                    minion.Health < 0.75*_player.GetSpellDamage(minion, SpellSlot.Q))
                {
                    _q.Cast(minion);
                }
                if (_e.IsReady() && useE && minion.Distance(_player.Position) < _e.Range &&
                    minion.Health < 0.75*_player.GetSpellDamage(minion, SpellSlot.E))
                {
                    _q.Cast(minion);
                }
            }
        }

        private static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(_player.ServerPosition, _q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var useQ = _config.Item("UseQJ").GetValue<bool>();
            var useW = _config.Item("UseWJ").GetValue<bool>();
            var useE = _config.Item("UseEJ").GetValue<bool>();
            var useR = _config.Item("UseRJ").GetValue<bool>();
            var rLimJ = _config.Item("RlimJ").GetValue<Slider>().Value;
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (useQ && _q.IsReady())
                {
                    _q.Cast(mob);
                }
                if (useW && _w.IsReady() && mob.Distance(_player.Position) < _q.Range)
                {
                    _w.Cast();
                }
                if (_e.IsReady() && useE)
                {
                    _e.Cast(mob);
                }
                if (_r.IsReady() && useR && GetBuffStacks() < rLimJ)
                {
                    _r.Cast(mob);
                }
            }
        }

        private static int GetBuffStacks()
        {
            if (_player.HasBuff("KogMawLivingArtillery"))
            {
                return _player.Buffs
                    .Where(x => x.DisplayName == "KogMawLivingArtillery")
                    .Select(x => x.Count)
                    .First();
            }
            else
            {
                return 0;
            }
        }

        private static HitChance Qchangecombo()
        {
            switch (_config.Item("Qchange").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    return HitChance.Low;
                case 1:
                    return HitChance.Medium;
                case 2:
                    return HitChance.High;
                case 3:
                    return HitChance.VeryHigh;
                default:
                    return HitChance.High;
            }
        }

        private static HitChance Echangecombo()
        {
            switch (_config.Item("Echange").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    return HitChance.Low;
                case 1:
                    return HitChance.Medium;
                case 2:
                    return HitChance.High;
                case 3:
                    return HitChance.VeryHigh;
                default:
                    return HitChance.High;
            }
        }

        private static HitChance Rchangecombo()
        {
            switch (_config.Item("Rchange").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    return HitChance.Low;
                case 1:
                    return HitChance.Medium;
                case 2:
                    return HitChance.High;
                case 3:
                    return HitChance.VeryHigh;
                default:
                    return HitChance.High;
            }
        }

        private static HitChance Qchangehar()
        {
            switch (_config.Item("QchangeHar").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    return HitChance.Low;
                case 1:
                    return HitChance.Medium;
                case 2:
                    return HitChance.High;
                case 3:
                    return HitChance.VeryHigh;
                default:
                    return HitChance.High;
            }
        }

        private static HitChance Echangehar()
        {
            switch (_config.Item("EchangeHar").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    return HitChance.Low;
                case 1:
                    return HitChance.Medium;
                case 2:
                    return HitChance.High;
                case 3:
                    return HitChance.VeryHigh;
                default:
                    return HitChance.High;
            }
        }

        private static HitChance Rchangehar()
        {
            switch (_config.Item("RchangeHar").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    return HitChance.Low;
                case 1:
                    return HitChance.Medium;
                case 2:
                    return HitChance.High;
                case 3:
                    return HitChance.VeryHigh;
                default:
                    return HitChance.High;
            }
        }

        private static bool Packets()
        {
            return _config.Item("usePackets").GetValue<bool>();
        }

        private static void KillSteal()
        {
            var target = SimpleTs.GetTarget(_e.Range, SimpleTs.DamageType.Magical);
            var igniteDmg = _player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
            if (target != null && _config.Item("useigniteks").GetValue<bool>() && _igniteSlot != SpellSlot.Unknown &&
                _player.SummonerSpellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
            {
                if (igniteDmg > target.Health)
                {
                    _player.SummonerSpellbook.CastSpell(_igniteSlot, target);
                }
            }
            if (_r.IsReady() && _config.Item("UseRM").GetValue<bool>())
                foreach (
                    var hero in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(
                                hero => hero.IsValidTarget(_r.Range) && _r.GetDamage(hero) > hero.Health))
                    _r.Cast(hero, false, true);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (_config.Item("CircleLag").GetValue<bool>())
            {
                if (_config.Item("DrawQ").GetValue<bool>())
                {
                    Utility.DrawCircle(ObjectManager.Player.Position, _q.Range, System.Drawing.Color.Gray,
                        _config.Item("CircleThickness").GetValue<Slider>().Value,
                        _config.Item("CircleQuality").GetValue<Slider>().Value);
                }
                if (_config.Item("DrawW").GetValue<bool>())
                {
                    Utility.DrawCircle(ObjectManager.Player.Position,
                        Orbwalking.GetRealAutoAttackRange(null) + 65 + _w.Range, System.Drawing.Color.Gray,
                        _config.Item("CircleThickness").GetValue<Slider>().Value,
                        _config.Item("CircleQuality").GetValue<Slider>().Value);
                }
                if (_config.Item("DrawE").GetValue<bool>())
                {
                    Utility.DrawCircle(ObjectManager.Player.Position, _e.Range, System.Drawing.Color.Gray,
                        _config.Item("CircleThickness").GetValue<Slider>().Value,
                        _config.Item("CircleQuality").GetValue<Slider>().Value);
                }
                if (_config.Item("DrawR").GetValue<bool>())
                {
                    Utility.DrawCircle(ObjectManager.Player.Position, _r.Range, System.Drawing.Color.Gray,
                        _config.Item("CircleThickness").GetValue<Slider>().Value,
                        _config.Item("CircleQuality").GetValue<Slider>().Value);
                }
            }
            else
            {
                if (_config.Item("DrawQ").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, _q.Range, System.Drawing.Color.White);
                }
                if (_config.Item("DrawW").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position,
                        Orbwalking.GetRealAutoAttackRange(null) + 65 + _w.Range, System.Drawing.Color.White);
                }
                if (_config.Item("DrawE").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, _e.Range, System.Drawing.Color.White);
                }

                if (_config.Item("DrawR").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, _r.Range, System.Drawing.Color.White);
                }

            }
        }
    }
}
  