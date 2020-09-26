using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class StorageStatus : MyGridProgram
    {
        private readonly List<IMyTextSurface> surfaces;
        private readonly List<IMyInventory> inventories;
        private readonly MyFixedPoint maxVolume;

        public StorageStatus()
        {
            surfaces = new List<IMyTextSurface>();
            surfaces.Add(Me.GetSurface(0));
            IMyTextSurfaceProvider block = GridTerminalSystem.GetBlockWithName("Miner - Cockpit") as IMyTextSurfaceProvider;
            surfaces.Add(block.GetSurface(0));
            surfaces.Add(GridTerminalSystem.GetBlockWithName("Miner - Butt LCD") as IMyTextSurface);

            List<IMyTerminalBlock> inventoryBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlockGroupWithName("Miner - Storage")?.GetBlocksOfType(inventoryBlocks, b => b.HasInventory);
            inventories = inventoryBlocks.SelectMany(b => Enumerable.Range(0, b.InventoryCount).Select(i => b.GetInventory(i))).ToList();

            maxVolume = inventories.Aggregate(MyFixedPoint.Zero, (agg, inv) => MyFixedPoint.AddSafe(agg, inv.MaxVolume));
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Save() { }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource.HasFlag(UpdateType.Update100))
            {
                MyFixedPoint cur = inventories.Aggregate(MyFixedPoint.Zero, (agg, inv) => MyFixedPoint.AddSafe(agg, inv.CurrentVolume));
                Echo($"{inventories.Count} inventories found with {MyFixedPoint.MultiplySafe(1000, cur)} / {MyFixedPoint.MultiplySafe(1000, maxVolume)} litres filled.");
                float percent = (float)cur.RawValue / maxVolume.RawValue;
                Color backColor = Color.Green;
                if (percent > 0.8f)
                {
                    backColor = Color.Red;
                }
                else if (percent > 0.5f)
                {
                    backColor = Color.Yellow;
                }

                foreach (IMyTextSurface surface in surfaces)
                {
                    surface.BackgroundColor = backColor;
                    surface.WriteText($"{percent * 100:00}%");
                }
            }
        }
    }
}
