# ManagedDoom Compatibility Roadmap

> Internal development note for adding Boom, full DeHackEd/BEX support, MBF, MBF21, an optional OpenGL renderer backend, and DSDHacked support to ManagedDoom.
>
> This plan assumes that compatibility support is being designed from scratch. The main goals are:
>
> 1. Compatibility correctness.
> 2. Performance.
> 3. Clean architecture.
> 4. Minimal changes to vanilla ManagedDoom code.
> 5. Real behavioral tests instead of testing every line of implementation code.
> 6. Deterministic patch-definition loading with no state leakage between WAD sets.

---

## 1. Compatibility hierarchy

Compatibility levels should be built as an inheritance chain:

```text
Vanilla
   ↓
Boom
   ↓
MBF
   ↓
MBF21
```

The vanilla ManagedDoom engine remains the base implementation.

Compatibility code extends the engine instead of replacing it.

```text
Vanilla ManagedDoom
        +
Compatibility/Boom
        +
Compatibility/Mbf
        +
Compatibility/Mbf21
```

Create:

```csharp
public enum GameCompatibility
{
    Vanilla = 0,
    Boom = 1,
    Mbf = 2,
    Mbf21 = 3
}
```

Do not scatter checks like this throughout the engine:

```csharp
if (compatibility == GameCompatibility.Boom)
```

MBF must automatically inherit Boom behavior, and MBF21 must inherit both Boom and MBF.

Create a central feature-gate class:

```csharp
public static class GameCompatibilityFeatures
{
    public static bool SupportsBoom(GameCompatibility compatibility)
        => compatibility >= GameCompatibility.Boom;

    public static bool SupportsMbf(GameCompatibility compatibility)
        => compatibility >= GameCompatibility.Mbf;

    public static bool SupportsMbf21(GameCompatibility compatibility)
        => compatibility >= GameCompatibility.Mbf21;
}
```

As support grows, add more specific feature gates:

```csharp
SupportsBoomLineSpecials(...)
SupportsGeneralizedLineSpecials(...)
SupportsGeneralizedSectorSpecials(...)
SupportsTransferHeights(...)
SupportsSectorFriction(...)
SupportsPushers(...)
SupportsScrollerSpecials(...)
SupportsTranslucentLines(...)
SupportsBoomThingFlags(...)
SupportsMbfFeatures(...)
SupportsMbf21Features(...)
```

`GameCompatibilityFeatures` should be the single place that knows which compatibility level inherits which feature.

### Definition-patch hierarchy is a separate axis

Do **not** add DeHackEd or DSDHacked to `GameCompatibility`.

They are definition-patch formats, not gameplay compatibility levels.

The intended relationship is:

```text
Gameplay compatibility:

Vanilla → Boom → MBF → MBF21

Definition patches:

Classic DeHackEd + Boom BEX
              ↓
        MBF DeHackEd additions
              ↓
       MBF21 DeHackEd additions
              ↓
          DSDHacked
```

A Boom WAD may use no DeHackEd at all, classic DeHackEd, or BEX.

An MBF/MBF21 WAD automatically inherits the older DeHackEd/BEX behavior and may add compatibility-level-specific fields or code pointers.

DSDHacked is implemented only **after MBF21** and extends the same definition-patch pipeline instead of creating a second engine.

---

## 2. Automatic compatibility detection

Create:

```text
Compatibility/
    CompatibilityDetector.cs
    CompatibilityDetectionResult.cs
    CompatibilityFeatureScanner.cs
    ComplvlReader.cs
```

Main API:

```csharp
public static CompatibilityDetectionResult Detect(Wad wad);
```

Detection order:

```text
COMPLVL present?
    │
    ├─ yes → use it
    │
    └─ no
         ↓
MBF21 features?
    │
    ├─ yes → MBF21
    │
    └─ no
         ↓
MBF features?
    │
    ├─ yes → MBF
    │
    └─ no
         ↓
Boom features?
    │
    ├─ yes → Boom
    │
    └─ no → Vanilla / Unknown
```

Do not return only the compatibility enum.

Use a result that also records how the level was selected:

```csharp
public readonly struct CompatibilityDetectionResult
{
    public GameCompatibility Compatibility { get; }
    public CompatibilityDetectionSource Source { get; }
}
```

Example source enum:

```csharp
public enum CompatibilityDetectionSource
{
    UserOverride,
    Complvl,
    FeatureScan,
    Default
}
```

The player must always be able to override automatic detection:

```text
AUTO
VANILLA
BOOM
MBF
MBF21
```

### Important rule

Feature detection determines only the **minimum compatibility level that can be proven from the WAD**.

If no Boom feature is detected, that does **not** prove that the WAD is vanilla.

Therefore:

```text
No extended features detected
→ Vanilla / Unknown
```

not:

```text
Guaranteed Vanilla
```

---

## 3. Suggested project structure

```text
Compatibility/
│
├── GameCompatibility.cs
├── GameCompatibilityFeatures.cs
│
├── Detection/
│   ├── CompatibilityDetector.cs
│   ├── CompatibilityDetectionResult.cs
│   ├── CompatibilityFeatureScanner.cs
│   └── ComplvlReader.cs
│
├── Boom/
│   ├── Lines/
│   ├── Sectors/
│   ├── Movement/
│   ├── Rendering/
│   ├── Resources/
│   └── BoomCompatibility.cs
│
├── Mbf/
│
└── Mbf21/

DefinitionPatches/
│
├── DeHackEd/
│   ├── DeHackEdLoader.cs
│   ├── DeHackEdParser.cs
│   ├── DeHackEdPatch.cs
│   ├── DeHackEdApplier.cs
│   ├── DeHackEdBaseline.cs
│   └── Bex/
│
└── Dsdhacked/
    ├── DsdhackedParser.cs
    └── DynamicDefinitionTables.cs
```

