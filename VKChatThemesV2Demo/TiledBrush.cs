using Microsoft.Graphics.Canvas.Effects;
using System;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace VKChatThemesV2Demo {
    public class TiledBrush : XamlCompositionBrushBase {
        #region Properties

        #region Compositor

        private Compositor Compositor {
            get { return Window.Current.Compositor; }
        }

        #endregion

        #region Source

        public Uri Source {
            get { return (Uri)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source), typeof(Uri), typeof(TiledBrush), new PropertyMetadata(0, OnSourceChanged));


        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var tiledBrush = d as TiledBrush;

            if (tiledBrush != null && tiledBrush.CompositionBrush != null)
                tiledBrush.UpdateSurface(tiledBrush.Source);
        }

        #endregion

        #endregion

        #region Methods

        #region UpdateSurface

        private CompositionSurfaceBrush surfaceBrush;
        private LoadedImageSurface surface;

        private void UpdateSurface(Uri uri) {
            using (surface) {
                surface = uri != null ? LoadedImageSurface.StartLoadFromUri(uri) : null;
                surfaceBrush.Surface = surface;
            }
        }

        #endregion

        #region OnConnected

        private CompositionEffectFactory borderEffectFactory;
        private CompositionEffectBrush borderEffectBrush;
        private BorderEffect borderEffect;

        protected override void OnConnected() {
            base.OnConnected();

            if (CompositionBrush == null) {
                surfaceBrush = Compositor.CreateSurfaceBrush();
                surfaceBrush.Stretch = CompositionStretch.None;
                UpdateSurface(Source);

                borderEffect = new BorderEffect() {
                    Source = new CompositionEffectSourceParameter("source"),
                    ExtendX = Microsoft.Graphics.Canvas.CanvasEdgeBehavior.Wrap,
                    ExtendY = Microsoft.Graphics.Canvas.CanvasEdgeBehavior.Wrap
                };

                borderEffectFactory = Compositor.CreateEffectFactory(borderEffect);
                borderEffectBrush = borderEffectFactory.CreateBrush();
                borderEffectBrush.SetSourceParameter("source", surfaceBrush);
                CompositionBrush = borderEffectBrush;
            }
        }

        #endregion

        #region OnDisconnected

        protected override void OnDisconnected() {
            using (borderEffectFactory)
            using (borderEffectBrush)
            using (borderEffect)
            using (CompositionBrush)
            using (surfaceBrush)
            using (surface) {
                CompositionBrush = null;

                borderEffectFactory = null;
                borderEffectBrush = null;
                borderEffect = null;

                surfaceBrush = null;
                surface = null;
            }

            base.OnDisconnected();
        }

        #endregion

        #endregion
    }
}