using Microsoft.UI.Composition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace AdditiveAnimationSample.AdditiveAnimation
{
    public static class AdditiveAnimationService
    {
        private static List<AdditiveAnimationEntry> entries = new List<AdditiveAnimationEntry>();

        public static void StartAnimation<T>(CompositionObject obj, string propertyName, T toValue, TimeSpan duration, CompositionEasingFunction? easingFunction)
        {
            if (!CompositionAnimationHelper.IsCompositionBaseType<T>())
            {
                CompositionAnimationHelper.ThrowInvalidOperationException<T>();
                return;
            }

            lock (entries)
            {
                AdditiveAnimationEntry? entry = null;
                for (var i = 0; i < entries.Count; i++)
                {
                    if (entries[i].CompositionObject == obj && entries[i].PropertyName == propertyName)
                    {
                        entry = entries[i];
                        break;
                    }
                }
                if (entry == null)
                {
                    entry = new AdditiveAnimationEntry(obj, propertyName);
                    entry.StartAnimation(toValue, duration, easingFunction);
                    entries.Add(entry);

                    entry.Completed += (s, a) =>
                    {
                        lock (entries)
                        {
                            entries.Remove(entry);
                            obj.StopAnimation(propertyName);
                            CompositionAnimationHelper.SetPropertySetValue(obj.Properties, propertyName, (T)a.FinalValue);
                            entry.Dispose();
                        }
                    };
                }
                else
                {
                    entry.StartAnimation(toValue, duration, easingFunction);
                }

            }
        }

        public static bool StopAnimation<T>(CompositionObject obj, string propertyName)
        {
            lock (entries)
            {
                AdditiveAnimationEntry? entry = null;
                for (var i = 0; i < entries.Count; i++)
                {
                    if (entries[i].CompositionObject == obj && entries[i].PropertyName == propertyName)
                    {
                        entry = entries[i];
                        break;
                    }
                }

                if (entry != null)
                {
                    var finalValue = entry.FinalValue;
                    entries.Remove(entry);
                    obj.StopAnimation(propertyName);
                    CompositionAnimationHelper.SetPropertySetValue(obj.Properties, propertyName, (T)finalValue);
                    entry.Dispose();

                    return true;
                }

                return false;
            }
        }
    }
}
