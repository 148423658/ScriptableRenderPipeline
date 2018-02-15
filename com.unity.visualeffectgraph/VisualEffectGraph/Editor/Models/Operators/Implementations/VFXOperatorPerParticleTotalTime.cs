using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Time")]
    class VFXOperatorPerParticleTotalTime : VFXOperator
    {
        public class OutputProperties
        {
            public float t;
        }

        public override string name
        {
            get
            {
                return "Total Time (Per-Particle)";
            }
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression[] output = new VFXExpression[] {
                VFXBuiltInExpression.TotalTime + (VFXBuiltInExpression.DeltaTime * VFXOperatorUtility.FixedRandom(0xc43388e9, true)),
            };
            return output;
        }
    }
}
