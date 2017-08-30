using System;
using UnityEngine;
using UnityEngine.VFX;
namespace UnityEditor.VFX
{
    class VFXExpressionExtractComponent : VFXExpressionFloatOperation
    {
        public VFXExpressionExtractComponent() : this(VFXValue<Vector4>.Default, 0) {}

        public VFXExpressionExtractComponent(VFXExpression parent, int iChannel)
            : base(new VFXExpression[1] { parent })
        {
            if (parent.ValueType == VFXValueType.kFloat || !IsFloatValueType(parent.ValueType))
            {
                throw new ArgumentException("Incorrect VFXExpressionExtractComponent");
            }

            m_Operation = VFXExpressionOp.kVFXExtractComponentOp;
            m_AdditionalParameters = new int[] { TypeToSize(parent.ValueType), iChannel };
            m_ValueType = VFXValueType.kFloat;
        }

        public int Channel { get { return m_AdditionalParameters[1]; } }

        static private float GetChannel(Vector2 input, int iChannel)
        {
            switch (iChannel)
            {
                case 0: return input.x;
                case 1: return input.y;
            }
            Debug.LogError("Incorrect channel (Vector2)");
            return 0.0f;
        }

        static private float GetChannel(Vector3 input, int iChannel)
        {
            switch (iChannel)
            {
                case 0: return input.x;
                case 1: return input.y;
                case 2: return input.z;
            }
            Debug.LogError("Incorrect channel (Vector2)");
            return 0.0f;
        }

        static private float GetChannel(Vector4 input, int iChannel)
        {
            switch (iChannel)
            {
                case 0: return input.x;
                case 1: return input.y;
                case 2: return input.z;
                case 3: return input.w;
            }
            Debug.LogError("Incorrect channel (Vector2)");
            return 0.0f;
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] reducedParents)
        {
            float readValue = 0.0f;
            var parent = reducedParents[0];
            switch (reducedParents[0].ValueType)
            {
                case VFXValueType.kFloat: readValue = parent.Get<float>(); break;
                case VFXValueType.kFloat2: readValue = GetChannel(parent.Get<Vector2>(), Channel); break;
                case VFXValueType.kFloat3: readValue = GetChannel(parent.Get<Vector3>(), Channel); break;
                case VFXValueType.kFloat4: readValue = GetChannel(parent.Get<Vector4>(), Channel); break;
            }
            return VFXValue.Constant(readValue);
        }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var parent = reducedParents[0];
            if (parent is VFXExpressionCombine)
                return parent.Parents[Channel];
            else if (parent.ValueType == VFXValueType.kFloat && Channel == 0)
                return parent;
            else
                return base.Reduce(reducedParents);
        }

        sealed public override string GetCodeString(string[] parents)
        {
            return string.Format("{0}[{1}]", parents[0], AdditionalParameters[1]);
        }

        public sealed override void FillOperands(int[] data, VFXExpressionGraph graph)
        {
            data[0] = graph.GetFlattenedIndex(Parents[0]);
            data[1] = Channel;
            data[2] = VFXExpression.TypeToSize(Parents[0].ValueType);
        }
    }
}
