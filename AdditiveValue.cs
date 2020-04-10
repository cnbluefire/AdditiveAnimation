using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Composition;

namespace AdditiveAnimation
{
    public class AdditiveValue<T> : IDisposable where T : struct
    {
        #region Default Values

        private static TimeSpan DefaultDuration = TimeSpan.FromSeconds(1);
        private static long DefaultTicks = DefaultDuration.Ticks / 19;
        private static CompositionEasingFunction GetDefaultEasingFunc(Compositor compositor) =>
            compositor.CreateCubicBezierEasingFunction(new Vector2(0.45f, 0f), new Vector2(0.55f, 1f));

        #endregion Default Values

        #region Ctor

        public AdditiveValue(Compositor compositor) : this(compositor, default) { }

        public AdditiveValue(Compositor compositor, T initValue)
        {
            AdditiveValueHelper.CheckType<T>();

            Compositor = compositor;
            Properties = Compositor.CreatePropertySet();
            innerPropSet = Compositor.CreatePropertySet();
            lastValue = initValue;
            AdditiveValueHelper.InsertValue(Properties, "Value", lastValue);
            exp = Compositor.CreateExpressionAnimation();
            exp.SetReferenceParameter("p", innerPropSet);
            easingFunc = Compositor.CreateCubicBezierEasingFunction(new Vector2(0.45f, 0f), new Vector2(0.55f, 1f));
        }

        #endregion Ctor

        #region Fields

        private CompositionPropertySet innerPropSet;
        private ExpressionAnimation exp;
        private CompositionEasingFunction easingFunc;
        private long completedIndex = -1;
        private long index = 0;
        private T lastValue;
        private T? nextValue;
        private TimeSpan? duration;
        private long unitTicks = -1;
        private long lastAnimateTick;
        private Queue<KeyFrameAnimation> animations = new Queue<KeyFrameAnimation>();
        private bool stop = true;
        private StringBuilder sb = new StringBuilder(463);
        private object locker = new object();

        #endregion Fields

        #region Properties

        public Compositor Compositor { get; }

        public TimeSpan Duration
        {
            get => duration ?? (duration = DefaultDuration).Value;
            set
            {
                if (duration.HasValue)
                {
                    throw new ArgumentException("不能重复设置Duration");
                }
                if (value == TimeSpan.Zero)
                {
                    throw new ArgumentException("Duration必须大于0");
                }
                duration = value;
                unitTicks = value.Ticks / 19;
            }
        }

        public CompositionEasingFunction EasingFunction
        {
            get => easingFunc ?? (easingFunc = GetDefaultEasingFunc(Compositor));
            set
            {
                if (easingFunc != null)
                {
                    throw new ArgumentException("不能重复设置EasingFunction");
                }
                if (value == null)
                {
                    throw new ArgumentException("EasingFunction不能为空");
                }
                easingFunc = value;
            }
        }

        public CompositionPropertySet Properties { get; private set; }

        /// <summary>
        /// 使用外部节流器时应设置为false，请确保(动画的Duration / 节流器的Interval) < 19
        /// </summary>
        public bool IsThrottleEnabled { get; set; } = true;

        #endregion Properties

        #region Methods

        public AdditiveValue<T> SetDuration(int millis)
        {
            Duration = TimeSpan.FromMilliseconds(millis);
            return this;
        }

        /// <summary>
        /// 使用外部节流器时应设置为false，请确保(动画的Duration / 节流器的Interval) < 19
        /// </summary>
        public AdditiveValue<T> SetThrottleEnabled(bool enabled)
        {
            IsThrottleEnabled = enabled;
            return this;
        }

        public void Stop(T to)
        {
            lock (locker)
            {
                if (Properties == null)
                {
                    throw new ObjectDisposedException(nameof(AdditiveValue<T>));
                }

                stop = true;
                while (index > completedIndex + 1)
                {
                    completedIndex++;
                    animations.Dequeue().Dispose();
                    var propName = $"p{completedIndex}";
                    innerPropSet.StopAnimation(propName);
                    AdditiveValueHelper.InsertValue(innerPropSet, propName, default(T));
                }
                lastValue = to;
                ResetInnerProperties();
            }
        }

