using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.VFX
{
    class VFXParameter : VFXSlotContainerModel<VFXModel, VFXModel>
    {
        protected VFXParameter()
        {
            m_exposedName = "exposedName";
            m_exposed = false;
            m_UICollapsed = false;
        }

        [SerializeField]
        private string m_exposedName;
        [SerializeField]
        private bool m_exposed;
        [SerializeField]
        public int order;


        // parameter control data;
        public VFXSerializableObject m_Min;
        public VFXSerializableObject m_Max;

        public string exposedName
        {
            get { return m_exposedName; }
            set
            {
                if (m_exposedName != value)
                {
                    m_exposedName = value;
                    Invalidate(InvalidationCause.kParamChanged); // TODO needs a special event for that
                }
            }
        }

        public bool exposed
        {
            get { return m_exposed; }
            set
            {
                if (m_exposed != value)
                {
                    m_exposed = value;
                    Invalidate(InvalidationCause.kExpressionGraphChanged); // Tmp
                }
            }
        }

        public Type type
        {
            get { return outputSlots[0].property.type; }
        }

        public object value
        {
            get { return outputSlots[0].value; }
            set { outputSlots[0].value = value; }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties { get { return PropertiesFromSlotsOrDefaultFromClass(VFXSlot.Direction.kOutput); } }

        public void Init(Type _type)
        {
            if (_type != null && outputSlots.Count == 0)
            {
                VFXSlot slot = VFXSlot.Create(new VFXProperty(_type, "o"), VFXSlot.Direction.kOutput);
                AddSlot(slot);

                if (!typeof(UnityEngine.Object).IsAssignableFrom(_type))
                    slot.value = System.Activator.CreateInstance(_type);
            }
            else
            {
                throw new InvalidOperationException("Cannot init VFXParameter");
            }
        }
    }
}
