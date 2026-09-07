//
// Copyright (C) 1993-1996 Id Software, Inc.
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
using System.Collections.Generic;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom;
using ManagedDoom.Compatibility.Boom.Friction;
using ManagedDoom.Compatibility.Boom.Pushers;
using ManagedDoom.Compatibility.Boom.Rendering;
using ManagedDoom.Compatibility.Boom.Scrolling;
using ManagedDoom.Compatibility.Boom.Sectors;

namespace ManagedDoom
{
    public class Specials
    {
        private static readonly int maxButtonCount = 32;
        private static readonly int buttonTime = 35;

        private World world;
        private BoomTranslucencyMapLookup boomTranslucencyMaps;
        private BoomSwitches boomSwitches;

        private bool levelTimer;
        private int levelTimeCount;

        private Button[] buttonList;

        private int[] textureTranslation;
        private int[] flatTranslation;

        private LineDef[] scrollLines;
        private BoomPointPusher[] boomPointPushers = Array.Empty<BoomPointPusher>();

        public Specials(World world, BoomTranslucencyMapLookup boomTranslucencyMaps, BoomSwitches boomSwitches)
        {
            this.world = world;
            this.boomTranslucencyMaps = boomTranslucencyMaps;
            this.boomSwitches = boomSwitches;

            levelTimer = false;

            buttonList = new Button[maxButtonCount];
            for (var i = 0; i < buttonList.Length; i++)
            {
                buttonList[i] = new Button();
            }

            textureTranslation = new int[world.Map.Textures.Count];
            for (var i = 0; i < textureTranslation.Length; i++)
            {
                textureTranslation[i] = i;
            }

            flatTranslation = new int[world.Map.Flats.Count];
            for (var i = 0; i < flatTranslation.Length; i++)
            {
                flatTranslation[i] = i;
            }
        }

        /// <summary>
        /// After the map has been loaded, scan for specials that spawn thinkers.
        /// </summary>
        public void SpawnSpecials(int levelTimeCount)
        {
            levelTimer = true;
            this.levelTimeCount = levelTimeCount;
            SpawnSpecials();
        }

