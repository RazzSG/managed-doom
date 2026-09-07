//
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//



using System;
using System.Linq;

using ManagedDoom;

namespace ManagedDoom.DefinitionPatches.DeHackEd
{
    /// <summary>
    /// Captures the pristine Doom definition tables before any DeHackEd patch is
    /// applied and restores them before each new content load.
    ///
    /// ManagedDoom currently keeps DoomInfo as process-global mutable tables.
    /// Until definitions become instance-owned, this baseline is the boundary
    /// that prevents one loaded WAD/DEH set from contaminating the next one.
    /// </summary>
    internal static class DeHackEdBaseline
    {
        private static readonly MobjInfoSnapshot[] mobjInfos;
        private static readonly StateSnapshot[] states;
        private static readonly WeaponInfoSnapshot[] weaponInfos;
        private static readonly int[] ammoMax;
        private static readonly int[] ammoClip;
        private static readonly int[][] doom1Pars;
        private static readonly int[] doom2Pars;
        private static readonly DeHackEdConstSnapshot constants;

        static DeHackEdBaseline()
        {
            // Force creation of the DoomString registry before the first patch.
            DoomInfo.Strings.PRESSKEY.GetHashCode();

            mobjInfos = DoomInfo.MobjInfos.Select(MobjInfoSnapshot.Capture).ToArray();
            states = DoomInfo.States.Select(StateSnapshot.Capture).ToArray();
            weaponInfos = DoomInfo.WeaponInfos.Select(WeaponInfoSnapshot.Capture).ToArray();
            ammoMax = (int[])DoomInfo.AmmoInfos.Max.Clone();
            ammoClip = (int[])DoomInfo.AmmoInfos.Clip.Clone();
            doom1Pars = DoomInfo.ParTimes.Doom1.Select(x => x.ToArray()).ToArray();
            doom2Pars = DoomInfo.ParTimes.Doom2.ToArray();
            constants = DeHackEdConstSnapshot.Capture();
        }

        public static void Restore()
        {
            if (DoomInfo.MobjInfos.Length != mobjInfos.Length ||
                DoomInfo.States.Length != states.Length ||
                DoomInfo.WeaponInfos.Length != weaponInfos.Length ||
                DoomInfo.AmmoInfos.Max.Length != ammoMax.Length ||
                DoomInfo.AmmoInfos.Clip.Length != ammoClip.Length)
            {
                throw new InvalidOperationException("DeHackEd baseline no longer matches DoomInfo table sizes.");
            }

            for (var i = 0; i < mobjInfos.Length; i++)
            {
                mobjInfos[i].Restore(DoomInfo.MobjInfos[i]);
            }

            for (var i = 0; i < states.Length; i++)
            {
                states[i].Restore(DoomInfo.States[i]);
            }

            for (var i = 0; i < weaponInfos.Length; i++)
            {
                weaponInfos[i].Restore(DoomInfo.WeaponInfos[i]);
            }

            Array.Copy(ammoMax, DoomInfo.AmmoInfos.Max, ammoMax.Length);
            Array.Copy(ammoClip, DoomInfo.AmmoInfos.Clip, ammoClip.Length);

            for (var episode = 0; episode < doom1Pars.Length; episode++)
            {
                var target = DoomInfo.ParTimes.Doom1[episode];
                if (target.Count != doom1Pars[episode].Length)
                {
                    throw new InvalidOperationException("DeHackEd baseline no longer matches Doom 1 par-time table sizes.");
                }

                for (var map = 0; map < doom1Pars[episode].Length; map++)
                {
                    target[map] = doom1Pars[episode][map];
                }
            }

            if (DoomInfo.ParTimes.Doom2.Count != doom2Pars.Length)
            {
                throw new InvalidOperationException("DeHackEd baseline no longer matches Doom 2 par-time table size.");
            }

            for (var map = 0; map < doom2Pars.Length; map++)
            {
                DoomInfo.ParTimes.Doom2[map] = doom2Pars[map];
            }

            constants.Restore();
            DoomInfo.ResetDeHackEdSoundInfos();
            DoomString.ResetAllReplacements();
        }

