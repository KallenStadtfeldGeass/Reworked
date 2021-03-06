﻿namespace ElDianaRevamped.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ElDianaRevamped.Components.Spells;
    using ElDianaRevamped.Enumerations;
    using ElDianaRevamped.Utils;

    using LeagueSharp;
    using LeagueSharp.Common;

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
                this.LoadSpells(new List<ISpell>() { new SpellR(), new SpellE(), new SpellQ(), new SpellW() });
            }
            catch (Exception e)
            {
                Logging.AddEntry(LoggingEntryTrype.Error, "@SpellManager.cs: Can not initialize the spells - {0}", e);
                throw;
            }

            Game.OnUpdate += this.Game_OnUpdate;
            AntiGapcloser.OnEnemyGapcloser += this.OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += this.OnInterruptableTarget;
            CustomEvents.Unit.OnDash += this.OnDash;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnDash(Obj_AI_Base sender, Dash.DashItem args)
        {
            if (!sender.IsEnemy)
            {
                return;
            }

            var eSpell = new SpellE();
            var distance = ObjectManager.Player.Distance(sender);

            if (MyMenu.RootMenu.Item("interrupt.e.dash").IsActive()
                && MyMenu.RootMenu.Item("interrupt.e.dash.mana").GetValue<Slider>().Value
                <= ObjectManager.Player.ManaPercent)
            {
                if (eSpell.SpellSlot.IsReady())
                {
                    if (!args.IsBlink && eSpell.Range <= distance)
                    {
                        eSpell.SpellObject.Cast();
                    }
                }
            }
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="gapcloser"></param>
        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            try
            {
                var eSpell = new SpellE();
                if (!gapcloser.Sender.IsValidTarget(eSpell.Range))
                {
                    return;
                }

                if (MyMenu.RootMenu.Item("gapcloser.e").IsActive())
                {
                    if (eSpell.SpellSlot.IsReady())
                    {
                        eSpell.SpellObject.Cast(gapcloser.Sender);
                    }
                }
            }
            catch (Exception e)
            {
                Logging.AddEntry(LoggingEntryTrype.Error, "@SpellManager.cs: AntiGapcloser - {0}", e);
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (args.DangerLevel != Interrupter2.DangerLevel.High)
            {
                return;
            }
            try
            {
                var eSpell = new SpellE();
                if (MyMenu.RootMenu.Item("interrupt.e").IsActive())
                {
                    if (eSpell.SpellSlot.IsReady() && eSpell.SpellObject.IsInRange(sender))
                    {
                        eSpell.SpellObject.Cast(sender);
                    }
                }
            }
            catch (Exception e)
            {
                Logging.AddEntry(LoggingEntryTrype.Error, "@SpellManager.cs: OnInterruptableTarget - {0}", e);
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
                    && (spellSlotNameLower.Equals("e") || spellSlotNameLower.Equals("w")
                        || spellSlotNameLower.Equals("r"))) || (orbwalkerModeLower.Equals("laneclear") && (spellSlotNameLower.Equals("e"))))
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
            if (ObjectManager.Player.IsDead || MenuGUI.IsChatOpen || MenuGUI.IsShopOpen) return;

            this.spells.Where(spell => IsSpellActive(spell.SpellSlot, Orbwalking.OrbwalkingMode.Combo))
                .ToList()
                .ForEach(spell => spell.OnCombo());

            this.spells.Where(spell => IsSpellActive(spell.SpellSlot, Orbwalking.OrbwalkingMode.LaneClear))
                .ToList()
                .ForEach(spell => spell.OnLaneClear());

            this.spells.Where(spell => IsSpellActive(spell.SpellSlot, Orbwalking.OrbwalkingMode.LaneClear))
               .ToList()
               .ForEach(spell => spell.OnJungleClear());

            this.spells.Where(spell => IsSpellActive(spell.SpellSlot, Orbwalking.OrbwalkingMode.LastHit))
                .ToList()
                .ForEach(spell => spell.OnLastHit());

            this.spells.Where(spell => IsSpellActive(spell.SpellSlot, Orbwalking.OrbwalkingMode.Mixed))
                .ToList()
                .ForEach(spell => spell.OnMixed());

            this.spells.ToList().ForEach(spell => spell.OnUpdate());
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
 