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
        public class Bulkhead
        {
            private const int DoorCountdown = 10;
            private int countdown;

            public List<IMyDoor> Doors { get; } = new List<IMyDoor>();
            public List<IMyAirVent> Vents { get; } = new List<IMyAirVent>();
            public string Id { get; set; }
            public bool Valid => Doors.Count > 0 && Vents.Count > 0;

            public void RequestOpenDoors()
            {
                float min = 1.0f;
                float max = 0.0f;
                foreach (IMyAirVent vent in Vents)
                {
                    float level = vent.GetOxygenLevel();
                    min = Math.Min(min, level);
                    max = Math.Max(max, level);
                }

                if (max - min < 0.1f)
                {
                    foreach (IMyDoor door in Doors)
                    {
                        door.OpenDoor();
                    }
                }
            }

            public void Update()
            {
                if (countdown > 0)
                {
                    countdown--;
                    if (countdown <= 0)
                    {
                        foreach (IMyDoor door in Doors)
                        {
                            door.CloseDoor();
                        }
                    }
                }
                else
                {
                    if (Doors.Exists(d => d.Status == DoorStatus.Open))
                    {
                        countdown = DoorCountdown;
                    }
                }
            }
        }
    }
}
