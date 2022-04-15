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
        private Dictionary<string, Airlock> airlocks;
        private IEnumerator<bool> startup;
        private readonly MyCommandLine commandLine;

        public Program()
        {
            startup = Startup();
            commandLine = new MyCommandLine();
            Runtime.UpdateFrequency = UpdateFrequency.Once;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource.HasFlag(UpdateType.Once) && startup != null)
            {
                if (startup.MoveNext())
                {
                    Runtime.UpdateFrequency = UpdateFrequency.Once;
                }
                else
                {
                    Runtime.UpdateFrequency = UpdateFrequency.Update100;
                    startup.Dispose();
                    startup = null;
                }

                return;
            }

            if ((updateSource.HasFlag(UpdateType.Terminal) || updateSource.HasFlag(UpdateType.Trigger))
                && startup == null && commandLine.TryParse(argument))
            {
                Airlock airlock;
                if (airlocks.TryGetValue(commandLine.Argument(1), out airlock))
                {
                    switch (commandLine.Argument(0))
                    {
                        case "open_inner":
                            airlock.RequestOpenInner();
                            break;
                        case "open_outer":
                            airlock.RequestOpenOuter();
                            break;
                        case "toggle":
                            airlock.Toggle();
                            break;
                    }
                }
            }

            if (updateSource.HasFlag(UpdateType.Update100))
            {
                foreach (Airlock airlock in airlocks.Values)
                {
                    airlock.Update();
                }
            }
        }
    }
}
