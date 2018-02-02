using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;
using UnityEditor.VFX;
using UnityEditor.VFX.UIElements;
using Object = UnityEngine.Object;
using Type = System.Type;
using EnumField = UnityEditor.VFX.UIElements.VFXEnumField;
using VFXVector2Field = UnityEditor.VFX.UIElements.VFXVector2Field;
using VFXVector4Field = UnityEditor.VFX.UIElements.VFXVector4Field;
using FloatField = UnityEditor.VFX.UIElements.VFXFloatField;

namespace UnityEditor.VFX.UI
{
    abstract class NumericPropertyRM<T, U> : SimpleUIPropertyRM<T, U>
    {
        public NumericPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            return 60;
        }

        protected virtual bool RangeShouldCreateSlider(Vector2 range)
        {
            return range != Vector2.zero && range.y != Mathf.Infinity;
        }

        VFXBaseSliderField<U> m_Slider;
        TextValueField<U> m_TextField;

        protected abstract INotifyValueChanged<U> CreateSimpleField(out TextValueField<U> textField);
        protected abstract INotifyValueChanged<U> CreateSliderField(out VFXBaseSliderField<U> slider);

        public override INotifyValueChanged<U> CreateField()
        {
            Vector2 range = VFXPropertyAttribute.FindRange(m_Provider.attributes);
            INotifyValueChanged<U> result;
            if (!RangeShouldCreateSlider(range))
            {
                result = CreateSimpleField(out m_TextField);
                m_TextField.RegisterCallback<BlurEvent>(OnFocusLost);
            }
            else
            {
                result = CreateSliderField(out m_Slider);
                m_Slider.RegisterCallback<BlurEvent>(OnFocusLost);
                m_Slider.range = range;
            }
            return result;
        }

        void OnFocusLost(BlurEvent e)
        {
            UpdateGUI();
        }

        protected override bool HasFocus()
        {
            if (m_Slider != null)
                return m_Slider.hasFocus;
            return m_TextField.hasFocus;
        }

        public override bool IsCompatible(IPropertyRMProvider provider)
        {
            if (!base.IsCompatible(provider)) return false;

            Vector2 range = VFXPropertyAttribute.FindRange(m_Provider.attributes);

            return RangeShouldCreateSlider(range) != (m_Slider == null);
        }

        public override void UpdateGUI()
        {
            if (m_Slider != null)
            {
                Vector2 range = VFXPropertyAttribute.FindRange(m_Provider.attributes);

                m_Slider.range = range;
            }
            base.UpdateGUI();
        }

        public abstract T FilterValue(Vector2 range, T value);
        public override object FilterValue(object value)
        {
            Vector2 range = VFXPropertyAttribute.FindRange(m_Provider.attributes);

            if (RangeShouldCreateSlider(range))
            {
                value = FilterValue(range, (T)value);
            }

            return value;
        }
    }

    class UintPropertyRM : NumericPropertyRM<uint, long>
    {
        public UintPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        protected override bool RangeShouldCreateSlider(Vector2 range)
        {
            return base.RangeShouldCreateSlider(range) && (uint)range.x < (uint)range.y;
        }

        protected override INotifyValueChanged<long> CreateSimpleField(out TextValueField<long> textField)
        {
            var field =  new VFXLabeledField<IntegerField, long>(m_Label);
            textField = field.control;
            return field;
        }

        protected override INotifyValueChanged<long> CreateSliderField(out VFXBaseSliderField<long> slider)
        {
            var field = new VFXLabeledField<VFXIntSliderField, long>(m_Label);
            slider = field.control;
            return field;
        }

        public override uint FilterValue(Vector2 range, uint value)
        {
            uint val = value;
            if (range.x > val)
            {
                val = (uint)range.x;
            }
            if (range.y < val)
            {
                val = (uint)range.y;
            }

            return val;
        }
    }

    class IntPropertyRM : NumericPropertyRM<int, long>
    {
        public IntPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        protected override bool RangeShouldCreateSlider(Vector2 range)
        {
            return base.RangeShouldCreateSlider(range) && (int)range.x < (int)range.y;
        }

        protected override INotifyValueChanged<long> CreateSimpleField(out TextValueField<long> textField)
        {
            var field = new VFXLabeledField<IntegerField, long>(m_Label);
            textField = field.control;
            return field;
        }

        protected override INotifyValueChanged<long> CreateSliderField(out VFXBaseSliderField<long> slider)
        {
            var field = new VFXLabeledField<VFXIntSliderField, long>(m_Label);
            slider = field.control;
            return field;
        }

        public override int FilterValue(Vector2 range, int value)
        {
            int val = value;
            if (range.x > val)
            {
                val = (int)range.x;
            }
            if (range.y < val)
            {
                val = (int)range.y;
            }

            return val;
        }
    }

    class FloatPropertyRM : NumericPropertyRM<float, float>
    {
        public FloatPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        protected override bool RangeShouldCreateSlider(Vector2 range)
        {
            return base.RangeShouldCreateSlider(range) && range.x < range.y;
        }

        protected override INotifyValueChanged<float> CreateSimpleField(out TextValueField<float> textField)
        {
            var field = new VFXLabeledField<VFXFloatField, float>(m_Label);
            textField = field.control;
            return field;
        }

        protected override INotifyValueChanged<float> CreateSliderField(out VFXBaseSliderField<float> slider)
        {
            var field = new VFXLabeledField<VFXFloatSliderField, float>(m_Label);
            slider = field.control;
            return field;
        }

        public override float FilterValue(Vector2 range, float value)
        {
            float val = value;
            if (range.x > val)
            {
                val = range.x;
            }
            if (range.y < val)
            {
                val = range.y;
            }

            return val;
        }
    }
}
