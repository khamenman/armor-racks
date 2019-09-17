﻿using System.Collections.Generic;
using System.Linq;
using ArmorRacks.Things;
using RimWorld;
using Verse;
using Verse.AI;

namespace ArmorRacks.Jobs
{
    public class JobDriverSwapWithRack : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetThingA, job, errorOnFailed: errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
            yield return Toils_Reserve.Reserve(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.WaitWith(TargetIndex.A, 100, true);
            yield return new Toil()
            {
                initAction = delegate
                {
                    var armorRack = TargetThingA as ArmorRack;
                    var removedPawnApparel = new List<Apparel>();
                    var removedRackApparel = new List<Apparel>();
                    foreach (Apparel apparel in pawn.apparel.WornApparel.ToList())
                    {
                        pawn.apparel.Remove(apparel);
                        removedPawnApparel.Add(apparel);
                    }
                    
                    foreach (Apparel apparel in armorRack.GetStoredApparel())
                    {
                        armorRack.InnerContainer.Remove(apparel);
                        removedRackApparel.Add(apparel);
                    }

                    foreach (var apparel in removedRackApparel)
                    {
                        pawn.apparel.Wear(apparel);
                    }
                    
                    foreach (var apparel in removedPawnApparel)
                    {
                        armorRack.InnerContainer.TryAdd(apparel);
                    }

                    ThingWithComps rackWeapon = (ThingWithComps)armorRack.GetStoredWeapon();
                    ThingWithComps pawnWeapon = pawn.equipment.Primary;
                    int hasRackWeapon = rackWeapon == null ? 0x00 : 0x01;
                    int hasPawnWeapon = pawnWeapon == null ? 0x00 : 0x10;
                    switch (hasRackWeapon | hasPawnWeapon)
                    {
                        case 0x11:
                            pawn.equipment.Remove(pawnWeapon);
                            armorRack.InnerContainer.TryAdd(pawnWeapon);
                            armorRack.InnerContainer.Remove(rackWeapon);
                            pawn.equipment.MakeRoomFor(rackWeapon);
                            pawn.equipment.AddEquipment(rackWeapon);
                            break;
                        case 0x01:
                            armorRack.InnerContainer.Remove(rackWeapon);
                            pawn.equipment.MakeRoomFor(rackWeapon);
                            pawn.equipment.AddEquipment(rackWeapon);
                            break;
                        case 0x10:
                            pawn.equipment.Remove(pawnWeapon);
                            armorRack.InnerContainer.TryAdd(pawnWeapon);
                            break;
                    }
                }
            };
        }
    }
}