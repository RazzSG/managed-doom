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
using ManagedDoom.Compatibility.Boom;
using ManagedDoom.Compatibility.Boom.Rendering;

namespace ManagedDoom
{
    public sealed class GameContent : IDisposable
    {
        private Wad wad;
        private Palette palette;
        private ColorMap colorMap;
        private TrueColorMap trueColorMap;
        private BoomTranslucencyMapLookup boomTranslucencyMaps;
        private ITextureLookup textures;
        private BoomSwitches boomSwitches;
        private IFlatLookup flats;
        private ISpriteLookup sprites;
        private TextureAnimation animation;
        private BoomAnimated boomAnimated;

        private GameContent()
        {
        }

        public GameContent(CommandLineArgs args)
        {
            try
            {
                wad = new Wad(ConfigUtilities.GetWadPaths(args));

                DeHackEd.Initialize(args, wad);

                palette = new Palette(wad);
                colorMap = new ColorMap(wad);
                trueColorMap = new TrueColorMap(palette, colorMap);
                boomTranslucencyMaps = new BoomTranslucencyMapLookup(wad);
                textures = new TextureLookup(wad);
                boomSwitches = new BoomSwitches(wad, textures);
                flats = new FlatLookup(wad);
                sprites = new SpriteLookup(wad);
                animation = new TextureAnimation(textures, flats);
                boomAnimated = new BoomAnimated(wad, textures, flats, animation);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public static GameContent CreateDummy(params string[] wadPaths)
        {
            var gc = new GameContent();

            try
            {
                gc.wad = new Wad(wadPaths);
                gc.palette = new Palette(gc.wad);
                gc.colorMap = new ColorMap(gc.wad);
                gc.trueColorMap = new TrueColorMap(gc.palette, gc.colorMap);
                gc.boomTranslucencyMaps = new BoomTranslucencyMapLookup(gc.wad);
                gc.textures = new DummyTextureLookup(gc.wad);
                gc.boomSwitches = new BoomSwitches(gc.wad, gc.textures);
                gc.flats = new DummyFlatLookup(gc.wad);
                gc.sprites = new DummySpriteLookup(gc.wad);
                gc.animation = new TextureAnimation(gc.textures, gc.flats);
                gc.boomAnimated = new BoomAnimated(gc.wad, gc.textures, gc.flats, gc.animation);

                return gc;
            }
            catch
            {
                gc.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (wad != null)
            {
                wad.Dispose();
                wad = null;
            }
        }

        public Wad Wad => wad;
        public Palette Palette => palette;
        public ColorMap ColorMap => colorMap;
        public TrueColorMap TrueColorMap => trueColorMap;
        public BoomTranslucencyMapLookup BoomTranslucencyMaps => boomTranslucencyMaps;
        public ITextureLookup Textures => textures;
        public BoomSwitches BoomSwitches => boomSwitches;
        public IFlatLookup Flats => flats;
        public ISpriteLookup Sprites => sprites;
        public TextureAnimation Animation => animation;
        public BoomAnimated BoomAnimated => boomAnimated;
    }
}
