using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Geometry")]
    class VFXOperatorAABoxVolume : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The box used for the volume calculation.")]
            public AABox box = new AABox();
        }

        public class OutputProperties
        {
            [Tooltip("The volume of the box.")]
            public float volume;
        }

        override public string name { get { return "Volume (Axis Aligned Box)"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { VFXOperatorUtility.BoxVolume(inputExpression[1]) };
        }
    }
}
