using AdditiveAnimationSample.AdditiveAnimation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AdditiveAnimationSample
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            LayoutRoot.Loaded += (s, a) =>
            {
                var centerPointBind = Compositor.CreateExpressionAnimation("Vector3(this.Target.Size.X / 2, this.Target.Size.Y / 2, 0)");
                FullViewContainer.GetVisualInternal().StartAnimation("CenterPoint", centerPointBind);
                IconBorder.GetVisualInternal().StartAnimation("CenterPoint", centerPointBind);

                var offsetBind = Compositor.CreateExpressionAnimation("Vector3(0, icon.Size.Y / 2 - this.Target.Size.Y / 2 + icon.Offset.Y, 0)");
                offsetBind.SetReferenceParameter("icon", IconBorder.GetVisualInternal());
                FullViewContainer.GetVisualInternal().StartAnimation("Offset", offsetBind);

                var scaleBind = Compositor.CreateExpressionAnimation("Vector3(icon.Size.X / this.Target.Size.X * icon.Scale.X, icon.Size.Y / this.Target.Size.Y * icon.Scale.Y, 1)");
                scaleBind.SetReferenceParameter("icon", IconBorder.GetVisualInternal());
                FullViewContainer.GetVisualInternal().StartAnimation("Scale", scaleBind);

                var opacityBind = Compositor.CreateExpressionAnimation("1 - icon.Opacity");
                opacityBind.SetReferenceParameter("icon", IconBorder.GetVisualInternal());
                FullViewContainer.GetVisualInternal().StartAnimation("Opacity", opacityBind);


            };
        }

        private bool opened = false;

        private void myButton_Click(object sender, RoutedEventArgs e)
        {
            var iconSize = new Vector2(80, 80);
            var iconOffsetOriginal = new Vector3(
                (float)((LayoutRoot.ActualWidth - iconSize.X) / 2),
                (float)(LayoutRoot.ActualHeight - iconSize.Y - 60),
                0);

            var iconVisual = IconBorder.GetVisualInternal();
            var fullViewVisual = FullView.GetVisualInternal();

            iconVisual.Properties.InsertVector3("Offset", iconOffsetOriginal);


            if (!opened)
            {
                var duration = TimeSpan.FromSeconds(0.35);
                var fullViewSize = new Vector2(
                    (float)(LayoutRoot.ActualWidth),
                    (float)(LayoutRoot.ActualHeight));
                var iconScaleTo = new Vector3(fullViewSize.X / iconSize.X, fullViewSize.Y / iconSize.Y, 1);
                var iconOffsetTo = new Vector3((fullViewSize.X - iconSize.X) / 2, (fullViewSize.Y - iconSize.Y) / 2, 0);

                var scaleEasingFunc = CompositionEasingFunction.CreateCubicBezierEasingFunction(Compositor, new Vector2(.6f, .29f), new Vector2(.43f, .98f));

                AdditiveAnimationService.StartAnimation(iconVisual, "Opacity", 0f, duration, null);
                AdditiveAnimationService.StartAnimation(iconVisual, "Scale", iconScaleTo, duration, scaleEasingFunc);
                AdditiveAnimationService.StartAnimation(iconVisual, "Offset", iconOffsetTo, duration, null);
            }
            else
            {
                var duration = TimeSpan.FromSeconds(0.4);
                var scaleEasingFunc = CompositionEasingFunction.CreateBackEasingFunction(Compositor, CompositionEasingFunctionMode.Out, 0.18f);
                var offsetEasingFunc = CompositionEasingFunction.CreateBackEasingFunction(Compositor, CompositionEasingFunctionMode.Out, 1f);

                AdditiveAnimationService.StartAnimation(iconVisual, "Opacity", 1f, duration, null);
                AdditiveAnimationService.StartAnimation(iconVisual, "Scale", Vector3.One, duration, scaleEasingFunc);
                AdditiveAnimationService.StartAnimation(iconVisual, "Offset", iconOffsetOriginal, duration, offsetEasingFunc);
            }
            opened = !opened;
        }
    }
}
