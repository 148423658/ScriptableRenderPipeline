using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Type = System.Type;

namespace UnityEditor.VFX
{
    abstract class VFXBlock : VFXSlotContainerModel<VFXContext, VFXModel>
    {
        public VFXBlock()
        {
            m_UICollapsed = false;
        }

        [SerializeField]
        protected bool m_Disabled = false;

        public bool enabled
        {
            get { return !m_Disabled; }
            set
            {
                m_Disabled = !value;
                Invalidate(this, InvalidationCause.kStructureChanged);
            }
        }

        public abstract VFXContextType compatibleContexts { get; }
        public abstract VFXDataType compatibleData { get; }
        public virtual IEnumerable<VFXAttributeInfo> attributes { get { return Enumerable.Empty<VFXAttributeInfo>(); } }
        public virtual IEnumerable<VFXNamedExpression> parameters { get { return GetExpressionsFromSlots(this); }}
        public virtual IEnumerable<string> includes { get { return Enumerable.Empty<string>(); } }
        public virtual string source { get { return null; } }

        protected VFXData GetData()
        {
            if (GetParent() != null)
                return GetParent().GetData();
            return null;
        }
    }
}