        private readonly struct MobjInfoSnapshot
        {
            private readonly int doomEdNum;
            private readonly MobjState spawnState;
            private readonly int spawnHealth;
            private readonly MobjState seeState;
            private readonly Sfx seeSound;
            private readonly int reactionTime;
            private readonly Sfx attackSound;
            private readonly MobjState painState;
            private readonly int painChance;
            private readonly Sfx painSound;
            private readonly MobjState meleeState;
            private readonly MobjState missileState;
            private readonly MobjState deathState;
            private readonly MobjState xdeathState;
            private readonly Sfx deathSound;
            private readonly int speed;
            private readonly Fixed radius;
            private readonly Fixed height;
            private readonly int mass;
            private readonly int damage;
            private readonly Sfx activeSound;
            private readonly MobjFlags flags;
            private readonly MobjState raiseState;

            private MobjInfoSnapshot(MobjInfo info)
            {
                doomEdNum = info.DoomEdNum;
                spawnState = info.SpawnState;
                spawnHealth = info.SpawnHealth;
                seeState = info.SeeState;
                seeSound = info.SeeSound;
                reactionTime = info.ReactionTime;
                attackSound = info.AttackSound;
                painState = info.PainState;
                painChance = info.PainChance;
                painSound = info.PainSound;
                meleeState = info.MeleeState;
                missileState = info.MissileState;
                deathState = info.DeathState;
                xdeathState = info.XdeathState;
                deathSound = info.DeathSound;
                speed = info.Speed;
                radius = info.Radius;
                height = info.Height;
                mass = info.Mass;
                damage = info.Damage;
                activeSound = info.ActiveSound;
                flags = info.Flags;
                raiseState = info.Raisestate;
            }

            public static MobjInfoSnapshot Capture(MobjInfo info)
            {
                return new MobjInfoSnapshot(info);
            }

            public void Restore(MobjInfo info)
            {
                info.DoomEdNum = doomEdNum;
                info.SpawnState = spawnState;
                info.SpawnHealth = spawnHealth;
                info.SeeState = seeState;
                info.SeeSound = seeSound;
                info.ReactionTime = reactionTime;
                info.AttackSound = attackSound;
                info.PainState = painState;
                info.PainChance = painChance;
                info.PainSound = painSound;
                info.MeleeState = meleeState;
                info.MissileState = missileState;
                info.DeathState = deathState;
                info.XdeathState = xdeathState;
                info.DeathSound = deathSound;
                info.Speed = speed;
                info.Radius = radius;
                info.Height = height;
                info.Mass = mass;
                info.Damage = damage;
                info.ActiveSound = activeSound;
                info.Flags = flags;
                info.Raisestate = raiseState;
            }
        }

        private readonly struct StateSnapshot
        {
            private readonly int number;
            private readonly Sprite sprite;
            private readonly int frame;
            private readonly int tics;
            private readonly Action<World, Player, PlayerSpriteDef> playerAction;
            private readonly Action<World, Mobj> mobjAction;
            private readonly MobjState next;
            private readonly int misc1;
            private readonly int misc2;

            private StateSnapshot(MobjStateDef state)
            {
                number = state.Number;
                sprite = state.Sprite;
                frame = state.Frame;
                tics = state.Tics;
                playerAction = state.PlayerAction;
                mobjAction = state.MobjAction;
                next = state.Next;
                misc1 = state.Misc1;
                misc2 = state.Misc2;
            }

            public static StateSnapshot Capture(MobjStateDef state)
            {
                return new StateSnapshot(state);
            }

            public void Restore(MobjStateDef state)
            {
                state.Number = number;
                state.Sprite = sprite;
                state.Frame = frame;
                state.Tics = tics;
                state.PlayerAction = playerAction;
                state.MobjAction = mobjAction;
                state.Next = next;
                state.Misc1 = misc1;
                state.Misc2 = misc2;
            }
        }

