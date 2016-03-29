using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
//using Color = System.Drawing.Color;

namespace AhriBuddy
{
    public class Program
    {
        private static string championName = "Ahri";
        private static Spell.Targeted Ignite, Flash;
        private static HpBarIndicator Indicator = new HpBarIndicator();
        private static AIHeroClient Player;
        private static Spell.Skillshot Q, W, E, EFlash, R;

        const float spellQSpeed = 2600;
        const float spellQSpeedMin = 400;
        const float spellQFarmSpeed = 1600;
        const float spellQAcceleration = -3200;

        private static Item Lich_Bane = new Item((int)ItemId.Lich_Bane, 1000);
        private static Item Sheen = new Item((int)ItemId.Sheen, 1000);

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += new Loading.LoadingCompleteHandler(Game_OnGameLoad);
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            if (Player.ChampionName != championName) return;

            Q = new Spell.Skillshot(SpellSlot.Q, 880, SkillShotType.Linear, 250, 1700, 50) { AllowedCollisionCount = int.MaxValue };
            W = new Spell.Skillshot(SpellSlot.W, 750, SkillShotType.Circular, 700, 1400, 300);
            E = new Spell.Skillshot(SpellSlot.E, 975, SkillShotType.Linear, 250, 1600, 60) { AllowedCollisionCount = 0 };
            EFlash = new Spell.Skillshot(SpellSlot.E, 1350, SkillShotType.Linear, 250, 1600, 60) { AllowedCollisionCount = 0 };
            R = new Spell.Skillshot(SpellSlot.R, 475, SkillShotType.Circular, 250, 1400, 300);

            var ignite_slot = Player.GetSpellSlotFromName("summonerdot");
            if (ignite_slot != SpellSlot.Unknown)
                Ignite = new Spell.Targeted(ignite_slot, 600);


            var flash_slot = Player.GetSpellSlotFromName("summonerflash");
            if (flash_slot != SpellSlot.Unknown)
                Flash = new Spell.Targeted(flash_slot, 425);

            MenuLoad();
            Game.OnUpdate += Game_OnGameUpdate;
            Game.OnTick += Game_OnTick;
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Interrupter.OnInterruptableSpell += new Interrupter.InterruptableSpellHandler(Interrupter_OnInterruptableSpell);
        }

        private static Menu Menu, HarassM, AutoHarassM, LaneClearM, JungleClearM, ComboM, KillStealM, MiscM, FleeM, SkinM, DrawM;


        private static bool GetValue(Menu menu, string MenuValue)
        {
            return menu[MenuValue].Cast<CheckBox>().CurrentValue;
        }

        private static bool GetKeyBind(Menu menu, string MenuValue)
        {
            return menu[MenuValue].Cast<KeyBind>().CurrentValue;
        }

        private static int Getslidervalue(Menu menu, string menuvalue)
        {
            return menu[menuvalue].Cast<Slider>().CurrentValue;
        }


        private static int GetCombobox(Menu menu, string menuvalue)
        {
            return menu[menuvalue].Cast<ComboBox>().CurrentValue;
        }
        private static void StringList(Menu menu, string uniqueId, string displayName, string[] values, int defaultValue)
        {
            Slider slider = menu.Add(uniqueId, new Slider(displayName, defaultValue, 0, values.Length - 1));
            slider.DisplayName = displayName + ": " + values[slider.CurrentValue];
            slider.OnValueChange += (sender, args) => sender.DisplayName = displayName + ": " + values[args.NewValue];
        }

