using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;

namespace AdditiveAnimation
{
    public sealed class ThrottleProvider<T> : IDisposable where T : struct
    {
        public ThrottleProvider()
        {
            timer.Elapsed += Timer_Elapsed;
        }

        private Timer timer = new Timer();
        private T? nextValue;
        private T? lastValue;

        public double Interval
        {
            get => timer.Interval;
            set => timer.Interval = value;
        }

        public ThrottleProvider<T> SetInterval(double millis)
        {
            Interval = millis;
            return this;
        }

        public void SetValue(T value)
        {
            if (!nextValue.HasValue || !EqualityComparer<T>.Default.Equals(nextValue.Value, value))
            {
                nextValue = value;
                if (!timer.Enabled)
                {
                    OnTimerElapsed();
                    timer.Start();
                }
            }
        }


        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            OnTimerElapsed();
        }

        private void OnTimerElapsed()
        {
            if (!nextValue.HasValue ||
                lastValue.HasValue && EqualityComparer<T>.Default.Equals(nextValue.Value, lastValue.Value))
            {
                timer.Stop();
            }
            else
            {
                lastValue = nextValue;
                OnElapsed();
            }
        }

        public event EventHandler<ThrottleProviderElapsedEventArgs<T>> Elapsed;

        private void OnElapsed()
        {
            if (lastValue.HasValue)
            {
                Elapsed?.Invoke(this, new ThrottleProviderElapsedEventArgs<T>(lastValue.Value));
            }
        }

        public void Dispose()
        {
            timer.Dispose();
            timer = null;
        }


    }

    public class ThrottleProviderElapsedEventArgs<T> where T : struct
    {
        public ThrottleProviderElapsedEventArgs(T value)
        {
            Value = value;
        }

        public T Value { get; }
    }
}