        /// <summary>
        /// After the map has been loaded, scan for specials that spawn thinkers.
        /// </summary>
        public void SpawnSpecials()
        {
            // Init special sectors.
            var lc = world.LightingChange;
            var sa = world.SectorAction;
            var supportsBoom = GameCompatibilityFeatures.SupportsBoom(world.Options.Compatibility);
            var supportsGeneralizedSectors =
                GameCompatibilityFeatures.SupportsGeneralizedSectorSpecials(world.Options.Compatibility);

            foreach (var sector in world.Map.Sectors)
            {
                var rawSpecial = (int)sector.Special;
                if (rawSpecial == 0)
                {
                    continue;
                }

                // Boom keeps the original sector action in the low five bits and
                // stores generalized effects above them. Decode the combined sector
                // special once so startup lighting and the secret count see the same
                // flags. Runtime damage/secret handling stays in PlayerBehavior.
                var boomSpecial = supportsGeneralizedSectors
                    ? BoomSectorSpecialDecoder.Decode(rawSpecial)
                    : default;

                if (supportsGeneralizedSectors)
                {
                    BoomSectorSecretHandler.Count(world, boomSpecial);
                }

                var startupSpecial = supportsGeneralizedSectors
                    ? boomSpecial.LightingSpecial
                    : rawSpecial;

                switch (startupSpecial)
                {
                    case 1:
                        // Flickering lights.
                        lc.SpawnLightFlash(sector);
                        break;

                    case 2:
                        // Strobe fast.
                        lc.SpawnStrobeFlash(sector, StrobeFlash.FastDark, false);
                        break;

                    case 3:
                        // Strobe slow.
                        lc.SpawnStrobeFlash(sector, StrobeFlash.SlowDark, false);
                        break;

                    case 4:
                        // Strobe fast / death slime. Exact legacy special 4 keeps
                        // its vanilla damage behavior; generalized damage comes only
                        // from the dedicated damage bitfield.
                        lc.SpawnStrobeFlash(sector, StrobeFlash.FastDark, false);
                        sector.Special = (SectorSpecial)((int)sector.Special | 4);
                        break;

                    case 8:
                        // Glowing light.
                        lc.SpawnGlowingLight(sector);
                        break;

                    case 9:
                        // Legacy secret sector. Generalized sectors use bit 7 and
                        // were counted above without treating lighting value 9 as
                        // a second secret.
                        if (!supportsGeneralizedSectors || rawSpecial < 32)
                        {
                            world.TotalSecrets++;
                        }
                        break;

                    case 10:
                        // Keep combined timed-sector actions out of this lighting
                        // phase; exact legacy specials retain their old behavior.
                        if (!supportsGeneralizedSectors || rawSpecial < 32)
                        {
                            sa.SpawnDoorCloseIn30(sector);
                        }
                        break;

                    case 12:
                        // Sync strobe slow.
                        lc.SpawnStrobeFlash(sector, StrobeFlash.SlowDark, true);
                        break;

                    case 13:
                        // Sync strobe fast.
                        lc.SpawnStrobeFlash(sector, StrobeFlash.FastDark, true);
                        break;

                    case 14:
                        // Keep combined timed-sector actions out of this lighting
                        // phase; exact legacy specials retain their old behavior.
                        if (!supportsGeneralizedSectors || rawSpecial < 32)
                        {
                            sa.SpawnDoorRaiseIn5Mins(sector);
                        }
                        break;

                    case 17:
                        lc.SpawnFireFlicker(sector);
                        break;
                }
            }

            // Transfer heights is static map setup. Apply it for every compatibility
            // mode so a repeated SpawnSpecials() also clears stale Boom references
            // when compatibility is switched back to Vanilla.
            BoomTransferHeightResolver.Apply(world);
            BoomTranslucentLineResolver.Apply(world, boomTranslucencyMaps);

            if (supportsBoom)
            {
                BoomLightTransferResolver.Apply(world);
                BoomScrollerSpawner.SpawnScrollers(world);
                BoomSectorFriction.Initialize(world);
                BoomSectorWind.Initialize(world);
                BoomSectorCurrent.Initialize(world);
                boomPointPushers = BoomPointPusher.Resolve(world);
            }
            else
            {
                boomPointPushers = Array.Empty<BoomPointPusher>();
            }

            var scrollList = new List<LineDef>();
            if (!supportsBoom)
            {
                foreach (var line in world.Map.Lines)
                {
                    switch ((int)line.Special)
                    {
                        case 48:
                            // Vanilla texture scroll. Boom uses BoomScroller thinkers instead.
                            scrollList.Add(line);
                            break;
                    }
                }
            }
            scrollLines = scrollList.ToArray();
        }

        public void ApplyBoomPointPushers(Mobj thing)
        {
            for (var i = 0; i < boomPointPushers.Length; i++)
                boomPointPushers[i].Apply(thing);
        }

