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
    partial class Program
    {
        public IEnumerator<bool> Startup()
        {
            // callback to check blocks for custom data
            Func<IMyTerminalBlock, bool> hasCustomData = b => !string.IsNullOrEmpty(b.CustomData);

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(blocks, hasCustomData);
            Echo("Scanning...");
            yield return true;

            // getting into the real work
            //int total = doors.Count + vents.Count;
            int total = blocks.Count;
            int progress = 0;
            MyIni ini = new MyIni();
            airlocks = new Dictionary<string, Airlock>();

            foreach (IMyTerminalBlock block in blocks)
            {
                // every block we care about should at least have Airlock > Id
                string id;
                if (ini.TryParse(block.CustomData) && ini.ContainsSection("Airlock") && ini.Get("Airlock", "Id").TryGetString(out id))
                {
                    // get or create Airlock obj
                    Airlock airlock;
                    if (!airlocks.TryGetValue(id, out airlock))
                    {
                        airlock = new Airlock() { Id = id };
                        airlocks.Add(id, airlock);
                    }

                    // is it a door?
                    string type;
                    IMyDoor door = block as IMyDoor;
                    if (door != null && ini.Get("Airlock", "Type").TryGetString(out type))
                    {
                        if (type == "Inner")
                        {
                            airlock.InnerDoors.Add(door);
                        }
                        else if (type == "Outer")
                        {
                            airlock.OuterDoors.Add(door);
                        }
                        else
                        {
                            // error?
                        }
                    }

                    // is it a vent?
                    IMyAirVent vent = block as IMyAirVent;
                    if (vent != null)
                    {
                        airlock.Vents.Add(vent);
                    }
                }

                // yield for the next round
                progress++;
                EchoProgress("Scanning...", progress, total);
                yield return true;
            }

            // go through and init the real ones and remove the bad ones
            foreach (string id in airlocks.Keys.ToArray())
            {
                if (airlocks[id].Valid)
                {
                    Echo($"{id} Valid");
                    airlocks[id].Init();
                }
                else
                {
                    Echo($"{id} Invalid");
                    airlocks.Remove(id);
                }

                yield return true;
            }

            // done
            yield return false;
        }

        private void EchoProgress(string message, float progress, float total)
        {
            // only update every tenth interation, == 1 so that it works on the first update
            if (progress % 10 == 1)
            {
                Echo($"{message} [{string.Join("", Enumerable.Repeat("|", (int)(10 * (progress / total)))),-10}]{@"\|/-"[(int)progress / 10 % 4]}");
            }
        }
    }
}
