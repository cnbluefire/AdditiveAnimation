using Microsoft.UI.Composition;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using WinRT;

namespace AdditiveAnimationSample.AdditiveAnimation
{
    internal class AdditiveAnimationEntry : IDisposable
    {
        private bool disposeValue;

        private CompositionObject compositionObject;
        private Compositor compositor;
        private readonly string propertyName;
        private ExpressionManager expressionManager;
        private object lastFinalValue;

        public AdditiveAnimationEntry(
            CompositionObject compositionObject,
            string propertyName)
        {
            this.compositionObject = compositionObject;
            this.propertyName = propertyName;

            compositor = compositionObject.Compositor;

            expressionManager = new ExpressionManager(compositor);
        }

        public CompositionAnimation Animation
        {
            get
            {
                ThrowIfDisposed();
                return expressionManager.ExpressionAnimation;
            }
        }

        public CompositionObject CompositionObject => compositionObject;

        public string PropertyName => propertyName;

        public object FinalValue => lastFinalValue;

        public void StartAnimation<T>(T toValue, TimeSpan duration, CompositionEasingFunction? easingFunction)
        {
            ThrowIfDisposed();

            lock (expressionManager)
            {
                if (this.lastFinalValue == null)
                {
                    InitAnimations<T>();
                }

                var subAnimation = CompositionAnimationHelper.CreateKeyFrameAnimation<T>(compositor);
                var node = new AnimationNode(subAnimation, expressionManager.CreateSubAnimationPropertyName());

                expressionManager.Add<T>(node);

                subAnimation.Duration = duration;
                subAnimation.StopBehavior = AnimationStopBehavior.SetToFinalValue;

                subAnimation.InsertExpressionKeyFrame(0, "finalValue - lastFinalValue");

                if (easingFunction == null) subAnimation.InsertExpressionKeyFrame(1, "initValue");
                else subAnimation.InsertExpressionKeyFrame(1, "initValue", easingFunction);

                CompositionAnimationHelper.SetAnimationParameter(subAnimation, "finalValue", toValue);
                CompositionAnimationHelper.SetAnimationParameter<T>(subAnimation, "initValue", default);

                T lastFinalValue = default;

                if (this.lastFinalValue != null)
                {
                    lastFinalValue = (T)this.lastFinalValue;
                    CompositionAnimationHelper.SetAnimationParameter<T>(subAnimation, "lastFinalValue", default);
                }
                CompositionAnimationHelper.SetAnimationParameter<T>(subAnimation, "lastFinalValue", lastFinalValue);

                this.lastFinalValue = toValue;

                var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);

                CompositionAnimationHelper.SetPropertySetValue(expressionManager.ExpressionAnimation.Properties, "initValue", toValue);
                expressionManager.StartNodeAnimation(node);

                batch.Completed += (s, a) =>
                {
                    lock (expressionManager)
                    {
                        expressionManager.Remove<T>(node, out var nodeValue);

                        if (!disposeValue && expressionManager.AnimationNodeCount == 0)
                        {
                            Completed?.Invoke(this, new CompletedEventArgs(this.lastFinalValue));
                        }
                    }
                };
                batch.End();

                expressionManager.UpdateExpressionAnimation<T>();
                compositionObject.StopAnimation(this.propertyName);
                compositionObject.StartAnimation(this.propertyName, expressionManager.ExpressionAnimation);
            }
        }

        private void InitAnimations<T>()
        {
            if (CompositionAnimationHelper.TryGetPropertySetValue<T>(compositionObject.Properties, propertyName, out var initValue) != CompositionGetValueStatus.Succeeded)
            {
                initValue = default;
            }
            lastFinalValue = initValue;
            CompositionAnimationHelper.SetPropertySetValue(expressionManager.ExpressionAnimation.Properties, $"initValue", initValue);
        }


        public event EventHandler<CompletedEventArgs>? Completed;

        public record class CompletedEventArgs
        {
            public CompletedEventArgs(object finalValue)
            {
                FinalValue = finalValue;
            }

            public object FinalValue { get; }
        }

        private void ThrowIfDisposed()
        {
            if (disposeValue)
            {
                throw new ObjectDisposedException(nameof(AdditiveAnimationEntry));
            }
        }

        public void Dispose()
        {
            lock (expressionManager)
            {
                if (!disposeValue)
                {
                    disposeValue = true;

                    Completed = null;

                    compositionObject = null;

                    expressionManager?.Dispose();
                }
            }
        }

        private record AnimationNode(KeyFrameAnimation KeyFrameAnimation, string PropertyName);

        private class ExpressionManager : IDisposable
        {
            private const int NodeAnimationCount = 50;
            private readonly Compositor compositor;
            private bool disposeValue;
            private ExpressionAnimationNode firstNode;
            private ExpressionAnimationNode lastNode;
            private uint subAnimationIndex;
            private HashSet<string> subAnimationPropertyNameTable;

            public ExpressionManager(Compositor compositor)
            {
                this.compositor = compositor;
                subAnimationPropertyNameTable = new HashSet<string>();
                firstNode = new ExpressionAnimationNode(compositor.CreateExpressionAnimation());
                lastNode = firstNode;
            }

            public ExpressionAnimation ExpressionAnimation => firstNode.Expression;