        private static void MenuLoad()
        {
            Menu = MainMenu.AddMenu("Ahri Buddy", "Ahri Buddy");

            HarassM = Menu.AddSubMenu("Harass", "Harass");
            HarassM.Add("Harass Q", new CheckBox("Use Q", true));
            HarassM.Add("Harass W", new CheckBox("Use W", false));
            HarassM.Add("Harass E", new CheckBox("Use E", true));

            AutoHarassM = Menu.AddSubMenu("Auto Harass", "Auto Harass");
            AutoHarassM.Add("Auto Harass Q", new CheckBox("Use Q", true));
            AutoHarassM.Add("Auto Harass W", new CheckBox("Use W", false));
            AutoHarassM.Add("Auto Harass E", new CheckBox("Use E", false));
            AutoHarassM.Add("Auto Harass Key", new KeyBind("Auto Harass Toggle Key", true, KeyBind.BindTypes.PressToggle, 'H'));

            LaneClearM = Menu.AddSubMenu("LaneClear", "LaneClear");
            LaneClearM.Add("LaneClear Q", new CheckBox("Use Q", true));
            LaneClearM.Add("LaneClear W", new CheckBox("Use W", false));
            LaneClearM.Add("LaneClear E", new CheckBox("Use E", false));
            LaneClearM.Add("LaneClearKey", new KeyBind("Lane Clear Toggle Key", true, KeyBind.BindTypes.PressToggle, 'L'));

            JungleClearM = Menu.AddSubMenu("JungleClear", "JungleClear");
            JungleClearM.Add("JungleClear Q", new CheckBox("Use Q", true));
            JungleClearM.Add("JungleClear W", new CheckBox("Use W", false));
            JungleClearM.Add("JungleClear E", new CheckBox("Use E", false));
            JungleClearM.Add("JungleClearKey", new KeyBind("Jungle Clear Toggle Key", false, KeyBind.BindTypes.PressToggle, 'J'));

            ComboM = Menu.AddSubMenu("Combo", "Combo");
            ComboM.Add("Combo Q", new CheckBox("Use Q", true));
            ComboM.Add("Combo W", new CheckBox("Use W", true));
            ComboM.Add("Combo E", new CheckBox("Use E", true));
            ComboM.Add("Combo Ignite", new CheckBox("Use Ignite", true));
            ComboM.Add("Combo Mode", new ComboBox("Combo Logic", 0, "Random", "E -> Q -> Random"));
            ComboM.Add("Combo R", new ComboBox("Combo R Mode(Mouse Pos)", 1, "Never", "Killble", "Always"));
            ComboM.Add("Combo EFlash", new KeyBind("Use E + Flash", false, KeyBind.BindTypes.HoldActive, 'G'));

            KillStealM = Menu.AddSubMenu("KillSteal", "KillSteal");
            KillStealM.Add("KillSteal Q", new CheckBox("Use Q", true));
            KillStealM.Add("KillSteal E", new CheckBox("Use E", true));

            MiscM = Menu.AddSubMenu("Misc", "Misc");
            MiscM.Add("Interrupter", new CheckBox("Auto Interrupter to Use E", true));
            MiscM.Add("gapcloser", new CheckBox("Auto Anti-gapcloser to Use E", true));
            //MiscM.Add("Assassin Manager", new Checkbox("Use Assassin Manager", true));

            FleeM = Menu.AddSubMenu("Flee", "Flee");
            FleeM.Add("Flee Q", new CheckBox("Use Q", true));
            FleeM.Add("Flee R", new CheckBox("Use R", false));

            SkinM = Menu.AddSubMenu("Skin Hack", "SKin Hack");
            SkinM.Add("Skin H onoff", new CheckBox("Skin Hack Off/ On", false));
            StringList(SkinM, "Skin H", "Skin Change", new[] { "Classic", "Dynasty", "Midnight", "Foxfire", "Popstar", "Challenger", "Academy" }, 0);

            DrawM = Menu.AddSubMenu("Draw", "Draw");
            DrawM.Add("Draw Q", new CheckBox("Use Q", false));
            DrawM.Add("Draw W", new CheckBox("Use W", false));
            DrawM.Add("Draw E", new CheckBox("Use E", false));
            DrawM.Add("Draw E Target", new CheckBox("Use E Target", true));
            DrawM.Add("Draw R", new CheckBox("Use R", false));
            DrawM.Add("Draw Damage", new CheckBox("Draw Damage Incidator", true));
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;

            KillSteal();

            if (GetKeyBind(ComboM, "Combo EFlash"))
                CastEFlash();

            if (GetKeyBind(AutoHarassM, "Auto Harass Key"))
                AutoHarass();

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
                Flee();

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                Harass();

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
                JCLear();

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                Clear();

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                Combo();
        }

        private static void Game_OnTick(EventArgs args)
        {
            Skin();
        }

