using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfSeasonalAnimations.FlowerBloom
{
    public class FlowerParticle
    {
        public enum Stage { Stem, Bud, Bloomed, Done }

        // ---- Identity ----
        public FlowerShapeFactory.CupStyle CupStyle;
        public double Scale;
        public double Width;   // of the design box * scale
        public double Height;

        // ---- Placement ----
        public Point Origin;       // where the stem base sits on the canvas
        public FlowerOrigin Edge;  // Bottom, Left, Right

        // ---- Growth ----
        public Stage CurrentStage = Stage.Stem;
        public double StageProgress; // 0..1 within the current stage
        public double GrowthSpeed;   // progress units per second

        // ---- Appearance ----
        public Brush FlowerFill;
        public Brush StemFill;
        public double Opacity;

        // ---- Visuals ----
        public Canvas Root;               // container for the four sub-shapes
        public Path StemPath;
        public Path StemLeafPath;
        public Path BudPath;
        public Path CupPath;
        public double BaseRotation;       // slight tilt for naturalness
        public int LeafSide = 1;          // +1 leaf on right of stem, -1 on left

        // ---- Lifecycle ----
        public bool IsDone;
        public DateTime? BloomedAt;

        private static readonly Random Rng = new Random();

        // Durations in "progress per second" units — scale with ParticleSpeed.
        private const double StemDuration = 1.6;
        private const double BudDuration = 0.8;
        // The cup opening is a bloom, not growth: it happens ~3x faster
        // than the bud stretched toward it.
        private const double CupDuration = BudDuration / 3.0;

        // ---- Design-unit layout inside the root canvas (before scaling) ----
        // Each sub-shape is a 100 x 100 geometry box placed at these offsets.
        private const double GeometryDesignSize = 100.0;
        private const double RootDesignWidth = 240.0;
        private const double RootDesignHeight = 210.0;
        // The stem base sits at (Width/2, Height) in design coords, which is
        // exactly the root's RenderTransformOrigin (0.5, 1.0) — so the base
        // stays pinned to Origin through scaling and tilting.
        private const double StemTipY = 110.0;     // top edge of the stem geometry
        private const double StemCenterX = 120.0;  // stem centerline (= Width/2)
        private const double LeafTop = 95.0;
        // Heads are slid along the stem's axis by this amount so the joint
        // always overlaps the stem apex and shows no seam: upright heads sink
        // down into it, hanging bell-style heads rise up over it.
        private const double AttachOverlap = 6.0;

        // Pixels per design unit.
        public double GeometryScale { get; private set; }

        /// <summary>
        /// Signed horizontal offset (canvas pixels) between the stem base and
        /// the flower head, produced by the base tilt. Positive leans right.
        /// The head sits roughly 150 design units above the base.
        /// </summary>
        public double HeadOffset
        {
            get
            {
                const double HeadDesignHeight = 150.0;
                return HeadDesignHeight * GeometryScale
                       * Math.Sin(BaseRotation * Math.PI / 180.0);
            }
        }

        public FlowerParticle(Point origin, FlowerOrigin edge,
                              double scaleFactor, double opacityFactor,
                              Brush flowerFill, Brush stemFill,
                              double particleSpeed)
        {
            Origin = origin;
            Edge = edge;

            Scale = (0.6 + Rng.NextDouble() * 0.8) * scaleFactor * 0.25;
            GeometryScale = 0.45 * Scale;
            Width = RootDesignWidth * GeometryScale;
            Height = RootDesignHeight * GeometryScale;

            CupStyle = FlowerShapeFactory.GetRandomCupStyle();
            LeafSide = Rng.Next(2) == 0 ? -1 : 1;
            FlowerFill = flowerFill ?? FlowerShapeFactory.GetRandomFlowerBrush();
            StemFill = stemFill;
            Opacity = opacityFactor;

            GrowthSpeed = Math.Max(0.1, particleSpeed);

            // Natural tilt per edge
            double tiltMagnitude = 15 + Rng.NextDouble() * 30; // 15..45 degrees
            switch (edge)
            {
                case FlowerOrigin.Bottom:
                    BaseRotation = (Rng.NextDouble() - 0.5) * 12;
                    break;
                case FlowerOrigin.Left:
                    BaseRotation = +tiltMagnitude;  // lean right, away from wall
                    break;
                case FlowerOrigin.Right:
                    BaseRotation = -tiltMagnitude;  // lean left, away from wall
                    break;
            }

            BuildVisuals();
            ApplyStageVisual();
        }

        private void BuildVisuals()
        {
            Root = new Canvas
            {
                Width = RootDesignWidth,
                Height = RootDesignHeight,
                IsHitTestVisible = false,
                Opacity = Opacity
            };

            // Stem: base at the root's pivot, tip at StemTipY.
            StemPath = new Path
            {
                Data = FlowerShapeFactory.Stem(),
                Fill = StemFill,
                Stretch = Stretch.Uniform,
                Width = GeometryDesignSize,
                Height = GeometryDesignSize,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(StemPath, StemCenterX - GeometryDesignSize / 2);
            Canvas.SetTop(StemPath, StemTipY);
            StemPath.RenderTransformOrigin = new Point(0.5, 1.0);
            StemPath.RenderTransform = new ScaleTransform(1, 1);

            // Side leaf: the geometry's attach seam sits at local x=2 and the
            // blade extends to local x=96. The transform origin is placed on
            // the seam just inside the stem, and ScaleX = LeafSide * grow
            // makes the blade grow outward (mirrored for LeafSide = -1), so it
            // never covers the stem.
            StemLeafPath = new Path
            {
                Data = FlowerShapeFactory.StemLeaf(),
                Fill = Shade(StemFill, 0.85),
                Stretch = Stretch.Uniform,
                Width = GeometryDesignSize,
                Height = GeometryDesignSize,
                IsHitTestVisible = false
            };
            double leafSeam = LeafSide > 0 ? StemCenterX + 4 : StemCenterX - 4;
            Canvas.SetLeft(StemLeafPath, leafSeam - 2);
            Canvas.SetTop(StemLeafPath, LeafTop);
            StemLeafPath.RenderTransformOrigin = new Point(0.02, 0.5);
            StemLeafPath.RenderTransform = new ScaleTransform(LeafSide * 0.2, 1);

            // Unopened bud (stage 1): pivot seated on the stem tip, sunk in
            // (or risen over, for a bell) so the joint shows no seam.
            BudPath = new Path
            {
                Data = FlowerShapeFactory.Bud(),
                Fill = StemFill,
                Stretch = Stretch.Fill,
                Width = GeometryDesignSize,
                Height = GeometryDesignSize,
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };
            Canvas.SetLeft(BudPath, StemCenterX - GeometryDesignSize / 2);
            // Same rule as the cup: a bellflower bud hangs below the tip, so
            // its pivot rises over the stem top; upright buds sink into it.
            double budOverlap = CupStyle == FlowerShapeFactory.CupStyle.Bell ? -AttachOverlap : AttachOverlap;
            Canvas.SetTop(BudPath, StemTipY - GeometryDesignSize + budOverlap);
            BudPath.RenderTransformOrigin = new Point(0.5, 1.0);
            BudPath.RenderTransform = new ScaleTransform(1, 1);

            // Opened cup (stage 2): attach point aligned to the stem tip, so
            // upright cups grow on top of the stem and the bell hangs from it.
            Point cupAttach = FlowerShapeFactory.GetCupAttachPoint(CupStyle);
            CupPath = new Path
            {
                Data = FlowerShapeFactory.GetCup(CupStyle),
                Fill = FlowerFill,
                Stretch = Stretch.Fill,
                Width = GeometryDesignSize,
                Height = GeometryDesignSize,
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };
            Canvas.SetLeft(CupPath, StemCenterX - cupAttach.X);
            // Overlap the stem apex along the stem axis so the joint never
            // shows a seam against the thin stem.
            double cupOverlap = CupStyle == FlowerShapeFactory.CupStyle.Bell ? -AttachOverlap : AttachOverlap;
            Canvas.SetTop(CupPath, StemTipY - cupAttach.Y + cupOverlap);
            CupPath.RenderTransformOrigin = new Point(0.5, CupStyle == FlowerShapeFactory.CupStyle.Bell ? 0.0 : 1.0);
            CupPath.RenderTransform = new ScaleTransform(0.2, 0.2);

            Root.Children.Add(StemLeafPath);
            Root.Children.Add(StemPath);
            Root.Children.Add(BudPath);
            Root.Children.Add(CupPath);

            // Place the root so the stem base sits at Origin, then scale the
            // whole design box down to screen size and tilt around the base.
            Canvas.SetLeft(Root, Origin.X - RootDesignWidth / 2);
            Canvas.SetTop(Root, Origin.Y - RootDesignHeight);

            var group = new TransformGroup();
            group.Children.Add(new ScaleTransform(GeometryScale, GeometryScale));
            group.Children.Add(new RotateTransform(BaseRotation));
            Root.RenderTransform = group;
            // Combined with the (0.5, 1.0) origin, the group scales and tilts
            // the whole plant around the stem base, which stays pinned to Origin.
            Root.RenderTransformOrigin = new Point(0.5, 1.0);
        }

        /// <summary>Move the stem base to a new canvas position.</summary>
        public void Reposition(Point origin)
        {
            Origin = origin;
            Canvas.SetLeft(Root, Origin.X - RootDesignWidth / 2);
            Canvas.SetTop(Root, Origin.Y - RootDesignHeight);
        }

        /// <summary>Advance the growth state machine.</summary>
        public void Update(double dt)
        {
            if (IsDone) return;

            switch (CurrentStage)
            {
                case Stage.Stem:
                    StageProgress += dt * GrowthSpeed / StemDuration;
                    if (StageProgress >= 1.0)
                    {
                        StageProgress = 0;
                        CurrentStage = Stage.Bud;
                    }
                    ApplyStageVisual();
                    break;

                case Stage.Bud:
                    StageProgress += dt * GrowthSpeed / BudDuration;
                    if (StageProgress >= 1.0)
                    {
                        StageProgress = 0;
                        CurrentStage = Stage.Bloomed;
                    }
                    ApplyStageVisual();
                    break;

                case Stage.Bloomed:
                    StageProgress += dt * GrowthSpeed / CupDuration;
                    if (StageProgress >= 1.0)
                    {
                        StageProgress = 1.0;
                        CurrentStage = Stage.Done;
                        IsDone = true;
                        BloomedAt = DateTime.UtcNow;
                    }
                    ApplyStageVisual();
                    break;
            }
        }

        /// <summary>Update which sub-shape is visible and how the flower grows.</summary>
        private void ApplyStageVisual()
        {
            switch (CurrentStage)
            {
                case Stage.Stem:
                    {
                        StemPath.Visibility = Visibility.Visible;
                        BudPath.Visibility = Visibility.Collapsed;
                        CupPath.Visibility = Visibility.Collapsed;

                        double grow = Math.Min(1.0, StageProgress + 0.2);
                        ((ScaleTransform)StemPath.RenderTransform).ScaleY = grow;

                        // The leaf only emerges once the stem has grown past
                        // its attach point, unfurling sideways from the seam.
                        double leafGrow = Math.Max(0.0, (grow - 0.65) / 0.35);
                        StemLeafPath.Visibility = leafGrow > 0.02
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                        ((ScaleTransform)StemLeafPath.RenderTransform).ScaleX =
                            LeafSide * leafGrow;
                        break;
                    }
                case Stage.Bud:
                    {
                        StemPath.Visibility = Visibility.Visible;
                        StemLeafPath.Visibility = Visibility.Visible;
                        BudPath.Visibility = Visibility.Visible;
                        CupPath.Visibility = Visibility.Collapsed;

                        ((ScaleTransform)StemPath.RenderTransform).ScaleY = 1.0;
                        ((ScaleTransform)StemLeafPath.RenderTransform).ScaleX = LeafSide;
                        // The bellflower's bud hangs below the stem tip just
                        // like the open bell does: a negative ScaleY around
                        // the (0.5, 1.0) base origin mirrors it downward.
                        double budSign = CupStyle == FlowerShapeFactory.CupStyle.Bell ? -1.0 : 1.0;
                        ((ScaleTransform)BudPath.RenderTransform).ScaleY =
                            budSign * Math.Min(1.0, 0.6 + StageProgress * 0.4);
                        break;
                    }
                case Stage.Bloomed:
                    {
                        StemPath.Visibility = Visibility.Visible;
                        StemLeafPath.Visibility = Visibility.Visible;
                        BudPath.Visibility = Visibility.Collapsed;
                        CupPath.Visibility = Visibility.Visible;

                        ((ScaleTransform)StemPath.RenderTransform).ScaleY = 1.0;
                        ((ScaleTransform)StemLeafPath.RenderTransform).ScaleX = LeafSide;

                        // The cup opens in place on top of the stem tip.
                        double open = 0.2 + Math.Min(1.0, StageProgress) * 0.8;
                        var cupScale = (ScaleTransform)CupPath.RenderTransform;
                        cupScale.ScaleX = open;
                        cupScale.ScaleY = open;
                        break;
                    }
                case Stage.Done:
                    {
                        StemPath.Visibility = Visibility.Visible;
                        StemLeafPath.Visibility = Visibility.Visible;
                        BudPath.Visibility = Visibility.Collapsed;
                        CupPath.Visibility = Visibility.Visible;

                        ((ScaleTransform)StemPath.RenderTransform).ScaleY = 1.0;
                        ((ScaleTransform)StemLeafPath.RenderTransform).ScaleX = LeafSide;
                        var cupScale = (ScaleTransform)CupPath.RenderTransform;
                        cupScale.ScaleX = 1.0;
                        cupScale.ScaleY = 1.0;
                        break;
                    }
            }
        }

        private static Brush Shade(Brush source, double factor)
        {
            if (source is SolidColorBrush scb)
            {
                var c = scb.Color;
                return new SolidColorBrush(Color.FromRgb(
                    (byte)(c.R * factor),
                    (byte)(c.G * factor),
                    (byte)(c.B * factor)));
            }
            return source;
        }
    }

    public enum FlowerOrigin { Bottom, Left, Right }
}
