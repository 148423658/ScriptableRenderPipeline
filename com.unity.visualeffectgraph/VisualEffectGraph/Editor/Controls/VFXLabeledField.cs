using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;
using System;

namespace UnityEditor.VFX.UIElements
{
    class VFXLabeledField<T, U> : VisualElement, INotifyValueChanged<U> where T : VisualElement, INotifyValueChanged<U>, new()
    {
        protected Label m_Label;
        protected T m_Control;

        public VisualElement m_IndeterminateLabel;

        public VFXLabeledField(Label existingLabel)
        {
            m_Label = existingLabel;

            CreateControl();
            SetupLabel();
        }

        bool m_Indeterminate;

        public bool indeterminate
        {
            get {return m_Control.parent == null; }

            set
            {
                if (m_Indeterminate != value)
                {
                    m_Indeterminate = value;
                    if (value)
                    {
                        m_Control.RemoveFromHierarchy();
                        Add(m_IndeterminateLabel);
                    }
                    else
                    {
                        m_IndeterminateLabel.RemoveFromHierarchy();
                        Add(m_Control);
                    }
                }
            }
        }

        public VFXLabeledField(string label)
        {
            if (!string.IsNullOrEmpty(label))
            {
                m_Label = new Label() { text = label };
                m_Label.AddToClassList("label");

                Add(m_Label);
            }
            style.flexDirection = FlexDirection.Row;

            CreateControl();
            SetupLabel();
        }

        void SetupLabel()
        {
            if (typeof(IValueField<U>).IsAssignableFrom(typeof(T)))
                if (typeof(U) == typeof(float))
                {
                    var dragger = new FieldMouseDragger<float>((IValueField<float>)m_Control);
                    dragger.SetDragZone(m_Label);
                }
                else if (typeof(U) == typeof(double))
                {
                    var dragger = new FieldMouseDragger<double>((IValueField<double>)m_Control);
                    dragger.SetDragZone(m_Label);
                }
                else if (typeof(U) == typeof(long))
                {
                    var dragger = new FieldMouseDragger<long>((IValueField<long> )m_Control);
                    dragger.SetDragZone(m_Label);
                }
                else if (typeof(U) == typeof(int))
                {
                    var dragger = new FieldMouseDragger<int>((IValueField<int> )m_Control);
                    dragger.SetDragZone(m_Label);
                }

            m_IndeterminateLabel = new Label()
            {
                name = "indeterminate",
                text = VFXControlConstants.indeterminateText
            };
            m_IndeterminateLabel.SetEnabled(false);
        }

        void CreateControl()
        {
            m_Control = new T();
            Add(m_Control);

            m_Control.RegisterCallback<ChangeEvent<U>>(OnControlChange);
        }

        void OnControlChange(ChangeEvent<U> e)
        {
            e.StopPropagation();
            using (ChangeEvent<U> evt = ChangeEvent<U>.GetPooled(e.previousValue, e.newValue))
            {
                evt.target = this;
                SendEvent(evt);
            }
        }

        public T control
        {
            get { return m_Control; }
        }

        public Label label
        {
            get { return m_Label; }
        }


        public void OnValueChanged(EventCallback<ChangeEvent<U>> callback)
        {
            (m_Control as INotifyValueChanged<U>).OnValueChanged(callback);
        }

        public void RemoveOnValueChanged(EventCallback<ChangeEvent<U>> callback)
        {
            (m_Control as INotifyValueChanged<U>).RemoveOnValueChanged(callback);
        }

        public void SetValueAndNotify(U newValue)
        {
            #pragma warning disable CS0618
            (m_Control as INotifyValueChanged<U>).SetValueAndNotify(newValue);
            #pragma warning restore CS0618
        }

        public void SetValueWithoutNotify(U newValue)
        {
            (m_Control as INotifyValueChanged<U>).SetValueWithoutNotify(newValue);
        }

        public U value
        {
            get { return m_Control.value; }
            set { m_Control.value = value; }
        }
    }

    abstract class ValueControl<T> : VisualElement
    {
        protected Label m_Label;

        protected ValueControl(Label existingLabel)
        {
            m_Label = existingLabel;
        }

        protected ValueControl(string label)
        {
            if (!string.IsNullOrEmpty(label))
            {
                m_Label = new Label() { text = label };
                m_Label.AddToClassList("label");

                Add(m_Label);
            }
            style.flexDirection = FlexDirection.Row;
        }

        public T GetValue()
        {
            return m_Value;
        }

        public void SetValue(T value)
        {
            m_Value = value;
            ValueToGUI(false);
        }

        public T value
        {
            get { return GetValue(); }
            set { SetValue(value); }
        }

        public void SetMultiplier(T multiplier)
        {
            m_Multiplier = multiplier;
        }

        protected T m_Value;
        protected T m_Multiplier;

        public System.Action OnValueChanged;

        protected abstract void ValueToGUI(bool force);
    }
}
