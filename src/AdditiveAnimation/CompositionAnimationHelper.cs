using Microsoft.UI.Composition;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace AdditiveAnimationSample.AdditiveAnimation
{
    internal static class CompositionAnimationHelper
    {
        public static bool IsCompositionBaseType<T>()
        {
            var type = typeof(T);

            if (type == typeof(bool)) return true;
            else if (type == typeof(Color)) return true;
            else if (type == typeof(CompositionPath)) return true;
            else if (type == typeof(Quaternion)) return true;
            else if (type == typeof(float)) return true;
            else if (type == typeof(Vector2)) return true;
            else if (type == typeof(Vector3)) return true;
            else if (type == typeof(Vector4)) return true;
            else return false;
        }

        public static KeyFrameAnimation CreateKeyFrameAnimation<T>(Compositor compositor)
        {
            var type = typeof(T);

            if (type == typeof(bool)) return compositor.CreateBooleanKeyFrameAnimation();
            else if (type == typeof(Color)) return compositor.CreateColorKeyFrameAnimation();
            else if (type == typeof(CompositionPath)) return compositor.CreatePathKeyFrameAnimation();
            else if (type == typeof(Quaternion)) return compositor.CreateQuaternionKeyFrameAnimation();
            else if (type == typeof(float)) return compositor.CreateScalarKeyFrameAnimation();
            else if (type == typeof(Vector2)) return compositor.CreateVector2KeyFrameAnimation();
            else if (type == typeof(Vector3)) return compositor.CreateVector3KeyFrameAnimation();
            else if (type == typeof(Vector4)) return compositor.CreateVector4KeyFrameAnimation();
            else
            {
                ThrowInvalidOperationException<T>();
                return null!;
            }
        }

        public static void SetAnimationParameter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(CompositionAnimation animation, string key, T value)
        {
            if (value is null) return;

            var type = typeof(T);

            if (type == typeof(bool)) animation.SetBooleanParameter(key, ChangeType<T, bool>(value));
            else if (type == typeof(Color)) animation.SetColorParameter(key, ChangeType<T, Color>(value));
            else if (type == typeof(Quaternion)) animation.SetQuaternionParameter(key, ChangeType<T, Quaternion>(value));
            else if (type == typeof(float)) animation.SetScalarParameter(key, ChangeType<T, float>(value));
            else if (type == typeof(Vector2)) animation.SetVector2Parameter(key, ChangeType<T, Vector2>(value));
            else if (type == typeof(Vector3)) animation.SetVector3Parameter(key, ChangeType<T, Vector3>(value));
            else if (type == typeof(Vector4)) animation.SetVector4Parameter(key, ChangeType<T, Vector4>(value));
            else if (type == typeof(Matrix3x2)) animation.SetMatrix3x2Parameter(key, ChangeType<T, Matrix3x2>(value));
            else if (type == typeof(Matrix4x4)) animation.SetMatrix4x4Parameter(key, ChangeType<T, Matrix4x4>(value));
            else if (type.IsAssignableTo(typeof(CompositionPropertySet))) animation.SetReferenceParameter(key, (CompositionPropertySet)(object)value);
            else if (type.GetInterfaceMap(typeof(IAnimationObject)).TargetType != null) animation.SetExpressionReferenceParameter(key, (IAnimationObject)(object)value);
            else if (type.IsAssignableTo(typeof(CompositionObject))) animation.SetReferenceParameter(key, (CompositionObject)(object)value);
            else
            {
                ThrowInvalidOperationException<T>();
            }
        }

        public static void SetPropertySetValue<T>(CompositionPropertySet propertySet, string propertyName, T value)
        {
            var type = typeof(T);

            if (type == typeof(bool)) propertySet.InsertBoolean(propertyName, ChangeType<T, bool>(value));
            else if (type == typeof(Color)) propertySet.InsertColor(propertyName, ChangeType<T, Color>(value));
            else if (type == typeof(Quaternion)) propertySet.InsertQuaternion(propertyName, ChangeType<T, Quaternion>(value));
            else if (type == typeof(float)) propertySet.InsertScalar(propertyName, ChangeType<T, float>(value));
            else if (type == typeof(Vector2)) propertySet.InsertVector2(propertyName, ChangeType<T, Vector2>(value));
            else if (type == typeof(Vector3)) propertySet.InsertVector3(propertyName, ChangeType<T, Vector3>(value));
            else if (type == typeof(Vector4)) propertySet.InsertVector4(propertyName, ChangeType<T, Vector4>(value));
            else if (type == typeof(Matrix3x2)) propertySet.InsertMatrix3x2(propertyName, ChangeType<T, Matrix3x2>(value));
            else if (type == typeof(Matrix4x4)) propertySet.InsertMatrix4x4(propertyName, ChangeType<T, Matrix4x4>(value));
            else
            {
                ThrowInvalidOperationException<T>();
            }
        }

        public static CompositionGetValueStatus TryGetPropertySetValue<T>(CompositionPropertySet propertySet, string propertyName, out T value)
        {
            var type = typeof(T);

            CompositionGetValueStatus result = CompositionGetValueStatus.TypeMismatch;
            value = default;

            if (type == typeof(bool)) { result = propertySet.TryGetBoolean(propertyName, out var tmpValue); value = ChangeType<bool, T>(tmpValue); }
            else if (type == typeof(Color)) { result = propertySet.TryGetColor(propertyName, out var tmpValue); value = ChangeType<Color, T>(tmpValue); }
            else if (type == typeof(Quaternion)) { result = propertySet.TryGetQuaternion(propertyName, out var tmpValue); value = ChangeType<Quaternion, T>(tmpValue); }
            else if (type == typeof(float)) { result = propertySet.TryGetScalar(propertyName, out var tmpValue); value = ChangeType<float, T>(tmpValue); }
            else if (type == typeof(Vector2)) { result = propertySet.TryGetVector2(propertyName, out var tmpValue); value = ChangeType<Vector2, T>(tmpValue); }
            else if (type == typeof(Vector3)) { result = propertySet.TryGetVector3(propertyName, out var tmpValue); value = ChangeType<Vector3, T>(tmpValue); }
            else if (type == typeof(Vector4)) { result = propertySet.TryGetVector4(propertyName, out var tmpValue); value = ChangeType<Vector4, T>(tmpValue); }
            else if (type == typeof(Matrix3x2)) { result = propertySet.TryGetMatrix3x2(propertyName, out var tmpValue); value = ChangeType<Matrix3x2, T>(tmpValue); }
            else if (type == typeof(Matrix4x4)) { result = propertySet.TryGetMatrix4x4(propertyName, out var tmpValue); value = ChangeType<Matrix4x4, T>(tmpValue); }

            return result;
        }

        public static T Add<T>(T v1, T v2)
        {
            var type = typeof(T);

            if (type == typeof(bool)) return v2;
            else if (type == typeof(Color)) return ChangeType<Color, T>(SumColor(ChangeType<T, Color>(v1), ChangeType<T, Color>(v2)));
            else if (type == typeof(CompositionPath)) return v2;
            else if (type == typeof(Quaternion)) return ChangeType<Quaternion, T>(ChangeType<T, Quaternion>(v1) + ChangeType<T, Quaternion>(v2));
            else if (type == typeof(float)) return ChangeType<float, T>(ChangeType<T, float>(v1) + ChangeType<T, float>(v2));
            else if (type == typeof(Vector2)) return ChangeType<Vector2, T>(ChangeType<T, Vector2>(v1) + ChangeType<T, Vector2>(v2));
            else if (type == typeof(Vector3)) return ChangeType<Vector3, T>(ChangeType<T, Vector3>(v1) + ChangeType<T, Vector3>(v2));
            else if (type == typeof(Vector4)) return ChangeType<Vector4, T>(ChangeType<T, Vector4>(v1) + ChangeType<T, Vector4>(v2));
            else
            {
                ThrowInvalidOperationException<T>();
                return default;
            }

            static Color SumColor(Color color1, Color color2)
            {
                unchecked
                {
                    return Color.FromArgb((byte)(color1.A + color2.A), (byte)(color1.R + color2.R), (byte)(color1.G + color2.G), (byte)(color1.B + color2.B));
                }
            }
        }

        public static T Subtract<T>(T v1, T v2)
        {
            var type = typeof(T);

            if (type == typeof(bool)) return v2;
            else if (type == typeof(Color)) return ChangeType<Color, T>(SubtractionColor(ChangeType<T, Color>(v1), ChangeType<T, Color>(v2)));
            else if (type == typeof(CompositionPath)) return v2;
            else if (type == typeof(Quaternion)) return ChangeType<Quaternion, T>(ChangeType<T, Quaternion>(v1) - ChangeType<T, Quaternion>(v2));
            else if (type == typeof(float)) return ChangeType<float, T>(ChangeType<T, float>(v1) - ChangeType<T, float>(v2));
            else if (type == typeof(Vector2)) return ChangeType<Vector2, T>(ChangeType<T, Vector2>(v1) - ChangeType<T, Vector2>(v2));
            else if (type == typeof(Vector3)) return ChangeType<Vector3, T>(ChangeType<T, Vector3>(v1) - ChangeType<T, Vector3>(v2));
            else if (type == typeof(Vector4)) return ChangeType<Vector4, T>(ChangeType<T, Vector4>(v1) - ChangeType<T, Vector4>(v2));
            else
            {
                ThrowInvalidOperationException<T>();
                return default;
            }

            static Color SubtractionColor(Color color1, Color color2)
            {
                unchecked
                {
                    return Color.FromArgb((byte)(color1.A - color2.A), (byte)(color1.R - color2.R), (byte)(color1.G - color2.G), (byte)(color1.B - color2.B));
                }
            }
        }

        private static unsafe TTo ChangeType<TFrom, TTo>(in TFrom fromValue)
        {
#pragma warning disable 8500
            fixed (TFrom* ptr = &fromValue)
            {
                return *(TTo*)(ptr);
            }
#pragma warning restore 8500
        }

        internal static void ThrowInvalidOperationException<T>()
        {
            throw new InvalidOperationException($"Can't create objects for type '{typeof(T).Name}'");
        }
    }
}
