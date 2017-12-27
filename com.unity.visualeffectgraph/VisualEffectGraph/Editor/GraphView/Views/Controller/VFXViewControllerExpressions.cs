using System;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    partial class VFXViewController : Controller<VFXAsset>
    {
        public event System.Action onRecompileEvent;
        public void RecompileExpressionGraphIfNeeded()
        {
            if (!ExpressionGraphDirty)
                return;

            ExpressionGraphDirty = false;

            try
            {
                CreateExpressionContext(true /*cause == VFXModel.InvalidationCause.kStructureChanged || cause == VFXModel.InvalidationCause.kConnectionChanged*/);
                m_ExpressionContext.Recompile();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (onRecompileEvent != null)
            {
                onRecompileEvent();
            }
        }

        public void InvalidateExpressionGraph(VFXModel model, VFXModel.InvalidationCause cause)
        {
            if (cause != VFXModel.InvalidationCause.kStructureChanged &&
                cause != VFXModel.InvalidationCause.kExpressionInvalidated)
                /*use != VFXModel.InvalidationCause.kConnectionChanged &&
                cause != VFXModel.InvalidationCause.kParamChanged &&
                cause != VFXModel.InvalidationCause.kSettingChanged)*/
                return;

            ExpressionGraphDirty = true;
        }

        private void CreateExpressionContext(bool forceRecreation)
        {
            if (!forceRecreation && m_ExpressionContext != null)
                return;

            m_ExpressionContext = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
            HashSet<Object> currentObjects = new HashSet<Object>();
            graph.CollectDependencies(currentObjects);

            int nbExpr = 0;
            foreach (var o in currentObjects)
            {
                if (o is VFXSlot)
                {
                    var exp = ((VFXSlot)o).GetExpression();
                    if (exp != null)
                    {
                        m_ExpressionContext.RegisterExpression(exp);
                        ++nbExpr;
                    }
                }
            }
        }

        public bool CanGetEvaluatedContent(VFXSlot slot)
        {
            if (m_ExpressionContext == null)
                return false;
            if (slot.GetExpression() == null)
                return false;

            var reduced = m_ExpressionContext.GetReduced(slot.GetExpression());
            return reduced != null && reduced.Is(VFXExpression.Flags.Value);
        }

        public object GetEvaluatedContent(VFXSlot slot)
        {
            if (!CanGetEvaluatedContent(slot))
                return null;

            var reduced = m_ExpressionContext.GetReduced(slot.GetExpression());
            return reduced.GetContent();
        }

        private VFXExpression.Context m_ExpressionContext;
        [NonSerialized]
        private bool ExpressionGraphDirty = true;
    }
}