        private readonly struct WeaponInfoSnapshot
        {
            private readonly AmmoType ammo;
            private readonly MobjState upState;
            private readonly MobjState downState;
            private readonly MobjState readyState;
            private readonly MobjState attackState;
            private readonly MobjState flashState;

            private WeaponInfoSnapshot(WeaponInfo info)
            {
                ammo = info.Ammo;
                upState = info.UpState;
                downState = info.DownState;
                readyState = info.ReadyState;
                attackState = info.AttackState;
                flashState = info.FlashState;
            }

            public static WeaponInfoSnapshot Capture(WeaponInfo info)
            {
                return new WeaponInfoSnapshot(info);
            }

            public void Restore(WeaponInfo info)
            {
                info.Ammo = ammo;
                info.UpState = upState;
                info.DownState = downState;
                info.ReadyState = readyState;
                info.AttackState = attackState;
                info.FlashState = flashState;
            }
        }

        private readonly struct DeHackEdConstSnapshot
        {
            private readonly int initialHealth;
            private readonly int initialBullets;
            private readonly int maxHealth;
            private readonly int maxArmor;
            private readonly int greenArmorClass;
            private readonly int blueArmorClass;
            private readonly int maxSoulsphere;
            private readonly int soulsphereHealth;
            private readonly int megasphereHealth;
            private readonly int godModeHealth;
            private readonly int idfaArmor;
            private readonly int idfaArmorClass;
            private readonly int idkfaArmor;
            private readonly int idkfaArmorClass;
            private readonly int bfgCellsPerShot;
            private readonly bool monstersInfight;

            private DeHackEdConstSnapshot(bool capture)
            {
                initialHealth = DoomInfo.DeHackEdConst.InitialHealth;
                initialBullets = DoomInfo.DeHackEdConst.InitialBullets;
                maxHealth = DoomInfo.DeHackEdConst.MaxHealth;
                maxArmor = DoomInfo.DeHackEdConst.MaxArmor;
                greenArmorClass = DoomInfo.DeHackEdConst.GreenArmorClass;
                blueArmorClass = DoomInfo.DeHackEdConst.BlueArmorClass;
                maxSoulsphere = DoomInfo.DeHackEdConst.MaxSoulsphere;
                soulsphereHealth = DoomInfo.DeHackEdConst.SoulsphereHealth;
                megasphereHealth = DoomInfo.DeHackEdConst.MegasphereHealth;
                godModeHealth = DoomInfo.DeHackEdConst.GodModeHealth;
                idfaArmor = DoomInfo.DeHackEdConst.IdfaArmor;
                idfaArmorClass = DoomInfo.DeHackEdConst.IdfaArmorClass;
                idkfaArmor = DoomInfo.DeHackEdConst.IdkfaArmor;
                idkfaArmorClass = DoomInfo.DeHackEdConst.IdkfaArmorClass;
                bfgCellsPerShot = DoomInfo.DeHackEdConst.BfgCellsPerShot;
                monstersInfight = DoomInfo.DeHackEdConst.MonstersInfight;
            }

            public static DeHackEdConstSnapshot Capture()
            {
                return new DeHackEdConstSnapshot(true);
            }

            public void Restore()
            {
                DoomInfo.DeHackEdConst.InitialHealth = initialHealth;
                DoomInfo.DeHackEdConst.InitialBullets = initialBullets;
                DoomInfo.DeHackEdConst.MaxHealth = maxHealth;
                DoomInfo.DeHackEdConst.MaxArmor = maxArmor;
                DoomInfo.DeHackEdConst.GreenArmorClass = greenArmorClass;
                DoomInfo.DeHackEdConst.BlueArmorClass = blueArmorClass;
                DoomInfo.DeHackEdConst.MaxSoulsphere = maxSoulsphere;
                DoomInfo.DeHackEdConst.SoulsphereHealth = soulsphereHealth;
                DoomInfo.DeHackEdConst.MegasphereHealth = megasphereHealth;
                DoomInfo.DeHackEdConst.GodModeHealth = godModeHealth;
                DoomInfo.DeHackEdConst.IdfaArmor = idfaArmor;
                DoomInfo.DeHackEdConst.IdfaArmorClass = idfaArmorClass;
                DoomInfo.DeHackEdConst.IdkfaArmor = idkfaArmor;
                DoomInfo.DeHackEdConst.IdkfaArmorClass = idkfaArmorClass;
                DoomInfo.DeHackEdConst.BfgCellsPerShot = bfgCellsPerShot;
                DoomInfo.DeHackEdConst.MonstersInfight = monstersInfight;
            }
        }
    }
}
