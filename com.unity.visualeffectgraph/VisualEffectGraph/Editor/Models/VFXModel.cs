using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.Profiling;

namespace UnityEditor.VFX
{
    [Serializable]
    abstract class VFXModel : ScriptableObject
    {
        public enum InvalidationCause
        {
            kStructureChanged,      // Model structure (hierarchy) has changed
            kParamChanged,          // Some parameter values have changed
            kParamPropagated,       // Some parameter values have change and was propagated from the parents
            kSettingChanged,        // A setting value has changed
            kConnectionChanged,     // Connection have changed
            kExpressionInvalidated, // No direct change to the model but a change in connection was propagated from the parents
            kExpressionGraphChanged,// Expression graph must be recomputed
            kUIChanged,             // UI stuff has changed
        }

        public new virtual string name  { get { return string.Empty; } }

        public delegate void InvalidateEvent(VFXModel model, InvalidationCause cause);

        public event InvalidateEvent onInvalidateDelegate;

        protected VFXModel()
        {
            m_UICollapsed = true;
        }

        public virtual void OnEnable()
        {
            if (m_Children == null)
                m_Children = new List<VFXModel>();
            else
            {
                int nbRemoved = m_Children.RemoveAll(c => c == null);// Remove bad references if any
                if (nbRemoved > 0)
                    Debug.Log(String.Format("Remove {0} child(ren) that couldnt be deserialized from {1} of type {2}", nbRemoved, name, GetType()));
            }
        }

        public virtual void Sanitize() {}

        public virtual void CollectDependencies(HashSet<UnityEngine.Object> objs)
        {
            foreach (var child in children)
            {
                objs.Add(child);
                child.CollectDependencies(objs);
            }
        }

        public virtual T Clone<T>() where T : VFXModel
        {
            T clone = CreateInstance(GetType()) as T;

            foreach (var child in children)
            {
                var cloneChild = child.Clone<VFXModel>();
                clone.AddChild(cloneChild, -1, false);
            }

            clone.m_UICollapsed = m_UICollapsed;
            clone.m_UIPosition = m_UIPosition;
            return clone;
        }

        protected virtual void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            if (onInvalidateDelegate != null)
            {
                Profiler.BeginSample("VFXEditor.OnInvalidateDelegate");
                try
                {
                    onInvalidateDelegate(model, cause);
                }
                finally
                {
                    Profiler.EndSample();
                }
            }
        }

        protected virtual void OnAdded() {}
        protected virtual void OnRemoved() {}

        public virtual bool AcceptChild(VFXModel model, int index = -1)
        {
            return false;
        }

        public void AddChild(VFXModel model, int index = -1, bool notify = true)
        {
            int realIndex = index == -1 ? m_Children.Count : index;
            if (model.m_Parent != this || realIndex != GetIndex(model))
            {
                if (!AcceptChild(model, index))
                    throw new ArgumentException("Cannot attach " + model + " to " + this);

                model.Detach(notify && model.m_Parent != this); // Dont notify if the owner is already this to avoid double invalidation
                realIndex = index == -1 ? m_Children.Count : index; // Recompute as the child may have been removed

                m_Children.Insert(realIndex, model);
                model.m_Parent = this;
                model.OnAdded();

                if (notify)
                    Invalidate(InvalidationCause.kStructureChanged);
            }
        }

        public void RemoveChild(VFXModel model, bool notify = true)
        {
            if (model.m_Parent != this)
                return;

            model.OnRemoved();
            m_Children.Remove(model);
            model.m_Parent = null;

            if (notify)
                Invalidate(InvalidationCause.kStructureChanged);
        }

        public void RemoveAllChildren(bool notify = true)
        {
            while (m_Children.Count > 0)
                RemoveChild(m_Children[m_Children.Count - 1], notify);
        }

        public VFXModel GetParent()
        {
            return m_Parent;
        }

        public void Attach(VFXModel parent, bool notify = true)
        {
            parent.AddChild(this, -1, notify);
        }

        public void Detach(bool notify = true)
        {
            if (m_Parent == null)
                return;

            m_Parent.RemoveChild(this, notify);
        }

        public IEnumerable<VFXModel> children
        {
            get { return m_Children; }
        }

