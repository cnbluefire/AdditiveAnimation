using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Composition;

namespace AdditiveAnimation
{
    public static class AdditiveValueHelper
    {
        public static KeyFrameAnimation CreateKeyFrameAnimation<T>(Compositor compositor) where T : struct
        {
            if (typeof(T) == typeof(float)) return compositor.CreateScalarKeyFrameAnimation();
            if (typeof(T) == typeof(Vector2)) return compositor.CreateVector2KeyFrameAnimation();
            if (typeof(T) == typeof(Vector3)) return compositor.CreateVector3KeyFrameAnimation();
            if (typeof(T) == typeof(Vector4)) return compositor.CreateVector4KeyFrameAnimation();
            if (typeof(T) == typeof(Quaternion)) return compositor.CreateQuaternionKeyFrameAnimation();

            throw new ArgumentException($"不支持的类型: {typeof(T).Name}");
        }


        public static KeyFrameAnimation CreateKeyFrameAnimation<T>(Compositor compositor, T from, T to) where T : struct
        {
            {
                if (from is float fromv && to is float tov)
                {
                    var an = compositor.CreateScalarKeyFrameAnimation();
                    an.InsertKeyFrame(0f, tov - fromv);
                    an.InsertKeyFrame(1f, default);
                    return an;
                }
            }

            {
                if (from is Vector2 fromv && to is Vector2 tov)
                {
                    var an = compositor.CreateVector2KeyFrameAnimation();
                    an.InsertKeyFrame(0f, tov - fromv);
                    an.InsertKeyFrame(1f, default);
                    return an;
                }
            }

            {
                if (from is Vector3 fromv && to is Vector3 tov)
                {
                    var an = compositor.CreateVector3KeyFrameAnimation();
                    an.InsertKeyFrame(0f, tov - fromv);
                    an.InsertKeyFrame(1f, default);
                    return an;
                }
            }

            {
                if (from is Vector4 fromv && to is Vector4 tov)
                {
                    var an = compositor.CreateVector4KeyFrameAnimation();
                    an.InsertKeyFrame(0f, tov - fromv);
                    an.InsertKeyFrame(1f, default);
                    return an;
                }
            }

            {
                if (from is Quaternion fromv && to is Quaternion tov)
                {
                    var an = compositor.CreateQuaternionKeyFrameAnimation();
                    an.InsertKeyFrame(0f, tov - fromv);
                    an.InsertKeyFrame(1f, default);
                    return an;
                }
            }

            return null;
        }


        public static void InsertValue<T>(CompositionPropertySet prop, string name, T value) where T : struct
        {
            { if (value is float v) prop.InsertScalar(name, v); }
            { if (value is Vector2 v) prop.InsertVector2(name, v); }
            { if (value is Vector3 v) prop.InsertVector3(name, v); }
            { if (value is Vector4 v) prop.InsertVector4(name, v); }
            { if (value is Quaternion v) prop.InsertQuaternion(name, v); }
        }

        public static CompositionGetValueStatus TryGetValue<T>(CompositionPropertySet properties, string propName, out T value)
        {
            var status = CompositionGetValueStatus.NotFound;

            value = default;

            if (typeof(T) == typeof(float))
            {
                status = properties.TryGetScalar(propName, out var v);
                value = (T)(object)v;
            }
            if (typeof(T) == typeof(Vector2))
            {
                status = properties.TryGetVector2(propName, out var v);
                value = (T)(object)v;
            }
            if (typeof(T) == typeof(Vector3))
            {
                status = properties.TryGetVector3(propName, out var v);
                value = (T)(object)v;
            }
            if (typeof(T) == typeof(Vector4))
            {
                status = properties.TryGetVector4(propName, out var v);
                value = (T)(object)v;
            }
            if (typeof(T) == typeof(Quaternion))
            {
                status = properties.TryGetQuaternion(propName, out var v);
                value = (T)(object)v;
            }

            return status;
        }

        public static bool CheckValueExist<T>(CompositionPropertySet properties, string propName) where T : struct
        {
            var status = CompositionGetValueStatus.NotFound;

            if (typeof(T) == typeof(float)) status = properties.TryGetScalar(propName, out _);
            if (typeof(T) == typeof(Vector2)) status = properties.TryGetVector2(propName, out _);
            if (typeof(T) == typeof(Vector3)) status = properties.TryGetVector3(propName, out _);
            if (typeof(T) == typeof(Vector4)) status = properties.TryGetVector4(propName, out _);
            if (typeof(T) == typeof(Quaternion)) status = properties.TryGetQuaternion(propName, out _);

            return status == CompositionGetValueStatus.Succeeded;
        }

        public static void CheckType<T>() where T : struct
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
    }
}
