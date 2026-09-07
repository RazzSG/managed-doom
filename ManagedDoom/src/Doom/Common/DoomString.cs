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

namespace ManagedDoom
{
    public sealed class DoomString
    {
        private static Dictionary<string, List<DoomString>> valueTable = new Dictionary<string, List<DoomString>>();
        private static Dictionary<string, DoomString> nameTable = new Dictionary<string, DoomString>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> replacementTable = new Dictionary<string, string>(StringComparer.Ordinal);

        private string original;
        private string replaced;

        public DoomString(string original)
        {
            this.original = original;
            replaced = Resolve(original);

            if (!valueTable.TryGetValue(original, out var matches))
            {
                matches = new List<DoomString>();
                valueTable.Add(original, matches);
            }

            matches.Add(this);
        }

        public DoomString(string name, string original) : this(original)
        {
            nameTable.Add(name, this);
        }

        public override string ToString()
        {
            return replaced;
        }

        // Classic DeHackEd Sprite blocks address the original executable
        // sprite-name table. Keep the pristine value available internally so
        // an earlier Text/Sprite replacement cannot accidentally change the
        // source name selected by a later Sprite Offset.
        internal string OriginalValue => original;

        internal void SetDirectReplacement(string value)
        {
            replaced = value;
        }

        public char this[int index]
        {
            get
            {
                return replaced[index];
            }
        }

        public static implicit operator string(DoomString ds)
        {
            return ds.replaced;
        }

        public static void ReplaceByValue(string original, string replaced)
        {
            TryReplaceByValue(original, replaced);
        }

        public static bool TryReplaceByValue(string original, string replaced)
        {
            // Classic DeHackEd Text can target hardcoded strings which do not
            // have a dedicated DoomString instance. Keep a global replacement
            // table so those strings can be resolved at their resource lookup
            // sites, while still updating every registered DoomString sharing
            // the same original value.
            replacementTable[original] = replaced;

            if (valueTable.TryGetValue(original, out var matches))
            {
                foreach (var ds in matches)
                {
                    ds.replaced = replaced;
                }
            }

            return true;
        }

        public static string Resolve(string original)
        {
            if (original == null)
            {
                return null;
            }

            return replacementTable.TryGetValue(original, out var replaced) ?
                replaced :
                original;
        }

        public static void ReplaceByName(string name, string value)
        {
            TryReplaceByName(name, value);
        }

        public static bool TryReplaceByName(string name, string value)
        {
            DoomString ds;
            if (nameTable.TryGetValue(name, out ds))
            {
                // Boom BEX resolves the mnemonic to the original executable
                // string and then installs a normal string substitution. This
                // intentionally shares classic by-value replacement semantics
                // when two registered strings have the same original text.
                return TryReplaceByValue(ds.original, value);
            }

            return false;
        }

        public static void ResetAllReplacements()
        {
            replacementTable.Clear();

            // Classic Text replacement may target several registered strings
            // with the same original value. Reset every unique instance from
            // both the value and name registries.
            var restored = new HashSet<DoomString>();

            foreach (var matches in valueTable.Values)
            {
                foreach (var entry in matches)
                {
                    if (restored.Add(entry))
                    {
                        entry.replaced = entry.original;
                    }
                }
            }

            foreach (var entry in nameTable.Values)
            {
                if (restored.Add(entry))
                {
                    entry.replaced = entry.original;
                }
            }
        }
    }
}
