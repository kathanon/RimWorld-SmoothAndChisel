using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SmoothAndChisel;
public static class MyDefOf {
    public static JobDef Job_Chisel;

    public static DesignationDef Designation_Chisel;

    public static void Setup() {
        Job_Chisel = MyJobDefOf.kathanon_SmoothAndChisel_Chisel;
        Designation_Chisel = MyDesignationDefOf.kathanon_SmoothAndChisel_Chisel;
    }
}

[DefOf]
public static class MyJobDefOf {
    public static JobDef kathanon_SmoothAndChisel_Chisel;
}
[DefOf]
public static class MyDesignationDefOf {
    public static DesignationDef kathanon_SmoothAndChisel_Chisel;
}