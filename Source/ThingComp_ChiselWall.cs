using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace SmoothAndChisel;
public class ThingComp_ChiselWall : ThingComp {
    private static readonly Setting Vanilla = new VanillaSetting();
    private static readonly Setting Wall    = new WallSetting();

    private Setting current = Vanilla;
    private Setting desired = null;

    public bool HasJob 
        => desired != null;

    public override IEnumerable<Gizmo> CompGetGizmosExtra() {
        yield return new ChiselGizmo(this);
    }

    public override void PostExposeData() {
        if (Scribe.mode == LoadSaveMode.Saving) {
            UpdateDesired();
        }

        Setting.Look(ref current, Strings.ID, Vanilla);
        Setting.Look(ref desired, Strings.ID + ".desired", null);

        if (Scribe.mode == LoadSaveMode.PostLoadInit && desired == null) {
            RemoveDesignation();
        }
    }

    private void AddDesignation() {
        if (Designation == null) {
            parent.Map?.designationManager.AddDesignation(new(parent, MyDefOf.Designation_Chisel));
        }
    }

    private void RemoveDesignation() {
        var des = Designation;
        if (des != null) {
            parent.Map?.designationManager.RemoveDesignation(des);
        }
    }

    private Setting UpdateDesired() {
        if (desired != null && Designation == null) {
            desired = null;
        }
        return desired;
    }

    private Designation Designation
        => parent.Map?.designationManager.DesignationOn(parent, MyDefOf.Designation_Chisel);

    public override void PostSpawnSetup(bool respawningAfterLoad) {
        if (UnityData.IsInMainThread) {
            Apply();
        } else { 
            LongEventHandler.ExecuteWhenFinished(Apply);
        }
    }

    private static void GizmoClicked(List<ThingComp_ChiselWall> comps) {
        Setting desired = comps[0].UpdateDesired();
        for (int i = 1, n = comps.Count; i < n; i++) {
            if (desired != comps[i].UpdateDesired()) desired = null;
        }

        var menu = Faction.OfPlayer.ideos.AllIdeos
            .Select(x => x.style.StyleForThingDef(ThingDefOf.Wall))
            .Where(x => x?.styleDef != null)
            .Select(x => x.category)
            .Distinct()
            .Select(StyleSetting.For)
            .Concat([ Wall, Vanilla ])
            .Select(x => x.MenuItem(comps, desired))
            .ToList();
        Find.WindowStack.Add(new FloatMenu(menu));
    }

    public void ApplyDesired() {
        current = desired;
        desired = null;
        RemoveDesignation();
        Apply();
    }

    private void Set(Setting value) {
        if (current == value) {
            desired = null;
            RemoveDesignation();
        } else if (DebugSettings.godMode) { 
            current = value;
            Apply();
        } else {
            desired = value;
            AddDesignation();
        }
    }

    private void Apply() 
        => SetGraphic(current.GraphicFor(parent));

    private void SetGraphic(Graphic graphic) {
        Traverse.Create(parent).Field<Graphic>("graphicInt").Value = graphic;
        parent.DirtyMapMesh(parent.Map);
    }

    private class ChiselGizmo : Command {
        private List<ThingComp_ChiselWall> comps;

        public ChiselGizmo(ThingComp_ChiselWall comp) {
            defaultLabel = Strings.ChiselLabel;
            defaultDesc  = Strings.ChiselDesc;
            icon = Textures.Chisel;
            alsoClickIfOtherInGroupClicked = false;
            comps = [ comp ];
        }

        public override void ProcessInput(Event ev) {
            base.ProcessInput(ev);
            GizmoClicked(comps);
        }

        public override bool GroupsWith(Gizmo other) 
            => other is ChiselGizmo;

        public override void MergeWith(Gizmo other) 
            => comps.AddRange((other as ChiselGizmo).comps);
    }

    private abstract class Setting { 

        public static void Look(ref Setting var, string name, Setting def) {
            string value = var?.Value ?? "null";
            Scribe_Values.Look(ref value, name, def?.Value ?? "null");
            if (Scribe.mode == LoadSaveMode.LoadingVars) {
                var = value switch {
                    "vanilla" => Vanilla,
                    "wall"    => Wall,
                    "style"   => StyleSetting.Dummy,
                    _         => null,
                };
            }
            var?.ExposeData(ref var, name, def);
        }

