using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfSeasonalAnimations.FlowerBloom
{
    public partial class FlowerBloomControl : UserControl
    {
        private readonly List<FlowerParticle> _flowers = new List<FlowerParticle>();
        private readonly Random _rng = new Random();

        private bool _renderingHooked;
        private DateTime _lastFrameTime = DateTime.UtcNow;
        private double _emitAccumulator;

        public FlowerBloomControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        // ------------------------------------------------------------------
        //  Common properties (mirrors LeafFall / Snowfall)
        // ------------------------------------------------------------------

        public static readonly DependencyProperty EmissionRateProperty =
            DependencyProperty.Register(nameof(EmissionRate), typeof(double), typeof(FlowerBloomControl),
                new PropertyMetadata(3.0));
        /// <summary>Flowers emitted per second.</summary>
        public double EmissionRate
        {
            get => (double)GetValue(EmissionRateProperty);
            set => SetValue(EmissionRateProperty, value);
        }

        public static readonly DependencyProperty ScaleFactorProperty =
            DependencyProperty.Register(nameof(ScaleFactor), typeof(double), typeof(FlowerBloomControl),
                new PropertyMetadata(1.0));
        /// <summary>Overall flower size multiplier.</summary>
        public double ScaleFactor
        {
            get => (double)GetValue(ScaleFactorProperty);
            set => SetValue(ScaleFactorProperty, value);
        }

        public static readonly DependencyProperty OpacityFactorProperty =
            DependencyProperty.Register(nameof(OpacityFactor), typeof(double), typeof(FlowerBloomControl),
                new PropertyMetadata(1.0));
        /// <summary>Opacity multiplier applied to each flower.</summary>
        public double OpacityFactor
        {
            get => (double)GetValue(OpacityFactorProperty);
            set => SetValue(OpacityFactorProperty, value);
        }

        public static readonly DependencyProperty ParticleSpeedProperty =
            DependencyProperty.Register(nameof(ParticleSpeed), typeof(double), typeof(FlowerBloomControl),
                new PropertyMetadata(1.0));
        /// <summary>Controls how quickly each flower grows and blooms.</summary>
        public double ParticleSpeed
        {
            get => (double)GetValue(ParticleSpeedProperty);
            set => SetValue(ParticleSpeedProperty, value);
        }

        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(FlowerBloomControl),
                new PropertyMetadata(null));
        /// <summary>
        /// Color of the opened flower cup. If null, a random flower color is
        /// chosen from a built-in palette per flower. Does NOT affect the stem.
        /// </summary>
        public Brush Fill
        {
            get => (Brush)GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        public static readonly DependencyProperty StemFillProperty =
            DependencyProperty.Register(nameof(StemFill), typeof(Brush), typeof(FlowerBloomControl),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50))));
        /// <summary>Color of the stem, side leaf, and unopened bud.</summary>
        public Brush StemFill
        {
            get => (Brush)GetValue(StemFillProperty);
            set => SetValue(StemFillProperty, value);
        }

        public static readonly DependencyProperty LeaveAnimationProperty =
            DependencyProperty.Register(nameof(LeaveAnimation), typeof(FlowerLeaveAnimation), typeof(FlowerBloomControl),
                new PropertyMetadata(FlowerLeaveAnimation.None));
        /// <summary>What happens after a flower has fully bloomed.</summary>
        public FlowerLeaveAnimation LeaveAnimation
        {
            get => (FlowerLeaveAnimation)GetValue(LeaveAnimationProperty);
            set => SetValue(LeaveAnimationProperty, value);
        }

        public static readonly DependencyProperty FadeAfterBloomSecondsProperty =
            DependencyProperty.Register(nameof(FadeAfterBloomSeconds), typeof(double), typeof(FlowerBloomControl),
                new PropertyMetadata(6.0));
        /// <summary>If LeaveAnimation is Fade, seconds to wait before fading begins.</summary>
        public double FadeAfterBloomSeconds
        {
            get => (double)GetValue(FadeAfterBloomSecondsProperty);
            set => SetValue(FadeAfterBloomSecondsProperty, value);
        }

        // ------------------------------------------------------------------
        //  Placement properties
        // ------------------------------------------------------------------

        public static readonly DependencyProperty BottomGroundLevelProperty =
            DependencyProperty.Register(nameof(BottomGroundLevel), typeof(double), typeof(FlowerBloomControl),
                new PropertyMetadata(0.15));
        /// <summary>
        /// Fraction of the control height used by flowers blooming from the
        /// bottom edge. 0.15 means flowers along the bottom occupy the lower
        /// 15% of the control.
        /// </summary>
        public double BottomGroundLevel
        {
            get => (double)GetValue(BottomGroundLevelProperty);
            set => SetValue(BottomGroundLevelProperty, value);
        }

        public static readonly DependencyProperty SideGroundLevelProperty =
            DependencyProperty.Register(nameof(SideGroundLevel), typeof(double), typeof(FlowerBloomControl),
                new PropertyMetadata(0.15));
        /// <summary>
        /// Fraction of the control width used by flowers blooming from the
        /// left and right edges.
        /// </summary>
        public double SideGroundLevel
        {
            get => (double)GetValue(SideGroundLevelProperty);
            set => SetValue(SideGroundLevelProperty, value);
        }

        public static readonly DependencyProperty SpawnFromBottomProperty =
            DependencyProperty.Register(nameof(SpawnFromBottom), typeof(bool), typeof(FlowerBloomControl),
                new PropertyMetadata(true));
        public bool SpawnFromBottom
        {
            get => (bool)GetValue(SpawnFromBottomProperty);
            set => SetValue(SpawnFromBottomProperty, value);
        }

        public static readonly DependencyProperty SpawnFromSidesProperty =
            DependencyProperty.Register(nameof(SpawnFromSides), typeof(bool), typeof(FlowerBloomControl),
                new PropertyMetadata(true));
        public bool SpawnFromSides
        {
            get => (bool)GetValue(SpawnFromSidesProperty);
            set => SetValue(SpawnFromSidesProperty, value);
        }

        // ------------------------------------------------------------------
        //  Lifecycle
        // ------------------------------------------------------------------

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_renderingHooked)
            {
                CompositionTarget.Rendering += OnRendering;
                _renderingHooked = true;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_renderingHooked)
            {
                CompositionTarget.Rendering -= OnRendering;
                _renderingHooked = false;
            }
        }

        // ------------------------------------------------------------------
        //  Main loop
        // ------------------------------------------------------------------

        private void OnRendering(object sender, EventArgs e)
        {
            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 1 || h <= 1) return;

            var now = DateTime.UtcNow;
            double dt = (now - _lastFrameTime).TotalSeconds;
            _lastFrameTime = now;
            if (dt <= 0 || dt > 0.5) dt = 1.0 / 60.0;
            double fps = 1.0 / dt;

            // --- Emit new flowers ---
            double perFrame = Math.Max(0, EmissionRate) / fps;
            _emitAccumulator += perFrame;
            while (_emitAccumulator >= 1.0)
            {
                _emitAccumulator -= 1.0;
                EmitFlower(w, h);
            }

            // --- Update existing flowers ---
            for (int i = _flowers.Count - 1; i >= 0; i--)
            {
                var f = _flowers[i];
                f.Update(dt);

                if (f.IsDone && f.BloomedAt.HasValue
                    && LeaveAnimation == FlowerLeaveAnimation.Fade
                    && (now - f.BloomedAt.Value).TotalSeconds > FadeAfterBloomSeconds)
                {
                    // Simple fade: decrease opacity until gone, then remove.
                    double fadeElapsed = (now - f.BloomedAt.Value).TotalSeconds - FadeAfterBloomSeconds;
                    double t = 1.0 - fadeElapsed / 2.0; // 2s fade
                    if (t <= 0)
                    {
                        ParticleCanvas.Children.Remove(f.Root);
                        _flowers.RemoveAt(i);
                        continue;
                    }
                    f.Root.Opacity = f.Opacity * t;
                }
            }
        }

        private void EmitFlower(double canvasWidth, double canvasHeight)
        {
            // Decide which edge this flower comes from.
            var edges = new List<FlowerOrigin>();
            if (SpawnFromBottom) edges.Add(FlowerOrigin.Bottom);
            if (SpawnFromSides)
            {
                edges.Add(FlowerOrigin.Left);
                edges.Add(FlowerOrigin.Right);
            }
            if (edges.Count == 0) return;

            FlowerOrigin edge = edges[_rng.Next(edges.Count)];

            // Compute the design-box size this flower will occupy.
            double scaleFactor = Math.Max(0.1, ScaleFactor);
            double approxWidth = 100 * scaleFactor;
            double approxHeight = 100 * scaleFactor;

            // Origin is the stem base's on-canvas position.
            Point origin;
            switch (edge)
            {
                case FlowerOrigin.Bottom:
                    {
                        double bandPx = canvasHeight * Clamp01(BottomGroundLevel);
                        origin = new Point(
                            _rng.NextDouble() * canvasWidth,
                            canvasHeight - _rng.NextDouble() * bandPx);
                        break;
                    }
                case FlowerOrigin.Left:
                    {
                        double bandPx = canvasWidth * Clamp01(SideGroundLevel);
                        origin = new Point(
                            _rng.NextDouble() * bandPx,
                            canvasHeight - approxHeight);
                        break;
                    }
                default: // Right
                    {
                        double bandPx = canvasWidth * Clamp01(SideGroundLevel);
                        origin = new Point(
                            canvasWidth - _rng.NextDouble() * bandPx,
                            canvasHeight - approxHeight);
                        break;
                    }
            }

            var flower = new FlowerParticle(
                origin,
                edge,
                scaleFactor,
                Math.Max(0, Math.Min(1, OpacityFactor)),
                Fill,
                StemFill,
                Math.Max(0.1, ParticleSpeed));

            ParticleCanvas.Children.Add(flower.Root);
            _flowers.Add(flower);
        }

        private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

        // ------------------------------------------------------------------
        //  Public helper
        // ------------------------------------------------------------------

        /// <summary>Remove all flowers immediately.</summary>
        public void Clear()
        {
            ParticleCanvas.Children.Clear();
            _flowers.Clear();
            _emitAccumulator = 0;
        }
    }

    public enum FlowerLeaveAnimation { None, Fade }
}
