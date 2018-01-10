using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;
using UnityEditor.VFX;
using UnityEditor.VFX.UIElements;
using Object = UnityEngine.Object;
using Type = System.Type;

namespace UnityEditor.VFX.UI
{
    interface IPropertyRMProvider
    {
        bool expanded { get; }
        bool expandable { get; }
        object value { get; set; }
        string name { get; }
        VFXPropertyAttribute[] attributes { get; }
        object[] customAttributes { get; }
        Type portType { get; }
        int depth {get; }
        bool editable { get; }
        void RetractPath();
        void ExpandPath();
    }

    abstract class PropertyRM : VisualElement
    {
        public abstract void SetValue(object obj);
        public abstract object GetValue();
        public virtual void SetMultiplier(object obj) {}

        public VisualElement m_Icon;
        Clickable m_IconClickable;

        Texture2D[] m_IconStates;

        public Label m_Label;


        public bool m_PropertyEnabled;

        public bool propertyEnabled
        {
            get { return m_PropertyEnabled; }

            set
            {
                m_PropertyEnabled = value;
                UpdateEnabled();
            }
        }


        public virtual bool IsCompatible(IPropertyRMProvider provider)
        {
            return GetType() == GetPropertyType(provider);
        }

        public virtual float GetPreferredLabelWidth()
        {
            if (m_Label.panel == null) return 40;

            VisualElement element = this;
            while (element != null && element.style.font.value == null)
            {
                element = element.parent;
            }
            if (element != null)
            {
                m_Label.style.font = element.style.font;
                return m_Label.DoMeasure(-1, MeasureMode.Undefined, m_Label.style.height, MeasureMode.Exactly).x + m_Provider.depth * VFXPropertyIM.depthOffset;
            }
            return 40 + m_Provider.depth * VFXPropertyIM.depthOffset;
        }

        public abstract float GetPreferredControlWidth();

        public void SetLabelWidth(float label)
        {
            m_labelWidth = label;
            m_Label.style.width = effectiveLabelWidth - m_Provider.depth * VFXPropertyIM.depthOffset;
        }

        protected abstract void UpdateEnabled();

        public void Update()
        {
            if (VFXPropertyAttribute.IsAngle(m_Provider.attributes))
                SetMultiplier(Mathf.PI / 180.0f);

            UpdateExpandable();

            SetValue(m_Provider.value);

            string text = ObjectNames.NicifyVariableName(m_Provider.name);
            string tooltip = null;
            VFXPropertyAttribute.ApplyToGUI(m_Provider.attributes, ref text, ref tooltip);
            m_Label.text = text;

            TooltipExtension.AddTooltip(m_Label, tooltip);
            //m_Label.AddTooltip(tooltip);
        }

        void UpdateExpandable()
        {
            if (m_Provider.expandable)
            {
                if (m_IconStates == null)
                {
                    m_IconStates = new Texture2D[] {
                        Resources.Load<Texture2D>("VFX/plus"),
                        Resources.Load<Texture2D>("VFX/minus")
                    };

                    m_Icon.AddManipulator(m_IconClickable);
                }
                m_Icon.style.backgroundImage = m_IconStates[m_Provider.expanded ? 1 : 0];
            }
            else
            {
                if (m_IconStates != null)
                {
                    m_IconStates = null;

                    m_Icon.RemoveManipulator(m_IconClickable);

                    m_Icon.style.backgroundImage = null;
                }
            }
        }

        public PropertyRM(IPropertyRMProvider provider, float labelWidth)
        {
            m_Provider = provider;
            m_labelWidth = labelWidth;

            m_Icon = new VisualElement() { name = "icon" };
            Add(m_Icon);

            m_IconClickable = new Clickable(OnExpand);


            if (VFXPropertyAttribute.IsAngle(provider.attributes))
                SetMultiplier(Mathf.PI / 180.0f);

            string labelText = provider.name;
            string labelTooltip = null;
            VFXPropertyAttribute.ApplyToGUI(provider.attributes, ref labelText, ref labelTooltip);
            m_Label = new Label() { name = "label", text = labelText };
            m_Label.AddTooltip(labelTooltip);

            if (provider.depth != 0)
            {
                for (int i = 0; i < provider.depth; ++i)
                {
                    VisualElement line = new VisualElement();
                    line.style.width = 1;
                    line.name = "line";
                    line.style.marginLeft = 0.5f * VFXPropertyIM.depthOffset;
                    line.style.marginRight = VFXPropertyIM.depthOffset * 0.5f;

                    Add(line);
                }
            }
            m_Label.style.width = effectiveLabelWidth - provider.depth * VFXPropertyIM.depthOffset;
            Add(m_Label);

            AddToClassList("propertyrm");


            RegisterCallback<MouseDownEvent>(OnCatchMouse);

            UpdateExpandable();
        }

        void OnCatchMouse(MouseDownEvent e)
        {
            e.StopPropagation();
        }

        protected float m_labelWidth = 100;

        public virtual float effectiveLabelWidth
        {
            get
            {
                return m_labelWidth;
            }
        }

