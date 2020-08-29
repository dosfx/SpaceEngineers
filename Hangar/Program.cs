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
using System.Runtime.CompilerServices;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private readonly IMyDoor airlockOuterDoor;
        private readonly IMyGasTank oxygenTank;
        private readonly List<IMyAirtightHangarDoor> hangarDoors;
        private readonly List<IMyAirVent> vents;

        private bool requestOpenHangarDoors;
        private bool requestFillHangar;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            airlockOuterDoor = GridTerminalSystem.GetBlockWithName("Hangar Airlock Outer Door") as IMyDoor;
            oxygenTank = GridTerminalSystem.GetBlockWithName("Hangar Oxygen Tank") as IMyGasTank;
            hangarDoors = new List<IMyAirtightHangarDoor>();
            GridTerminalSystem.GetBlockGroupWithName("Hangar Doors")?.GetBlocksOfType(hangarDoors);
            vents = new List<IMyAirVent>();
            GridTerminalSystem.GetBlockGroupWithName("Hangar Vents")?.GetBlocksOfType(vents);
        }

        public void Save() { }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource.HasFlag(UpdateType.Terminal) ||
                updateSource.HasFlag(UpdateType.Trigger) ||
                updateSource.HasFlag(UpdateType.IGC))
            {
                switch (argument)
                {
                    case "ToggleDoors":
                        if (hangarDoors[0].Status == DoorStatus.Closed ||
                            hangarDoors[0].Status == DoorStatus.Closing)
                        {
                            OpenHangarDoors();
                        }
                        else
                        {
                            CloseHangarDoors();
                        }
                        break;
                    case "ToggleHangar":
                        if (HangarDepressurize)
                        {
                            FillHangar();
                        }
                        else
                        {
                            DrainHangar();
                        }
                        break;
                    case "ToggleAirlock":
                        break;
                }
            }

            if (updateSource.HasFlag(UpdateType.Update100))
            {
                // oxLevel as percent 0.0 to 1.0 
                float oxLevel = vents[0].GetOxygenLevel();
                airlockOuterDoor.Enabled = oxLevel >= 0.9f;
                HangarDoorsEnabled = CanHangarDoorsOpen;

                Echo($"CanHangarDoorsOpen: {CanHangarDoorsOpen}");
                Echo($"requestOpenHangarDoors: {requestOpenHangarDoors}");
                Echo($"requestFillHangar: {requestFillHangar}");

                if (requestOpenHangarDoors)
                {
                    OpenHangarDoors();
                }

                if (requestFillHangar)
                {
                    FillHangar();
                }
            }
        }

        private void OpenHangarDoors()
        {
            // in case we can't
            requestOpenHangarDoors = true;

            // check conditions
            if (CanHangarDoorsOpen)
            {
                // safe, clear the request
                requestOpenHangarDoors = false;
                // take action
                HangarDoorsClosed = false;
            }
            else
            {
                // take action to make safe
                DrainHangar();
            }
        }

        private void CloseHangarDoors()
        {
            // no conditions, just do it
            // take action
            HangarDoorsClosed = true;
        }

        private void FillHangar()
        {
            // in case we can't
            requestFillHangar = true;

            // check conditions
            if (HangarDoorsClosed)
            {
                // safe, clear the request
                requestFillHangar = false;
                // take action
                HangarDepressurize = false;
            }
            else
            {
                // take action to make safe
                CloseHangarDoors();
            }
        }

        private void DrainHangar()
        {
            // no conditions
            // take action
            HangarDepressurize = true;
        }

        private bool CanHangarDoorsOpen
        {
            get
            {
                return vents[0].GetOxygenLevel() == 0.0f || oxygenTank.FilledRatio == 1.0; // in case the tank is full and the hangar can't drain any more
            }
        }

        private bool HangarDoorsClosed
        {
            get
            {
                return hangarDoors.All(d => d.Status == DoorStatus.Closed);
            }
            set
            {
                foreach (IMyAirtightHangarDoor door in hangarDoors)
                {
                    if (value)
                    {
                        door.CloseDoor();
                    }
                    else
                    {
                        door.OpenDoor();
                    }
                }
            }
        }

        private bool HangarDoorsEnabled
        {
            get
            {
                return hangarDoors[0].Enabled;
            }
            set
            {
                foreach (IMyAirtightHangarDoor door in hangarDoors)
                {
                    door.Enabled = value;
                }
            }
        }

        private bool HangarDepressurize
        {
            get
            {
                return vents[0].Depressurize;
            }
            set
            {
                foreach (IMyAirVent vent in vents)
                {
                    vent.Depressurize = value;
                }
            }
        }
    }
}