Do not create one giant file such as:

```text
BoomCompatibility.cs
```

containing thousands of lines.

Keep functionality split by responsibility.

---

## 4. Rule for integration with vanilla code

Do not rewrite complete vanilla methods only to add Boom support.

Bad:

```csharp
public void CrossSpecialLine(...)
{
    // hundreds of lines of vanilla logic
    // hundreds of lines of Boom logic
    // later MBF logic
    // later MBF21 logic
}
```

Prefer a small extension point:

```csharp
public void CrossSpecialLine(LineDef line, int side, Mobj thing)
{
    if (GameCompatibilityFeatures.SupportsBoomLineSpecials(world.Options.Compatibility) &&
        BoomLineSpecials.TryCross(world, line, side, thing))
    {
        return;
    }

    CrossVanillaSpecialLine(line, side, thing);
}
```

Use the same principle for:

```text
Use
Cross
Shoot
Push
Movement
Sector effects
Rendering
Thing spawning
```

The target architecture is:

```text
Vanilla method
     ↓
small compatibility hook
     ↓
separate Boom subsystem
```

---

## 5. Boom linedef architecture

Do not implement generalized specials as hundreds or thousands of `case` statements.

Create:

```text
Boom/
    Lines/
        BoomLineSpecials.cs
        BoomTriggerType.cs

        BoomFloorSpecial.cs
        BoomFloorTranslator.cs

        BoomCeilingSpecial.cs
        BoomCeilingTranslator.cs

        BoomDoorSpecial.cs
        BoomDoorTranslator.cs

        BoomLockedDoorSpecial.cs
        BoomLockedDoorTranslator.cs

        BoomLiftSpecial.cs
        BoomLiftTranslator.cs

        BoomStairSpecial.cs
        BoomStairTranslator.cs

        BoomCrusherSpecial.cs
        BoomCrusherTranslator.cs
```

Common entry points:

```csharp
BoomLineSpecials.TryUse(...)
BoomLineSpecials.TryCross(...)
BoomLineSpecials.TryShoot(...)
BoomLineSpecials.TryPush(...)
```

These entry points determine:

```text
special type
activation type
repeatability
monster activation
tag behavior
action
```

---

## 6. Generalized Boom linedefs

Implement all generalized categories:

```text
Generalized Floors
Generalized Ceilings
Generalized Doors
Generalized Locked Doors
Generalized Lifts
Generalized Stairs
Generalized Crushers
```

Each generalized special should follow this flow:

```text
integer special
      ↓
translator / decoder
      ↓
readonly specification
      ↓
SectorAction
```

Example:

```csharp
var spec = BoomFloorTranslator.Translate(line.Special);

world.SectorAction.DoBoomFloor(line, spec);
```

The specification should contain already decoded values such as:

```text
speed
direction
target
change type
crush
model
activation
repeat
monster activation
```

Decode the bitfield once when the action is activated.

Do not spread bit operations throughout the engine.

---

## 7. Extended regular Boom linedefs

Implement all regular Boom specials that are not generalized specials.

Categories:

```text
extended floors
extended ceilings
elevators
lighting
silent teleporters
line-to-line teleporters
scrollers
friction
wind/current
push/pull
property transfers
translucency
exits
other Boom-specific actions
```

Use small focused handlers where appropriate:

```text
BoomTeleportSpecials
BoomScrollerSpecials
BoomTransferSpecials
BoomEnvironmentSpecials
```

---

## 8. Generalized Boom sector specials

Do not turn the vanilla:

```csharp
switch (sector.Special)
```

into a huge compatibility switch.

Create:

```text
BoomSectorSpecial.cs
BoomSectorSpecialDecoder.cs
BoomSectorEffects.cs
```

Decode the Boom sector special into independent properties:

```text
lighting
damage
secret
friction enabled
pusher enabled
```

A Boom sector may contain several effects at the same time.

Example:

```csharp
var spec = BoomSectorSpecialDecoder.Decode(sector.Special);
```

Then:

```csharp
BoomSectorEffects.ApplyPlayerEffects(...);
```

---

## 9. Boom teleporters

Create a separate subsystem:

```text
BoomTeleport/
    BoomTeleport.cs
    BoomSilentTeleport.cs
    BoomLineTeleport.cs
```

Support:

```text
silent thing teleport
silent line-to-line teleport
reversed line teleport
monster-only variants
```

Correctly preserve or transform:

```text
X/Y relative position
Z
angle
momentum
player view
interpolation state
activation semantics
```

Do not copy the entire vanilla `Teleport()` implementation.

Extract only the common pieces that are genuinely shared.

---

## 10. Transfer Heights / fake floors

Treat Boom transfer heights as a dedicated rendering/gameplay feature.

Create:

```text
BoomTransferHeightResolver
BoomTransferHeightRenderState
BoomTransferHeightGeometry
BoomTransferHeightSpriteVisibility
BoomTransferHeightZone
```

The vanilla `Sector` should only need small references such as:

```csharp
HeightSector
HeightSectorLine
```

The renderer should ask a resolver:

```csharp
var state = BoomTransferHeightResolver.Resolve(...);
```

instead of embedding Boom-specific rules in many renderer locations.

Support:

```text
normal zone
below fake floor
above fake ceiling
fake flat
fake light
fake colormap
sky special cases
sprite visibility
wall clipping
```

---

## 11. Floor and ceiling light transfers

Add sector references such as:

```csharp
FloorLightSector
CeilingLightSector
```

Resolve their control sectors once when the map starts.

Do not search for the control sector every frame.

The renderer should read already resolved values:

```csharp
var floorLight = sector.FloorLightSector?.LightLevel ?? sector.LightLevel;
```

and the same for ceilings.

---

## 12. Scrollers

Create:

```text
BoomScroller
BoomScrollerType
BoomScrollerTranslator
```

Support:

```text
wall scrolling
floor scrolling
ceiling scrolling
carry
scroll + carry
displacement scrollers
accelerative scrollers
tagged wall scrollers
sidedef-offset wall scrollers
```

### Performance rule

Scroller targets must be resolved once during map initialization.

Do not do this every tic:

```csharp
foreach (var line in world.Map.Lines)
foreach (var sector in world.Map.Sectors)
```

Instead:

```text
line/tag
   ↓
resolve target at map load
   ↓
BoomScroller(target)
   ↓
Thinker list
```

`Tick()` should only update offset or momentum.

---

## 13. Friction

Create:

```text
BoomFrictionTranslator
BoomSectorFriction
```

Store resolved values in `Sector`:

```csharp
Fixed Friction
Fixed MoveFactor
```

Calculate them during map initialization.

The movement hot path should only read:

```csharp
thing.Subsector.Sector.Friction
```

Do not recalculate friction from linedef geometry every tic.

---

## 14. Wind, current and pushers

Create:

```text
BoomPusher
BoomPusherType
BoomPusherSpawner
```

Support:

```text
wind
current
point push
point pull
```

Controller things:

```text
5001
5002
```

must be resolved at map load.

For point pushers, do not scan every thing in the map every tic.

Use a spatial lookup such as the existing BLOCKMAP / thing-block traversal.

This is especially important on large maps with thousands of actors.

---

## 15. Boom PassThru

Add the Boom linedef flag:

```text
PassThru
```

Do not replace vanilla use logic globally.

Use a separate Boom traversal path when required:

```csharp
if (GameCompatibilityFeatures.SupportsBoom(world.Options.Compatibility))
    BoomUseTraversal.TryUse(...);
else
    VanillaUse(...);
```

Boom traversal must be able to continue after activating a valid PassThru line.

---

## 16. Boom thing flags

Add support for:

```text
Not in Deathmatch
Not in Coop
```

Thing spawning must honor these flags only at the appropriate compatibility level.

Vanilla behavior must remain unchanged in `Vanilla` compatibility.

---

## 17. SWITCHES lump

Create:

```text
BoomSwitches.cs
BoomSwitchDefinition.cs
```

During content loading:

```text
SWITCHES exists
    ↓
parse once
    ↓
build switch lookup
```

If no `SWITCHES` lump exists:

```text
use vanilla switch table
```

Do not perform WAD string/lump searches every time a switch is activated.

---

## 18. ANIMATED lump

Create:

```text
BoomAnimated.cs
BoomAnimationDefinition.cs
```

Parse `ANIMATED` once during content loading.

Convert definitions into the existing:

```text
TextureAnimation
```

system where possible.

Do not build a second independent animation scheduler unless necessary.

---

## 19. Boom COLORMAP resources

Support:

```text
C_START
C_END
custom COLORMAP lumps
WATERMAP
242 colormap names
PWAD override order
```

Create a lookup layer such as:

```csharp
BoomColorMapLookup
```

Build the lookup once when loading resources.

The renderer should receive ready-to-use map references or indices.

Avoid string lookup in render hot paths.

---

## 20. TRANMAP and translucency

For the Classic renderer, support the Boom-style:

```text
256 × 256 TRANMAP
```

Load it once.

The pixel operation should remain cheap:

```csharp
map[(background << 8) | foreground]
```

Do not perform RGB alpha blending per Classic-mode pixel.

True Color may later have an optional enhanced alpha-blending path, but the Boom compatibility path must remain accurate and separate.

---

## 21. Renderer architecture

ManagedDoom uses a **single-threaded software renderer**, so compatibility checks inside pixel loops must be avoided.

Do not repeatedly do this inside raster loops:

```csharp
if (SupportsBoom(...))
if (transferHeight...)
if (translucency...)
if (...)
```

Move branching upward:

```csharp
if (normalWall)
    DrawNormalWall(...);
else
    DrawBoomSpecialWall(...);
```

or resolve the complete render state once:

```csharp
var renderState = GetRenderState(sector);
```

The rasterizer should receive simple ready-to-use values:

```text
height
texture
light
offset
colormap
```

Core principle:

```text
complex compatibility logic
        ↓ once
ready render state
        ↓
fast pixel loop
```

---

## 22. Performance rules

The target environment is:

```text
software renderer
one CPU thread
large WADs
large maps
thousands of monsters
thousands of visible sprites
```

A compatibility feature is not considered correctly designed if it causes a large FPS regression.

Avoid in hot paths:

```text
LINQ
per-tic allocations
per-frame allocations
closures/delegates
Dictionary lookups per pixel
global sector scans every tic
global linedef scans every tic
repeated tag scans
WAD lookups during gameplay
string comparisons in renderer/ticker
unnecessary virtual/interface dispatch in inner loops
```

Prefer preprocessing during:

```text
WAD load
map load
linedef activation
```

instead of:

```text
every tic
every frame
every pixel
```

### Tags

Do not repeatedly do:

```csharp
foreach (var sector in map.Sectors)
{
    if (sector.Tag == tag)
    {
        ...
    }
}
```

Build a map-local index:

```text
BoomTagIndex
```

Example structure:

```text
tag → sectors[]
tag → lines[]
```

Create it once when loading the map.

This becomes increasingly important because Boom uses tags much more heavily than vanilla Doom.

---

## 23. No global "Boom update"

Do not implement:

```csharp
UpdateBoom()
{
    ScanAllLines();
    ScanAllSectors();
    ScanAllThings();
}
```

every tic.