        public void ChangeSwitchTexture(LineDef line, bool useAgain)
        {
            if (!useAgain)
            {
                line.Special = 0;
            }

            var frontSide = line.FrontSide;
            var topTexture = frontSide.TopTexture;
            var middleTexture = frontSide.MiddleTexture;
            var bottomTexture = frontSide.BottomTexture;

            var sound = Sfx.SWTCHN;

            // Exit switch?
            if ((int)line.Special == 11)
            {
                sound = Sfx.SWTCHX;
            }

            var switchList = GameCompatibilityFeatures.SupportsBoom(world.Options.Compatibility)
                ? boomSwitches.SwitchList
                : world.Map.Textures.SwitchList;

            for (var i = 0; i < switchList.Length; i++)
            {
                if (switchList[i] == topTexture)
                {
                    world.StartSound(line.SoundOrigin, sound, SfxType.Misc);
                    frontSide.TopTexture = switchList[i ^ 1];

                    if (useAgain)
                    {
                        StartButton(line, ButtonPosition.Top, switchList[i], buttonTime);
                    }

                    return;
                }
                else
                {
                    if (switchList[i] == middleTexture)
                    {
                        world.StartSound(line.SoundOrigin, sound, SfxType.Misc);
                        frontSide.MiddleTexture = switchList[i ^ 1];

                        if (useAgain)
                        {
                            StartButton(line, ButtonPosition.Middle, switchList[i], buttonTime);
                        }

                        return;
                    }
                    else
                    {
                        if (switchList[i] == bottomTexture)
                        {
                            world.StartSound(line.SoundOrigin, sound, SfxType.Misc);
                            frontSide.BottomTexture = switchList[i ^ 1];

                            if (useAgain)
                            {
                                StartButton(line, ButtonPosition.Bottom, switchList[i], buttonTime);
                            }

                            return;
                        }
                    }
                }
            }
        }

        private void StartButton(LineDef line, ButtonPosition w, int texture, int time)
        {
            // See if button is already pressed.
            for (var i = 0; i < maxButtonCount; i++)
            {
                if (buttonList[i].Timer != 0 && buttonList[i].Line == line)
                {
                    return;
                }
            }

            for (var i = 0; i < maxButtonCount; i++)
            {
                if (buttonList[i].Timer == 0)
                {
                    buttonList[i].Line = line;
                    buttonList[i].Position = w;
                    buttonList[i].Texture = texture;
                    buttonList[i].Timer = time;
                    buttonList[i].SoundOrigin = line.SoundOrigin;
                    return;
                }
            }

            throw new Exception("No button slots left!");
        }

        /// <summary>
        /// Animate planes, scroll walls, etc.
        /// </summary>
        public void Update()
        {
            // Level timer.
            if (levelTimer)
            {
                levelTimeCount--;
                if (levelTimeCount == 0)
                {
                    world.ExitLevel();
                }
            }

            // Animate flats and textures globally.
            var animations = world.Map.Animation.Animations;
            for (var k = 0; k < animations.Length; k++)
            {
                var anim = animations[k];
                for (var i = anim.BasePic; i < anim.BasePic + anim.NumPics; i++)
                {
                    var pic = anim.BasePic + ((world.LevelTime / anim.Speed + i) % anim.NumPics);
                    if (anim.IsTexture)
                    {
                        textureTranslation[i] = pic;
                    }
                    else
                    {
                        flatTranslation[i] = pic;
                    }
                }
            }

            // Animate line specials.
            foreach (var line in scrollLines)
            {
                line.FrontSide.TextureOffset += Fixed.One;
            }

            // Do buttons.
            for (var i = 0; i < maxButtonCount; i++)
            {
                if (buttonList[i].Timer > 0)
                {
                    buttonList[i].Timer--;

                    if (buttonList[i].Timer == 0)
                    {
                        switch (buttonList[i].Position)
                        {
                            case ButtonPosition.Top:
                                buttonList[i].Line.FrontSide.TopTexture = buttonList[i].Texture;
                                break;

                            case ButtonPosition.Middle:
                                buttonList[i].Line.FrontSide.MiddleTexture = buttonList[i].Texture;
                                break;

                            case ButtonPosition.Bottom:
                                buttonList[i].Line.FrontSide.BottomTexture = buttonList[i].Texture;
                                break;
                        }

                        world.StartSound(buttonList[i].SoundOrigin, Sfx.SWTCHN, SfxType.Misc, 50);
                        buttonList[i].Clear();
                    }
                }
            }
        }

        public int[] TextureTranslation => textureTranslation;
        public int[] FlatTranslation => flatTranslation;
    }
}
