using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfSeasonalAnimations.LeafFall
{
    public partial class LeafFallControl : UserControl
    {
        private readonly List<LeafParticle> _particles = new List<LeafParticle>();
        private readonly Random _rng = new Random();

        private DateTime _lastEmit = DateTime.MinValue;
        private bool _renderingHooked;
        private double _emitAccumulator;

        public LeafFallControl()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += (_, __) => { /* nothing special; canvas resizes with us */ };
        }

        // ------------------------------------------------------------------
        //  Dependency Properties
        // ------------------------------------------------------------------

        public static readonly DependencyProperty EmissionRateProperty =
            DependencyProperty.Register(nameof(EmissionRate), typeof(double), typeof(LeafFallControl),
                new PropertyMetadata(5.0));

        /// <summary>Leaves emitted per second.</summary>
        public double EmissionRate
        {
            get => (double)GetValue(EmissionRateProperty);
            set => SetValue(EmissionRateProperty, value);
        }

        public static readonly DependencyProperty ScaleFactorProperty =
            DependencyProperty.Register(nameof(ScaleFactor), typeof(double), typeof(LeafFallControl),
                new PropertyMetadata(1.0));

        /// <summary>Multiplier on leaf size.</summary>
        public double ScaleFactor
        {
            get => (double)GetValue(ScaleFactorProperty);
            set => SetValue(ScaleFactorProperty, value);
        }

        public static readonly DependencyProperty OpacityFactorProperty =
            DependencyProperty.Register(nameof(OpacityFactor), typeof(double), typeof(LeafFallControl),
                new PropertyMetadata(1.0));

        /// <summary>Overall opacity multiplier (0..1).</summary>
        public double OpacityFactor
        {
            get => (double)GetValue(OpacityFactorProperty);
            set => SetValue(OpacityFactorProperty, value);
        }

        public static readonly DependencyProperty ParticleSpeedProperty =
            DependencyProperty.Register(nameof(ParticleSpeed), typeof(double), typeof(LeafFallControl),
                new PropertyMetadata(1.0));

        /// <summary>Fall speed multiplier.</summary>
        public double ParticleSpeed
        {
            get => (double)GetValue(ParticleSpeedProperty);
            set => SetValue(ParticleSpeedProperty, value);
        }

        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(LeafFallControl),
                new PropertyMetadata(null));

        /// <summary>
        /// If set, all leaves use this brush. If null, each leaf gets a random
        /// autumn color from LeafShapeFactory.
        /// </summary>
        public Brush Fill
        {
            get => (Brush)GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        public static readonly DependencyProperty LeaveAnimationProperty =
            DependencyProperty.Register(nameof(LeaveAnimation), typeof(LeafLeaveAnimation), typeof(LeafFallControl),
                new PropertyMetadata(LeafLeaveAnimation.Fade));

        /// <summary>What happens when a leaf exits the bottom of the control.</summary>
        public LeafLeaveAnimation LeaveAnimation
        {
            get => (LeafLeaveAnimation)GetValue(LeaveAnimationProperty);
            set => SetValue(LeaveAnimationProperty, value);
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

            // --- Emit new leaves ---
            // CompositionTarget.Rendering runs at display refresh rate (~60 Hz).
            // Accumulate fractional emissions so low rates still work.
            double perSecond = Math.Max(0, EmissionRate);
            double perFrame = perSecond / 60.0;
            _emitAccumulator += perFrame;
            while (_emitAccumulator >= 1.0)
            {
                _emitAccumulator -= 1.0;
                EmitLeaf(w);
            }

            // --- Update existing leaves ---
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var leaf = _particles[i];
                leaf.Update(w, h);

                if (!leaf.IsAlive)
                {
                    // Leave animation: for now we simply remove. Extend here
                    // if you want a fade-out before removal.
                    ParticleCanvas.Children.Remove(leaf.Visual);
                    _particles.RemoveAt(i);
                }
            }
        }

        private void EmitLeaf(double canvasWidth)
        {
            var geometry = LeafShapeFactory.GetRandomGeometry();
            Brush brush = Fill ?? LeafShapeFactory.GetRandomBrush();

            var leaf = new LeafParticle(
                canvasWidth,
                topY: 0,
                fill: brush,
                speedFactor: Math.Max(0.1, ParticleSpeed),
                scaleFactor: Math.Max(0.1, ScaleFactor),
                geometry: geometry);

            // Apply opacity factor
            leaf.Visual.Opacity *= Math.Max(0, Math.Min(1, OpacityFactor));

            ParticleCanvas.Children.Add(leaf.Visual);
            _particles.Add(leaf);
        }

        /// <summary>
        /// Immediately clears all leaves. Useful if you want manual control.
        /// </summary>
        public void Clear()
        {
            ParticleCanvas.Children.Clear();
            _particles.Clear();
            _emitAccumulator = 0;
        }
    }

    public enum LeafLeaveAnimation
    {
        Fade,
        None
    }
}
