using System;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Spawner")]
    class VFXSpawnerPeriodicBurst : VFXAbstractSpawner
    {
        public override string name { get { return "PeriodicBurst"; } }
        public override VFXTaskType spawnerType { get { return VFXTaskType.kSpawnerPeriodicBurst; } }
        public class InputProperties
        {
            public Vector2 nb = new Vector2(0, 10);
            public Vector2 period = new Vector2(0, 1);
        }
    }
}
