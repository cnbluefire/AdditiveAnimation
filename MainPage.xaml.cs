using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Timers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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

            CoreWindow.GetForCurrentThread().PointerPressed += MainPage_PointerPressed;
            CoreWindow.GetForCurrentThread().PointerMoved += MainPage_PointerMoved;
        }

        private Compositor Compositor => Window.Current.Compositor;
        private List<TranslationFastVisual> translationVisuals = new List<TranslationFastVisual>();

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            var container = Compositor.CreateContainerVisual();

            var colors = typeof(Colors).GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty)
                .Select(c => c.GetValue(null))
                .OfType<Color>()
                .ToList();

            ExpressionAnimation exp = null;

            for (int i = 1; i < 20; i++)
            {
                var c = colors[i % colors.Count];
                var t = new TranslationFastVisual(
                    compositor: Compositor,
                    size: new Vector2(50, 50),
                    offset: Vector3.Zero,
                    color: c,
                    dampingRatio: 0.3f);

                if (exp != null)
                {
                    t.Translation.StartExpression(exp);
                }

                exp = Compositor.CreateExpressionAnimation("prop.Value");
                exp.SetReferenceParameter("prop", t.Translation.Properties);

                translationVisuals.Add(t);
                container.Children.InsertAtBottom(t.Visual);
            }

            ElementCompositionPreview.SetElementChildVisual(this, container);
        }


        private void MainPage_PointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            var position = args.CurrentPoint.Position;
            var to = new Vector3((float)(position.X - 25), (float)(position.Y - 25), 0f);

            //translationVisuals.ForEach(c => c.Translation.Stop(to));
            translationVisuals[0].Translation.Stop(to);
        }


        private void MainPage_PointerMoved(CoreWindow sender, PointerEventArgs args)
        {
            var position = args.CurrentPoint.Position;
            var to = new Vector3((float)(position.X - 25), (float)(position.Y - 25), 0f);
            //translationVisuals.ForEach(c => c.Translation.Animate(to));
            translationVisuals[0].Translation.Animate(to);
        }
    }

    public class TranslationVisual
    {
        public Visual Visual { get; private set; }
        public AdditiveValue<Vector3> Translation { get; private set; }

        public TranslationVisual(Compositor compositor, Vector2 size, Vector3 offset, Color color, int durationInMillis)
        {
            var trans = AdditiveValue.Create(compositor, Vector3.Zero)
                .SetDuration(durationInMillis);
            Translation = trans;

            var sv = compositor.CreateSpriteVisual();
            sv.Size = size;
            sv.Brush = compositor.CreateColorBrush(color);

            var exp = compositor.CreateExpressionAnimation("offset + trans.Value");
            exp.SetVector3Parameter("offset", offset);
            exp.SetReferenceParameter("trans", trans.Properties);

            sv.StartAnimation("Offset", exp);
            Visual = sv;
        }
    }

    public class TranslationFastVisual
    {
        public Visual Visual { get; private set; }
        public AdditiveValueFast<Vector3> Translation { get; private set; }

        public TranslationFastVisual(Compositor compositor, Vector2 size, Vector3 offset, Color color, float dampingRatio)
        {
            var trans = AdditiveValueFast.Create(compositor, Vector3.Zero)
                .SetDampingRatio(dampingRatio);
            Translation = trans;

            var sv = compositor.CreateSpriteVisual();
            sv.Size = size;
            sv.Brush = compositor.CreateColorBrush(color);

            var exp = compositor.CreateExpressionAnimation("offset + trans.Value");
            exp.SetVector3Parameter("offset", offset);
            exp.SetReferenceParameter("trans", trans.Properties);

            sv.StartAnimation("Offset", exp);
            Visual = sv;
        }
    }
}
