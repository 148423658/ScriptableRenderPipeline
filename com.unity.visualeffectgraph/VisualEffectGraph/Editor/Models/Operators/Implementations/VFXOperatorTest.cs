using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorTest : VFXOperator
    {
        override public string name { get { return "Test"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] {};
        }

        public class InputProperties
        {
            public float aFloat = 123.456f;
            [Range(-32, 150)]
            public float aRange = 123.456f;
            //public Gradient aGradient = new Gradient();
            public Vector2 aVector2 = Vector2.left;
            public Vector3 aVector3 = Vector3.forward;
            public Vector4 aVector4 = Vector4.one;
            public Color aColor = Color.gray;
            //public Texture2D aTexture2D = null;
            //public Texture3D aTexture3D = null;
            public Sphere aSphere;
            public Vector aVector = new Vector { space = CoordinateSpace.Local, vector = Vector3.one };
            public Position aPosition = new Position { space = CoordinateSpace.Local, position = Vector3.forward };
            //public int anInt;
            //public uint anUint;
            //public FlipBook aFlipBook;
            //public bool aBool;
            //public AnimationCurve curve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(1, 1)});
            //public Mesh aMesh;
        }


        public enum RotateMode
        {
            Quaternion,
            Euler
        }

        [System.Serializable]
        public class Settings
        {
            public RotateMode mode;
            public bool rotateTwice;
        }
    }
}
