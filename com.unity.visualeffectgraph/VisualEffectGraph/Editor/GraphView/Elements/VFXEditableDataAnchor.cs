using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using System.Collections.Generic;
using Type = System.Type;

namespace UnityEditor.VFX.UI
{
    public class VFXDataGUIStyles
    {
        public static VFXDataGUIStyles instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new VFXDataGUIStyles();
                return s_Instance;
            }
        }

        static VFXDataGUIStyles s_Instance;

        public GUIStyle baseStyle;

        VFXDataGUIStyles()
        {
            baseStyle = GUI.skin.textField;
        }

        public GUIStyle GetGUIStyleForExpandableType(Type type)
        {
            GUIStyle style = null;

            if (typeStyles.TryGetValue(type, out style))
            {
                return style;
            }

            GUIStyle typeStyle = new GUIStyle(baseStyle);
            typeStyle.normal.background = Resources.Load<Texture2D>("VFX/" + type.Name + "_plus");
            if (typeStyle.normal.background == null)
                typeStyle.normal.background = Resources.Load<Texture2D>("VFX/Default_plus");
            typeStyle.active.background = typeStyle.focused.background = null;
            typeStyle.onNormal.background = Resources.Load<Texture2D>("VFX/" + type.Name + "_minus");
            if (typeStyle.onNormal.background == null)
                typeStyle.onNormal.background = Resources.Load<Texture2D>("VFX/Default_minus");
            typeStyle.border.top = 0;
            typeStyle.border.left = 0;
            typeStyle.border.bottom = typeStyle.border.right = 0;
            typeStyle.padding.top = 3;

            typeStyles.Add(type, typeStyle);


            return typeStyle;
        }

        public GUIStyle GetGUIStyleForType(Type type)
        {
            GUIStyle style = null;

            if (typeStyles.TryGetValue(type, out style))
            {
                return style;
            }

            GUIStyle typeStyle = new GUIStyle(baseStyle);
            typeStyle.normal.background = Resources.Load<Texture2D>("VFX/" + type.Name);
            if (typeStyle.normal.background == null)
                typeStyle.normal.background = Resources.Load<Texture2D>("VFX/Default");
            typeStyle.active.background = typeStyle.focused.background = null;
            typeStyle.border.top = 0;
            typeStyle.border.left = 0;
            typeStyle.border.bottom = typeStyle.border.right = 0;

            typeStyles.Add(type, typeStyle);


            return typeStyle;
        }

        static Dictionary<Type, GUIStyle> typeStyles = new Dictionary<Type, GUIStyle>();

        public void Reset()
        {
            typeStyles.Clear();
        }

        public float lineHeight
        { get { return 16; } }
    }

    partial class VFXEditableDataAnchor : VFXDataAnchor
    {
        VFXPropertyIM   m_PropertyIM;
        IMGUIContainer  m_Container;


        PropertyRM      m_PropertyRM;


        // TODO This is a workaround to avoid having a generic type for the anchor as generic types mess with USS.
        public static new VFXEditableDataAnchor Create<TEdgePresenter>(VFXDataAnchorPresenter presenter) where TEdgePresenter : VFXDataEdgePresenter
        {
            var anchor = new VFXEditableDataAnchor();

            anchor.m_EdgeConnector = new EdgeConnector<TEdgePresenter>(anchor);
            anchor.presenter = presenter;
            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        protected VFXEditableDataAnchor()
        {
            clipChildren = false;
        }

        void BuildProperty()
        {
            VFXDataAnchorPresenter presenter = GetPresenter<VFXDataAnchorPresenter>();
            if (m_PropertyRM != null)
            {
                Remove(m_PropertyRM);
            }

            m_PropertyRM = PropertyRM.Create(presenter, 100);
            if (m_PropertyRM != null)
            {
                Add(m_PropertyRM);
                if (m_Container != null)
                    Remove(m_Container);
                m_Container = null;
            }
            else
            {
                m_PropertyIM = VFXPropertyIM.Create(presenter.anchorType, 100);

                m_Container = new IMGUIContainer(OnGUI) { name = "IMGUI" };
                Add(m_Container);
            }
        }

        void OnGUI()
        {
            // update the GUISTyle from the element style defined in USS


            //try
            {
                bool changed = m_PropertyIM.OnGUI(GetPresenter<VFXDataAnchorPresenter>());

                if (changed)
                {
                    Dirty(ChangeType.Transform | ChangeType.Repaint);
                }

                if (Event.current.type != EventType.Layout && Event.current.type != EventType.Used)
                {
                    /*  Rect r = GUILayoutUtility.GetLastRect();
                    m_Container.height = r.yMax;*/
                }
            }
            /*catch(System.Exception e)
            {
                Debug.LogError(e.Message);
            }*/
        }

        Type m_EditedType;

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            VFXDataAnchorPresenter presenter = GetPresenter<VFXDataAnchorPresenter>();

            if ((m_PropertyIM == null && m_PropertyRM == null) || m_EditedType != presenter.anchorType)
            {
                BuildProperty();
                m_EditedType = presenter.anchorType;
            }
            /*if (m_Container != null)
                m_Container.executionContext = presenter.GetInstanceID();*/

            OnRecompile();

            clipChildren = false;
        }

        public void OnRecompile()
        {
            VFXDataAnchorPresenter presenter = GetPresenter<VFXDataAnchorPresenter>();
            if (m_PropertyRM != null && presenter != null)
            {
                m_PropertyRM.SetEnabled(presenter.editable && !presenter.collapsed);
                m_PropertyRM.Update();
            }
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            return layout.Contains(localPoint);
            //return GraphElement.ContainsPoint(localPoint);
            // Here local point comes without position offset...
            //localPoint -= position.position;
            //return m_ConnectorBox.ContainsPoint(m_ConnectorBox.transform.MultiplyPoint3x4(localPoint));
        }
    }
}
