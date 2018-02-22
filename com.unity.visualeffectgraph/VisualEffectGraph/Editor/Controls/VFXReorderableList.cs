using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.VFX;
using UnityEditor.VFX.UIElements;

namespace UnityEditor.VFX.UI
{
    class LineDragger : Manipulator
    {
        VFXReorderableList m_Root;
        VisualElement m_Line;

        public LineDragger(VFXReorderableList root, VisualElement item)
        {
            m_Root = root;
            m_Line = item;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        bool m_Dragging;
        Vector2 startPosition;


        object m_Ctx;

        void Release()
        {
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.ReleaseMouseCapture();
            m_Dragging = false;
            m_Ctx = null;
        }

        protected void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0)
            {
                evt.StopPropagation();
                target.TakeMouseCapture();
                m_Dragging = true;
                startPosition = m_Root.WorldToLocal(evt.mousePosition);
                target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                m_Ctx = m_Root.StartDragging(m_Line);
            }
        }

        protected void OnMouseUp(MouseUpEvent evt)
        {
            Vector2 listRelativeMouse = m_Root.WorldToLocal(evt.mousePosition);
            m_Root.EndDragging(m_Ctx, m_Line, listRelativeMouse.y - startPosition.y, evt.mousePosition);
            evt.StopPropagation();
            Release();
        }