That type of architecture can easily turn:

```text
120 FPS
↓
30 FPS
```

Boom systems should be event-driven:

```text
level start
→ create required thinkers

line activated
→ create mover/action

tic
→ update only active thinkers
```

Use Doom's existing architecture instead of creating a parallel compatibility scheduler.

---

## 24. Thinkers

Dynamic Boom functionality should reuse the existing Thinker model whenever possible:

```text
Door
FloorMover
CeilingMover
Platform
Scroller
Pusher
Button
Lighting
```

A thinker exists only while it is needed.

Do not create a separate global Boom scheduler.

---

## 25. Reuse vanilla movers where possible

If a Boom generalized floor differs from a vanilla floor only in:

```text
target
speed
direction
texture/type transfer
```

then prefer:

```text
Boom decoder
    ↓
resolved parameters
    ↓
shared FloorMover
```

Do not automatically create:

```text
VanillaFloorMover
BoomFloorMover
MbfFloorMover
Mbf21FloorMover
```

A new mover is justified only when its runtime semantics are genuinely different.

---

## 26. Woof as architectural reference

Use Woof as the main behavioral/architectural reference where it fits ManagedDoom.

The useful ideas are:

```text
compatibility levels
+
feature inheritance
+
shared Doom engine
```

Do not copy the Woof C source structure blindly.

ManagedDoom can use cleaner C# abstractions such as:

```text
Translator
Resolver
Specification
Behavior
Feature gate
```

where that reduces duplication without hurting performance.

Rule:

```text
Use Woof as reference behavior,
not as a 1:1 file template.
```

---

## 27. Test strategy

Do not create tests for every small helper or every branch.

Avoid:

```text
10 implementation methods
→ 80 unit tests
```

The goal is not line coverage.

The goal is to protect important contracts and real behavior.

For a major feature, a good default is:

```text
1–3 focused decoder/unit tests
+
1 real behavioral/integration test
```

Example for generalized floors:

```text
Translator decodes a representative special correctly

+

Load a map
activate the linedef
run N tics
assert final floor height
```

Do not test every individual `if`.

---

## 28. Essential Boom tests

Important behavior groups:

```text
Generalized floor actually moves
Generalized ceiling actually moves
Generalized door opens/closes
Generalized locked door checks keys
Generalized lift completes its cycle
Generalized stairs build a chain
Generalized crusher damages/reverses

Silent teleport preserves expected state
Line teleport preserves relative position/momentum

Scroller changes texture offset
Carry scroller moves player/thing

Friction changes movement
Wind/current/pusher affects actor

Transfer Heights changes geometry/render state correctly
Floor/ceiling light transfers work

PassThru activates multiple valid lines

Custom SWITCHES entry changes texture
Custom ANIMATED entry advances

TRANMAP produces expected blending

Generalized sector damage/secret combination works
```

Also keep several full-map smoke tests.

---

## 29. Compatibility corpus

After individual feature tests, maintain a small WAD corpus:

```text
Vanilla IWAD
Boom reference / feature WAD
several real Boom megawads
large Boom WAD
```

Verify:

```text
load
spawn
several hundred/thousand tics
render
special activation
no exceptions
reasonable performance
```

### DeHackEd-dependent corpus rule

Many real Boom/MBF-family WADs also contain an embedded `DEHACKED` lump or require an external `.deh` / `.bex` patch.

Do not disable that patch while using the WAD as a compatibility corpus target.

A failure such as:

```text
Unknown type
invalid frame
missing DoomEdNum
unknown code pointer
```

must first be classified as either:

```text
Boom gameplay failure

or
DeHackEd definition-patch failure
```

before changing Boom code.

Real Boom corpus entries that depend on DeHackEd are intentionally deferred until the full DeHackEd/BEX phase is complete.

---

## 30. Performance regression workflow

Do not write fragile automated tests that expect:

```text
FPS == 120
```

Instead use fixed manual profiling scenes.

Recommended scenes:

```text
DOOM2 MAP01
Boom feature map
NUTS
Okuplok or another huge map
large sprite-heavy scene
```

Compare:

```text
ThreeD render ms
sprite ms
BSP/wall ms
game tick ms
allocations/frame
```

### Hard requirement

When:

```text
Compatibility = Vanilla
```

the Boom subsystem should have practically zero runtime cost.

Ideally the cost is no more than a small feature-gate branch at a high-level entry point.

Boom support must never add compatibility work inside vanilla pixel loops or repeated whole-map scans.

---

## 31. Compatibility correctness over "better" behavior

Do not replace Boom behavior with behavior that merely appears more logical.

If Boom behaves strangely:

```text
reproduce Boom
```

Visual or gameplay enhancements must remain separate:

```text
Compatibility behavior
≠
Visual/QoL enhancement
```

Example:

```text
Boom TRANMAP
```

and:

```text
True Color enhanced alpha blending
```

should not accidentally become the same feature.

---

## 32. Map format and compatibility are separate systems

Do not confuse:

```text
Doom binary
XNOD
ZNOD
```

with:

```text
Vanilla
Boom
MBF
MBF21
```

A Boom-compatible map may still use:

```text
Doom binary + XNOD
```

These are two independent axes:

```text
Map container / node format
            +
Gameplay compatibility
```

Keep node-format loading separate from gameplay compatibility.

---

## 33. Limit-removing features are separate from Boom

Features such as:

```text
large BLOCKMAP
XNOD/ZNOD
dynamic renderer arrays
large maps
many sprites
removed vanilla limits
```

must not depend on:

```text
Compatibility == Boom
```

These are engine capabilities.

A vanilla-compatible WAD should still be able to use limit-removing engine improvements.

---

## 34. MBF after Boom and DeHackEd stabilization

