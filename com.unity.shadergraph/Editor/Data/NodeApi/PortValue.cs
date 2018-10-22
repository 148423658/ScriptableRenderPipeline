using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [StructLayout(LayoutKind.Explicit)]
    public struct PortValue
    {
        [FieldOffset(0)]
        PortValueType m_Type;

        [FieldOffset(4)]
        Vector4 m_Vector;

        PortValue(PortValueType type)
            : this()
        {
            m_Type = type;
        }

        public static PortValue Vector1(float value) => new PortValue(PortValueType.Vector1) { m_Vector = new Vector4(value, 0) };

        public static PortValue Vector2(Vector2 value) => new PortValue(PortValueType.Vector2) { m_Vector = value };

        public static PortValue Vector3() => new PortValue(PortValueType.Vector3) { m_Vector = UnityEngine.Vector3.zero };

        public static PortValue Vector3(Vector3 value) => new PortValue(PortValueType.Vector3) { m_Vector = value };

        public static PortValue Vector4(Vector4 value) => new PortValue(PortValueType.Vector4) { m_Vector = value };

        public float vector1Value => m_Vector.x;

        public Vector2 vector2Value => m_Vector;

        public Vector3 vector3Value => m_Vector;

        public Vector4 vector4Value => m_Vector;
    }
}
