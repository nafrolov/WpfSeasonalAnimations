using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfSeasonalAnimations.LeafFall
{
    /// <summary>
    /// A single falling leaf. Holds physics state and the visual Path element
    /// that renders it on the canvas.
    /// </summary>
    public class LeafParticle
    {
        // ---- Path Scale ----
        private const double _geometryDesignSize = 100.0;

        // ---- Wind interaction ----
        public double WindInfluence = 1.0;   // 0 = unaffected by wind, 1 = fully affected
        public double SwayInWindFactor = 0.5; // how much sway amplitude is amplified by wind

        // ---- Ground state ----
        public bool IsGrounded;              // true once the leaf has settled
        public DateTime GroundedAt;          // when it landed
        public double OriginalOpacity;       // to restore opacity if needed

        // ---- Physics state ----
        public Point Position;          // top-left of the leaf's bounding box, in canvas coords
        public Vector Velocity;         // px per frame
        public double RotationAngle;    // degrees
        public double AngularVelocity;  // degrees per frame

        // ---- Sway / drift ----
        public double SwayPhase;
        public double SwayAmplitude;    // px per frame horizontal wobble
        public double SwayFrequency;    // radians per frame
        public double DriftBias;        // persistent lateral bias (px per frame)

        // ---- Appearance ----
        public double Scale;
        public double Width;
        public double Height;

        // ---- Visual ----
        public Path Visual;
        public RotateTransform Rotate;
        public TranslateTransform Translate;

        // ---- Lifecycle ----
        public bool IsAlive = true;

        // Random generator shared across the simulation
        private static readonly Random Rng = new Random();

        public LeafParticle(double canvasWidth, double topY, Brush fill, double speedFactor, double scaleFactor, PathGeometry geometry)
        {
            // Random scale (scaled by the user's ScaleFactor)
            Scale = (0.6 + Rng.NextDouble() * 0.8) * scaleFactor;

            Width = 20 * Scale;
            Height = 20 * Scale;

            // Random horizontal start position (leave a margin so leaves don't start clipped)
            double startX = Rng.NextDouble() * Math.Max(1, canvasWidth - Width);
            Position = new Point(startX, topY - Height);

            // Initial velocity: slight downward, random horizontal drift
            Velocity = new Vector(
                (Rng.NextDouble() - 0.5) * 1.5,
                0.3 + Rng.NextDouble() * 0.5);

            // Rotation: random starting angle, random spin speed (positive or negative)
            RotationAngle = Rng.NextDouble() * 360;
            AngularVelocity = (Rng.NextDouble() - 0.5) * 6.0; // degrees per frame

            // Sway parameters
            SwayPhase = Rng.NextDouble() * Math.PI * 2;
            SwayAmplitude = 0.3 + Rng.NextDouble() * 1.2;
            SwayFrequency = 0.02 + Rng.NextDouble() * 0.05;

            // Persistent drift bias: some leaves drift left, some right
            DriftBias = (Rng.NextDouble() - 0.5) * 1.2;

            // Build the visual
            Visual = new Path
            {
                Data = geometry,
                Fill = fill,
                Opacity = 0.75 + Rng.NextDouble() * 0.25,
                IsHitTestVisible = false,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };

            double geometryScale = Width / _geometryDesignSize;

            Rotate = new RotateTransform(RotationAngle, Width / 2, Height / 2);
            Translate = new TranslateTransform(Position.X, Position.Y);
            // The geometry is authored in a nominal 100x100 box; bake scale in.
            var scaleAdjust = new ScaleTransform(geometryScale, geometryScale);

            var group = new TransformGroup();
            group.Children.Add(scaleAdjust);
            group.Children.Add(Rotate);
            group.Children.Add(Translate);
            Visual.RenderTransform = group;

            // Stash the base speed factor so the update loop can apply it
            _speedFactor = speedFactor;
        }

        private readonly double _speedFactor;

        // Tunables
        private const double Gravity = 0.12;
        private const double AirDrag = 0.015;
        private const double TerminalSpeed = 3.2;
        private const double AngularDamping = 0.985;
        private const double AngularPerturbation = 0.25;
        private const double LiftThreshold = 3.5;   // |AngularVelocity| above this can trigger lift
        private const double LiftChance = 0.02;
        private const double LiftImpulse = 0.6;

        public void Update(double canvasWidth, double canvasHeight, double wind, double groundY, bool accumulateOnGround)
        {
            if (IsGrounded)
            {
                return; // nothing to simulate
            }

            // --- Vertical: gravity + drag, capped at terminal speed ---
            Velocity.Y += Gravity * _speedFactor;
            Velocity.Y *= (1 - AirDrag);
            double term = TerminalSpeed * _speedFactor;
            if (Velocity.Y > term) Velocity.Y = term;

            // --- Horizontal: drift bias + sway + wind ---
            SwayPhase += SwayFrequency;
            double swayAmp = SwayAmplitude * (1.0 + Math.Abs(wind) * SwayInWindFactor);
            double sway = Math.Sin(SwayPhase) * swayAmp;
            double targetVx = DriftBias + sway + wind * WindInfluence;

            Velocity.X += (targetVx - Velocity.X) * 0.08;

            // --- Rotation ---
            AngularVelocity += (Rng.NextDouble() - 0.5) * AngularPerturbation;
            AngularVelocity *= AngularDamping;
            RotationAngle += AngularVelocity * _speedFactor;

            // --- Occasional lift ---
            if (Math.Abs(AngularVelocity) > LiftThreshold && Rng.NextDouble() < LiftChance)
            {
                Velocity.Y -= LiftImpulse * _speedFactor;
                if (Velocity.Y < -1.5) Velocity.Y = -1.5;
            }

            // --- Integrate position ---
            Position.X += Velocity.X * _speedFactor;
            Position.Y += Velocity.Y * _speedFactor;

            // --- Wrap horizontally ---
            if (Position.X < -Width) Position.X = canvasWidth;
            else if (Position.X > canvasWidth) Position.X = -Width;

            // --- Ground collision ---
            if (accumulateOnGround)
            {
                double visualBottom = Position.Y + GetVisualBottomOffset();
                if (visualBottom >= groundY)
                {
                    SettleOnGround(groundY);
                }
            }

            // --- Apply to visuals ---
            Rotate.Angle = RotationAngle;
            Translate.X = Position.X;
            Translate.Y = Position.Y;

            // --- Off-screen removal (only if not accumulating) ---
            if (!accumulateOnGround && Position.Y > canvasHeight + Height)
            {
                IsAlive = false;
            }
        }

        /// <summary>
        /// Freeze the leaf at its current position and stop physics.
        /// </summary>
        public void SettleOnGround(double groundY)
        {
            IsGrounded = true;
            GroundedAt = DateTime.UtcNow;

            // Freeze the physics but keep the current angle.
            Velocity = new Vector(0, 0);
            AngularVelocity = 0;

            // Place the leaf so its VISUAL bottom sits exactly on groundY.
            double visualBottomOffset = GetVisualBottomOffset();
            Position.Y = groundY - visualBottomOffset;

            Rotate.Angle = RotationAngle;
            Translate.X = Position.X;
            Translate.Y = Position.Y;
        }

        /// <summary>
        /// How far the leaf's lowest rendered point extends below its
        /// unrotated bounding box top edge (Position.Y), given the current rotation.
        /// </summary>
        public double GetVisualBottomOffset()
        {
            double rad = RotationAngle * Math.PI / 180.0;
            double halfW = Width / 2.0;
            double halfH = Height / 2.0;
            double margin = 40.0;

            // Distance from the box's top edge to the rotated rectangle's lowest point
            double bottomFromCenter = Math.Abs(halfW * Math.Sin(rad)) + Math.Abs(halfH * Math.Cos(rad));
            return halfH + bottomFromCenter + margin;
        }
    }
}