        private static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs args)
        {
            if (GetValue(MiscM, "Interrupter") && E.IsReady() && Player.Distance(sender) < E.Range && sender.IsEnemy)
                E.Cast(args.Sender);
        }


        private static void Gapcloser_OnGapCloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs gapcloser)
        {
            if (GetValue(MiscM, "gapcloser") && E.IsReady() && Player.Distance(sender) < E.Range && sender.IsEnemy)
                E.Cast(gapcloser.Sender);
        }

        private static void KillSteal()
        {
            if (Q.IsReady() && GetValue(KillStealM, "Killsteal Q"))
            {
                var Qtarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                if (Qtarget.IsValidTarget(Q.Range))
                {
                    var prediction = Q.GetPrediction(Qtarget);
                    if (Qtarget.Health < DamageLibrary.GetSpellDamage(Player, Qtarget, SpellSlot.Q) && prediction.HitChance >= HitChance.High)
                    {
                        var Speed = GetDynamicQSpeed(ObjectManager.Player.Distance(prediction.CastPosition));
                        if (Speed > 0f)
                            Q.Cast(prediction.CastPosition);
                    }
                }
            }

            if (E.IsReady() && GetValue(KillStealM, "Killsteal E"))
            {
                var Etarget = TargetSelector.GetTarget(E.Range, DamageType.Magical);
                if (Etarget.IsValidTarget(E.Range))
                {
                    var prediction = E.GetPrediction(Etarget);
                    if (Etarget.Health < DamageLibrary.GetSpellDamage(Player, Etarget, SpellSlot.E) && prediction.HitChance >= HitChance.High)
                        E.Cast(prediction.CastPosition);
                }
            }
        }

        private static void Harass()
        {
            if (GetValue(HarassM, "Harass Q"))
                CastQ();

            if (GetValue(HarassM, "Harass W"))
                CastW();

            if (GetValue(HarassM, "Harass E"))
                CastE();
        }

        private static void AutoHarass()
        {
            if (GetValue(AutoHarassM, "Auto Harass Q"))
                CastQ();

            if (GetValue(AutoHarassM, "Auto Harass W"))
                CastW();

            if (GetValue(AutoHarassM, "Auto Harass E"))
                CastE();
        }

        private static void Clear()
        {
            if (GetKeyBind(LaneClearM, "LaneClearKey"))
            {
                var Minions = EntityManager.MinionsAndMonsters.Get(EntityManager.MinionsAndMonsters.EntityType.Minion, EntityManager.UnitTeam.Enemy, Player.ServerPosition, E.Range);
                if (Minions.Count() == 0) return;

                if (GetValue(LaneClearM, "LaneClear Q") && Q.IsReady())
                {
                    var farmLocation = EntityManager.MinionsAndMonsters.GetLineFarmLocation(Minions, Q.Width, (int)Q.Range);

                    if (Minions.Count() >= 4)
                        if (farmLocation.HitNumber >= (Minions.Count() - 1))
                            Q.Cast(farmLocation.CastPosition);

                    if (farmLocation.HitNumber >= 3)
                        Q.Cast(farmLocation.CastPosition);
                }

                if (GetValue(LaneClearM, "LaneClear W"))
                {
                    var Minion = EntityManager.MinionsAndMonsters.EnemyMinions.FirstOrDefault(x => x.IsValidTarget(E.Range) && x.Health < eDamageCalc(x));
                }
                    EloBuddy.Player.CastSpell(SpellSlot.W);

                if (GetValue(LaneClearM, "LaneClear E"))
                {
                    var Minion = EntityManager.MinionsAndMonsters.EnemyMinions.FirstOrDefault(x => x.IsValidTarget(E.Range) && x.Health < eDamageCalc(x));
                    if (Minion.IsValidTarget())
                    {
                        var pre = E.GetPrediction(Minion);
                        if (pre.HitChance >= HitChance.Low)
                            E.Cast(Minion.Position);
                    }
                }
            }
        }

        private static void JCLear()
        {
            if (GetKeyBind(JungleClearM, "JungleClearKey"))
            {
                var Mobs = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.ServerPosition, E.Range, true);
                if (Mobs.Count() == 0) return;

                if (GetValue(JungleClearM, "JungleClear Q"))
                {
                    var Mob = EntityManager.MinionsAndMonsters.GetLineFarmLocation(Mobs, Q.Width, (int)Q.Range);
                    if (Mobs.Count() == 4 && Mob.HitNumber >= 3)
                        Q.Cast(Mob.CastPosition);

                    if (Mobs.Count() == 3 && Mob.HitNumber >= 2)
                        Q.Cast(Mob.CastPosition);

                    if (Mobs.Count() <= 2)
                        Q.Cast(Mob.CastPosition);
                }

                if (GetValue(JungleClearM, "JungleClear W"))
                    EloBuddy.Player.CastSpell(SpellSlot.W);

                if (GetValue(JungleClearM, "JungleClear E"))
                {
                    var Mob = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.ServerPosition, E.Range, true).FirstOrDefault(x => x.IsValidTarget() && x.Health < eDamageCalc(x));
                    if (Mob.IsValidTarget())
                    {
                        var pre = E.GetPrediction(Mob);
                        if (pre.HitChance >= HitChance.Low)
                            E.Cast(Mob.Position);
                    }
                }
            }
        }

        private static void Combo()
        {

            if (GetCombobox(ComboM, "Combo Mode") == 0)
            {
                if (GetValue(ComboM, "Combo Q"))
                    CastQ();

                if (GetValue(ComboM, "Combo W"))
                    CastW();

                if (GetValue(ComboM, "Combo E"))
                    CastE();
            }

            if (GetCombobox(ComboM, "Combo Mode") == 1)
            {
                if (GetValue(ComboM, "Combo Q") || GetValue(ComboM, "Combo E"))
                    CastEQ();

                if (GetValue(ComboM, "Combo W"))
                    CastW();
            }

            CastR();

            if (GetValue(ComboM, "Combo Ignite"))
                CastIgnite();
        }

        private static void CastQ()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            if (target.IsValidTarget())
            {
                var prediction = Q.GetPrediction(target);
                if (prediction.HitChance >= HitChance.High)
                {
                    var Speed = GetDynamicQSpeed(ObjectManager.Player.Distance(prediction.CastPosition));
                    if (Speed > 0f)
                        Q.Cast(prediction.CastPosition);
                }
            }
        }

        private static void CastW()
        {
            var target = TargetSelector.GetTarget(W.Range, DamageType.Magical);
            if (target.IsValidTarget())
            {
                var pred = W.GetPrediction(target);
                if (Dash.IsDashing(Player) && GetRCount() > 0 && pred.HitChance > HitChance.Low)
                    EloBuddy.Player.CastSpell(SpellSlot.W);

                if (!Player.IsDashing() && Player.Distance(target) < 550 && pred.HitChance >= HitChance.Low)
                    EloBuddy.Player.CastSpell(SpellSlot.W);
            }
        }

        private static void CastE()
        {
            var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            if (target.IsValidTarget() && !target.IsInvulnerable)
            {
                var predition = E.GetPrediction(target);

                if (predition.HitChance >= HitChance.High && !Dash.IsDashing(target))
                    E.Cast(predition.CastPosition);
            }
        }
        
        private static void CastEQ()
        {
            if (E.IsReady() && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);
                if (target.IsValidTarget())
                {
                    var epre = E.GetPrediction(target);
                    if (epre.HitChance >= HitChance.High)
                        if (E.Cast(epre.CastPosition))
                            Q.Cast(target);
                }
            }

            if (!E.IsReady() || !Q.IsReady())
            {
                CastQ();
                CastE();
            }
        }
    
        private static void CastEFlash()
        {
            EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            if (Flash.IsReady())
            {
                var target = TargetSelector.GetTarget(EFlash.Range + 100, DamageType.Magical);
                //var target = TargetSelector.SelectedTarget;
                if (target.IsValidTarget() && !target.IsInvulnerable)
                {
                    var pre = EFlash.GetPrediction(target);
                    var postion = EloBuddy.Player.Instance.ServerPosition.Extend(target.ServerPosition, Flash.Range);
                    int Delay = E.CastDelay + Game.Ping - 60;

                    if (E.IsReady() && pre.HitChance >= HitChance.High)
                        if (EFlash.Cast(pre.CastPosition))
                            Core.DelayAction(delegate ()
                            {
                                Flash.Cast(postion.To3DWorld());
                            }, new Random(DateTime.Now.Millisecond * (int)(Game.CursorPos.X + Player.Position.Y)).Next(Delay, Delay + 30));
                }
            }
        }

        private static void CastR()
        {
            // Player.CalculateDamageOnUnit(target, DamageType.Magical, damage);
            if (R.IsReady())
            {
                var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);
                if (target.IsValidTarget())
                {
                    float totaldamage = GetComboDamage(target) * 0.9f;
                    if (GetCombobox(ComboM, "Combo R") == 1 && target.Health < totaldamage)
                    {
                        R.Cast(Game.CursorPos);
                    }

                    if (GetCombobox(ComboM, "Combo R") == 2)
                        R.Cast(Game.CursorPos);
                }
            }
        }

        private static void CastIgnite()
        {
            // Player.CalculateDamageOnUnit(target, DamageType.Magical, damage);
            var target = TargetSelector.GetTarget(600, DamageType.Magical);
            if (Ignite != null && Ignite.IsReady() && target.IsValidTarget())
                if ((Player.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health) || target.Health < GetComboDamage(target))
                    Ignite.Cast(target);
        }

        private static void Flee()
        {

            if (GetValue(FleeM, "Flee Q") && Q.IsReady() && Player.IsMoving)
            {
                var lastPos = EloBuddy.Player.Instance.ServerPosition;
                Q.Cast(lastPos);
            }

            if (GetValue(FleeM, "Flee R"))
                R.Cast(Game.CursorPos);
        }

        static int GetRCount()
        {
            var buff = Player.Buffs.FirstOrDefault(x => x.Name.Equals("AhriTumble"));
            return buff == null ? 3 : buff.Count;
        }

        private static float GetComboDamage(AIHeroClient Enemy)
        {
            float Damage = 0;

            if (Q.IsReady())
                Damage += (Player.GetSpellDamage(Enemy, SpellSlot.Q) * 2);

            if (W.IsReady())
                Damage += (Player.GetSpellDamage(Enemy, SpellSlot.W));

            if (E.IsReady())
                Damage += (Player.GetSpellDamage(Enemy, SpellSlot.E));

            if (R.IsReady())
                Damage += (Player.GetSpellDamage(Enemy, SpellSlot.R) * GetRCount());

            if (Ignite != null && Ignite.IsReady())
                Damage += DamageLibrary.GetSummonerSpellDamage(Player, Enemy, DamageLibrary.SummonerSpells.Ignite);


            if (Player.HasBuff("itemmagicshankcharge"))
            {
                if (Player.GetBuff("itemmagicshankcharge").Count == 100)
                {
                    Damage += Player.CalculateDamageOnUnit(Enemy, DamageType.Magical, 100f + 0.1f * Player.FlatMagicDamageMod);
                }
            }

            if (Sheen.IsReady() && Sheen.IsOwned())
                Damage += Player.GetAutoAttackDamage(Enemy) + EloBuddy.Player.Instance.BaseAttackDamage * 2;

            if (Lich_Bane.IsReady() && Lich_Bane.IsOwned())
            {
                Damage += Player.GetAutoAttackDamage(Enemy) * 0.75f;
                Damage += Player.CalculateDamageOnUnit(Enemy, DamageType.Magical, Player.FlatMagicDamageMod * 0.5f);
            }

            Damage += Player.GetAutoAttackDamage(Enemy, true);
            Damage -= Enemy.AllShield;

            return Damage;
        }

        static float GetDynamicQSpeed(float distance)
        {
            var a = 0.5f * spellQAcceleration;
            var b = spellQSpeed;
            var c = -distance;

            if (b * b - 4 * a * c <= 0f)
            {
                return 0;
            }

            var t = (float)(-b + Math.Sqrt(b * b - 4 * a * c)) / (2 * a);
            return distance / t;
        }

        static float wDamageCalc(Obj_AI_Minion target)
        {

        }

        static float eDamageCalc(Obj_AI_Minion target)
        {
            return Player.CalculateDamageOnUnit(target, DamageType.Magical, 35f * E.Level + 25 + 0.5f * Player.TotalMagicalDamage);
        }

        static void Skin()
        {
            if (GetValue(SkinM, "Skin H onoff"))
            {
                switch (Getslidervalue(SkinM, "Skin H"))
                {
                    case 0:
                        EloBuddy.Player.SetSkinId(0);
                        break;
                    case 1:
                        EloBuddy.Player.SetSkinId(1);
                        break;
                    case 2:
                        EloBuddy.Player.SetSkinId(2);
                        break;
                    case 3:
                        EloBuddy.Player.SetSkinId(3);
                        break;
                    case 4:
                        EloBuddy.Player.SetSkinId(4);
                        break;
                    case 5:
                        EloBuddy.Player.SetSkinId(5);
                        break;
                    case 6:
                        EloBuddy.Player.SetSkinId(6);
                        break;
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;

            var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);

            if (GetValue(DrawM, "Draw Q")) Circle.Draw(Color.White, Q.Range, EloBuddy.Player.Instance.Position);
            if (GetValue(DrawM, "Draw W")) Circle.Draw(Color.White, W.Range, EloBuddy.Player.Instance.Position);
            if (GetValue(DrawM, "Draw E")) Circle.Draw(Color.White, E.Range, EloBuddy.Player.Instance.Position);
            if (GetValue(DrawM, "Draw E target")) { if (target.IsValidTarget() && !target.IsInvulnerable) Drawing.DrawCircle(target.Position, 150, System.Drawing.Color.Green); }
            if (GetValue(DrawM, "Draw R")) Circle.Draw(Color.White, R.Range, EloBuddy.Player.Instance.Position);
        }

        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (GetValue(DrawM, "Draw Damage"))
            {
                foreach (
                  var enemy in
                      ObjectManager.Get<AIHeroClient>()
                          .Where(ene => ene.IsValidTarget() && !ene.IsZombie && ene.IsEnemy))
                {
                    Indicator.unit = enemy;
                    Indicator.drawDmg(GetComboDamage(enemy), new ColorBGRA(255, 204, 0, 160));
                }
            }
        }
    }
}