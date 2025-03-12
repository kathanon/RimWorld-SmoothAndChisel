using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace SmoothAndChisel;
public class WorkGiver_Chisel : WorkGiver_Scanner {
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) 
        => pawn.Map.designationManager
            .SpawnedDesignationsOfDef(MyDefOf.Designation_Chisel)
            .Select(x => x.target.Thing);

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
        var comp = t.TryGetComp<ThingComp_ChiselWall>();
        if (comp == null || !comp.HasJob) return null;
        if (pawn.Map.designationManager.DesignationOn(t, MyDefOf.Designation_Chisel) == null) return null;
        if (!pawn.CanReserve(t, ignoreOtherReservations: forced)) return null;

        Thing haul = null;
        for (int i = 0; i < 8; i++) {
            IntVec3 pos = t.Position + GenAdj.AdjacentCells[i];
            if (pos.InBounds(pawn.Map) 
                    && ReachabilityImmediate.CanReachImmediate(pos, t, pawn.Map, PathEndMode.Touch, pawn)) {
                if (pos.Standable(pawn.Map)) { 
                    return JobMaker.MakeJob(MyDefOf.Job_Chisel, t);
                } else if (pos.Walkable(pawn.Map)) {
                    foreach (var thing in pos.GetThingList(t.Map)) {
                        if (thing.def.designateHaulable && thing.def.passability == Traversability.PassThroughOnly) {
                            haul = thing; 
                            break;
                        }
                    }
                }
            }
        }

        if (haul != null) {
            return HaulAIUtility.HaulAsideJobFor(pawn, haul);
        } else {
            JobFailReason.Is("NoPath".Translate());
            return null;
        }
    }
}