        static readonly Dictionary<Type, Type> m_TypeDictionary =  new Dictionary<Type, Type>
        {
            {typeof(Vector), typeof(VectorPropertyRM)},
            {typeof(Position), typeof(PositionPropertyRM)},
            {typeof(DirectionType), typeof(DirectionPropertyRM)},
            {typeof(ISpaceable), typeof(SpaceablePropertyRM<ISpaceable>)},
            {typeof(bool), typeof(BoolPropertyRM)},
            {typeof(float), typeof(FloatPropertyRM)},
            {typeof(int), typeof(IntPropertyRM)},
            {typeof(uint), typeof(UintPropertyRM)},
            {typeof(FlipBook), typeof(FlipBookPropertyRM)},
            {typeof(Vector2), typeof(Vector2PropertyRM)},
            {typeof(Vector3), typeof(Vector3PropertyRM)},
            {typeof(Vector4), typeof(Vector4PropertyRM)},
            {typeof(Matrix4x4), typeof(Matrix4x4PropertyRM)},
            {typeof(Color), typeof(ColorPropertyRM)},
            {typeof(Gradient), typeof(GradientPropertyRM)},
            {typeof(AnimationCurve), typeof(CurvePropertyRM)},
            {typeof(Object), typeof(ObjectPropertyRM)},
            {typeof(string), typeof(StringPropertyRM)}
        };


        static Type GetPropertyType(IPropertyRMProvider controller)
        {
            Type propertyType = null;
            Type type = controller.portType;

            if (type.IsEnum)
            {
                propertyType = typeof(EnumPropertyRM);
            }
            else
            {
                while (type != typeof(object) && type != null)
                {
                    if (!m_TypeDictionary.TryGetValue(type, out propertyType))
                    {
                        foreach (var inter in type.GetInterfaces())
                        {
                            if (m_TypeDictionary.TryGetValue(inter, out propertyType))
                            {
                                break;
                            }
                        }
                    }
                    if (propertyType != null)
                    {
                        break;
                    }
                    type = type.BaseType;
                }
            }
            if (propertyType == null)
            {
                propertyType = typeof(EmptyPropertyRM);
            }

            return propertyType;
        }

        public static PropertyRM Create(IPropertyRMProvider controller, float labelWidth)
        {
            Type propertyType = GetPropertyType(controller);

            return System.Activator.CreateInstance(propertyType, new object[] { controller, labelWidth }) as PropertyRM;
        }

        protected void NotifyValueChanged()
        {
            m_Provider.value = GetValue();
        }

        void OnExpand()
        {
            if (m_Provider.expanded)
            {
                m_Provider.RetractPath();
            }
            else
            {
                m_Provider.ExpandPath();
            }
        }

        protected IPropertyRMProvider m_Provider;

        public abstract bool showsEverything { get; }
    }

    interface IFloatNAffector<T>
    {
        T GetValue(object floatN);
    }

    abstract class PropertyRM<T> : PropertyRM
    {
        public PropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {}
        public override void SetValue(object obj)
        {
            if (obj != null)
            {
                if (obj is FloatN)
                {
                    m_Value = ((IFloatNAffector<T>)FloatNAffector.Default).GetValue(obj);
                }
                else
                {
                    try
                    {
                        m_Value = (T)obj;
                    }
                    catch (System.Exception)
                    {
                        Debug.Log("Error Trying to convert" + (obj != null ? obj.GetType().Name : "null") + " to " +  typeof(T).Name);
                    }
                }
            }

            UpdateGUI();
        }

        public override object GetValue()
        {
            return m_Value;
        }

        public abstract void UpdateGUI();

        protected T m_Value;
    }

    abstract class SimplePropertyRM<T> : PropertyRM<T>
    {
        public abstract ValueControl<T> CreateField();

        public SimplePropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_Field = CreateField();
            m_Field.AddToClassList("fieldContainer");
            m_Field.OnValueChanged += OnValueChanged;
            Add(m_Field);

            //m_Field.SetEnabled(enabledSelf);
        }

        public void OnValueChanged()
        {
            T newValue = m_Field.GetValue();
            if (!newValue.Equals(m_Value))
            {
                m_Value = newValue;
                NotifyValueChanged();
            }
        }

        protected override void UpdateEnabled()
        {
            m_Field.SetEnabled(propertyEnabled);
        }

        protected ValueControl<T> m_Field;
        public override void UpdateGUI()
        {
            m_Field.SetValue(m_Value);
        }

        public override bool showsEverything { get { return true; } }

        public override void SetMultiplier(object obj)
        {
            try
            {
                m_Field.SetMultiplier((T)obj);
            }
            catch (System.Exception)
            {
            }
        }
    }


    abstract class SimpleUIPropertyRM<T, U> : PropertyRM<T>
    {
        public abstract INotifyValueChanged<U> CreateField();

        public SimpleUIPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_Field = CreateField();

            VisualElement fieldElement = m_Field as VisualElement;
            fieldElement.AddToClassList("fieldContainer");
            fieldElement.RegisterCallback<ChangeEvent<U>>(OnValueChanged);
            Add(fieldElement);
        }

        public void OnValueChanged(ChangeEvent<U> e)
        {
            T newValue = (T)System.Convert.ChangeType(m_Field.value, typeof(T));
            if (!newValue.Equals(m_Value))
            {
                m_Value = newValue;
                NotifyValueChanged();
            }
        }

        protected override void UpdateEnabled()
        {
            (m_Field as VisualElement).SetEnabled(propertyEnabled);
        }

        INotifyValueChanged<U> m_Field;
        public override void UpdateGUI()
        {
            m_Field.value = (U)System.Convert.ChangeType(m_Value, typeof(U));
        }

        public override bool showsEverything { get { return true; } }
    }


    class EmptyPropertyRM : PropertyRM
    {
        public override float GetPreferredControlWidth()
        {
            return 0;
        }

        public override void SetValue(object obj)
        {
        }

        public override object GetValue()
        {
            return null;
        }

        protected override void UpdateEnabled()
        {
        }

        public EmptyPropertyRM(IPropertyRMProvider provider, float labelWidth) : base(provider, labelWidth)
        {
        }

        public override bool showsEverything { get { return true; } }
    }
}
