using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace SmoothAndChisel;
public class JobDriver_Chisel : JobDriver {
    private const float WorkAmount = 1000f;

    private float progress;

    public override bool TryMakePreToilReservations(bool errorOnFailed) 
        => pawn.Reserve(TargetA, job, errorOnFailed: errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils() { 
        var comp = TargetThingA.TryGetComp<ThingComp_ChiselWall>();
        if (comp == null) {
            yield break;
        }

        this.FailOnDespawnedNullOrForbidden(TargetIndex.A)
            .FailOn(() => pawn.IsPlayerControlled && !job.ignoreDesignations
                && pawn.Map.designationManager.DesignationOn(TargetThingA, MyDefOf.Designation_Chisel) == null);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

        Toil toil = ToilMaker.MakeToil("DoChisel");
        toil.WithProgressBar(TargetIndex.A, () => progress);
        toil.AddEndCondition(() => (progress >= 1f) ? JobCondition.Succeeded : JobCondition.Ongoing);
        toil.AddFinishAction(comp.ApplyDesired);
        toil.defaultCompleteMode = ToilCompleteMode.Never;
        toil.initAction = () => progress = 0;
        toil.activeSkill = () => SkillDefOf.Artistic;
        toil.tickAction = () => { 
            float work = 1.7f * toil.actor.GetStatValue(StatDefOf.WorkSpeedGlobal);
            progress += work / WorkAmount;
            toil.actor.skills?.Learn(SkillDefOf.Artistic, 0.02f);
        };
        yield return toil;
    }

    public override void ExposeData() {
        base.ExposeData();
        Scribe_Values.Look(ref progress, "progress", 0f);
    }
}
