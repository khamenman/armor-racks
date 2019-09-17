﻿using System;
using System.Collections.Generic;
using System.Linq;
using ArmorRacks.Commands;
using ArmorRacks.DefOfs;
using ArmorRacks.Things;
using RimWorld;
using Verse;
using Verse.AI;

namespace ArmorRacks.ThingComps
{
    public class ArmorRackCompProperties : CompProperties
    {
        public bool myExampleBool;

        public ArmorRackCompProperties()
        {
            this.compClass = typeof(ArmorRackComp);
        }

        public ArmorRackCompProperties(Type compClass) : base(compClass)
        {
            this.compClass = compClass;
        }
    }

    public class ArmorRackComp : ThingComp
    {
        public ArmorRackCompProperties Props => (ArmorRackCompProperties) this.props;

        protected bool IsPawnAllowedWeapons(Pawn pawn)
        {
            return !(pawn.story.WorkTagIsDisabled(WorkTags.Violent) && ((ArmorRack) parent).GetStoredWeapon() != null);
        }

        protected bool RackHasItems()
        {
            return ((ArmorRack) parent).InnerContainer.Count != 0;
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            ArmorRack armorRack = this.parent as ArmorRack;
            if (ForbidUtility.IsForbidden(armorRack, selPawn))
            {
                yield break;
            }
            
            if (!selPawn.CanReach(armorRack, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn))
            {
                FloatMenuOption failer = new FloatMenuOption("CannotUseNoPath".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null);
                yield return failer;
                yield break;
            }

            var nonViolentOptionYielded = false;
            if (IsPawnAllowedWeapons(selPawn))
            {
                // Swap with
                var swapWithOption = new FloatMenuOption("ArmorRacks_SwapRackFloatMenuLabel".Translate(), delegate
                {
                    var target_info = new LocalTargetInfo(armorRack);
                    var wearRackJob = new Job(ArmorRacksJobDefOf.ArmorRacks_JobSwapWithRack, target_info);
                    selPawn.jobs.TryTakeOrderedJob(wearRackJob);
                });
                yield return FloatMenuUtility.DecoratePrioritizedTask(swapWithOption, selPawn, armorRack, "ReservedBy");
            }
            else
            {
                yield return new FloatMenuOption("ArmorRacks_CannotEquipNonviolent".Translate(), null);
                nonViolentOptionYielded = true;
            }
            
            // Assign
            var assignLabel = "ArmorRacks_AssignRackFloatMenuLabel".Translate();
            if (armorRack.AssignedPawns.Count() != 0)
            {
                assignLabel = "ArmorRacks_AssignRackFloatMenuLabel_Owned".Translate((NamedArgument) armorRack.AssignedPawns.First());
            }
            yield return new FloatMenuOption(assignLabel, delegate
            {
                armorRack.TryAssignPawn(selPawn);
            }); 

            if (RackHasItems())
            {
                if (IsPawnAllowedWeapons(selPawn))
                {
                    // Equip from
                    var equipFromOption = new FloatMenuOption("ArmorRacks_WearRackFloatMenuLabel".Translate(), delegate
                    {
                        var target_info = new LocalTargetInfo(armorRack);
                        var wearRackJob = new Job(ArmorRacksJobDefOf.ArmorRacks_JobWearRack, target_info);
                        selPawn.jobs.TryTakeOrderedJob(wearRackJob);
                    });
                    yield return FloatMenuUtility.DecoratePrioritizedTask(equipFromOption, selPawn, armorRack, "ReservedBy");
                }
                else if (!nonViolentOptionYielded)
                {
                    yield return new FloatMenuOption("ArmorRacks_CannotEquipNonviolent".Translate(), null);
                }
                
                // Clear out
                var clearOutOption = new FloatMenuOption("ArmorRacks_ClearRackFloatMenuLabel".Translate(), delegate
                {
                    var target_info = new LocalTargetInfo(armorRack);
                    var clearRackJob = new Job(ArmorRacksJobDefOf.ArmorRacks_JobClearRack, target_info);
                    selPawn.jobs.TryTakeOrderedJob(clearRackJob);
                });
                yield return FloatMenuUtility.DecoratePrioritizedTask(clearOutOption, selPawn, armorRack, "ReservedBy");
            
                // Clear out and forbid
                var clearOutForbidOption = new FloatMenuOption("ArmorRacks_ClearForbidRackFloatMenuLabel".Translate(), delegate
                {
                    var target_info = new LocalTargetInfo(armorRack);
                    var clearRackJob = new Job(ArmorRacksJobDefOf.ArmorRacks_JobClearForbidRack, target_info);
                    selPawn.jobs.TryTakeOrderedJob(clearRackJob);
                });
                yield return FloatMenuUtility.DecoratePrioritizedTask(clearOutForbidOption, selPawn, armorRack, "ReservedBy");
            }
            else
            {
                yield return new FloatMenuOption("ArmorRacks_WearRackFloatMenuLabel_Empty".Translate(), null);
                yield return new FloatMenuOption("ArmorRacks_ClearRackFloatMenuLabel_Empty".Translate(), null);
            }
        }
    }

    public class ArmorRackPawnOwnershipComp : ThingComp
    {
        public List<ArmorRack> assignedArmorRacks = new List<ArmorRack>();
    }

    public class ArmorRackUseCommandComp : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (this.parent is Pawn pawn)
            {
                var racksComp = pawn.GetComp<ArmorRackPawnOwnershipComp>();
                foreach (ArmorRack armorRack in racksComp.assignedArmorRacks)
                {
                    // TODO validation
                    yield return new ArmorRackWearCommand(armorRack, pawn);    
                    yield return new ArmorRackSwapCommand(armorRack, pawn);    
                }
            }
        }
    }

}