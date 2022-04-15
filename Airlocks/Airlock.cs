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
        public enum AirlockStatus
        {
            Draining,
            Filling,
            NotPressurized,
            Pressurized
        }

        public class Airlock
        {
            private const int DoorCountdown = 10;
            private int countdown;
            private List<IMyDoor> countdownDoors;

            public string Id { get; set; }
            public List<IMyDoor> InnerDoors { get; } = new List<IMyDoor>();
            public List<IMyDoor> OuterDoors { get; } = new List<IMyDoor>();
            public List<IMyAirVent> Vents { get; } = new List<IMyAirVent>();
            public AirlockStatus Status { get; private set; }
            public bool Valid => InnerDoors.Count > 0 && OuterDoors.Count > 0 && Vents.Count > 0;

            private bool IsNotPressurized => Vents.First().GetOxygenLevel() < 0.1f;
            private bool IsPressurized => Vents.First().GetOxygenLevel() > 0.9f;

            public void Init()
            {
                if (IsNotPressurized)
                {
                    Status = AirlockStatus.NotPressurized;
                }
                else if (IsPressurized)
                {
                    Status = AirlockStatus.Pressurized;
                }
            }

            public void RequestOpenInner()
            {
                if (Status == AirlockStatus.Pressurized)
                {
                    // if its safe just open
                    OpenDoor(InnerDoors);
                }
                else
                {
                    // otherwise change what we're doing
                    Status = AirlockStatus.Filling;
                }
            }

            public void RequestOpenOuter()
            {
                if (Status == AirlockStatus.NotPressurized)
                {
                    // if its safe just open
                    OpenDoor(OuterDoors);
                }
                else
                {
                    // otherwise change what we're doing
                    Status = AirlockStatus.Draining;
                }
            }

            public void Toggle()
            {
                switch (Status)
                {
                    case AirlockStatus.NotPressurized:
                        Status = AirlockStatus.Filling;
                        break;
                    case AirlockStatus.Pressurized:
                        Status = AirlockStatus.Draining;
                        break;
                }
            }

            public void Update()
            {
                SetEnabled(InnerDoors, IsPressurized);
                SetEnabled(OuterDoors, IsNotPressurized);

                if (countdown > 0)
                {
                    countdown--;
                    if (countdown <= 0)
                    {
                        CloseDoor(countdownDoors);
                    }
                }
                
                switch (Status)
                {
                    case AirlockStatus.Draining:
                    case AirlockStatus.Filling:
                        // make sure both doors are closed before changing pressure
                        if (Closed(InnerDoors) && Closed(OuterDoors))
                        {
                            // one frame of both doors locked to prevent mistakes
                            SetEnabled(InnerDoors, false);
                            SetEnabled(OuterDoors, false);
                            SetDepressurize(Status == AirlockStatus.Draining);
                        }
                        else
                        {
                            CloseDoor(InnerDoors);
                            CloseDoor(OuterDoors);
                        }

                        if (Status == AirlockStatus.Draining)
                        {
                            if (IsNotPressurized)
                            {
                                Status = AirlockStatus.NotPressurized;
                                OpenDoor(OuterDoors);
                            }
                        }
                        else
                        {
                            if (IsPressurized)
                            {
                                Status = AirlockStatus.Pressurized;
                                OpenDoor(InnerDoors);
                            }
                        }
                        break;
                }
            }

            private bool Closed(List<IMyDoor> doors)
            {
                return doors.All(d => d.Status == DoorStatus.Closed);
            }

            private void CloseDoor(List<IMyDoor> doors)
            {
                if (countdownDoors == doors)
                {
                    countdown = 0;
                }

                foreach (IMyDoor door in doors)
                {
                    door.Enabled = true;
                    door.CloseDoor();
                }
            }

            private void OpenDoor(List<IMyDoor> doors)
            {
                countdown = DoorCountdown;
                countdownDoors = doors;
                foreach (IMyDoor door in doors)
                {
                    door.Enabled = true;
                    door.OpenDoor();
                }
            }

            private void SetDepressurize(bool depressurize)
            {
                foreach (IMyAirVent vent in Vents)
                {
                    vent.Depressurize = depressurize;
                }
            }

            private void SetEnabled<T>(List<T> blocks, bool enabled) where T: IMyFunctionalBlock
            {
                foreach (T block in blocks)
                {
                    block.Enabled = enabled;
                }
            }
        }
    }
}