            public bool Add<T>(AnimationNode node)
            {
                lock (this)
                {
                    if (subAnimationPropertyNameTable.Add(node.PropertyName))
                    {
                        var n = lastNode;
                        if (n.Animations.Count < NodeAnimationCount)
                        {
                            n.Animations.Add(node);
                            CompositionAnimationHelper.SetPropertySetValue<T>(n.Expression.Properties, node.PropertyName, default);
                        }
                        else
                        {
                            var expNode = new ExpressionAnimationNode(compositor.CreateExpressionAnimation());
                            expNode.Animations.Add(node);
                            expNode.PrevNode = lastNode;
                            lastNode.NextNode = expNode;
                            lastNode = expNode;
                            CompositionAnimationHelper.SetPropertySetValue<T>(expNode.Expression.Properties, node.PropertyName, default);
                        }
                        return true;
                    }
                    return false;
                }
            }

            public void Remove<T>(AnimationNode node, out T nodeValue)
            {
                nodeValue = default;
                lock (this)
                {
                    subAnimationPropertyNameTable.Remove(node.PropertyName);
                    var n = lastNode;
                    while (n != null)
                    {
                        if (n.Animations.Remove(node))
                        {
                            if (CompositionAnimationHelper.TryGetPropertySetValue<T>(n.Expression.Properties, node.PropertyName, out nodeValue) != CompositionGetValueStatus.Succeeded)
                            {
                                nodeValue = default;
                            }
                            CompositionAnimationHelper.SetPropertySetValue<T>(n.Expression.Properties, node.PropertyName, default);
                            break;
                        }
                        n = n.PrevNode;
                    }
                }
            }

            public void StartNodeAnimation(AnimationNode node)
            {
                lock (this)
                {
                    var n = firstNode;
                    while (n != null)
                    {
                        if (n.Animations.Contains(node))
                        {
                            n.Expression.Properties.StartAnimation(node.PropertyName, node.KeyFrameAnimation);
                            break;
                        }
                        n = n.NextNode;
                    }
                }
            }

            public int AnimationNodeCount => subAnimationPropertyNameTable.Count;

            public string CreateSubAnimationPropertyName()
            {
                lock (this)
                {
                    for (uint i = 0; i < uint.MaxValue; i++)
                    {
                        unchecked { subAnimationIndex++; }
                        var name = $"a{subAnimationIndex}";
                        if (!subAnimationPropertyNameTable.Contains(name))
                        {
                            return name;
                        }
                    }
                }
                return "";
            }


            public void UpdateExpressionAnimation<T>()
            {
                lock (this)
                {
                    var n = lastNode.PrevNode;
                    while (n != null)
                    {
                        if (n.NextNode.Animations.Count == 0)
                        {
                            n.Expression.Properties.StopAnimation("cValue");
                            CompositionAnimationHelper.SetPropertySetValue<T>(n.Expression.Properties, "cValue", default);

                            n.NextNode = n.NextNode.NextNode;
                            n.NextNode.PrevNode = n;
                        }

                        n = n.PrevNode;
                    }

                    n = firstNode;
                    while (n.NextNode != null) n = n.NextNode;
                    lastNode = n;

                    if (AnimationNodeCount == 0)
                    {
                        firstNode.Expression.Expression = "This.CurrentValue";
                    }
                    else
                    {
                        n = lastNode;
                        while (n != null)
                        {
                            var sb = new StringBuilder("t.cValue");
                            if (n.PrevNode == null) sb.Append("+t.initValue");
                            for (int i = 0; i < n.Animations.Count; i++)
                            {
                                sb.Append('-').Append("t.").Append(n.Animations[i].PropertyName);
                            }
                            n.Expression.Expression = sb.ToString();

                            if (n == lastNode)
                            {
                                CompositionAnimationHelper.SetPropertySetValue<T>(n.Expression.Properties, "cValue", default);
                            }

                            if (n.PrevNode != null)
                            {
                                CompositionAnimationHelper.SetPropertySetValue<T>(n.PrevNode.Expression.Properties, "cValue", default);
                                n.PrevNode.Expression.Properties.StartAnimation("cValue", n.Expression);
                            }

                            n = n.PrevNode;
                        }
                    }
                }
            }


            public void Dispose()
            {
                if (!disposeValue)
                {
                    disposeValue = true;

                    var n = lastNode;
                    while (n != null)
                    {
                        var n1 = n.PrevNode;
                        if (n1 != null)
                        {
                            n1.Expression.Properties.StopAnimation("cValue");
                            n1.NextNode = null;
                            n.PrevNode = null;
                        }

                        for (int i = 0; i < n.Animations.Count; i++)
                        {
                            n.Expression.Properties.StopAnimation(n.Animations[i].PropertyName);
                        }

                        n.Expression.Dispose();
                        n = n1;
                    }
                    lastNode = firstNode = null;
                }
            }

            private class ExpressionAnimationNode
            {
                public ExpressionAnimationNode(ExpressionAnimation expression)
                {
                    Expression = expression;
                    Animations = new List<AnimationNode>();
                    CompositionAnimationHelper.SetAnimationParameter(Expression, "t", Expression.Properties);
                }

                public ExpressionAnimation Expression { get; }
                public List<AnimationNode> Animations { get; }

                public ExpressionAnimationNode PrevNode { get; set; }

                public ExpressionAnimationNode NextNode { get; set; }
            }
        }
    }
}