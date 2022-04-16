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
        private readonly Dictionary<string, Airlock> airlocks;
        private readonly MyCommandLine commandLine;

        public Program()
        {
            airlocks = new Dictionary<string, Airlock>();
            commandLine = new MyCommandLine();
            Coroutine.Runtime = Runtime;
            Coroutine.Start(Startup());
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if ((updateSource.HasFlag(UpdateType.Terminal) || updateSource.HasFlag(UpdateType.Trigger))
                && commandLine.TryParse(argument) && commandLine.ArgumentCount >= 2)
            {
                Airlock airlock;
                if (airlocks.TryGetValue(commandLine.Argument(1), out airlock))
                {
                    switch (commandLine.Argument(0))
                    {
                        case "OpenBarrier":
                            airlock.RequestOpenBarrer();
                            break;
                        case "OpenInner":
                            airlock.RequestOpenInner();
                            break;
                        case "OpenOuter":
                            airlock.RequestOpenOuter();
                            break;
                        case "Toggle":
                            airlock.Toggle();
                            break;
                    }
                }
            }

            Coroutine.Update(updateSource);
        }
    }
}
