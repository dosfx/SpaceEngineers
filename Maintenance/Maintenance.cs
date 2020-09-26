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
    partial class Maintenance : MyGridProgram
    {
        private List<IMyDoor> watchDoors;
        private List<IMyBatteryBlock> batteries;
        private List<IMyFunctionalBlock> generators;

        public Maintenance()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            watchDoors = new List<IMyDoor>();
            GridTerminalSystem.GetBlocksOfType(watchDoors, b =>
                b.BlockDefinition.TypeIdString == "MyObjectBuilder_AirtightSlideDoor" ||
                (b.BlockDefinition.TypeIdString == "MyObjectBuilder_Door" && (b.BlockDefinition.SubtypeId == "" || b.BlockDefinition.SubtypeId == "LargeBlockOffsetDoor")));

            batteries = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlockGroupWithName("Batteries")?.GetBlocksOfType(batteries);

            generators = new List<IMyFunctionalBlock>();
            GridTerminalSystem.GetBlockGroupWithName("Generators")?.GetBlocksOfType(generators);
        }

        public void Save() { }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource.HasFlag(UpdateType.Update100))
            {
                // battery watcher
                float currentCharge = 0;
                float maxCharge = 0;
                foreach (IMyBatteryBlock battery in batteries)
                {
                    currentCharge += battery.CurrentStoredPower;
                    maxCharge += battery.MaxStoredPower;
                }
                float charge = currentCharge / maxCharge;
                foreach (IMyFunctionalBlock engine in generators)
                {
                    engine.Enabled = (engine.Enabled || charge < 0.5f) && charge < 0.8f;
                }

                // door watcher
                foreach (IMyDoor door in watchDoors)
                {
                    if (door.Status != DoorStatus.Opening)
                    {
                        door.CloseDoor();
                    }
                }
            }
        }
    }
}
