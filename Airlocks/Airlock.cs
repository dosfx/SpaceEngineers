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
        public class Airlock
        {
            public const int DoorCountdown = 4;
            private Coroutine doorCoroutine;
            private int barrierCountdown;
            private int innerCountdown;
            private int outerCountdown;

            public string Id { get; set; }
            public List<IMyDoor> BarrierDoors { get; } = new List<IMyDoor>();
            public List<IMyDoor> InnerDoors { get; } = new List<IMyDoor>();
            public List<IMyDoor> OuterDoors { get; } = new List<IMyDoor>();
            public List<IMyAirVent> Vents { get; } = new List<IMyAirVent>();
            public List<IMyTextSurface> DisplaySurfaces { get; } = new List<IMyTextSurface>();
            public bool Valid => (BarrierDoors.Count > 0 || (InnerDoors.Count > 0 && OuterDoors.Count > 0)) && Vents.Count > 0;

            private bool IsEmpty => Vents.First().GetOxygenLevel() < 0.1f;
            private bool IsFilled => Vents.First().GetOxygenLevel() > 0.9f;
            private string AirlockStatus
            {
                get
                {
                    if (IsEmpty) return "Depressurized";
                    if (IsFilled) return "Pressurized";
                    return Vents.First().Depressurize ? "Depressurizing" : "Pressurizing"; 
                }
            }
            private bool BarrierSafe
            {
                get
                {
                    float min = 1.0f;
                    float max = 0.0f;
                    foreach (IMyAirVent vent in Vents)
                    {
                        float level = vent.GetOxygenLevel();
                        min = Math.Min(min, level);
                        max = Math.Max(max, level);
                    }

                    return max - min < 0.1f;
                }
            }

            public void Init()
            {
                // start the door watcher
                Coroutine.Start(DoorWatcher(), UpdateFrequency.Update100);
            }

            public string StatusString()
            {
                if (BarrierDoors.Count > 0)
                {
                    return $"Barrier {Id}: {(BarrierSafe ? "Safe" : "Unsafe")}";
                }
                return $"Airlock {Id}: {AirlockStatus}";
            }

            public void RequestOpenBarrer()
            {
                if (BarrierSafe)
                {
                    foreach (IMyDoor door in BarrierDoors)
                    {
                        door.OpenDoor();
                    }
                }
            }

            public void RequestOpenInner()
            {
                if (InnerDoors.Count == 0) return;

                if (IsFilled)
                {
                    // if its safe just open
                    OpenDoors(InnerDoors);
                }
                else
                {
                    // start a coroutine to open the door carefully
                    StartDoorCoroutine(OpenInnerCoroutine());
                }
            }

            public void RequestOpenOuter()
            {
                if (OuterDoors.Count == 0) return;
                if (IsEmpty)
                {
                    // if its safe just open
                    OpenDoors(OuterDoors);
                }
                else
                {
                    // start a coroutine to open the door carefully
                    StartDoorCoroutine(OpenOuterCoroutine());
                }
            }

            public void Toggle()
            {
                if (IsEmpty)
                {
                    RequestOpenInner();
                }
                else if (IsFilled)
                {
                    RequestOpenOuter();
                }
            }

            private void StartDoorCoroutine(IEnumerator<bool> iter)
            {
                // cancel the current op if running
                if (doorCoroutine != null)
                {
                    doorCoroutine.Cancel = true;
                }

                // make a new one to start
                doorCoroutine = new Coroutine(iter);
                Coroutine.Start(doorCoroutine);
            }

            private IEnumerator<bool> OpenInnerCoroutine()
            {
                return DoorCoroutine(InnerDoors, false, () => IsFilled);
            }

            private IEnumerator<bool> OpenOuterCoroutine()
            {
                return DoorCoroutine(OuterDoors, true, () => IsEmpty);
            }

            private IEnumerator<bool> DoorCoroutine(IEnumerable<IMyDoor> doorsToOpen, bool depressurize, Func<bool> completed)
            {
                // close the doors and set to pressurize
                CloseDoors(InnerDoors);
                CloseDoors(OuterDoors);
                UpdateDisplay(depressurize ? "Depressurizing" : "Pressurizing");

                // wait until the doors are all closed
                while (!Closed(InnerDoors) || !Closed(OuterDoors))
                {
                    yield return true;
                }

                // wait a tick
                yield return true;

                // change pressure
                SetDepressurize(depressurize);

                // clear the watch countdowns
                innerCountdown = outerCountdown = 0;

                // turn the doors off
                SetEnabled(InnerDoors, false);
                SetEnabled(OuterDoors, false);

                // wait until we're pressurized
                while (!completed())
                {
                    yield return true;
                }

                UpdateDisplay();

                // open the requested doors
                OpenDoors(doorsToOpen);
                yield return false;
            }

            private IEnumerator<bool> DoorWatcher()
            {
                while (true)
                {
                    CheckCloseDoors(BarrierDoors, ref barrierCountdown);
                    CheckCloseDoors(InnerDoors, ref innerCountdown);
                    CheckCloseDoors(OuterDoors, ref outerCountdown);
                    yield return true;
                }
            }

            private void CheckCloseDoors(IEnumerable<IMyDoor> doors, ref int doorCountdown)
            {
                if (doorCountdown > 0)
                {
                    doorCountdown--;
                    if (doorCountdown <= 0)
                    {
                        CloseDoors(doors);
                    }
                }
                else if (!Closed(doors))
                {
                    doorCountdown = DoorCountdown;
                }
            }

            private void UpdateDisplay()
            {
                UpdateDisplay(AirlockStatus);
            }

            private void UpdateDisplay(string status)
            {
                foreach (IMyTextSurface surface in DisplaySurfaces)
                {
                    surface.WriteText(status);
                }
            }

            private bool Closed(IEnumerable<IMyDoor> doors)
            {
                return doors.All(d => d.Status == DoorStatus.Closed);
            }

            private void CloseDoors(IEnumerable<IMyDoor> doors)
            {
                foreach (IMyDoor door in doors)
                {
                    if (door.Status == DoorStatus.Open || door.Status == DoorStatus.Opening)
                    {
                        door.Enabled = true;
                        door.CloseDoor();
                    }
                }
            }

            private void OpenDoors(IEnumerable<IMyDoor> doors)
            {
                foreach (IMyDoor door in doors)
                {
                    if (door.Status == DoorStatus.Closed || door.Status == DoorStatus.Closing)
                    {
                        door.Enabled = true;
                        door.OpenDoor();
                    }
                }
            }

            private void SetDepressurize(bool depressurize)
            {
                foreach (IMyAirVent vent in Vents)
                {
                    vent.Depressurize = depressurize;
                }
            }

            private void SetEnabled(IEnumerable<IMyFunctionalBlock> blocks, bool enabled)
            {
                foreach (IMyFunctionalBlock block in blocks)
                {
                    block.Enabled = enabled;
                }
            }
        }
    }
}
