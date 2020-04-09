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
            CheckType();

            Compositor = compositor;
            properties = Compositor.CreatePropertySet();
            innerPropSet = Compositor.CreatePropertySet();
            lastValue = initValue;
            InsertValue(properties, "Value", lastValue);
            exp = Compositor.CreateExpressionAnimation();
            exp.SetReferenceParameter("p", innerPropSet);
            easingFunc = Compositor.CreateCubicBezierEasingFunction(new Vector2(0.45f, 0f), new Vector2(0.55f, 1f));
        }

        #endregion Ctor

        #region Fields

        private CompositionPropertySet properties;
        private CompositionPropertySet innerPropSet;
        private ExpressionAnimation exp;
        private CompositionEasingFunction easingFunc;
        private long completedIndex = -1;
        private long index = 0;
        private T lastValue;
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

        public CompositionPropertySet Properties => properties;

        #endregion Properties

        #region Methods

        public AdditiveValue<T> SetDuration(int millis)
        {
            Duration = TimeSpan.FromMilliseconds(millis);
            return this;
        }

        public void Stop(T to)
        {
            lock (locker)
            {
                if (properties == null)
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
                    InsertValue(innerPropSet, propName, default);
                }
                lastValue = to;
                ResetInnerProperties();
            }
        }

        public bool Animate(T to)
        {
            lock (locker)
            {
                if (properties == null)
                {
                    throw new ObjectDisposedException("");
                }

                stop = false;
                var now = Stopwatch.GetTimestamp();
                if (now - lastAnimateTick > (unitTicks == -1 ? DefaultTicks : unitTicks))
                {
                    lastAnimateTick = now;
                }
                else
                {
                    return false;
                }

                var from = lastValue;
                var an = CreateKeyFrameAnimation(from, to);
                if (an == null) throw new ArgumentNullException(nameof(an));
                an.Duration = Duration;

                lastValue = to;

                var propName = $"p{index}";
                index++;
                InsertValue(innerPropSet, propName, default);
                InsertValue(innerPropSet, "e", to);
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
                properties.Dispose();
                properties = null;
            }
        }

        #endregion Methods

        #region Utilities

        private void StartExpressionAnimation()
        {
            properties.StopAnimation("Value");
            sb.Append("p.e");
            for (var i = completedIndex + 1; i < index; i++)
            {
                sb.Append("-").Append("p.p").Append(i);
            }
            exp.Expression = sb.ToString();
            sb.Clear();
            if (!CheckValueExist())
            {
                InsertValue(properties, "Value", default);
            }
            properties.StartAnimation("Value", exp);
        }


        private void ResetInnerProperties()
        {
            lock (locker)
            {
                properties.StopAnimation("Value");
                index = 0;
                completedIndex = -1;
                innerPropSet.Dispose();
                innerPropSet = Compositor.CreatePropertySet();
                exp.SetReferenceParameter("p", innerPropSet);
                InsertValue(innerPropSet, "e", lastValue);
                InsertValue(properties, "Value", lastValue);
            }
        }

        protected KeyFrameAnimation CreateKeyFrameAnimation(T from, T to)
        {
            {
                if (from is float fromv && to is float tov)
                {
                    var an = Compositor.CreateScalarKeyFrameAnimation();
                    an.InsertKeyFrame(0f, tov - fromv);
                    an.InsertKeyFrame(1f, default);
                    return an;
                }
            }

            {
                if (from is Vector2 fromv && to is Vector2 tov)
                {
                    var an = Compositor.CreateVector2KeyFrameAnimation();
                    an.InsertKeyFrame(0f, tov - fromv);
                    an.InsertKeyFrame(1f, default);
                    return an;
                }
            }

            {
                if (from is Vector3 fromv && to is Vector3 tov)
                {
                    var an = Compositor.CreateVector3KeyFrameAnimation();
                    an.InsertKeyFrame(0f, tov - fromv);
                    an.InsertKeyFrame(1f, default);
                    return an;
                }
            }

            {
                if (from is Vector4 fromv && to is Vector4 tov)
                {
                    var an = Compositor.CreateVector4KeyFrameAnimation();
                    an.InsertKeyFrame(0f, tov - fromv);
                    an.InsertKeyFrame(1f, default);
                    return an;
                }
            }

            {
                if (from is Quaternion fromv && to is Quaternion tov)
                {
                    var an = Compositor.CreateQuaternionKeyFrameAnimation();
                    an.InsertKeyFrame(0f, tov - fromv);
                    an.InsertKeyFrame(1f, default);
                    return an;
                }
            }

            return null;
        }

        protected void InsertValue(CompositionPropertySet prop, string name, T value)
        {
            { if (value is float v) prop.InsertScalar(name, v); }
            { if (value is Vector2 v) prop.InsertVector2(name, v); }
            { if (value is Vector3 v) prop.InsertVector3(name, v); }
            { if (value is Vector4 v) prop.InsertVector4(name, v); }
            { if (value is Quaternion v) prop.InsertQuaternion(name, v); }
        }

        protected bool CheckValueExist()
        {
            var status = CompositionGetValueStatus.NotFound;

            if (typeof(T) == typeof(float)) status = properties.TryGetScalar("Value", out _);
            if (typeof(T) == typeof(Vector2)) status = properties.TryGetVector2("Value", out _);
            if (typeof(T) == typeof(Vector3)) status = properties.TryGetVector3("Value", out _);
            if (typeof(T) == typeof(Vector4)) status = properties.TryGetVector4("Value", out _);
            if (typeof(T) == typeof(Quaternion)) status = properties.TryGetQuaternion("Value", out _);

            return status == CompositionGetValueStatus.Succeeded;
        }

        protected void CheckType()
        {
            if (typeof(T) != typeof(float) &&
                typeof(T) != typeof(Vector2) &&
                typeof(T) != typeof(Vector3) &&
                typeof(T) != typeof(Vector4) &&
                typeof(T) != typeof(Quaternion))
            {
                throw new ArgumentException($"不支持的类型: {typeof(T).Name}");
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
