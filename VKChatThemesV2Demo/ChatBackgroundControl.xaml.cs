using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Composition;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.Web.Http;
using System.Numerics;
using Windows.Foundation;
using Windows.Graphics.DirectX;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Microsoft.Graphics.Canvas.Svg;
using Windows.UI.Popups;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace VKChatThemesV2Demo {
    public sealed partial class ChatBackgroundControl : UserControl {
        long id = 0;

        public static readonly DependencyProperty ChatStyleProperty = DependencyProperty.Register(
            nameof(ChatStyle), typeof(Style), typeof(ChatBackgroundControl), new PropertyMetadata(default));

        public Style ChatStyle {
            get { return (Style)GetValue(ChatStyleProperty); }
            set { SetValue(ChatStyleProperty, value); }
        }

        SpriteVisual blurVisual;
        SpriteVisual svgVisual;
        GaussianBlurEffect effect;
        CompositionEffectBrush brush;
        Blur blur; // for vector background
        HttpClient hc = new HttpClient();

        const double RADIUS_DIVIDE = 2.5; // необходимо для уменьшения blur.radius у векторных фонов настолько,
                                        // чтобы размытые круги как можно ближе соответствовало тем, как они отображаются в офклиенте

        public ChatBackgroundControl() {
            this.InitializeComponent();
            Loaded += ChatBackgroundControl_Loaded;
        }

        private void ChatBackgroundControl_Loaded(object sender, RoutedEventArgs e) {
            SetUp(this, ChatStyleProperty);

            id = RegisterPropertyChangedCallback(ChatStyleProperty, SetUp);
            Unloaded += (a, b) => {
                Loaded -= ChatBackgroundControl_Loaded;
                if (id != 0) UnregisterPropertyChangedCallback(ChatStyleProperty, id);
            };
        }

        private void SetUp(DependencyObject sender, DependencyProperty dp) {
            EllipsesRoot.Children.Clear();
            Gradient.Fill = null;
            ElementCompositionPreview.SetElementChildVisual(BlurLayer, null);
            ElementCompositionPreview.SetElementChildVisual(SVGBackgroundLayer, null);
            OpacityLayer.Opacity = 0;
            blur = null;
            dbg.Text = String.Empty;

            Style style = (Style)GetValue(dp);
            if (style == null) return;

            var background = App.Styles.Backgrounds.Where(b => b.Id == style.BackgroundId).FirstOrDefault();
            if (background == null) return;

            SetupBackground(App.Current.RequestedTheme == ApplicationTheme.Dark ? background.Dark : background.Light);
            // SetupBackground(background.Dark);
        }

        private async void SetupBackground(BackgroundSources source) {
            if (source.Type == "vector") {
                try {
                    SetupGradient(source.Vector.Gradient);
                    double radius = source.Vector.Blur != null ? source.Vector.Blur.Radius : 0;
                    SetupEllipses(source.Vector.ColorEllipses, radius);
                    SetupBlur(source.Vector.Blur, source.Vector.ColorEllipses.Count > 0);
                    SetupSVGBackground(source.Vector.SVG);
                } catch (Exception ex) {
                    await new MessageDialog(ex.Message, $"Error 0x{ex.HResult.ToString("x8")}").ShowAsync();
                }
            }
        }

        private void SetupGradient(Gradient gradient) {
            LinearGradientBrush brush = new LinearGradientBrush {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1)
            };

            brush.RelativeTransform = new RotateTransform {
                CenterX = 0.5,
                CenterY = 0.5,
                Angle = gradient.Angle
            };

            double offsetStep = 1.0 / (gradient.Colors.Count - 1);
            double currentOffset = 1;

            foreach (var color in gradient.Colors) {
                brush.GradientStops.Add(new GradientStop {
                    Offset = currentOffset,
                    Color = ParseHex(color)
                });
                currentOffset = currentOffset - offsetStep;
            }

            Gradient.Fill = brush;
        }

        private void SetupEllipses(List<ColorEllipse> ellipses, double blurRadius) {
            blurRadius = blurRadius / RADIUS_DIVIDE;
            foreach (ColorEllipse e in ellipses) {
                var color = ParseHex(e.Color);
                Ellipse ellipse = new Ellipse {
                    Width = e.RadiusX * EllipsesRoot.Width + (blurRadius / 2.5),
                    Height = e.RadiusY * EllipsesRoot.Height + (blurRadius / 2.5),
                    Fill = new SolidColorBrush(color)
                };
                Canvas.SetLeft(ellipse, e.X * EllipsesRoot.Width - (ellipse.Width / 2.5));
                Canvas.SetTop(ellipse, e.Y * EllipsesRoot.Height - (ellipse.Height / 2.5));
                EllipsesRoot.Children.Add(ellipse);
            }
        }

        private void SetupBlur(Blur blur, bool hasEllipses) {
            if (blur == null) return;
            dbg.Text = $"Opacity: {blur.Opacity}\nRadius: {blur.Radius}\nColor: {blur.Color}";

            OpacityLayer.Fill = new SolidColorBrush(ParseHex(blur.Color));
            OpacityLayer.Opacity = blur.Opacity;

            if (!hasEllipses || blur.Opacity == 1) return;

            this.blur = blur;
            var visual = ElementCompositionPreview.GetElementVisual(BlurLayer);
            var compositor = visual.Compositor;
            blurVisual = compositor.CreateSpriteVisual();
            blurVisual.Size = new Vector2((float)ActualWidth, (float)ActualHeight);

            // Blur amout more than 250 is not allowed in UWP!
            effect = new GaussianBlurEffect {
                Name = "Blur",
                BlurAmount = Math.Min(((float)blur.Radius / (float)RADIUS_DIVIDE) / 640f * (float)ActualWidth, 250f),
                BorderMode = EffectBorderMode.Hard,
                Optimization = EffectOptimization.Speed,
                Source = new CompositionEffectSourceParameter("source")
            };

            var bb = compositor.CreateBackdropBrush();
            var factory = compositor.CreateEffectFactory(effect, new[] { "Blur.BlurAmount" });
            brush = factory.CreateBrush();
            blurVisual.Brush = brush;
            brush.SetSourceParameter("source", compositor.CreateBackdropBrush());

            ElementCompositionPreview.SetElementChildVisual(BlurLayer, blurVisual);
        }

        private async void SetupSVGBackground(VectorBackgroundSource svg) {
            bool isModernWindows = ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7);
            var scale = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

            double nw = svg.Width;
            double nh = svg.Height;

            var response = await hc.GetAsync(new Uri(svg.Url));
            string xml = await response.Content.ReadAsStringAsync();

            // Replacing width and height in svg (bad way)
            if (scale > 1) {
                nw = svg.Width * scale;
                nh = svg.Height * scale;
                xml = xml.Replace($"width=\"{svg.Width}\"", $"width=\"{nw}\"");
                xml = xml.Replace($"height=\"{svg.Height}\"", $"height=\"{nh}\"");
            }

            var compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
            var canvasDevice = CanvasDevice.GetSharedDevice();
            var doc = CanvasSvgDocument.LoadFromXml(canvasDevice, xml);
            var graphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(compositor, canvasDevice);

            var drawingSurface = graphicsDevice.CreateDrawingSurface(new Size(nw, nh),
                DirectXPixelFormat.B8G8R8A8UIntNormalized, DirectXAlphaMode.Premultiplied);
            using (var ds = CanvasComposition.CreateDrawingSession(drawingSurface)) {
                ds.Clear(Colors.Transparent);
                ds.DrawSvg(doc, new Size(nw, nh));
            }

            var surfaceBrush = compositor.CreateSurfaceBrush(drawingSurface);
            surfaceBrush.Stretch = CompositionStretch.None;
            surfaceBrush.Scale = new Vector2(1 / (float)scale);

            var border = new BorderEffect {
                ExtendX = CanvasEdgeBehavior.Wrap,
                ExtendY = CanvasEdgeBehavior.Wrap,
                Source = new CompositionEffectSourceParameter("source")
            };

            var fxFactory = compositor.CreateEffectFactory(border);
            var fxBrush = fxFactory.CreateBrush();
            fxBrush.SetSourceParameter("source", surfaceBrush);

            CompositionEffectBrush cebrush = fxBrush;
            if (isModernWindows) {
                var blend = new BlendEffect {
                    Background = new CompositionEffectSourceParameter("Main"),
                    Foreground = new CompositionEffectSourceParameter("Tint"),
                    Mode = BlendEffectMode.Overlay
                };

                var blendFactory = compositor.CreateEffectFactory(blend);
                cebrush = blendFactory.CreateBrush();
                cebrush.SetSourceParameter("Main", fxBrush);
                cebrush.SetSourceParameter("Tint", blurVisual.Brush);
            }

            svgVisual = compositor.CreateSpriteVisual();
            svgVisual.Size = new Vector2((float)ActualWidth, (float)ActualHeight);
            svgVisual.Opacity = isModernWindows ? (float)svg.Opacity : 0.35f;
            svgVisual.Brush = cebrush;

            ElementCompositionPreview.SetElementChildVisual(SVGBackgroundLayer, svgVisual);
        }

        private static Color ParseHex(string hex) {
            string aa, rs, gs, bs;

            if (hex.Length == 7) {
                aa = String.Empty;
                rs = hex.Substring(1, 2);
                gs = hex.Substring(3, 2);
                bs = hex.Substring(5, 2);
            } else if (hex.Length == 9) {
                aa = hex.Substring(1, 2);
                rs = hex.Substring(3, 2);
                gs = hex.Substring(5, 2);
                bs = hex.Substring(7, 2);
            } else {
                throw new ArgumentException("Hex-value is wrong!");
            }

            byte a = String.IsNullOrEmpty(aa) ? (byte)255 : Byte.Parse(aa, NumberStyles.AllowHexSpecifier);
            byte r = Byte.Parse(rs, NumberStyles.AllowHexSpecifier);
            byte g = Byte.Parse(gs, NumberStyles.AllowHexSpecifier);
            byte b = Byte.Parse(bs, NumberStyles.AllowHexSpecifier);

            return Color.FromArgb(a, r, g, b);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
            clipRec.Rect = new Rect(0, 0, VectorColorsRoot.ActualWidth, VectorColorsRoot.ActualHeight);

            if (blurVisual != null) {
                blurVisual.Size = new Vector2((float)ActualWidth, (float)ActualHeight);

                float blurAmount = Math.Min(((float)blur.Radius / (float)RADIUS_DIVIDE) / 640f * (float)ActualWidth, 250f);
                brush.Properties.InsertScalar("Blur.BlurAmount", blurAmount);
            }

            if (svgVisual != null) {
                svgVisual.Size = new Vector2((float)ActualWidth, (float)ActualHeight);
            }
        }
    }
}