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
        public class Coroutine
        {
            private static readonly List<Coroutine> update1 = new List<Coroutine>();
            private static readonly List<Coroutine> update10 = new List<Coroutine>();
            private static readonly List<Coroutine> update100 = new List<Coroutine>();
            private readonly IEnumerator<bool> iter;

            public Coroutine(IEnumerator<bool> iter)
            {
                this.iter = iter;
            }

            public bool Cancel { get; set; }

            public UpdateFrequency UpdateFrequency { get; set; } = UpdateFrequency.Update1;

            public static IMyGridProgramRuntimeInfo Runtime { private get; set; }

            public static void Start(IEnumerator<bool> coroutine, UpdateFrequency updateFrequency = UpdateFrequency.Update1)
            {
                Start(new Coroutine(coroutine) { UpdateFrequency = updateFrequency });
            }

            public static void Start(Coroutine coroutine)
            {
                Runtime.UpdateFrequency |= coroutine.UpdateFrequency;
                if (coroutine.UpdateFrequency == UpdateFrequency.Update1)
                {
                    update1.Add(coroutine);
                }
                else if (coroutine.UpdateFrequency == UpdateFrequency.Update10)
                {
                    update10.Add(coroutine);
                }
                else if (coroutine.UpdateFrequency == UpdateFrequency.Update100)
                {
                    update100.Add(coroutine);
                }
                else
                {
                    coroutine.iter.Dispose();
                }
            }

            public static int Update(UpdateType updateSource)
            {
                if (updateSource.HasFlag(UpdateType.Update1) && !Update(update1))
                {
                    Runtime.UpdateFrequency &= ~UpdateFrequency.Update1;
                }

                if (updateSource.HasFlag(UpdateType.Update10) && !Update(update10))
                {
                    Runtime.UpdateFrequency &= ~UpdateFrequency.Update10;
                }

                if (updateSource.HasFlag(UpdateType.Update100) && !Update(update100))
                {
                    Runtime.UpdateFrequency &= ~UpdateFrequency.Update100;
                }

                return update1.Count + update10.Count + update100.Count;
            }

            private static bool Update(List<Coroutine> coroutines)
            {
                List<int> remove = new List<int>();
                for (int i = 0; i < coroutines.Count; i++)
                {
                    Coroutine cur = coroutines[i];
                    if (cur.Cancel || !cur.iter.MoveNext() || !cur.iter.Current)
                    {
                        cur.iter.Dispose();
                        remove.Add(i);
                    }
                }

                // helper function from SE
                coroutines.RemoveIndices(remove);
                return coroutines.Count > 0;
            }
        }
    }
}
