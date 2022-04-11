using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private readonly List<Airlock> airlocks;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            airlocks = new List<Airlock>
            {
                new Airlock()
                {
                    InnerDoors = new IMyDoor[] { GridTerminalSystem.GetBlockWithName("Inner Door") as IMyDoor },
                    OuterDoors = new IMyDoor[] { GridTerminalSystem.GetBlockWithName("Outer Door") as IMyDoor },
                    Vents = new IMyAirVent[] { GridTerminalSystem.GetBlockWithName("Vent") as IMyAirVent }
                }
            };

            airlocks[0].Init();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument == "open_inner")
            {
                airlocks[0].RequestOpenInner();
            }
            else if (argument == "open_outer")
            {
                airlocks[0].RequestOpenOuter();
            }
            else if (argument == "toggle")
            {
                airlocks[0].Toggle();
            }

            if (updateSource.HasFlag(UpdateType.Update100))
            {
                foreach (Airlock airlock in airlocks)
                {
                    airlock.Update();
                }
            }
        }
    }
}
