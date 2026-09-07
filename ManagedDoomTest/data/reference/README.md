# External Boom reference WADs

Phase 4.69 verifies the port against `BOOMEDIT.WAD`, Jim Flynn's historical
Boom feature demonstration map.

`BOOMEDIT.WAD` is intentionally **not** stored in this repository. Keep a local
copy at:

```text
ManagedDoomTest/data/reference/BOOMEDIT.WAD
```

or set the environment variable:

```text
MANAGEDDOOM_BOOMEDIT_WAD=<full path to BOOMEDIT.WAD>
```

The reference test is tagged `ExternalReference`. If the WAD is not present,
the test reports **Inconclusive/Skipped** rather than failing the normal unit
test suite.

Use the original `BOOMEDIT.WAD` distributed in the TeamTNT Boom archive. The
map should be used together with a legitimate Doom II-compatible IWAD already
available to the test environment.

The test does not copy, modify, or redistribute the reference WAD. It validates
that the real map loads through the normal Boom pipeline and exposes documented
Boom features such as generalized/extended linedefs, PassThru, generalized
sectors, point-source wind/current things, friction, transfer heights, custom
translucency, `SWITCHES`, `ANIMATED`, and custom colormaps.

## Phase 4.70 real Boom WAD corpus

Phase 4.70 runs several independent real Boom PWADs through the same runtime
content and game-loop path used by the port. The external WADs remain local and
are not stored in this repository.

Copy:

```text
ManagedDoomTest/data/reference/boom-corpus.example.json
```

to:

```text
ManagedDoomTest/data/reference/boom-corpus.json
```

Then edit the manifest so each entry points at your local IWAD/PWAD files. Paths
may be absolute, relative to the test working directory, or relative to the
manifest. A convenient local layout is:

```text
ManagedDoomTest/data/reference/
  boom-corpus.json
  corpus/
    resurge.wad
    sodfinal.wad
    aaliens.wad
```

You may instead set:

```text
MANAGEDDOOM_BOOM_CORPUS_MANIFEST=<full path to boom-corpus.json>
```

The committed example uses three well-known Doom II Boom-targeting megawads as
representative suggestions only. You can replace them with other real Boom
PWADs. Phase 4.70 requires at least three independent entries and at least two
representative maps per entry.

Each configured entry is loaded with the normal `GameContent` pipeline. The
test leaves compatibility on `Auto` and requires the loaded WAD set to resolve to
the Boom compatibility family (`Boom`, `Mbf`, or `Mbf21`) through `COMPLVL` or
feature scanning. This is intentional: real-world WADs commonly described as
"Boom-compatible" may contain later MBF-family markers while still exercising
the Boom runtime feature set. `Vanilla` is still rejected. Every configured map
is started with `NoMonsters`, and the real `DoomGame` loop advances for the
configured number of ticks.

The harness always attempts every configured corpus entry. A failure in one WAD
does not stop the remaining entries from running; all failures are reported
together at the end, and the detected compatibility/source for each entry is
written to the test output.

Embedded/external DeHackEd processing is deliberately disabled for this corpus
test (`-nodeh`). DeHackEd mutates shared static `DoomInfo` state, so applying
unrelated external patches sequentially inside one MSTest process would make the
corpus order-dependent. This phase verifies Boom WAD/map/runtime compatibility;
DeHackEd compatibility should be tested separately.

If `boom-corpus.json` is absent, the corpus test reports Inconclusive/Skipped.
A real Phase 4.70 verification run should show the corpus test as RUN/PASS, not
Skipped.

## Phase 17.6 real DeHackEd/BEX corpus

Phase 17.6 verifies the completed classic DeHackEd + Boom BEX pipeline against
multiple real projects without redistributing third-party WAD or patch files.
The corpus assets remain local.

Copy:

```text
ManagedDoomTest/data/reference/dehacked-corpus.example.json
```

to:

```text
ManagedDoomTest/data/reference/dehacked-corpus.json
```

Then replace the example paths with at least three independent real corpus
entries. Each entry may use embedded `DEHACKED` lumps, external `.deh`/`.bex`
files, or both. Paths may be absolute, relative to the test working directory,
relative to the test project, relative to `data/reference`, or relative to the
manifest itself.

You may instead set:

```text
MANAGEDDOOM_DEHACKED_CORPUS_MANIFEST=<full path to dehacked-corpus.json>
```

For every configured entry the test:

1. restores the pristine ManagedDoom definition baseline;
2. captures a deterministic definition fingerprint;
3. loads the real IWAD/PWAD/DEH/BEX set through normal `GameContent` processing;
4. requires at least one embedded `DEHACKED` lump or external patch source;
5. requires the loaded patch set to change the definition fingerprint by
   default;
6. starts every configured representative map through the real `DoomGame`
   pipeline and advances the configured number of game ticks.

The fingerprint covers the process-global DeHackEd/BEX definition state,
including actors/states, patchable constants, par times, sound metadata,
registered Doom strings/resource names, and raw classic-Text replacement
mappings. This prevents the corpus test from passing merely because a WAD loads
while its definition patch was silently ignored.

Set `"requireDefinitionChange": false` only for a deliberate corpus entry whose
patch is known to be semantically empty. The default is `true`.

The harness attempts every manifest entry and reports all failures together. If
`dehacked-corpus.json` is absent, the test is marked Inconclusive/Skipped so the
normal unit suite remains self-contained. A real Phase 17.6 sign-off requires
this external corpus test to be configured and to report PASS, not Skipped.
