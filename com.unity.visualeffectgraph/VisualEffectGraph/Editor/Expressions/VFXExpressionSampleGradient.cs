using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionSampleGradient : VFXExpression
    {
        public VFXExpressionSampleGradient() : this(VFXValue<Gradient>.Default, VFXValue<float>.Default)
        {
        }

        public VFXExpressionSampleGradient(VFXExpression gradient, VFXExpression time)
            : base(Flags.None, new VFXExpression[2] { gradient, time })
        {}

        sealed public override VFXExpressionOp Operation { get { return VFXExpressionOp.kVFXSampleGradient; } }
        sealed public override VFXValueType ValueType { get { return VFXValueType.kFloat4; } }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var timeReduce = constParents[1];
            var gradientReduce = constParents[0];

            var gradient = gradientReduce.Get<Gradient>();
            var time = timeReduce.Get<float>();
            return VFXValue<Vector4>.Constant(gradient.Evaluate(time));
        }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("sampleGradient({0},{1})", parents[0], parents[1]);
        }
    }
}
