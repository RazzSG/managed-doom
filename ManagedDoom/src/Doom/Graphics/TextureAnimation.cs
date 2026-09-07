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
using System.Runtime.ExceptionServices;

namespace ManagedDoom
{
    public sealed class TextureAnimation
    {
        private TextureAnimationInfo[] animations;

        public TextureAnimation(ITextureLookup textures, IFlatLookup flats)
            : this(textures, flats, DoomInfo.TextureAnimation)
        {
        }

        public TextureAnimation(ITextureLookup textures, IFlatLookup flats, IEnumerable<AnimationDef> definitions)
        {
            try
            {
                Console.Write("Load texture animation info: ");

                var list = new List<TextureAnimationInfo>();

                foreach (var animDef in definitions)
                {
                    var startName = DoomString.Resolve(animDef.StartName);
                    var endName = DoomString.Resolve(animDef.EndName);

                    int picNum;
                    int basePic;
                    if (animDef.IsTexture)
                    {
                        if (textures.GetNumber(startName) == -1)
                        {
                            continue;
                        }

                        picNum = textures.GetNumber(endName);
                        basePic = textures.GetNumber(startName);
                    }
                    else
                    {
                        if (flats.GetNumber(startName) == -1)
                        {
                            continue;
                        }

                        picNum = flats.GetNumber(endName);
                        basePic = flats.GetNumber(startName);
                    }

                    var anim = new TextureAnimationInfo(
                        animDef.IsTexture,
                        picNum,
                        basePic,
                        picNum - basePic + 1,
                        animDef.Speed);

                    if (anim.NumPics < 2)
                    {
                        throw new Exception("Bad animation cycle from " + startName + " to " + endName + "!");
                    }

                    list.Add(anim);
                }

                animations = list.ToArray();

                Console.WriteLine("OK");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed");
                ExceptionDispatchInfo.Throw(e);
            }
        }

        public TextureAnimationInfo[] Animations => animations;
    }
}
