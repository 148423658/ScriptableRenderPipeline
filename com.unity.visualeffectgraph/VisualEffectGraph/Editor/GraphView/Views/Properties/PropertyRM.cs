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
        Type anchorType { get; }
        int depth {get; }
        bool editable { get; }
        void RetractPath();
        void ExpandPath();
    }

    abstract class PropertyRM : VisualElement
    {
        public abstract void SetValue(object obj);
        public abstract object GetValue();

        public VisualElement m_Icon;

        Texture2D[] m_IconStates;

        public VisualElement m_Label;

        public void Update()
        {
            m_Icon.style.backgroundImage = m_IconStates[m_Provider.expanded && m_Provider.expandable ? 1 : 0];
            SetValue(m_Provider.value);

            string text = m_Provider.name;
            string tooltip = null;
            VFXPropertyAttribute.ApplyToGUI(m_Provider.attributes, ref text, ref tooltip);
            m_Label.text = text;

            TooltipExtension.AddTooltip(m_Label, tooltip);
            //m_Label.AddTooltip(tooltip);
        }

        public PropertyRM(IPropertyRMProvider provider, float labelWidth)
        {
            m_Provider = provider;
            m_labelWidth = labelWidth;

            m_Icon =  new VisualElement() {name = "icon"};
            Add(m_Icon);

            if (provider.expandable)
            {
                m_IconStates = new Texture2D[] {
                    Resources.Load<Texture2D>("VFX/" + provider.anchorType.Name + "_plus"),
                    Resources.Load<Texture2D>("VFX/" + provider.anchorType.Name + "_minus")
                };

                if (m_IconStates[0] == null)
                {
                    m_IconStates[0] = Resources.Load<Texture2D>("VFX/Default_plus");
                    m_IconStates[1] = Resources.Load<Texture2D>("VFX/Default_minus");
                }
                m_Icon.AddManipulator(new Clickable(OnExpand));
            }
            else
            {
                m_IconStates = new Texture2D[] {
                    Resources.Load<Texture2D>("VFX/" + provider.anchorType.Name)
                };

                if (m_IconStates[0] == null)
                {
                    m_IconStates[0] = Resources.Load<Texture2D>("VFX/Default");
                }
            }

            m_Icon.style.backgroundImage = m_IconStates[0];

            string labelText = provider.name;
            string labelTooltip = null;
            VFXPropertyAttribute.ApplyToGUI(provider.attributes, ref labelText, ref labelTooltip);
            m_Label = new VisualElement() { name = "label", text = labelText };
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
            //m_Label.marginLeft = presenter.depth * VFXPropertyIM.depthOffset;
            Add(m_Label);

            AddToClassList("propertyrm");
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
            {typeof(Vector2), typeof(Vector2PropertyRM)},
            {typeof(Vector3), typeof(Vector3PropertyRM)},
            {typeof(Vector4), typeof(Vector4PropertyRM)},
            {typeof(Color), typeof(ColorPropertyRM)},
            {typeof(AnimationCurve), typeof(CurvePropertyRM)},
            {typeof(string), typeof(StringPropertyRM)}
        };

        public static PropertyRM Create(IPropertyRMProvider presenter, float labelWidth)
        {
            Type propertyType = null;

            Type type = presenter.anchorType;

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

            return (propertyType != null) ? System.Activator.CreateInstance(propertyType, new object[] { presenter, labelWidth }) as PropertyRM : null;
        }

        protected void NotifyValueChanged()
        {
            //m_Presenter.SetPropertyValue(GetValue());
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
        public PropertyRM(IPropertyRMProvider presenter, float labelWidth) : base(presenter, labelWidth)
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

        public SimplePropertyRM(IPropertyRMProvider presenter, float labelWidth) : base(presenter, labelWidth)
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

        ValueControl<T> m_Field;
        public override void UpdateGUI()
        {
            m_Field.SetValue(m_Value);
        }

        /*
        public override void SetEnabled(bool value)
        {
            base.SetEnabled(value);
            if (m_Field != null)
                m_Field.SetEnabled(value);
        }*/
        public override bool showsEverything { get { return true; } }
    }
}
