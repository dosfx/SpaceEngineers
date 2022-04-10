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
    partial class Program : MyGridProgram
    {
        private const double SpaceElevation = 45000;

        private bool assentAssist;
        private bool decentAssist;

        private IMyCockpit cockpit;
        private List<IMyThrust> upThrust;

        private readonly float maxThrust;

        public Program()
        {
            cockpit = GridTerminalSystem.GetBlockWithName("Bumblebee - Cockpit") as IMyCockpit;

            upThrust = new List<IMyThrust>();
            GridTerminalSystem.GetBlockGroupWithName("Bumblebee - Up Thrust")?.GetBlocksOfType(upThrust);
            maxThrust = upThrust.Sum(t => t.MaxThrust);
            
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            assentAssist = true;
        }

        public void Save() { }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource.HasFlag(UpdateType.Terminal))
            {
                switch (argument)
                {
                    case "AssentAssist":
                        assentAssist = !assentAssist;
                        decentAssist = false;
                        break;
                    case "DecentAssist":
                        assentAssist = false;
                        decentAssist = !decentAssist;
                        break;
                }
            }

            // see if we need to run regular updates
            Runtime.UpdateFrequency = assentAssist || decentAssist ? UpdateFrequency.Update10 : UpdateFrequency.None;

            if (updateSource.HasFlag(UpdateType.Update10))
            {
                if (assentAssist)
                {
                    // vf ^ 2 = vi ^ 2 + (2.a.d)
                    // 0 = ss + (2.a.d)
                    // -ss = 2.a.d
                    // a = -ss / 2d

                    double elevation;
                    if (!cockpit.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out elevation))
                    {
                        // exit in some way
                        Echo("SPACE");
                    }
                    else
                    {
                        Echo($"{elevation}");
                        // read the situation
                        Vector3 grav = cockpit.GetNaturalGravity();
                        Vector3 velocity = cockpit.GetShipVelocities().LinearVelocity;
                        double mass = cockpit.CalculateShipMass().TotalMass;

                        // distance till we get to space, in meters
                        double spaceDistance = SpaceElevation - elevation;
                        // normalize converts the vector and returns the old length, handy!
                        double g = grav.Normalize();                        
                        // calculate the up velocity in m/s relative to gravity
                        double velocityUp = -Vector3.Dot(grav, cockpit.GetShipVelocities().LinearVelocity);
                        // acceleration needed to stop in space
                        double accel = -Math.Pow(velocityUp, 2) / (2 * spaceDistance);
                        // subtract g to get accel the rockets need to apply
                        accel -= g;
                        // thrust force to achieve accel
                        double force = mass * accel;
                        float thrusterStr = (float)Math.Abs(force / maxThrust);
                        foreach (IMyThrust thrust in upThrust)
                        {
                            thrust.ThrustOverridePercentage = thrusterStr;
                        }

                        StringBuilder builder = new StringBuilder();
                        builder.AppendLine($"VUp: {(velocityUp):0.000}");
                        builder.AppendLine($"Distance: {spaceDistance:0.000}");
                        builder.AppendLine($"Acceleration: {accel + g:0.000}");
                        builder.AppendLine($"G: {g}");
                        builder.AppendLine($"Mass: {mass}");
                        builder.AppendLine($"Thrust: {thrusterStr * 100:000.000}%");
                        cockpit.GetSurface(0).WriteText(builder);
                    }

                    //MyShipMass mass = cockpit.CalculateShipMass();
                    //Echo($"Mass: {mass.TotalMass}");
                    //Echo($"Gravity: {cockpit.GetNaturalGravity()}");
                    //Echo($"Speed: {cockpit.GetShipSpeed()}");                    

                    // Up vector of the ship
                    // don't need this right now, just remember this is how to get the directory of the ship
                    // Vector3 up = cockpit.WorldMatrix.Up;
                }
                else if (decentAssist)
                {

                }
                else
                {

                }
            }
        }
    }
}
