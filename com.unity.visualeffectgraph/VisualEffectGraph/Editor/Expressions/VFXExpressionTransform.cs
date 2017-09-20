using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionTRSToMatrix : VFXExpression
    {
        public VFXExpressionTRSToMatrix() : this(new VFXExpression[] { VFXValue<Vector3>.Default, VFXValue<Vector3>.Default, VFXValue<Vector3>.Default }
                                                 )
        {
        }

        public VFXExpressionTRSToMatrix(VFXExpression[] parents) : base(VFXExpression.Flags.None, parents)
        {
        }

        public override VFXExpressionOp operation
        {
            get
            {
                return VFXExpressionOp.kVFXTRSToMatrixOp;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var posReduce = constParents[0];
            var rotReduce = constParents[1];
            var scaleReduce = constParents[2];

            var pos = posReduce.Get<Vector3>();
            var rot = rotReduce.Get<Vector3>();
            var scale = scaleReduce.Get<Vector3>();

            var quat = Quaternion.Euler(rot);

            Matrix4x4 matrix = new Matrix4x4();
            matrix.SetTRS(pos, quat, scale);

            return VFXValue.Constant(matrix);
        }
    }

    class VFXExpressionExtractPositionFromMatrix : VFXExpression
    {
        public VFXExpressionExtractPositionFromMatrix() : this(VFXValue<Matrix4x4>.Default)
        {
        }

        public VFXExpressionExtractPositionFromMatrix(VFXExpression parent) : base(VFXExpression.Flags.None, new VFXExpression[] { parent })
        {
        }

        public override VFXExpressionOp operation
        {
            get
            {
                return VFXExpressionOp.kVFXExtractPositionFromMatrixOp;
            }
        }

        public override VFXValueType valueType
        {
            get
            {
                return VFXValueType.kFloat3;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrixReduce = constParents[0];
            var matrix = matrixReduce.Get<Matrix4x4>();

            return VFXValue.Constant<Vector3>(matrix.GetColumn(3));
        }
    }

    class VFXExpressionExtractAnglesFromMatrix : VFXExpression
    {
        public VFXExpressionExtractAnglesFromMatrix() : this(VFXValue<Matrix4x4>.Default)
        {
        }

        public VFXExpressionExtractAnglesFromMatrix(VFXExpression parent) : base(VFXExpression.Flags.InvalidOnGPU, new VFXExpression[] { parent })
        {
        }

        public override VFXExpressionOp operation
        {
            get
            {
                return VFXExpressionOp.kVFXExtractAnglesFromMatrixOp;
            }
        }

        public override VFXValueType valueType
        {
            get
            {
                return VFXValueType.kFloat3;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrixReduce = constParents[0];
            var matrix = matrixReduce.Get<Matrix4x4>();

            return VFXValue.Constant(matrix.rotation.eulerAngles);
        }
    }

    class VFXExpressionExtractScaleFromMatrix : VFXExpression
    {
        public VFXExpressionExtractScaleFromMatrix() : this(VFXValue<Matrix4x4>.Default)
        {
        }

        public VFXExpressionExtractScaleFromMatrix(VFXExpression parent) : base(VFXExpression.Flags.InvalidOnGPU, new VFXExpression[] { parent })
        {
        }

        public override VFXExpressionOp operation
        {
            get
            {
                return VFXExpressionOp.kVFXExtractScaleFromMatrixOp;
            }
        }

        public override VFXValueType valueType
        {
            get
            {
                return VFXValueType.kFloat3;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrixReduce = constParents[0];
            var matrix = matrixReduce.Get<Matrix4x4>();

            return VFXValue.Constant(matrix.lossyScale);
        }
    }

    class VFXExpressionTransformPosition : VFXExpression
    {
        public VFXExpressionTransformPosition() : this(VFXValue<Matrix4x4>.Default, VFXValue<Vector3>.Default)
        {
        }

        public VFXExpressionTransformPosition(VFXExpression matrix, VFXExpression position) : base(VFXExpression.Flags.None, new VFXExpression[] { matrix, position })
        {
        }

        public override VFXExpressionOp operation
        {
            get
            {
                return VFXExpressionOp.kVFXTransformPosOp;
            }
        }

        public override VFXValueType valueType
        {
            get
            {
                return VFXValueType.kFloat3;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrixReduce = constParents[0];
            var positionReduce = constParents[1];

            var matrix = matrixReduce.Get<Matrix4x4>();
            var position = positionReduce.Get<Vector3>();

            return VFXValue.Constant(matrix.MultiplyPoint(position));
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("mul({0}, float4({1}, 1.0)).xyz", parents[0], parents[1]);
        }
    }

    class VFXExpressionTransformVector : VFXExpression
    {
        public VFXExpressionTransformVector() : this(VFXValue<Matrix4x4>.Default, VFXValue<Vector3>.Default)
        {
        }

        public VFXExpressionTransformVector(VFXExpression matrix, VFXExpression vector) : base(VFXExpression.Flags.None, new VFXExpression[] { matrix, vector })
        {
        }

        public override VFXExpressionOp operation
        {
            get
            {
                return VFXExpressionOp.kVFXTransformPosOp;
            }
        }

        public override VFXValueType valueType
        {
            get
            {
                return VFXValueType.kFloat3;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrixReduce = constParents[0];
            var positionReduce = constParents[1];

            var matrix = matrixReduce.Get<Matrix4x4>();
            var vector = positionReduce.Get<Vector3>();

            return VFXValue.Constant(matrix.MultiplyVector(vector));
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("mul((float3x3){0}, {1})", parents[0], parents[1]);
        }
    }

    class VFXExpressionTransformDirection : VFXExpression
    {
        public VFXExpressionTransformDirection() : this(VFXValue<Matrix4x4>.Default, VFXValue<Vector3>.Default)
        {
        }

        public VFXExpressionTransformDirection(VFXExpression matrix, VFXExpression vector) : base(VFXExpression.Flags.None, new VFXExpression[] { matrix, vector })
        {
        }

        public override VFXExpressionOp operation
        {
            get
            {
                return VFXExpressionOp.kVFXTransformDirOp;
            }
        }

        public override VFXValueType valueType
        {
            get
            {
                return VFXValueType.kFloat3;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrixReduce = constParents[0];
            var positionReduce = constParents[1];

            var matrix = matrixReduce.Get<Matrix4x4>();
            var vector = positionReduce.Get<Vector3>();

            return VFXValue.Constant(matrix.MultiplyVector(vector).normalized);
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("normalize(mul((float3x3){0}, {1}))", parents[0], parents[1]);
        }
    }
}