Only begin MBF after:

```text
Boom feature implementation
+
full classic DeHackEd / Boom BEX support
+
Boom real-WAD stabilization
```

are complete enough to form stable lower layers.

Create:

```text
Compatibility/Mbf/
```

MBF should contain only MBF-specific additions and changes.

It automatically inherits all Boom features through:

```csharp
compatibility >= GameCompatibility.Boom
```

Do not copy Boom classes into the MBF folder.

---

## 35. MBF21 after MBF

Use the same inheritance model for gameplay **and** definition patches.

MBF21 adds DeHackEd fields, flags, state arguments and code pointers on top of the already-complete DeHackEd/BEX/MBF patch pipeline.

Use the same gameplay inheritance model:

```text
MBF21
=
Vanilla
+ Boom
+ MBF
+ MBF21 extensions
```

Do not create a separate MBF21 engine.

---

## 35A. OpenGL renderer after MBF21

After MBF21 gameplay and definition-patch support are stable, add an **optional OpenGL renderer backend** before moving on to DSDHacked.

OpenGL is **not** a gameplay compatibility level and must never be added to `GameCompatibility`.

The intended architecture is:

```text
World / BSP / gameplay / compatibility
                │
                ▼
       renderer-independent scene data
           /                 \
          /                   \
Software renderer         OpenGL renderer
(reference/fallback)      (GPU backend)
```

Hard requirements:

```text
Software rendering remains available.
OpenGL must not change deterministic gameplay state.
Boom / MBF / MBF21 behavior must be identical regardless of renderer.
Renderer selection happens outside gameplay compatibility detection.
Do not put OpenGL-specific state into World simulation code.
```

The OpenGL backend should initially reuse Doom's existing BSP traversal and visibility model rather than replacing the engine with a new scene graph.

Preferred progression:

```text
visible BSP segs        → GPU wall geometry
visible subsectors      → triangulated floor/ceiling geometry
visible mobjs           → camera-facing sprite quads
masked middle textures  → alpha-tested/translucent geometry
sky surfaces            → dedicated sky path
```

The first target is rendering parity, not visual enhancement. Keep classic texture coordinates, pegging rules, palette/colormap lighting semantics, interpolation and compatibility-specific rendering behavior correct before adding optional filtering or other presentation features.

OpenGL is expected to address GPU-suitable bottlenecks and software-rasterizer-only artifacts such as column/span coverage seams, but it must **not** be used to hide incorrect BSP, map geometry or gameplay logic.

---

## 36. DeHackEd/BEX is not optional infrastructure

ManagedDoom already contains partial DeHackEd support, but it must be treated as **unverified and incomplete** until audited against reference behavior.

The current implementation already has useful pieces:

```text
external -deh loading
embedded DEHACKED loading
Thing
Frame
Pointer
Ammo
Weapon
Misc
Text
BEX [STRINGS]
BEX [PARS]
BEX [CODEPTR]
```

However, the current source also contains obvious audit targets:

```text
Sound block    → handler currently empty
Cheat block    → handler currently empty
Sprite block   → handler currently empty

BEX support    → only a subset is currently parsed
DoomInfo       → patched through process-global mutable static tables
reload/reset   → no production definition-lifecycle boundary yet
multiple patches/lumps → load order and precedence must be verified
parser syntax  → case, whitespace, malformed input and diagnostics need tests
```

Therefore the next goal is **not** to add more ad-hoc cases to the existing parser.

First establish what is correct, what is incomplete, and what architecture is required for deterministic loading.

Reference behavior should be compared against conservative ports/specifications such as:

```text
original DeHackEd format
Boom DeHackEd/BEX documentation
Chocolate Doom
PrBoom+/DSDA-Doom
```

---

## 37. Full DeHackEd/BEX architecture

The long-term pipeline should be separated into four responsibilities:

```text
Loader
  ↓
Parser
  ↓
Patch model
  ↓
Applier / runtime definition set
```

Avoid one static method that parses text and mutates `DoomInfo` immediately.

Preferred shape:

```text
DeHackEdLoader
    reads external files and embedded lumps

DeHackEdParser
    converts text into typed patch operations

DeHackEdPatch
    stores parsed changes without touching runtime state

DeHackEdApplier
    applies patches in deterministic order

DeHackEdBaseline / DoomDefinitionSet
    owns a clean definition baseline for one WAD/game-content load
```

### Patch lifecycle requirement

Loading one WAD must never permanently modify the next WAD loaded in the same process.

This is a hard requirement because DeHackEd can change:

```text
DoomEdNum
thing properties
states
code pointers
weapons
ammo
misc constants
strings
par times
sounds / sprite mappings
```

The engine must support:

```text
load WAD A + its patches
run
close

load WAD B
→ exact clean baseline before applying WAD B patches
```

A test-only snapshot around global static state is acceptable as a temporary verification tool, but it is **not** the final production architecture.

### Loader and precedence

Audit and define exact behavior for:

```text
-deh patch1.deh patch2.bex ...
embedded DEHACKED lumps
multiple loaded PWADs
multiple patches in one launch
patch application order
-nodeh
```

Do not silently process only one patch if the reference behavior expects multiple patches to contribute.

### Classic DeHackEd blocks

Full support must cover and test:

```text
Thing
Frame
Pointer
Sound
Ammo
Weapon
Cheat
Misc
Text
Sprite
```

For each block test both parsing and actual runtime effect.

Examples:

```text
Thing ID #
→ map thing spawns as the patched actor

Frame / Pointer
→ patched state executes the expected action

Weapon / Ammo
→ firing and pickup logic use patched values

Text
→ real UI/game string changes
```

### Boom BEX

Classic Boom BEX support belongs to the DeHackEd phase, before MBF.

