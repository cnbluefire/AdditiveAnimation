using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Windows.UI.Composition;

namespace AdditiveAnimation
{
    public sealed class AdditiveValueFast<T> : IDisposable where T : struct
    {
        #region Default Values
        
        private static TimeSpan defaultDuration = TimeSpan.FromMilliseconds(1000 / 60);

        #endregion Default Values

        #region Ctor

        public AdditiveValueFast(Compositor compositor) : this(compositor, default) { }

        public AdditiveValueFast(Compositor compositor, T initValue)
        {
            Compositor = compositor;
            Properties = Compositor.CreatePropertySet();
            innerPropSet = Compositor.CreatePropertySet();
            AdditiveValueHelper.InsertValue(innerPropSet, "end", initValue);
            AdditiveValueHelper.InsertValue(Properties, "Value", initValue);
            DampingRatio = 0.33333f;
            easingFunc = Compositor.CreateLinearEasingFunction();
            resetTimer = new Timer(5000);
            resetTimer.Elapsed += ResetTimer_Elapsed;
        }

        #endregion Ctor

        #region Fields
        
        private CompositionPropertySet innerPropSet;
        private KeyFrameAnimation animation;
        private CompositionEasingFunction easingFunc;
        private float dampingRatio;
        private Timer resetTimer;
        private object locker = new object();
        private bool stoped = true;
        private bool hasEndExpression = false;

        #endregion Fields

        #region Properties
        
        public Compositor Compositor { get; }

        public CompositionPropertySet Properties { get; private set; }

        public float DampingRatio
        {
            get => dampingRatio;
            set
            {
                if (value <= 0 || value >= 1)
                {
                    throw new ArgumentException("DampingRatio必须在0和1之间");
                }
                dampingRatio = value;
                innerPropSet.InsertScalar("dampingRatio", dampingRatio);
            }
        }

        #endregion Properties

        #region Methods

        public AdditiveValueFast<T> SetDampingRatio(double value)
        {
            DampingRatio = (float)value;
            return this;
        }

        public AdditiveValueFast<T> SetDampingRatio(float value)
        {
            DampingRatio = value;
            return this;
        }

        public void Stop(T to)
        {
            lock (locker)
            {
                if (hasEndExpression)
                {
                    innerPropSet.StopAnimation("end");
                }
                Properties.StopAnimation("Value");
                resetTimer.Stop();
                stoped = true;
                AdditiveValueHelper.InsertValue(innerPropSet, "end", to);
                AdditiveValueHelper.InsertValue(Properties, "Value", to);
            }
        }

        public void Animate(T to)
        {
            lock (locker)
            {
                if (hasEndExpression)
                {
                    innerPropSet.StopAnimation("end");
                }
                if (resetTimer == null)
                {
                    throw new ObjectDisposedException("");
                }
                if (!resetTimer.Enabled)
                {
                    resetTimer.Start();
                }
                CreateAnimation();
                if (stoped)
                {
                    stoped = false;
                    Properties.StartAnimation("Value", animation);
                }
                AdditiveValueHelper.InsertValue(innerPropSet, "end", to);
            }
        }

        public void StartExpression(ExpressionAnimation exp)
        {
            lock (locker)
            {
                if (resetTimer == null)
                {
                    throw new ObjectDisposedException("");
                }
                if (!resetTimer.Enabled)
                {
                    resetTimer.Start();
                }
                CreateAnimation();
                if (stoped)
                {
                    stoped = false;
                    Properties.StartAnimation("Value", animation);
                }
                innerPropSet.StartAnimation("end", exp);
            }
        }


        public void Dispose()
        {
            lock (locker)
            {
                resetTimer.Stop();
                resetTimer.Dispose();
                resetTimer = null;
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

        private void ResetAnimation()
        {
            lock (locker)
            {
                Properties.StopAnimation("Value");
                animation.Dispose();
                animation = null;
                CreateAnimation();
            }
        }

        private void CreateAnimation()
        {
            lock (locker)
            {
                if (animation == null)
                {
                    animation = AdditiveValueHelper.CreateKeyFrameAnimation<T>(Compositor);
                    animation.InsertExpressionKeyFrame(0f, "this.CurrentValue");
                    animation.InsertExpressionKeyFrame(1f, "this.CurrentValue + (prop.end - this.CurrentValue) * prop.dampingRatio", easingFunc);
                    animation.Duration = defaultDuration;
                    animation.IterationBehavior = AnimationIterationBehavior.Forever;
                    animation.SetReferenceParameter("prop", innerPropSet);
                    Properties.StartAnimation("Value", animation);
                }
            }
        }

        private void ResetTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ResetAnimation();
        }


        #endregion Utilities

    }

    public class AdditiveValueFast
    {
        #region Factory
        public static AdditiveValueFast<T> Create<T>(Compositor compositor) where T : struct
        {
            return new AdditiveValueFast<T>(compositor);
        }

        public static AdditiveValueFast<T> Create<T>(Compositor compositor, T initValue) where T : struct
        {
            return new AdditiveValueFast<T>(compositor, initValue);
        }

        #endregion Factory
    }
}