        protected void OnMouseMove(MouseMoveEvent evt)
        {
            evt.StopPropagation();

            Vector2 listRelativeMouse = m_Root.WorldToLocal(evt.mousePosition);

            m_Root.ItemDragging(m_Ctx, m_Line, listRelativeMouse.y - startPosition.y, evt.mousePosition);
        }
    }

    class LineSelecter : Manipulator
    {
        VFXReorderableList m_Root;
        VisualElement m_Line;

        public LineSelecter(VFXReorderableList root, VisualElement item)
        {
            m_Root = root;
            m_Line = item;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown, Capture.Capture);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown, Capture.Capture);
        }

        void OnMouseDown(MouseDownEvent e)
        {
            m_Root.Select(m_Line);
        }
    }

    class VFXReorderableList : VisualElement
    {
        int m_SelectedLine = -1;

        class DraggingContext
        {
            public Rect[] originalPositions;
            public VisualElement[] items;
            public Rect myOriginalPosition;
            public int draggedIndex;
        }

        public void Select(int index)
        {
            if (m_SelectedLine != -1 && m_SelectedLine < m_ListContainer.childCount)
            {
                m_ListContainer.ElementAt(m_SelectedLine).pseudoStates &= ~PseudoStates.Selected;
            }

            m_SelectedLine = index;

            if (m_SelectedLine != -1 && m_SelectedLine < m_ListContainer.childCount)
            {
                m_ListContainer.ElementAt(m_SelectedLine).pseudoStates |= PseudoStates.Selected;
                m_Remove.SetEnabled(true);
            }
            else
            {
                m_Remove.SetEnabled(false);
            }
        }

        public void Select(VisualElement item)
        {
            Select(m_ListContainer.IndexOf(item));
        }

        public object StartDragging(VisualElement item)
        {
            //Fix all item so that they can be animated and we can control their positions
            DraggingContext context = new DraggingContext();


            context.items = m_ListContainer.Children().ToArray();
            context.originalPositions = context.items.Select(t => t.layout).ToArray();
            context.draggedIndex = m_ListContainer.IndexOf(item);
            context.myOriginalPosition = m_ListContainer.layout;

            Select(context.draggedIndex);

            for (int i = 0; i < context.items.Length; ++i)
            {
                VisualElement child = context.items[i];
                Rect rect = context.originalPositions[i];
                child.style.positionType = PositionType.Absolute;
                child.style.positionLeft = rect.x;
                child.style.positionTop = rect.y;
                child.style.width = rect.width;
                child.style.height = rect.height;
            }

            item.BringToFront();

            m_ListContainer.style.width = context.myOriginalPosition.width;
            m_ListContainer.style.height = context.myOriginalPosition.height;

            return context;
        }

        public void EndDragging(object ctx, VisualElement item, float offset, Vector2 mouseWorldPosition)
        {
            DraggingContext context = (DraggingContext)ctx;

            foreach (var child in m_ListContainer.Children())
            {
                child.ResetPositionProperties();
            }
            int hoveredIndex = GetHoveredIndex(context, mouseWorldPosition);

            m_ListContainer.Insert(hoveredIndex != -1 ? hoveredIndex : context.draggedIndex, item);
            m_ListContainer.ResetPositionProperties();

            if (hoveredIndex != -1)
            {
                ElementMoved(context.draggedIndex, hoveredIndex);
            }
        }

        public void ItemDragging(object ctx, VisualElement item, float offset, Vector2 mouseWorldPosition)
        {
            DraggingContext context = (DraggingContext)ctx;

            item.style.positionTop = context.originalPositions[context.draggedIndex].y + offset;

            int hoveredIndex = GetHoveredIndex(context, mouseWorldPosition);

            if (hoveredIndex != -1)
            {
                float draggedHeight = context.originalPositions[context.draggedIndex].height;

                if (hoveredIndex < context.draggedIndex)
                {
                    for (int i = 0; i < hoveredIndex; ++i)
                    {
                        context.items[i].style.positionTop = context.originalPositions[i].y;
                    }
                    for (int i = hoveredIndex; i < context.draggedIndex; ++i)
                    {
                        context.items[i].style.positionTop = context.originalPositions[i].y + draggedHeight;
                    }
                    for (int i = context.draggedIndex + 1; i < context.items.Length; ++i)
                    {
                        context.items[i].style.positionTop = context.originalPositions[i].y;
                    }
                }
                else if (hoveredIndex > context.draggedIndex)
                {
                    for (int i = 0; i < context.draggedIndex; ++i)
                    {
                        context.items[i].style.positionTop = context.originalPositions[i].y;
                    }
                    for (int i = hoveredIndex; i > context.draggedIndex; --i)
                    {
                        context.items[i].style.positionTop = context.originalPositions[i].y - draggedHeight;
                    }
                    for (int i = hoveredIndex + 1; i < context.items.Length; ++i)
                    {
                        context.items[i].style.positionTop = context.originalPositions[i].y;
                    }
                }
            }
            else
            {
                for (int i = 0; i < context.items.Length; ++i)
                {
                    if (i != context.draggedIndex)
                        context.items[i].style.positionTop = context.originalPositions[i].y;
                }
            }
        }

        int GetHoveredIndex(DraggingContext context, Vector2 mouseWorldPosition)
        {
            Vector2 mousePosition = m_ListContainer.WorldToLocal(mouseWorldPosition);

            int hoveredIndex = -1;

            for (int i = 0; i < context.items.Length; ++i)
            {
                if (i != context.draggedIndex && context.originalPositions[i].Contains(mousePosition))
                {
                    hoveredIndex = i;
                    break;
                }
            }
            return hoveredIndex;
        }

        protected virtual void ElementMoved(int movedIndex, int targetIndex)
        {
            if (m_SelectedLine == movedIndex)
            {
                m_SelectedLine = targetIndex;
            }
        }

        VisualElement m_ListContainer;

        public VFXReorderableList()
        {
            m_ListContainer = new VisualElement() {name = "ListContainer"};

            Add(m_ListContainer);

            var toolbar = new VisualElement() { name = "Toolbar"};

            var add = new VisualElement() { name = "Add" };
            add.Add(new VisualElement() { name = "icon" });
            add.AddManipulator(new Clickable(OnAdd));
            toolbar.Add(add);

            m_Remove = new VisualElement() { name = "Remove" };
            m_Remove.Add(new VisualElement() { name = "icon" });
            m_Remove.AddManipulator(new Clickable(OnRemoveButton));
            toolbar.Add(m_Remove);

            m_Remove.SetEnabled(false);

            Add(toolbar);
            AddStyleSheetPath("VFXReorderableList");
            if (EditorGUIUtility.isProSkin)
            {
                AddStyleSheetPath("VFXReorderableListDark");
            }
            else
            {
                AddStyleSheetPath("VFXReorderableListLight");
            }
            AddToClassList("ReorderableList");
        }

        VisualElement m_Remove;

        public void AddItem(VisualElement item)
        {
            m_ListContainer.Add(item);
            item.AddManipulator(new LineSelecter(this, item));

            var draggingHandle = new VisualElement() { name = "DraggingHandle" };
            draggingHandle.Add(new VisualElement() { name = "icon" });

            item.Insert(0, draggingHandle);
            draggingHandle.AddManipulator(new LineDragger(this, item));
        }

        public void RemoveItemAt(int index)
        {
            m_ListContainer.RemoveAt(index);

            if (m_SelectedLine >= m_ListContainer.childCount)
            {
                Select(m_ListContainer.childCount - 1);
            }
        }

        public VisualElement ItemAt(int index)
        {
            return m_ListContainer.ElementAt(index);
        }

        public int itemCount
        {
            get { return m_ListContainer.childCount; }
        }

        public virtual void OnAdd()
        {
        }

        void OnRemoveButton()
        {
            OnRemove(m_SelectedLine);
        }

        public virtual void OnRemove(int index)
        {
        }
    }
}
