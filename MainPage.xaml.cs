using System;
using System.Numerics;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace AdditiveAnimation
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private Compositor Compositor => Window.Current.Compositor;

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            var container = Compositor.CreateContainerVisual();

            ElementCompositionPreview.SetElementChildVisual(this, container);

            var visual = Compositor.CreateSpriteVisual();
            visual.Brush = Compositor.CreateColorBrush(Colors.Green);
            visual.Size = new Vector2(100, 100);
            container.Children.InsertAtTop(visual);

            var timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(10),
            };
            Vector3 position = default;
            Vector3 lastOffset = visual.Offset;
            long lastAnimationTimestamp = 0;

            this.PointerMoved += (s, a) =>
            {
                position = new Vector3(a.GetCurrentPoint(this).Position.ToVector2(), 0);
            };

            timer.Tick += (s, a) =>
            {
                var offset = position - new Vector3(visual.Size / 2, 0);

                if (lastOffset != offset)
                {
                    visual.StopAnimation("Offset");

                    var animationTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    var springAnimation = Compositor.CreateSpringVector3Animation();
                    springAnimation.InitialValueExpressions["FinalValue"] = "offset";
                    springAnimation.InitialValueExpressions["InitialVelocity"] = "(lastOffset - this.StartingValue) / Vector3(duration,duration,duration)";

                    springAnimation.SetVector3Parameter("offset", offset);
                    springAnimation.SetVector3Parameter("lastOffset", lastOffset);
                    springAnimation.SetScalarParameter("duration", (animationTimestamp - lastAnimationTimestamp) / 1000f);

                    springAnimation.StopBehavior = AnimationStopBehavior.LeaveCurrentValue;

                    visual.StartAnimation("Offset", springAnimation);
                    lastOffset = offset;
                    lastAnimationTimestamp = animationTimestamp;
                }
            };
            timer.Start();

        }
    }
}
