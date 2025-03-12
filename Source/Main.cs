using HarmonyLib;
using Verse;
using UnityEngine;
using RimWorld;

namespace SmoothAndChisel;

[StaticConstructorOnStartup]
public class Main : Mod {
    public static Main Instance { get; private set; }

    static Main() {
        var harmony = new Harmony(Strings.ID);
        harmony.PatchAll();
    }

    public Main(ModContentPack content) : base(content) {
        Instance = this;
    }
    
    public static void OnInit() {
        MyDefOf.Setup();
		// Instance.Settings();
	}
    
    /*
    public Settings Settings() 
		=> GetSettings<Settings>();

    public override void DoSettingsWindowContents(Rect inRect) 
        => Settings().DoGUI(inRect);

    public override string SettingsCategory() 
        => Strings.Name;
	*/
}

[HarmonyPatch]
public static class InitHook {
    [HarmonyPatch(typeof(MainMenuDrawer), nameof(MainMenuDrawer.Init))]
    [HarmonyPostfix]
    public static void Init() 
        => Main.OnInit();
}
