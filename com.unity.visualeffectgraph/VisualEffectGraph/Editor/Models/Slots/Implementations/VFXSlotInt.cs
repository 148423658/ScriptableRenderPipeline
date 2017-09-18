using System;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(int))]
    class VFXSlotInt : VFXSlot
    {
        sealed protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type)
                || type == typeof(uint)
                || type == typeof(float)
                || type == typeof(Vector2)
                || type == typeof(Vector3)
                || type == typeof(Vector4)
                || type == typeof(Color);
        }

        sealed protected override bool CanConvertFrom(VFXExpression expr)
        {
            return base.CanConvertFrom(expr) || CanConvertFrom(VFXExpression.TypeToType(expr.valueType));
        }

        sealed protected override VFXExpression ConvertExpression(VFXExpression expression)
        {
            if (expression.valueType == VFXValueType.kInt)
            {
                return expression;
            }

            if (expression.valueType == VFXValueType.kUint)
            {
                return new VFXExpressionCastUintToInt(expression);
            }

            if (expression.valueType == VFXValueType.kFloat)
            {
                return new VFXExpressionCastFloatToInt(expression);
            }

            if (expression.valueType == VFXValueType.kFloat2
                ||  expression.valueType == VFXValueType.kFloat3
                ||  expression.valueType == VFXValueType.kFloat4)
            {
                var floatExpression = new VFXExpressionExtractComponent(expression, 0);
                return new VFXExpressionCastFloatToInt(floatExpression);
            }

            throw new Exception("Unexpected type of expression " + expression);
        }

        sealed protected override VFXValue DefaultExpression()
        {
            return new VFXValue<int>(0, VFXValue.Mode.FoldableVariable);
        }
    }
}
