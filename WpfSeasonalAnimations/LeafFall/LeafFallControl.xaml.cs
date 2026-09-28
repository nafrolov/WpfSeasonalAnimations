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
        private bool _renderingHooked;
        private double _emitAccumulator;
        private readonly Random _rng = new Random();

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
        //  Wind
        // ------------------------------------------------------------------

        public static readonly DependencyProperty WindProperty =
            DependencyProperty.Register(nameof(Wind), typeof(double), typeof(LeafFallControl),
                new PropertyMetadata(0.0));

        /// <summary>
        /// Horizontal wind strength in px/frame. Positive pushes leaves right,
        /// negative pushes them left. Range roughly -3 .. +3 feels natural.
        /// </summary>
        public double Wind
        {
            get => (double)GetValue(WindProperty);
            set => SetValue(WindProperty, value);
        }

        public static readonly DependencyProperty GustinessProperty =
            DependencyProperty.Register(nameof(Gustiness), typeof(double), typeof(LeafFallControl),
                new PropertyMetadata(0.4));

        /// <summary>
        /// How much the wind fluctuates over time. 0 = steady wind, 1 = very gusty.
        /// </summary>
        public double Gustiness
        {
            get => (double)GetValue(GustinessProperty);
            set => SetValue(GustinessProperty, value);
        }

        // ------------------------------------------------------------------
        //  Ground accumulation
        // ------------------------------------------------------------------

        public static readonly DependencyProperty AccumulateOnGroundProperty =
            DependencyProperty.Register(nameof(AccumulateOnGround), typeof(bool), typeof(LeafFallControl),
                new PropertyMetadata(true));

        /// <summary>Whether leaves should pile up at the bottom instead of vanishing.</summary>
        public bool AccumulateOnGround
        {
            get => (bool)GetValue(AccumulateOnGroundProperty);
            set => SetValue(AccumulateOnGroundProperty, value);
        }

        public static readonly DependencyProperty GroundLevelProperty =
            DependencyProperty.Register(nameof(GroundLevel), typeof(double), typeof(LeafFallControl),
                new PropertyMetadata(1.0));

        /// <summary>
        /// Where the "ground" sits, as a fraction of control height (0 = top, 1 = bottom).
        /// Leaves accumulate at this Y position.
        /// </summary>
        public double GroundLevel
        {
            get => (double)GetValue(GroundLevelProperty);
            set => SetValue(GroundLevelProperty, value);
        }

        public static readonly DependencyProperty MaxGroundLeavesProperty =
            DependencyProperty.Register(nameof(MaxGroundLeaves), typeof(int), typeof(LeafFallControl),
                new PropertyMetadata(200));

        /// <summary>
        /// Maximum number of leaves that can rest on the ground. Oldest are
        /// removed when this is exceeded, which keeps memory and render cost bounded.
        /// </summary>
        public int MaxGroundLeaves
        {
            get => (int)GetValue(MaxGroundLeavesProperty);
            set => SetValue(MaxGroundLeavesProperty, value);
        }

        public static readonly DependencyProperty GroundFadeSecondsProperty =
            DependencyProperty.Register(nameof(GroundFadeSeconds), typeof(double), typeof(LeafFallControl),
                new PropertyMetadata(0.0));

        /// <summary>
        /// If > 0, ground leaves fade out over this many seconds after landing,
        /// then are removed. 0 = they stay forever (or until MaxGroundLeaves evicts them).
        /// </summary>
        public double GroundFadeSeconds
        {
            get => (double)GetValue(GroundFadeSecondsProperty);
            set => SetValue(GroundFadeSecondsProperty, value);
        }

        // ------------------------------------------------------------------
        //  Internal state for wind + accumulation
        // ------------------------------------------------------------------

        private readonly List<LeafParticle> _groundLeaves = new List<LeafParticle>();
        private double _windPhase;          // for time-varying gusts
        private DateTime _lastFrameTime = DateTime.UtcNow;

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

            // ----- Time delta (for smooth gusts and fade timers) -----
            var now = DateTime.UtcNow;
            double dt = (now - _lastFrameTime).TotalSeconds;
            _lastFrameTime = now;
            if (dt <= 0 || dt > 0.5) dt = 1.0 / 60.0; // guard against long pauses
            double fps = 1.0 / dt;

            // ----- Time-varying wind (gusts) -----
            _windPhase += dt * 1.5; // gust frequency
            double gust = Math.Sin(_windPhase) * Math.Sin(_windPhase * 0.37 + 1.1);
            double effectiveWind = Wind + gust * Gustiness * 2.0;

            // ----- Ground Y position -----
            double groundY = h * Math.Max(0.0, Math.Min(1.0, GroundLevel));

            // ----- Emit new leaves -----
            double perSecond = Math.Max(0, EmissionRate);
            double perFrame = perSecond / fps;
            _emitAccumulator += perFrame;
            while (_emitAccumulator >= 1.0)
            {
                _emitAccumulator -= 1.0;
                EmitLeaf(w);
            }

            // ----- Update airborne leaves -----
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var leaf = _particles[i];
                leaf.Update(w, h, effectiveWind, groundY, AccumulateOnGround);

                if (leaf.IsGrounded)
                {
                    // Move it from the airborne list to the ground list.
                    _particles.RemoveAt(i);
                    _groundLeaves.Add(leaf);

                    // Cap the number of ground leaves: evict oldest if needed.
                    while (_groundLeaves.Count > MaxGroundLeaves)
                    {
                        var oldest = _groundLeaves[0];
                        ParticleCanvas.Children.Remove(oldest.Visual);
                        _groundLeaves.RemoveAt(0);
                    }
                }
                else if (!leaf.IsAlive)
                {
                    ParticleCanvas.Children.Remove(leaf.Visual);
                    _particles.RemoveAt(i);
                }
            }

            // ----- Fade and remove ground leaves if requested -----
            if (GroundFadeSeconds > 0)
            {
                for (int i = _groundLeaves.Count - 1; i >= 0; i--)
                {
                    var g = _groundLeaves[i];
                    double age = (now - g.GroundedAt).TotalSeconds;
                    if (age >= GroundFadeSeconds)
                    {
                        ParticleCanvas.Children.Remove(g.Visual);
                        _groundLeaves.RemoveAt(i);
                    }
                    else
                    {
                        // Linear fade
                        double t = 1.0 - (age / GroundFadeSeconds);
                        g.Visual.Opacity = g.OriginalOpacity * t;
                    }
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

            leaf.Visual.Opacity *= Math.Max(0, Math.Min(1, OpacityFactor));
            leaf.OriginalOpacity = leaf.Visual.Opacity;

            // Slightly random wind sensitivity per leaf, so gusts scatter them
            leaf.WindInfluence = 0.7 + _rng.NextDouble() * 0.6; // 0.7 .. 1.3

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
            _groundLeaves.Clear();
            _emitAccumulator = 0;
        }
    }

    public enum LeafLeaveAnimation
    {
        Fade,
        None
    }
}
