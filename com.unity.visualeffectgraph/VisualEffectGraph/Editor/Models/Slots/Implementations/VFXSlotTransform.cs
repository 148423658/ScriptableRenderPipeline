using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Transform))]
    class VFXSlotTransform : VFXSlot
    {
        protected override VFXValue DefaultExpression()
        {
            return new VFXValue<Matrix4x4>(Matrix4x4.identity, VFXValue.Mode.FoldableVariable);
        }

        protected override bool CanConvertFrom(Type type)
        {
            return type == typeof(Matrix4x4);
        }

        protected override VFXExpression ExpressionFromChildren(VFXExpression[] expr)
        {
            return new VFXExpressionTRSToMatrix(expr);
        }

        protected override VFXExpression[] ExpressionToChildren(VFXExpression expr)
        {
            // TODO - decompose matrix into TRS for display in the GUI
            return new VFXExpression[3]
            {
                VFXValue.Constant(Vector3.zero),
                VFXValue.Constant(Vector3.zero),
                VFXValue.Constant(Vector3.zero)
            };
        }
    }
}