Required core sections/features:

```text
[STRINGS]
[PARS]
[CODEPTR]
NULL code pointer
code pointer assignment to arbitrary valid frames
INCLUDE
INCLUDE NOTEXT
```

Do not mix later Eternity/ZDoom-specific BEX extensions into the Boom target unless we explicitly decide to support them.

### Error handling

Malformed patches must fail predictably.

Do not allow accidental:

```text
IndexOutOfRangeException
NullReferenceException
KeyNotFoundException
```

for user-facing patch errors.

Prefer diagnostics that include:

```text
patch source
line number
block
field
bad value/index
```

---

## 38. DeHackEd compatibility extensions belong to their compatibility level

Do not implement every modern DeHackEd extension during the classic DeHackEd phase.

Use the inheritance chain:

```text
Classic DeHackEd
+ Boom BEX
        ↓
MBF-specific DeHackEd additions
        ↓
MBF21-specific DeHackEd additions
```

MBF support later owns MBF-specific action pointers and definition semantics.

MBF21 later owns features such as:

```text
Thing groups
MBF21 thing flags
MBF21 frame flags
Fast speed
Melee range
Rip sound
Ammo per shot
Args1 ... Args8
MBF21 DeHackEd code pointers
```

The parser should therefore be extensible by feature set instead of becoming one large switch statement with all eras mixed together.

---

## 39. DSDHacked after MBF21 and the OpenGL milestone

DSDHacked is implemented after MBF21 gameplay/DeHackEd support are stable and after the planned OpenGL renderer milestone has reached its renderer-parity sign-off.

It is an extension of the same DeHackEd pipeline, not a separate compatibility level.

A DSDHacked patch signals its indexing model with:

```text
Doom version = 2021
```

The architectural requirement is dynamic definition tables.

DSDHacked allows effectively unbounded/high indices for:

```text
Things
States / Frames
Sprites
Sounds
```

Therefore the implementation must not use C# enums or the original static array lengths as storage limits.

The roadmap must prepare for:

```text
dynamic Thing definitions
dynamic State definitions
dynamic Sprite name table
dynamic Sound definition/name table
safe high-index references
allocation defaults for newly-created indices
```

New DSDHacked indices must receive the specification-defined defaults before patch fields are applied.

Support the numeric-index form used by DSDHacked tables, including:

```text
[SPRITES]
1234 = NEW1

[SOUNDS]
3930 = TWISTR
```

Do not duplicate the DeHackEd parser for DSDHacked.

The same pipeline should become:

```text
DeHackEd parser
    +
compatibility-specific field registry
    +
dynamic definition storage
```

DSDHacked verification must include stress tests with high/non-vanilla indices and sequential WAD reloads to prove that dynamic definitions do not leak between games.

---

# 40. Implementation order

## Phase 1 — Infrastructure

```text
1. GameCompatibility
2. GameCompatibilityFeatures
3. CompatibilityDetector
4. COMPLVL reader
5. Feature scanner
6. AUTO / VANILLA / BOOM / MBF / MBF21 user override
```

## Phase 2 — Boom action architecture

```text
7. BoomTriggerType
8. BoomLineSpecials router
9. map-local tag lookup/index
10. common Boom action specifications
```

## Phase 3 — Generalized actions

```text
11. Floors
12. Ceilings
13. Doors
14. Locked Doors
15. Lifts
16. Stairs
17. Crushers
```

## Phase 4 — Extended regular actions

```text
18. Elevators
19. extended floor/ceiling actions
20. extended lighting
21. exits
22. remaining regular Boom linedefs
```

## Phase 5 — Teleport

```text
23. silent thing teleport
24. line-to-line teleport
25. reversed teleport
26. monster-only variants
```

## Phase 6 — Environment

```text
27. scrollers
28. carry
29. displacement scrollers
30. accelerative scrollers
31. friction
32. wind
33. current
34. point push/pull
```

## Phase 7 — Sector system

```text
35. generalized sector decoder
36. combined lighting
37. damage
38. secret
39. friction flag
40. pusher flag
```

## Phase 8 — Property transfers

```text
41. floor light transfer
42. ceiling light transfer
43. transfer heights / fake floors
44. fake colormaps
45. sprite/render clipping
```

## Phase 9 — Rendering resources

```text
46. translucent lines
47. TRANMAP
48. custom translucency maps
49. C_START/C_END
50. WATERMAP
```

## Phase 10 — WAD resources

```text
51. SWITCHES
52. ANIMATED
53. custom switch tables
54. custom texture/flat animations
```

## Phase 11 — Map semantics

```text
55. PassThru
56. Boom thing spawn flags
57. push/pull source things
58. monster activation rules
59. W1/WR/S1/SR/G1/GR/P1/PR semantics
60. repeat/reset semantics
```

## Phase 12 — Compatibility polish

```text
61. Boom movement quirks
62. Boom collision quirks
63. Boom sector movement behavior
64. teleport quirks
65. tag/zero-tag behavior
66. compatibility bug fixes required by Boom
```

## Phase 13 — Boom feature verification (before full DeHackEd)

```text
67. representative unit tests
68. behavioral map tests
69. official/reference Boom feature maps
70. initial real Boom corpus using entries that do not require unimplemented DeHackEd behavior
```

Do not use a DeHackEd-heavy WAD failure to reopen Boom code during this phase.

Those corpus entries are resumed after Phase 17.

## Phase 14 — DeHackEd audit and definition lifecycle

```text
71. inventory every currently supported DeHackEd/BEX field
72. compare current behavior against reference implementations/specification
73. split loader / parser / patch model / applier responsibilities
74. create clean vanilla definition baseline / reset lifecycle
75. define -deh and embedded DEHACKED load order
76. support deterministic multiple-patch application
77. parser syntax, header, comments, whitespace and diagnostics audit
```

