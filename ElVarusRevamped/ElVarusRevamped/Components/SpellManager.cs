﻿namespace ElVarusRevamped.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ElVarusRevamped.Components.Spells;
    using ElVarusRevamped.Enumerations;
    using ElVarusRevamped.Utils;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;

    /// <summary>
    ///     The spell manager.
    /// </summary>
    internal class SpellManager
    {
        #region Fields

        /// <summary>
        ///     The spells.
        /// </summary>
        private readonly List<ISpell> spells = new List<ISpell>();

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="SpellManager" /> class.
        /// </summary>
        internal SpellManager()
        {
            try
            {
                this.LoadSpells(new List<ISpell>() { new SpellQ(), new SpellE(), new SpellR() });
            }
            catch (Exception e)
            {
                Logging.AddEntry(LoggingEntryTrype.Error, "@SpellManager.cs: Can not initialize the spells - {0}", e);
                throw;
            }

            Game.OnUpdate += this.Game_OnUpdate;
            AntiGapcloser.OnEnemyGapcloser += this.OnEnemyGapcloser;
        }


        /// <summary>
        ///     
        /// </summary>
        /// <param name="gapcloser"></param>
        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            try
            {
                var qSpell = new SpellQ();
                if (qSpell.SpellObject.IsCharging)
                {
                    return;
                }

                var eSpell = new SpellE();
                if (MyMenu.RootMenu.Item("gapclosereuse").IsActive() && eSpell.SpellSlot.IsReady())
                {
                    if (gapcloser.End.Distance(ObjectManager.Player.Position) <= eSpell.Range)
                    {
                        eSpell.SpellObject.Cast(gapcloser.End);
                    }
                }
            }
            catch (Exception e)
            {
                Logging.AddEntry(LoggingEntryTrype.Error, "@SpellManager.cs: AntiGapcloser - {0}", e);
                throw;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     The is the spell active method.
        /// </summary>
        /// <param name="spellSlot">
        ///     The spell slot.
        /// </param>
        /// <param name="orbwalkingMode">
        ///     The orbwalking mode.
        /// </param>
        /// <returns>
        ///     <see cref="bool" />
        /// </returns>
        private static bool IsSpellActive(SpellSlot spellSlot, Orbwalking.OrbwalkingMode orbwalkingMode)
        {
            if (Program.Orbwalker.ActiveMode != orbwalkingMode || !spellSlot.IsReady())
            {
                return false;
            }
            try
            {
                var orbwalkerModeLower = Program.Orbwalker.ActiveMode.ToString().ToLower();
                var spellSlotNameLower = spellSlot.ToString().ToLower();

                if ((orbwalkerModeLower.Equals("lasthit")
                    && (spellSlotNameLower.Equals("w")
                        || spellSlotNameLower.Equals("r") || spellSlotNameLower.Equals("e"))) || (orbwalkerModeLower.Equals("laneclear") && (spellSlotNameLower.Equals("r")))
                        || (orbwalkerModeLower.Equals("mixed") && (spellSlotNameLower.Equals("r"))))
                {
                    return false;
                }

                return MyMenu.RootMenu.Item(orbwalkerModeLower + spellSlotNameLower + "use").GetValue<bool>()
                       && MyMenu.RootMenu.Item(orbwalkerModeLower + spellSlotNameLower + "mana")
                              .GetValue<Slider>()
                              .Value <= ObjectManager.Player.ManaPercent;
            }
            catch (Exception e)
            {
                Logging.AddEntry(LoggingEntryTrype.Error, "@SpellManager.cs: Can not get spell active state for slot {0} - {1}", spellSlot.ToString(), e);
                throw;
            }
        }

        /// <summary>
        ///     The game on update callback.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        private void Game_OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || MenuGUI.IsChatOpen || MenuGUI.IsShopOpen || Program.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.None) return;

            if (MyMenu.RootMenu.Item("forceqalways").IsActive())
            {
                var target = HeroManager.Enemies.FirstOrDefault(x => Misc.GetWStacks(x) > 0 && x.IsValidTarget(Orbwalking.GetRealAutoAttackRange(ObjectManager.Player) + 150) && !x.IsDead && x.IsVisible);
                if (target != null)
                {
                    Program.Orbwalker.ForceTarget(target);
                }
                else
                {
                    Program.Orbwalker.ForceTarget(null);
                }
            }

            this.spells.Where(spell => IsSpellActive(spell.SpellSlot, Orbwalking.OrbwalkingMode.Combo))
                        .ToList()
                        .ForEach(spell => spell.OnCombo());

            this.spells.Where(spell => IsSpellActive(spell.SpellSlot, Orbwalking.OrbwalkingMode.Mixed))
                        .ToList()
                        .ForEach(spell => spell.OnMixed());

            this.spells.Where(spell => IsSpellActive(spell.SpellSlot, Orbwalking.OrbwalkingMode.LaneClear))
                        .ToList()
                        .ForEach(spell => spell.OnLaneClear());

            this.spells.Where(spell => IsSpellActive(spell.SpellSlot, Orbwalking.OrbwalkingMode.LaneClear))
                        .ToList()
                        .ForEach(spell => spell.OnJungleClear());

            this.spells.Where(spell => IsSpellActive(spell.SpellSlot, Orbwalking.OrbwalkingMode.LastHit))
                        .ToList()
                        .ForEach(spell => spell.OnLastHit());
        }

        /// <summary>
        ///     The load spells method.
        /// </summary>
        /// <param name="spellList">
        ///     The spells.
        /// </param>
        private void LoadSpells(IEnumerable<ISpell> spellList)
        {
            foreach (var spell in spellList)
            {
                MyMenu.GenerateSpellMenu(spell.SpellSlot);
                this.spells.Add(spell);
            }
        }

        #endregion
    }
}
