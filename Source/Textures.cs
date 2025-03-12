using UnityEngine;
using Verse;

namespace SmoothAndChisel;

[StaticConstructorOnStartup]
public static class Textures {
    private const string Prefix = Strings.ID + "/";

    public static readonly Texture2D Chisel = ContentFinder<Texture2D>.Get(Prefix + "Chisel");
}