        public Graphic GraphicFor(Thing thing) {
            var graphic = Graphic;
            if (ShouldColor) {
                graphic = graphic?.GetColoredVersion(graphic.Shader, thing.DrawColor, thing.DrawColorTwo);
            }
            return graphic;
        }

        public FloatMenuOption MenuItem(List<ThingComp_ChiselWall> comps, Setting desired) {
            Func<Rect, bool> check = null;
            float checkWidth = 0f;
            if (desired == this) {
                check = r => { 
                    Widgets.CheckboxDraw(r.x + 2f, r.y, true, false); 
                    return false;
                };
                checkWidth = Widgets.CheckboxSize + 2f;
            }

            return MenuItem(Apply, check, checkWidth);


            void Apply() {
                foreach (var comp in comps) {
                    comp.Set(this);
                }
            }
        }

        protected virtual FloatMenuOption MenuItem(Action action, Func<Rect, bool> checkFunc, float checkWidth) 
            => new(Label, action, extraPartWidth: checkWidth, extraPartOnGUI: checkFunc);

        protected virtual void ExposeData(ref Setting var, string name, Setting def) {}

        protected virtual bool ShouldColor => true;

        protected abstract string Value { get; }

        protected abstract string Label { get; }

        protected abstract Graphic Graphic { get; }
    }

    private class VanillaSetting : Setting {
        protected override string Value => "vanilla";

        protected override string Label => Strings.Smooth;

        protected override Graphic Graphic => null;
    }

    private class WallSetting : Setting {
        private static Graphic graphic;

        private static readonly StuffAppearanceDef bricks = 
            DefDatabase<StuffAppearanceDef>.GetNamed("Bricks");

        protected override string Value => "wall";

        protected override string Label => Strings.Brick;

        protected override Graphic Graphic 
            => graphic ??= MakeGraphic();

        private static Graphic MakeGraphic() {
            var graphic = ThingDefOf.Wall.graphicData.Graphic;
            if (graphic is not Graphic_Linked) return graphic;

            Traverse<Graphic> subGraphic = Traverse.Create(graphic).Field<Graphic>("subGraphic");
            if (subGraphic.Value is not Graphic_Appearances graphicApp) return graphic;

            subGraphic.Value = graphicApp.SubGraphicFor(bricks);
            var copy = graphic.GetColoredVersion(graphic.Shader, graphic.Color, graphic.ColorTwo);
            subGraphic.Value = graphicApp;
            return copy;
        }
    }

    private class StyleSetting : Setting {
        public static StyleSetting Dummy = new();

        private static readonly ConditionalWeakTable<StyleCategoryDef, StyleSetting> cache = new();

        private readonly StyleCategoryDef category;
        private readonly ThingStyleDef Style;

        public static StyleSetting For(StyleCategoryDef style)
            => cache.GetValue(style, x => new(x));

        private StyleSetting() {}

        private StyleSetting(StyleCategoryDef category) {
            this.category = category;
            Style = category.GetStyleForThingDef(ThingDefOf.Wall);
        }

        protected override string Value => "style";

        protected override string Label => category.LabelCap;

        protected override Graphic Graphic => GraphicOf(Style);

        protected override bool ShouldColor 
            => Style.graphicData != null;

        protected override void ExposeData(ref Setting var, string name, Setting def) {
            StyleCategoryDef cat = category;
            Scribe_Defs.Look(ref cat, name + ".style");
            if (Scribe.mode == LoadSaveMode.LoadingVars) {
                var = (cat == null) ? def : For(cat);
            }
        }

        protected override FloatMenuOption MenuItem(Action action, Func<Rect, bool> checkFunc, float checkWidth) 
            => new(Label, action, category.Icon, Color.white, extraPartWidth: checkWidth, extraPartOnGUI: checkFunc);

        private static Graphic GraphicOf(ThingStyleDef style)
            => style.graphicData?.Graphic ?? style.Graphic;
    }
}
