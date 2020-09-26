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
    partial class Hangar : MyGridProgram
    {
        private readonly IMyDoor airlockOuterDoor;
        private readonly IMyDoor airlockInnerDoor;
        private readonly IMyAirVent airlockVent;
        private readonly IMyGasTank oxygenTank;
        private readonly IMyShipConnector connector;
        private readonly List<IMyAirtightHangarDoor> hangarDoors;
        private readonly List<IMyAirVent> vents;

        private bool requestOpenHangarDoors;
        private bool requestToggleAirlock;

        public Hangar()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            airlockOuterDoor = GridTerminalSystem.GetBlockWithName("Hangar Airlock Outer Door") as IMyDoor;
            airlockInnerDoor = GridTerminalSystem.GetBlockWithName("Hangar Airlock Inner Door") as IMyDoor;
            airlockVent = GridTerminalSystem.GetBlockWithName("Hangar Airlock Vent") as IMyAirVent;
            oxygenTank = GridTerminalSystem.GetBlockWithName("Hangar Oxygen Tank") as IMyGasTank;
            connector = GridTerminalSystem.GetBlockWithName("Hangar Connector") as IMyShipConnector;
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
                // clear memory
                requestOpenHangarDoors = false;
                requestToggleAirlock = false;
                switch (argument)
                {
                    case "ToggleDoors":
                        if (HangarDoorsClosedOrClosing)
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
                        ToggleAirlock();
                        break;
                }
            }

            if (updateSource.HasFlag(UpdateType.Update100))
            {
                // oxLevel as percent 0.0 to 1.0 
                float hangarOxLevel = vents[0].GetOxygenLevel();
                float airlockOxLevel = airlockVent.GetOxygenLevel();

                // lock/unlock doors based on safety
                airlockOuterDoor.Enabled = Math.Abs(hangarOxLevel - airlockOxLevel) < 0.1f;
                airlockInnerDoor.Enabled = airlockOxLevel >= 0.9f;
                foreach (IMyAirtightHangarDoor door in hangarDoors)
                {
                    door.Enabled = CanHangarDoorsOpen;
                }

                // refill Hangar Oxygen tank                 
                if (oxygenTank.FilledRatio < 0.4 - (0.3 * hangarOxLevel))
                {
                    oxygenTank.Stockpile = true;
                    connector.Connect();
                }
                else
                {
                    oxygenTank.Stockpile = false;
                    connector.Disconnect();
                }


                // resume an action
                if (requestOpenHangarDoors)
                {
                    OpenHangarDoors();
                }

                if (requestToggleAirlock)
                {
                    ToggleAirlock();
                }
            }
        }

        private void OpenHangarDoors()
        {
            // check conditions
            if (CanHangarDoorsOpen)
            {
                // safe, clear the request
                requestOpenHangarDoors = false;

                // take action
                foreach (IMyAirtightHangarDoor door in hangarDoors)
                {
                    door.OpenDoor();
                }
            }
            else
            {
                // take action to make safe
                DrainHangar();

                // remember what we were trying to do
                requestOpenHangarDoors = true;
            }
        }

        private void CloseHangarDoors()
        {
            // no conditions, just do it
            foreach (IMyAirtightHangarDoor door in hangarDoors)
            {
                door.CloseDoor();
            }
        }

        private void FillHangar()
        {
            // Close the doors too to hold in the pressure
            CloseHangarDoors();

            // take action
            foreach (IMyAirVent vent in vents)
            {
                vent.Depressurize = false;
            }
        }

        private void DrainHangar()
        {
            // no conditions
            foreach (IMyAirVent vent in vents)
            {
                vent.Depressurize = true;
            }
        }

        private void ToggleAirlock()
        {
            // make sure both doors are closed before changing pressure
            if (airlockInnerDoor.Status == DoorStatus.Closed &&
                airlockOuterDoor.Status == DoorStatus.Closed)
            {
                // one frame of both doors locked to prevent mistakes
                airlockInnerDoor.Enabled = false;
                airlockOuterDoor.Enabled = false;
                airlockVent.Depressurize = !airlockVent.Depressurize;
                requestToggleAirlock = false;
            }
            else
            {
                // close the doors and wait
                airlockInnerDoor.CloseDoor();
                airlockOuterDoor.CloseDoor();
                requestToggleAirlock = true;
            }
        }

        private bool CanHangarDoorsOpen
        {
            get
            {
                // in case the tank is full and the hangar can't drain any more
                return vents[0].GetOxygenLevel() == 0.0f || oxygenTank.FilledRatio == 1.0;
            }
        }

        private bool HangarDoorsClosedOrClosing
        {
            get
            {
                // just use the first to infer all of them
                return hangarDoors[0].Status == DoorStatus.Closed || hangarDoors[0].Status == DoorStatus.Closing;
            }
        }

        private bool HangarDepressurize
        {
            get
            {
                // just use the first to infer all of them
                return vents[0].Depressurize;
            }
        }
    }
}
