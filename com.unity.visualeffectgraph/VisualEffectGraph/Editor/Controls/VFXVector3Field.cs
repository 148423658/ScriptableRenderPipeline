using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;

using FloatField = UnityEditor.VFX.UIElements.VFXLabeledField<UnityEditor.VFX.UIElements.VFXFloatField, float>;
namespace UnityEditor.VFX.UIElements
{
    class VFXVector3Field : VFXControl<Vector3>
    {
        FloatField m_X;
        FloatField m_Y;
        FloatField m_Z;
        void CreateTextField()
        {
            m_X = new FloatField("X");
            m_Y = new FloatField("Y");
            m_Z = new FloatField("Z");

            m_X.label.AddToClassList("first");
            m_X.control.AddToClassList("fieldContainer");
            m_Y.control.AddToClassList("fieldContainer");
            m_Z.control.AddToClassList("fieldContainer");
            m_X.AddToClassList("fieldContainer");
            m_Y.AddToClassList("fieldContainer");
            m_Z.AddToClassList("fieldContainer");

            m_X.RegisterCallback<ChangeEvent<float>>(OnXValueChanged);
            m_Y.RegisterCallback<ChangeEvent<float>>(OnYValueChanged);
            m_Z.RegisterCallback<ChangeEvent<float>>(OnZValueChanged);
        }

        void OnXValueChanged(ChangeEvent<float> e)
        {
            Vector3 newValue = value;
            newValue.x = (float)m_X.value;
            SetValueAndNotify(newValue);
        }

        void OnYValueChanged(ChangeEvent<float> e)
        {
            Vector3 newValue = value;
            newValue.y = (float)m_Y.value;
            SetValueAndNotify(newValue);
        }

        void OnZValueChanged(ChangeEvent<float> e)
        {
            Vector3 newValue = value;
            newValue.z = (float)m_Z.value;
            SetValueAndNotify(newValue);
        }

        public VFXVector3Field()
        {
            CreateTextField();

            style.flexDirection = FlexDirection.Row;
            Add(m_X);
            Add(m_Y);
            Add(m_Z);
        }

        protected override void ValueToGUI()
        {
            if (!m_X.control.hasFocus)
                m_X.value = value.x;

            if (!m_Y.control.hasFocus)
                m_Y.value = value.y;

            if (!m_Z.control.hasFocus)
                m_Z.value = value.z;
        }
    }
}
