using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental.UIElements.GraphView;

namespace UnityEditor.VFX.UI
{
    class VFXSettingPresenter : Controller, IPropertyRMProvider
    {
        [SerializeField]
        IVFXSlotContainer m_Owner;
        public IVFXSlotContainer owner { get { return m_Owner; } }

        System.Type m_SettingType;

        string m_Name;

        public System.Type portType { get { return m_SettingType; } }

        public void Init(IVFXSlotContainer owner, string name, System.Type type)
        {
            m_Owner = owner;
            m_Name = name;
            m_SettingType = type;
        }

        public string name
        {
            get { return m_Name; }
        }

        public object value
        {
            get
            {
                if (portType != null)
                {
                    return VFXConverter.ConvertTo(owner.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(owner), portType);
                }
                else
                {
                    return null;
                }
            }

            set
            {
                m_Owner.SetSettingValue(name, VFXConverter.ConvertTo(value, portType));
            }
        }


        public string path
        {
            get { return name; }
        }

        public int depth
        {
            get { return 0; }
        }

        public bool expanded
        {
            get { return false; }
        }

        public virtual bool expandable
        {
            get { return false; }
        }

        public virtual string iconName
        {
            get { return portType.Name; }
        }

        public bool editable
        {
            get { return true; }
        }

        public string tooltip
        {
            get { return null; }
        }

        public VFXPropertyAttribute[] attributes
        {
            get
            {
                return VFXPropertyAttribute.Create(customAttributes);
            }
        }

        public object[] customAttributes
        {
            get
            {
                var customAttributes = owner.GetType().GetField(path, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetCustomAttributes(true);
                return customAttributes;
            }
        }

        public void ExpandPath()
        {
        }

        public void RetractPath()
        {
        }

        public override void ApplyChanges()
        {
        }
    }
}