        public VFXModel this[int index]
        {
            get { return m_Children[index]; }
        }

        public Vector2 position
        {
            get { return m_UIPosition; }
            set
            {
                if (m_UIPosition != value)
                {
                    m_UIPosition = value;
                    Invalidate(InvalidationCause.kUIChanged);
                }
            }
        }

        public bool collapsed
        {
            get { return m_UICollapsed; }
            set
            {
                if (m_UICollapsed != value)
                {
                    m_UICollapsed = value;
                    Invalidate(InvalidationCause.kUIChanged);
                }
            }
        }

        public bool superCollapsed
        {
            get { return m_UISuperCollapsed; }
            set
            {
                if (m_UISuperCollapsed != value)
                {
                    m_UISuperCollapsed = value;
                    Invalidate(InvalidationCause.kUIChanged);
                }
            }
        }

        public int GetNbChildren()
        {
            return m_Children.Count;
        }

        public int GetIndex(VFXModel child)
        {
            return m_Children.IndexOf(child);
        }

        public void SetSettingValue(string name, object value)
        {
            SetSettingValue(name, value, true);
        }

        protected void SetSettingValue(string name, object value, bool notify)
        {
            var field = GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                throw new ArgumentException(string.Format("Unable to find field {0} in {1}", name, GetType().ToString()));
            }

            var currentValue = field.GetValue(this);
            if (currentValue != value)
            {
                field.SetValue(this, value);
                if (notify)
                {
                    Invalidate(InvalidationCause.kSettingChanged);
                }
            }
        }

        public void Invalidate(InvalidationCause cause)
        {
            string sampleName = GetType().Name + "-" + name + "-" + cause;
            Profiler.BeginSample("VFXEditor.Invalidate" + sampleName);
            try
            {
                Invalidate(this, cause);
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        protected virtual void Invalidate(VFXModel model, InvalidationCause cause)
        {
            OnInvalidate(model, cause);
            if (m_Parent != null)
                m_Parent.Invalidate(model, cause);
        }

        public IEnumerable<FieldInfo> GetSettings(bool listHidden, VFXSettingAttribute.VisibleFlags flags = VFXSettingAttribute.VisibleFlags.All)
        {
            return GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(f =>
                {
                    var attrArray = f.GetCustomAttributes(typeof(VFXSettingAttribute), true);
                    if (attrArray.Length == 1)
                    {
                        var attr = attrArray[0] as VFXSettingAttribute;
                        if ((attr.visibleFlags & flags) == 0)
                        {
                            return false;
                        }

                        if (!filteredOutSettings.Contains(f.Name) || listHidden)
                        {
                            return true;
                        }
                    }
                    return false;
                });
        }

        protected virtual IEnumerable<string> filteredOutSettings
        {
            get
            {
                return Enumerable.Empty<string>();
            }
        }

        public VFXAsset GetAsset()
        {
            var graph = GetGraph();
            if (graph != null)
                return graph.vfxAsset;
            return null;
        }

        public VFXGraph GetGraph()
        {
            var graph = this as VFXGraph;
            if (graph != null)
                return graph;
            var parent = GetParent();
            if (parent != null)
                return parent.GetGraph();
            return null;
        }

        [SerializeField]
        protected VFXModel m_Parent;

        [SerializeField]
        protected List<VFXModel> m_Children;

        [SerializeField]
        protected Vector2 m_UIPosition;

        [SerializeField]
        protected bool m_UICollapsed;
        [SerializeField]
        protected bool m_UISuperCollapsed;
    }

    abstract class VFXModel<ParentType, ChildrenType> : VFXModel
        where ParentType : VFXModel
        where ChildrenType : VFXModel
    {
        public override bool AcceptChild(VFXModel model, int index = -1)
        {
            return index >= -1 && index <= m_Children.Count && model is ChildrenType;
        }

        public new ParentType GetParent()
        {
            return (ParentType)m_Parent;
        }

        public new int GetNbChildren()
        {
            return m_Children.Count;
        }

        public new ChildrenType this[int index]
        {
            get { return m_Children[index] as ChildrenType; }
        }

        public new IEnumerable<ChildrenType> children
        {
            get { return m_Children.Cast<ChildrenType>(); }
        }
    }
}
