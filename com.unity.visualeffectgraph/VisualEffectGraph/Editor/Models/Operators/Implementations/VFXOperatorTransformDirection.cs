using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Geometry")]
    class VFXOperatorTransformDirection : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The transform.")]
            public Transform transform = new Transform();
            [Tooltip("The normalized vector to be transformed.")]
            public DirectionType direction = new DirectionType();
        }

        public class OutputProperties
        {
            public Vector3 tDir;
        }

        override public string name { get { return "Transform (Direction)"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { new VFXExpressionTransformDirection(inputExpression[0], inputExpression[1]) };
        }
    }
}
