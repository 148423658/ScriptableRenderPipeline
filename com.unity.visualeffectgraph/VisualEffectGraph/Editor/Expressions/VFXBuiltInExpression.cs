using System;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    sealed class VFXBuiltInExpression : VFXExpression
    {
        public static readonly VFXExpression TotalTime = new VFXBuiltInExpression(VFXExpressionOp.kVFXTotalTimeOp, VFXValueType.kFloat);
        public static readonly VFXExpression DeltaTime = new VFXBuiltInExpression(VFXExpressionOp.kVFXDeltaTimeOp, VFXValueType.kFloat);
        public static readonly VFXExpression SystemSeed = new VFXBuiltInExpression(VFXExpressionOp.kVFXSystemSeedOp, VFXValueType.kUint);

        private static readonly VFXExpression[] AllExpressions = VFXReflectionHelper.CollectStaticReadOnlyExpression<VFXExpression>(typeof(VFXBuiltInExpression));
        public static readonly VFXExpressionOp[] All = AllExpressions.Select(e => e.operation).ToArray();

        public static VFXExpression Find(VFXExpressionOp op)
        {
            var expression = AllExpressions.FirstOrDefault(e => e.operation == op);
            if (expression == null)
            {
                throw new InvalidOperationException(string.Format("Unable to find BuiltIn Parameter from op : {0}", op));
            }
            return expression;
        }

        private VFXExpressionOp m_Operation;
        private VFXValueType m_ValueType;

        private VFXBuiltInExpression(VFXExpressionOp op, VFXValueType valueType)
            : base(Flags.None)
        {
            m_Operation = op;
            m_ValueType = valueType;
        }

        public sealed override VFXExpressionOp operation
        {
            get
            {
                return m_Operation;
            }
        }

        public sealed override VFXValueType valueType
        {
            get
            {
                return m_ValueType;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is VFXBuiltInExpression))
                return false;

            var other = (VFXBuiltInExpression)obj;
            return valueType == other.valueType && operation == other.operation;
        }

        public override int GetHashCode()
        {
            return operation.GetHashCode();
        }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            return this;
        }
    }
}
