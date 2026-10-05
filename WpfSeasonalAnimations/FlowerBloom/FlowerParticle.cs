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
        public Canvas Root;               // container for the three sub-shapes
        public Path StemPath;
        public Path StemLeafPath;
        public Path BudPath;
        public Path CupPath;
        public double BaseRotation;       // slight tilt for naturalness

        // ---- Lifecycle ----
        public bool IsDone;
        public DateTime? BloomedAt;

        private static readonly Random Rng = new Random();

        // Durations in "progress per second" units — scale with ParticleSpeed.
        private const double StemDuration = 1.6;
        private const double BudDuration = 0.8;

        public FlowerParticle(Point origin, FlowerOrigin edge,
                              double scaleFactor, double opacityFactor,
                              Brush flowerFill, Brush stemFill,
                              double particleSpeed)
        {
            Origin = origin;
            Edge = edge;

            Scale = (0.6 + Rng.NextDouble() * 0.8) * scaleFactor;
            Width = 20 * Scale;
            Height = 20 * Scale;

            CupStyle = FlowerShapeFactory.GetRandomCupStyle();
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
                Width = Width,
                Height = Height,
                IsHitTestVisible = false,
                Opacity = Opacity,
                RenderTransformOrigin = new Point(0.5, 1.0) // rotate around stem base
            };

            // How much to shrink the 100-unit geometry down to Width/Height
            const double GeometryDesignSize = 100.0;

            // Stem
            StemPath = new Path
            {
                Data = FlowerShapeFactory.Stem(),
                Fill = StemFill,
                Stretch = Stretch.Fill,
                Width = GeometryDesignSize,
                Height = GeometryDesignSize,
                IsHitTestVisible = false
            };

            // Side leaf — same stem color, slightly darker
            StemLeafPath = new Path
            {
                Data = FlowerShapeFactory.StemLeaf(),
                Fill = Shade(StemFill, 0.85),
                Stretch = Stretch.Fill,
                Width = GeometryDesignSize,
                Height = GeometryDesignSize,
                IsHitTestVisible = false
            };

            // Unopened bud (stage 1) — same stem color
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

            // Opened cup (stage 2) — flower color
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

            StemPath.RenderTransformOrigin = new Point(0.5, 1.0);
            StemPath.RenderTransform = new ScaleTransform(1, 1);

            StemLeafPath.RenderTransformOrigin = new Point(0.5, 1.0);
            StemLeafPath.RenderTransform = new ScaleTransform(1, 1);

            BudPath.RenderTransformOrigin = new Point(0.5, 1.0);
            BudPath.RenderTransform = new ScaleTransform(1, 1);

            CupPath.RenderTransformOrigin = new Point(0.5, 1.0);
            CupPath.RenderTransform = new ScaleTransform(1, 1);

            Root.Children.Add(StemLeafPath);
            Root.Children.Add(StemPath);
            Root.Children.Add(BudPath);
            Root.Children.Add(CupPath);

            // Place the root so the stem base sits at Origin, and rotate slightly.
            Canvas.SetLeft(Root, Origin.X - Width / 2);
            Canvas.SetTop(Root, Origin.Y - Height);

            double geometryScale = Width / GeometryDesignSize; // = 20 * Scale / 100 = 0.2 * Scale

            var group = new TransformGroup();
            group.Children.Add(new ScaleTransform(geometryScale, geometryScale));
            group.Children.Add(new RotateTransform(BaseRotation, Width / 2, Height));
            Root.RenderTransform = group;
            Root.RenderTransformOrigin = new Point(0.5, 1.0); // pivot at stem base
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
                        BloomedAt = DateTime.UtcNow;
                    }
                    ApplyStageVisual();
                    break;

                case Stage.Bloomed:
                    IsDone = true;
                    ApplyStageVisual();
                    break;
            }
        }

        /// <summary>Update which sub-shape is visible and how tall the flower appears.</summary>
        private void ApplyStageVisual()
        {
            switch (CurrentStage)
            {
                case Stage.Stem:
                    {
                        StemPath.Visibility = Visibility.Visible;
                        StemLeafPath.Visibility = Visibility.Visible;
                        BudPath.Visibility = Visibility.Collapsed;
                        CupPath.Visibility = Visibility.Collapsed;

                        double grow = Math.Min(1.0, StageProgress + 0.2);
                        ((ScaleTransform)StemPath.RenderTransform).ScaleY = grow;
                        ((ScaleTransform)StemLeafPath.RenderTransform).ScaleY = grow;
                        break;
                    }
                case Stage.Bud:
                    {
                        StemPath.Visibility = Visibility.Visible;
                        StemLeafPath.Visibility = Visibility.Visible;
                        BudPath.Visibility = Visibility.Visible;
                        CupPath.Visibility = Visibility.Collapsed;

                        ((ScaleTransform)StemPath.RenderTransform).ScaleY = 1.0;
                        ((ScaleTransform)StemLeafPath.RenderTransform).ScaleY = 1.0;
                        ((ScaleTransform)BudPath.RenderTransform).ScaleY =
                            Math.Min(1.0, 0.6 + StageProgress * 0.4);
                        break;
                    }
                case Stage.Bloomed:
                    {
                        StemPath.Visibility = Visibility.Visible;
                        StemLeafPath.Visibility = Visibility.Visible;
                        BudPath.Visibility = Visibility.Collapsed;
                        CupPath.Visibility = Visibility.Visible;

                        ((ScaleTransform)StemPath.RenderTransform).ScaleY = 1.0;
                        ((ScaleTransform)StemLeafPath.RenderTransform).ScaleY = 1.0;
                        ((ScaleTransform)CupPath.RenderTransform).ScaleY = 1.0;
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
