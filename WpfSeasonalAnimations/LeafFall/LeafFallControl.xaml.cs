using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

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

        private static readonly DependencyPropertyKey CurrentGustKey =
            DependencyProperty.RegisterReadOnly(nameof(CurrentGust), typeof(double), typeof(LeafFallControl),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty CurrentGustProperty = CurrentGustKey.DependencyProperty;

        /// <summary>Current gust value in [-1, 1]. Sign = direction, magnitude = strength.</summary>
        public double CurrentGust => (double)GetValue(CurrentGustProperty);

        public static readonly DependencyProperty GustEnvelopeProperty =
            DependencyProperty.Register(nameof(GustEnvelope), typeof(double), typeof(LeafFallControl),
                new PropertyMetadata(1.0));

        /// <summary>
        /// Multiplier on the procedural gust signal. 0 = calm, 1 = normal gusts,
        /// 2+ = storm. Animate this DP to make gusts come and go over time.
        /// </summary>
        public double GustEnvelope
        {
            get => (double)GetValue(GustEnvelopeProperty);
            set => SetValue(GustEnvelopeProperty, value);
        }

        // ------------------------------------------------------------------
        //  Gust model state
        // ------------------------------------------------------------------

        /// <summary>Running phase for the gust waveform.</summary>
        private double _gustPhase;

        /// <summary>
        /// A smooth, directional gust signal in [-1, 1]. Sign = direction,
        /// magnitude = strength. Zero means "no gust right now."
        /// </summary>
        private double _gust;

        /// <summary>
        /// How fast the gust waveform oscillates. Larger = more frequent gusts.
        /// </summary>
        private const double GustFrequency = 0.9;   // radians per second

        /// <summary>
        /// How "peaky" the gust waveform is. 1 = pure sine (gentle), higher =
        /// sharper peaks separated by calmer periods.
        /// </summary>
        private const double GustSharpness = 2.4;

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

            StartGustEnvelopeLoop();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_renderingHooked)
            {
                CompositionTarget.Rendering -= OnRendering;
                _renderingHooked = false;
            }

            StopGustEnvelopeLoop();
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

            // --- Gust signal ---
            UpdateGust(dt);
            double effectiveWind = Wind + _gust * Gustiness * 3.0 * GustEnvelope;

            // --- Ground ---
            double groundY = h * Math.Max(0.0, Math.Min(1.0, GroundLevel));

            // --- Emit ---
            double perSecond = Math.Max(0, EmissionRate);
            double perFrame = perSecond / fps;
            _emitAccumulator += perFrame;
            while (_emitAccumulator >= 1.0)
            {
                _emitAccumulator -= 1.0;
                EmitLeaf(w);
            }

            // --- Update airborne leaves ---
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var leaf = _particles[i];
                leaf.Update(w, h, effectiveWind, _gust, groundY, AccumulateOnGround);

                if (leaf.IsGrounded)
                {
                    _particles.RemoveAt(i);
                    _groundLeaves.Add(leaf);

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

            // --- Ground fade (unchanged) ---
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

        private void UpdateGust(double dt)
        {
            _gustPhase += dt * GustFrequency;

            // Base sine
            double s = Math.Sin(_gustPhase);

            // Shape it: preserve the sign, but sharpen the peaks so we get
            // moments of strong wind and moments of near-calm.
            double shaped = Math.Sign(s) * Math.Pow(Math.Abs(s), 1.0 / GustSharpness);

            // Second, slower oscillation so gusts don't feel perfectly periodic.
            double slow = 0.6 + 0.4 * Math.Sin(_gustPhase * 0.23 + 1.7);

            _gust = shaped * slow; // still in [-1, 1]

            SetValue(CurrentGustKey, _gust);
        }

        private DispatcherTimer _envelopeTimer;

        private void StartGustEnvelopeLoop()
        {
            _envelopeTimer = new DispatcherTimer();
            _envelopeTimer.Tick += OnGustEnvelopeTick;
            _envelopeTimer.Interval = TimeSpan.FromSeconds(2.0);
            _envelopeTimer.Start();
        }

        private void StopGustEnvelopeLoop()
        {
            if (_envelopeTimer != null)
            {
                _envelopeTimer.Stop();
                _envelopeTimer.Tick -= OnGustEnvelopeTick;
                _envelopeTimer = null;
            }
        }

        private void OnGustEnvelopeTick(object? sender, EventArgs e)
        {
            if (_envelopeTimer == null)
                return;
            _envelopeTimer.Stop();

            // --- Choose envelope ramp parameters ---
            double from = GustEnvelope;
            double to = 0.3 + _rng.NextDouble() * 1.7;      // 0.3 .. 2.0
            double seconds = 2.0 + _rng.NextDouble() * 4.0; // 2 .. 6 s total (up + down)

            // --- Animate GustEnvelope ---
            var envelopeAnim = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromSeconds(seconds * 0.5),
                AutoReverse = true,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
                FillBehavior = FillBehavior.Stop
            };
            envelopeAnim.Completed += (_, __) => GustEnvelope = from;
            BeginAnimation(GustEnvelopeProperty, envelopeAnim);

            // --- Optional: occasionally shift the mean wind direction ---
            if (_rng.NextDouble() < 0.5)
            {
                double targetWind = (_rng.NextDouble() - 0.5) * 1.5; // -0.75 .. +0.75
                AnimateWindDirection(targetWind, seconds);
            }

            // Schedule the next envelope to start after this one mostly settles.
            _envelopeTimer.Interval = TimeSpan.FromSeconds(seconds + 0.5 + _rng.NextDouble() * 3.0);
            _envelopeTimer.Start();
        }

        private void AnimateWindDirection(double targetWind, double seconds)
        {
            var anim = new DoubleAnimation
            {
                From = Wind,
                To = targetWind,
                Duration = TimeSpan.FromSeconds(seconds),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
                FillBehavior = FillBehavior.Stop
            };
            anim.Completed += (_, __) => Wind = targetWind;
            BeginAnimation(WindProperty, anim);
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
