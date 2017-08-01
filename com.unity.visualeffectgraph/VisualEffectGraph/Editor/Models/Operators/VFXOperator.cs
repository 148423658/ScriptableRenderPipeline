using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.VFX
{
    abstract class VFXOperator : VFXSlotContainerModel<VFXModel, VFXModel>
    {
        protected VFXOperator()
        {
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (outputSlots.Count == 0)
                UpdateOutputs();
        }

        virtual protected IEnumerable<VFXExpression> GetInputExpressions()
        {
            List<VFXExpression> results = new List<VFXExpression>();
            GetInputExpressionsRecursive(results, inputSlots);
            return results;
        }

        private static void GetInputExpressionsRecursive(List<VFXExpression> results, IEnumerable<VFXSlot> slots)
        {
            foreach (var s in slots)
            {
                if (s.GetExpression() != null)
                {
                    results.Add(s.GetExpression());
                }
                else
                {
                    GetInputExpressionsRecursive(results, s.GetChildren());
                }
            }
        }

        private static void CopyLink(VFXSlot from, VFXSlot to)
        {
            var linkedSlots = from.LinkedSlots.ToArray();
            for (int iLink = 0; iLink < linkedSlots.Length; ++iLink)
            {
                to.Link(linkedSlots[iLink]);
            }

            var fromChild = from.children.ToArray();
            var toChild = to.children.ToArray();
            fromChild = fromChild.Take(toChild.Length).ToArray();
            toChild = toChild.Take(fromChild.Length).ToArray();
            for (int iChild = 0; iChild < toChild.Length; ++iChild)
            {
                CopyLink(fromChild[iChild], toChild[iChild]);
            }
        }

        private Queue<VFXExpression[]> outputExpressionQueue = new Queue<VFXExpression[]>();

        private void DequeueOutputSlotFromExpression()
        {
            var outputExpressionArray = outputExpressionQueue.First();

            //Check change
            bool bOuputputLayoutChanged = false;
            if (outputExpressionArray.Length != outputSlots.Count())
            {
                bOuputputLayoutChanged = true;
            }
            else
            {
                for (int iSlot = 0; iSlot < outputExpressionArray.Length; ++iSlot)
                {
                    var slot = GetOutputSlot(iSlot);
                    var expression = outputExpressionArray[iSlot];
                    if (slot.property.type != VFXExpression.TypeToType(expression.ValueType))
                    {
                        bOuputputLayoutChanged = true;
                        break;
                    }
                }
            }

            if (bOuputputLayoutChanged)
            {
                var slotToRemove = outputSlots.ToArray();

                for (int iSlot = 0; iSlot < outputExpressionArray.Length; ++iSlot)
                {
                    var expression = outputExpressionArray[iSlot];
                    AddSlot(VFXSlot.Create(new VFXProperty(VFXExpression.TypeToType(expression.ValueType), "o"), VFXSlot.Direction.kOutput));
                    if (iSlot < slotToRemove.Length)
                    {
                        CopyLink(slotToRemove[iSlot], outputSlots.Last());
                    }
                }

                foreach (var slot in slotToRemove)
                {
                    RemoveSlot(slot);
                    slot.UnlinkAll(false);
                }
            }

            //Apply
            for (int iSlot = 0; iSlot < outputExpressionArray.Length; ++iSlot)
            {
                GetOutputSlot(iSlot).SetExpression(outputExpressionArray[iSlot]);
            }

            outputExpressionQueue.Dequeue();
        }

        protected void SetOuputSlotFromExpression(IEnumerable<VFXExpression> outputExpression)
        {
            var outputExpressionArray = outputExpression.ToArray();
            /*if (outputExpressionQueue.Count > 0)
            {
                //TODOPAUL : Is it an hotfix ?
                var current = outputExpressionQueue.First();
                if (current.Length == outputExpressionArray.Length)
                {
                    bool sequenceEqual = true;
                    for (int i = 0; i < current.Length; ++i)
                    {
                        var left = current[i];
                        var right = outputExpressionArray[i];
                        if (!left.Equals(right))
                        {
                            sequenceEqual = false;
                            break;
                        }
                    }

                    if (sequenceEqual)
                    {
                        //Adding twice the same outputExpressionArray (can happen due to a call to GetExpression from Reset in presenter)
                        return;
                    }
                }
            }*/

            outputExpressionQueue.Enqueue(outputExpressionArray);

            if (outputExpressionQueue.Count > 1)
                return;

            // Dequeue
            while (outputExpressionQueue.Count > 0)
                DequeueOutputSlotFromExpression();
        }

        protected abstract VFXExpression[] BuildExpression(VFXExpression[] inputExpression);

        virtual protected void OnInputConnectionsChanged()
        {
            UpdateOutputs();
        }

        sealed override protected void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            if (cause == InvalidationCause.kConnectionChanged)
                OnInputConnectionsChanged();
            else if (cause == InvalidationCause.kSettingChanged)
                UpdateOutputs();
            base.OnInvalidate(model, cause);
        }

        public override void UpdateOutputs()
        {
            var inputExpressions = GetInputExpressions();
            var ouputExpressions = BuildExpression(inputExpressions.ToArray());
            SetOuputSlotFromExpression(ouputExpressions);
        }
    }
}