## Phase 15 — Complete classic DeHackEd

```text
78. Thing blocks
79. Frame blocks
80. Pointer blocks
81. Sound blocks
82. Ammo blocks
83. Weapon blocks
84. Cheat blocks
85. Misc blocks
86. Text blocks
87. Sprite blocks
```

Do not consider a block complete only because it parses.

Each block requires at least one behavioral/runtime test.

## Phase 16 — Complete Boom BEX

```text
88. [STRINGS]
89. [PARS]
90. [CODEPTR]
91. NULL and arbitrary-frame code-pointer assignment
92. INCLUDE
93. INCLUDE NOTEXT
94. deterministic merge/order behavior across DEH + BEX patches
```

MBF-only and MBF21-only code pointers are **not** part of this phase.

## Phase 17 — DeHackEd verification

```text
95. parser/decoder unit contracts
96. runtime DoomEdNum / custom actor tests
97. state and code-pointer behavioral tests
98. weapon/ammo/misc/string/par runtime tests
99. sequential WAD load/reset isolation tests
100. real DeHackEd/BEX WAD corpus
101. external -deh + embedded DEHACKED integration tests
102. Vanilla performance/regression run with no patch loaded
```

## Phase 18 — Return to Boom stabilization

Only after full classic DeHackEd/BEX support:

```text
103. resume real Boom corpus including DeHackEd-heavy WADs
104. classify each failure as Boom vs DeHackEd before fixing code
105. fix Boom bugs exposed by real WADs
106. compare representative behavior against Boom/PrBoom/DSDA references
107. Boom performance profiling
108. full Vanilla regression run
109. final Boom sign-off
```

This is where deferred WADs such as large Boom projects with embedded `DEHACKED` return to the corpus.

## Phase 19 — MBF — COMPLETE

```text
✔ 110. MBF gameplay compatibility layer
✔ 111. MBF AI / actor behavior
✔ 112. MBF linedef/sector additions
✔ 113. MBF-specific DeHackEd fields and code pointers
✔ 114. MBF behavioral tests and real-WAD corpus
✔ 115. Vanilla + Boom regression
```

MBF inherits the complete Boom + DeHackEd/BEX implementation.

Final Phase 19 validation:

```text
✔ full automated test suite passes
✔ representative MBFEDIT!.WAD corpus loads successfully
✔ embedded MBF DeHackEd patch loads without invalid Codep Frame warnings
✔ MBFEDIT!.WAD MAP01 builds successfully (1/1 maps OK)
✔ AUTO compatibility resolves MBFEDIT!.WAD to Mbf via FeatureScan
✔ forced Vanilla/Boom profiles remain isolated from MBF runtime semantics
✔ Vanilla/Boom WAD loading remains regression-clean
✔ no observed FPS regression from the MBF compatibility layer
```

Phase 19 is signed off. The next active compatibility milestone is **Phase 20 — MBF21**.

## Phase 20 — MBF21

```text
116. MBF21 gameplay/map features
117. MBF21 thing fields, groups and flags
118. MBF21 frame flags
119. Args1 ... Args8 state arguments
120. MBF21 DeHackEd code pointers
121. MBF21 weapon/ammo extensions
122. MBF21 behavioral tests and real-WAD corpus
123. Vanilla + Boom + MBF regression
```

## Phase 21 — OpenGL renderer

OpenGL is a rendering backend, **not** a new compatibility level. The software renderer remains available as the reference/fallback path throughout this phase.

```text
124. renderer backend abstraction and runtime renderer selection
125. OpenGL context, frame lifecycle and resize/fullscreen integration
126. GPU texture/flat/sprite upload and cache lifecycle
127. one-sided and two-sided wall geometry with Doom texture offsets/pegging
128. subsector floor/ceiling polygon generation and triangulation
129. sprites, masked middle textures and sky rendering
130. palette/colormap lighting and true-color parity
131. Boom rendering features: translucency, transfer heights and related clipping semantics
132. interpolated camera/world rendering parity with the software backend
133. nearest/bilinear texture filtering as renderer options
134. batching, state sorting, culling and GPU performance profiling
135. software ↔ OpenGL visual/behavioral parity tests and representative WAD corpus
136. OpenGL renderer sign-off with Vanilla/Boom/MBF/MBF21 regression
```

OpenGL Phase 21 sign-off requires that switching renderers does not alter gameplay state, demo-relevant simulation behavior, compatibility detection or DeHackEd/MBF21 definitions.

## Phase 22 — DSDHacked

```text
137. dynamic Thing / State / Sprite / Sound definition tables
138. Doom version = 2021 detection
139. allocate and initialize high/new indices with DSDHacked defaults
140. numeric [SPRITES] entries
141. numeric [SOUNDS] entries
142. high-index cross-reference validation
143. MBF21 Args/code-pointer integration with dynamic states/things
144. sequential reload/reset isolation
145. high-index stress tests
146. real DSDHacked WAD corpus
```

## Phase 23 — Full-stack verification

```text
147. Vanilla regression
148. Boom regression
149. classic DeHackEd/BEX regression
150. MBF regression
151. MBF21 regression
152. software/OpenGL renderer regression
153. DSDHacked regression
154. performance profiling
155. mixed real-WAD corpus
```

---

# Definition of Done — Boom

Boom **feature implementation** is complete when the specification features are present and focused/reference tests pass.

Boom **final stabilization** is completed only after the DeHackEd/BEX phase, because real Boom WADs can depend on patched actors/states and otherwise produce misleading failures.

Boom support is **not** considered complete merely because a Boom WAD loads.

Boom support is complete only when the important parts of the specification work together:

```text
✔ generalized linedefs
✔ extended linedefs
✔ generalized sectors
✔ scrollers
✔ friction
✔ pushers
✔ silent teleporters
✔ transfer heights
✔ light transfers
✔ translucency / TRANMAP
✔ SWITCHES
✔ ANIMATED
✔ Boom colormaps
✔ PassThru
✔ Boom thing flags
✔ activation semantics
✔ Boom compatibility quirks

✔ Vanilla tests remain green
✔ Boom integration tests pass
✔ representative real Boom WADs behave correctly
✔ no major Vanilla performance regression
✔ no catastrophic Boom performance regression
```

---

# Definition of Done — DeHackEd / BEX

DeHackEd support is complete only when:

```text
✔ all classic block types have real semantics
✔ Sound / Cheat / Sprite are no longer placeholder handlers
✔ external -deh files work
✔ embedded DEHACKED lumps work
✔ multiple patches apply in deterministic order
✔ Thing ID # changes affect real map spawning
✔ state/frame changes affect runtime actors/weapons
✔ Pointer and BEX [CODEPTR] execute the correct actions
✔ Ammo / Weapon / Misc values are consumed by gameplay code
✔ Text and [STRINGS] affect real UI/game strings
✔ [PARS] affects real par times
✔ Boom BEX INCLUDE behavior is defined and tested
✔ malformed patch errors are diagnostic rather than random runtime exceptions
✔ loading a second WAD starts from a clean definition baseline
✔ real DeHackEd/BEX WAD corpus passes
✔ no meaningful Vanilla overhead exists when no patch is loaded
```

---

# Definition of Done — MBF

MBF support is complete when the MBF-specific layer works on top of the already-stable Boom + DeHackEd/BEX foundation without leaking behavior into older compatibility profiles:

```text
✔ MBF compatibility inherits Boom behavior rather than duplicating it
✔ MBF compatibility options are parsed and consumed by runtime behavior
✔ MBF AI / actor behavior is covered by focused behavioral tests
✔ FRIEND, TOUCHY and BOUNCES use canonical MBF flag semantics
✔ helper-dog / friendly-monster behavior is implemented and regression-tested
✔ implemented MBF code pointers execute through the shared DeHackEd/BEX pipeline
✔ classic MBF Codep Frame source indices resolve to the correct implemented actions
✔ external -deh / .bex and embedded DEHACKED participate consistently in AUTO detection
✔ -nodeh behavior matches the real patch-loading path
✔ MBF runtime features remain disabled under forced Vanilla/Boom compatibility
✔ representative end-to-end MBF behavioral integration tests pass
✔ representative real MBF WAD corpus passes (MBFEDIT!.WAD)
✔ Vanilla + Boom regression suite remains green
✔ no observed FPS regression is introduced by MBF support
```

---

# Definition of Done — OpenGL renderer

The OpenGL backend is complete only when:

```text
✔ software renderer remains selectable and functional
✔ renderer choice does not change World/gameplay simulation
✔ one-sided and two-sided walls match Doom texture placement/pegging rules
✔ floors and ceilings render without software-column/span coverage seams
✔ sprites and masked middle textures clip correctly against world geometry
✔ sky rendering matches expected Doom/Boom behavior
✔ palette/colormap lighting has a compatibility-correct GPU path
✔ true-color rendering works without requiring CPU per-pixel rasterization
✔ Boom translucency and transfer-height rendering are supported
✔ interpolation produces stable geometry without changing tic simulation
✔ nearest and bilinear filtering are selectable renderer options
✔ representative Vanilla/Boom/MBF/MBF21 WADs pass visual parity checks
✔ OpenGL materially reduces rendering cost in GPU-suitable heavy scenes
✔ renderer resources are rebuilt safely across WAD reloads/resolution changes
✔ no renderer-specific state leaks between sequential game loads
```

---

# Definition of Done — DSDHacked

DSDHacked support is complete only when:

```text
✔ MBF21 DeHackEd is already complete
✔ Doom version = 2021 is recognized
✔ Thing/State/Sprite/Sound tables can grow dynamically
✔ high indices are not limited by legacy enums/static array sizes
✔ new indices receive correct DSDHacked defaults
✔ numeric [SPRITES] works
✔ numeric [SOUNDS] works
✔ high-index state/thing/sprite/sound references work at runtime
✔ MBF21 code pointers and Args work with dynamic indices
✔ invalid high-index references fail cleanly
✔ sequential WAD loads do not leak dynamic definitions
✔ stress tests with thousands of definitions pass
✔ representative real DSDHacked WADs pass
```

---

# Core development rules

```text
DO NOT REWRITE THE VANILLA ENGINE FOR BOOM
IF A SMALL EXTENSION POINT CAN DO THE JOB.

DO NOT SCAN THE WHOLE WORLD EVERY TIC
IF THE DATA CAN BE RESOLVED WHEN THE MAP LOADS.

DO NOT PUT COMPATIBILITY CHECKS INTO PIXEL LOOPS
IF THE RENDER PATH CAN BE SELECTED BEFORE RASTERIZATION.

DO NOT TEST EVERY LINE OF IMPLEMENTATION CODE.
TEST REAL BEHAVIOR AND IMPORTANT CONTRACTS.

KEEP LIMIT-REMOVING ENGINE FEATURES SEPARATE
FROM GAMEPLAY COMPATIBILITY.

KEEP RENDERER BACKENDS SEPARATE FROM GAMEPLAY COMPATIBILITY.
SOFTWARE AND OPENGL MUST RENDER THE SAME SIMULATION STATE.

VANILLA → BOOM → MBF → MBF21.

EVERY NEW COMPATIBILITY LEVEL INHERITS
THE FEATURES OF THE PREVIOUS LEVEL.
```
