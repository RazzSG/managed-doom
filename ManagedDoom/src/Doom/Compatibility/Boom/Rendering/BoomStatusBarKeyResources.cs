using System;
using System.IO;
using ManagedDoom;

namespace ManagedDoom.Compatibility.Boom.Rendering;

/// <summary>
/// Loads Boom's predefined combined-key status-bar patches embedded in
/// managed-doom. A PWAD can still override STKEYS6..8 through the normal
/// WAD lookup; these resources are only the built-in fallback.
/// </summary>
public static class BoomStatusBarKeyResources
{
    private const int FirstPatchIndex = BoomStatusBarKeys.CombinedPatchBase;
    private const int LastPatchIndex = BoomStatusBarKeys.PatchCount - 1;
    private const string ResourcePrefix = "ManagedDoom.Resources.Boom.";

    public static Patch LoadPatch(int patchIndex)
    {
        if (patchIndex < FirstPatchIndex || patchIndex > LastPatchIndex)
            throw new ArgumentOutOfRangeException(nameof(patchIndex));

        var patchName = "STKEYS" + patchIndex;
        var resourceName = ResourcePrefix + patchName + ".lmp";
        var assembly = typeof(BoomStatusBarKeyResources).Assembly;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException(
                $"Embedded Boom status-bar resource '{resourceName}' was not found.");
        }

        if (stream.Length > int.MaxValue)
            throw new InvalidDataException($"Embedded resource '{resourceName}' is too large.");

        var data = new byte[(int)stream.Length];
        var offset = 0;
        while (offset < data.Length)
        {
            var read = stream.Read(data, offset, data.Length - offset);
            if (read == 0)
                throw new EndOfStreamException($"Unexpected end of embedded resource '{resourceName}'.");

            offset += read;
        }

        return Patch.FromData(patchName, data);
    }
}