        public bool Animate(T to)
        {
            lock (locker)
            {
                if (Properties == null)
                {
                    throw new ObjectDisposedException("");
                }

                stop = false;

                if (IsThrottleEnabled)
                {
                    nextValue = to;
                    var now = Stopwatch.GetTimestamp();
                    if (now - lastAnimateTick > (unitTicks == -1 ? DefaultTicks : unitTicks))
                    {
                        lastAnimateTick = now;
                    }
                    else
                    {
                        return false;
                    }
                }

                var from = lastValue;
                var an = AdditiveValueHelper.CreateKeyFrameAnimation(Compositor, from, to);
                if (an == null) throw new ArgumentNullException(nameof(an));
                an.Duration = Duration;

                lastValue = to;

                var propName = $"p{index}";
                index++;
                Properties.StopAnimation("Value");
                AdditiveValueHelper.InsertValue(innerPropSet, propName, default(T));
                AdditiveValueHelper.InsertValue(innerPropSet, "e", to);
                animations.Enqueue(an);

                var batch = Compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
                batch.Completed += (s, a) =>
                {
                    lock (locker)
                    {
                        if (!stop)
                        {
                            animations.Dequeue().Dispose();
                            completedIndex++;
                            var completedPropName = $"p{completedIndex}";
                            if (nextValue.HasValue && !EqualityComparer<T>.Default.Equals(nextValue.Value, lastValue))
                            {
                                Animate(nextValue.Value);
                            }
                            else
                            {
                                if (index > 200 && index == completedIndex + 1)
                                {
                                    ResetInnerProperties();
                                }
                                else
                                {
                                    StartExpressionAnimation();
                                }
                            }
                        }
                    }
                };
                innerPropSet.StartAnimation(propName, an);
                batch.End();
                StartExpressionAnimation();
                return true;
            }
        }

        public void Dispose()
        {
            lock (locker)
            {
                Stop(default);
                exp.Dispose();
                exp = null;
                easingFunc.Dispose();
                easingFunc = null;
                innerPropSet.Dispose();
                innerPropSet = null;
                Properties.Dispose();
                Properties = null;
            }
        }

        #endregion Methods

        #region Utilities

        private void StartExpressionAnimation()
        {
            Properties.StopAnimation("Value");
            sb.Append("p.e");
            for (var i = completedIndex + 1; i < index; i++)
            {
                sb.Append("-").Append("p.p").Append(i);
            }
            exp.Expression = sb.ToString();
            sb.Clear();
            if (!AdditiveValueHelper.CheckValueExist<T>(Properties, "Value"))
            {
                AdditiveValueHelper.InsertValue(Properties, "Value", default(T));
            }
            Properties.StartAnimation("Value", exp);
        }


        private void ResetInnerProperties()
        {
            lock (locker)
            {
                Properties.StopAnimation("Value");
                index = 0;
                completedIndex = -1;
                innerPropSet.Dispose();
                innerPropSet = Compositor.CreatePropertySet();
                Properties.Dispose();
                Properties = null;
                exp.SetReferenceParameter("p", innerPropSet);
                AdditiveValueHelper.InsertValue(innerPropSet, "e", lastValue);
                AdditiveValueHelper.InsertValue(Properties, "Value", lastValue);
            }
        }

        #endregion Utilities

    }

    public class AdditiveValue
    {
        #region Factory
        public static AdditiveValue<T> Create<T>(Compositor compositor) where T : struct
        {
            return new AdditiveValue<T>(compositor);
        }

        public static AdditiveValue<T> Create<T>(Compositor compositor, T initValue) where T : struct
        {
            return new AdditiveValue<T>(compositor, initValue);
        }

        #endregion Factory
    }

}
