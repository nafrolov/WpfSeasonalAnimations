using System;
using System.Windows;
using System.Windows.Media;

namespace WpfSeasonalAnimations.FlowerBloom
{
    /// <summary>
    /// Supplies stem geometry, stem-leaf geometry, and three cup styles
    /// (tulip, daisy/sunflower, bellflower). All geometries are authored
    /// in a 100 x 100 design box.
    /// </summary>
    public static class FlowerShapeFactory
    {
        private static readonly Random Rng = new Random();

        public enum CupStyle { Tulip, Daisy, Bell }

        public static CupStyle GetRandomCupStyle()
        {
            switch (Rng.Next(0, 3))
            {
                case 0: return CupStyle.Tulip;
                case 1: return CupStyle.Daisy;
                default: return CupStyle.Bell;
            }
        }

        /// <summary>Random bright flower color for the opened cup.</summary>
        public static Brush GetRandomFlowerBrush()
        {
            Color[] palette =
            {
                Color.FromRgb(0xE9, 0x1E, 0x63), // rose
                Color.FromRgb(0xF4, 0x8F, 0xB1), // pink
                Color.FromRgb(0xFF, 0xC1, 0x07), // marigold
                Color.FromRgb(0xE6, 0x4A, 0x19), // orange-red
                Color.FromRgb(0x9C, 0x27, 0xB0), // purple
                Color.FromRgb(0xFF, 0xEB, 0x3B), // sunflower yellow
                Color.FromRgb(0x79, 0x48, 0xE8), // iris
                Color.FromRgb(0xE8, 0xE8, 0xE8), // white daisy
                Color.FromRgb(0xFF, 0x6F, 0x91), // coral
            };
            return new SolidColorBrush(palette[Rng.Next(palette.Length)]);
        }

        // ------------------------------------------------------------------
        //  STEM — drawn from the base (bottom) upward to the cup attach point.
        //  Coordinates assume origin at bottom-left of the design box.
        // ------------------------------------------------------------------

        public static PathGeometry Stem()
        {
            var fig = new PathFigure
            {
                StartPoint = new Point(56, 100), // base right
                IsClosed = true,
                IsFilled = true
            };

            // Right edge of stem, going up (gentle bow, tapering to a point
            // at the tip so any bud or cup seated on it covers the joint).
            fig.Segments.Add(new BezierSegment(
                new Point(58, 70),  // cp1
                new Point(54, 35),  // cp2
                new Point(50, 4),   // apex (single point)
                true));

            // Left edge of stem, coming back down
            fig.Segments.Add(new BezierSegment(
                new Point(46, 35),  // cp1
                new Point(42, 70),  // cp2
                new Point(44, 100), // base left
                true));

            // Close along the base
            fig.Segments.Add(new LineSegment(new Point(56, 100), true));

            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            geo.Freeze();
            return geo;
        }

        /// <summary>
        /// Stem leaf. Authored so the flat attach seam lies on the box's LEFT
        /// edge (x≈2, y≈55..72) and the blade extends fully to the RIGHT,
        /// clear of the stem. Left-side leaves are produced by mirroring in the
        /// particle, so the seam never covers the stem.
        /// </summary>
        public static PathGeometry StemLeaf()
        {
            var fig = new PathFigure
            {
                StartPoint = new Point(2, 55), // top attach (on stem)
                IsClosed = true,
                IsFilled = true
            };

            // Upper edge, curving out to the right to the tip
            fig.Segments.Add(new BezierSegment(
                new Point(40, 28),  // cp1
                new Point(78, 30),  // cp2
                new Point(96, 42),  // tip
                true));

            // Lower edge, curving back to the stem seam
            fig.Segments.Add(new BezierSegment(
                new Point(72, 66),  // cp1
                new Point(36, 74),  // cp2
                new Point(2, 72),   // bottom attach
                true));

            // Close the attach seam
            fig.Segments.Add(new LineSegment(new Point(2, 55), true));

            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            geo.Freeze();
            return geo;
        }

        // ------------------------------------------------------------------
        //  CUPS — three styles. Each has an attach point on the border of the
        //  design box (see GetCupAttachPoint); at bloom time the cup is
        //  positioned so its attach point coincides with the stem tip.
        // ------------------------------------------------------------------

        /// <summary>Tulip: closed cup with three subtle petals visible.</summary>
        public static PathGeometry TulipCup()
        {
            var fig = new PathFigure
            {
                StartPoint = new Point(50, 100), // base center
                IsClosed = true,
                IsFilled = true
            };

            // Right side up to the right petal tip
            fig.Segments.Add(new BezierSegment(
                new Point(60, 90),  // cp1
                new Point(72, 60),  // cp2
                new Point(70, 30),  // right petal tip
                true));

            // Right petal's inner dip
            fig.Segments.Add(new BezierSegment(
                new Point(65, 25),  // cp1
                new Point(58, 28),  // cp2
                new Point(55, 18),  // center petal tip (right)
                true));

            // Over the top of the center petal
            fig.Segments.Add(new BezierSegment(
                new Point(53, 14),  // cp1
                new Point(47, 14),  // cp2
                new Point(45, 18),  // center petal tip (left)
                true));

            // Left petal's inner dip
            fig.Segments.Add(new BezierSegment(
                new Point(42, 28),  // cp1
                new Point(35, 25),  // cp2
                new Point(30, 30),  // left petal tip
                true));

            // Left side down to base
            fig.Segments.Add(new BezierSegment(
                new Point(28, 60),  // cp1
                new Point(40, 90),  // cp2
                new Point(50, 100), // base center
                true));

            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            geo.Freeze();
            return geo;
        }

        /// <summary>
        /// Daisy/sunflower: ten straight, stretched rectangular petals with
        /// slightly rounded outer corners, radiating from the head center
        /// (50, 62). The bottom petal points straight down and its tip edge
        /// lies exactly on the attach point (50, 100), so the head can
        /// overlap the stem tip cleanly.
        /// </summary>
        public static PathGeometry DaisyCup()
        {
            const double Cx = 50, Cy = 62;   // head center
            const double Tip = 38;           // center-to-tip radius
            const double Half = 7;           // half width of each petal
            const double Base = -8;          // pushed past center so petals fuse
            const double Corner = 3;         // rounded tip-corner radius

            // Nonzero fuses the overlapping petals so the head center stays
            // solid (the EvenOdd default would punch holes where neighbors
            // overlap).
            var geo = new PathGeometry { FillRule = FillRule.Nonzero };

            for (int k = 0; k < 10; k++)
            {
                double a = (90 + k * 36) * Math.PI / 180.0;
                double dx = Math.Cos(a), dy = Math.Sin(a);    // petal axis
                double nx = Math.Sin(a), ny = -Math.Cos(a);   // perpendicular

                // Maps local petal coords (u = across, v = along) to design
                // box coords. Traversal is clockwise on screen, so convex
                // corner arcs sweep Clockwise.
                Point P(double u, double v) => new Point(
                    Cx + u * nx + v * dx,
                    Cy + u * ny + v * dy);

                var fig = new PathFigure
                {
                    StartPoint = P(-Half, Base),
                    IsClosed = true,
                    IsFilled = true
                };
                fig.Segments.Add(new LineSegment(P(Half, Base), true));
                fig.Segments.Add(new LineSegment(P(Half, Tip - Corner), true));
                fig.Segments.Add(new ArcSegment(
                    P(Half - Corner, Tip),
                    new Size(Corner, Corner), 0, false,
                    SweepDirection.Clockwise, true));
                fig.Segments.Add(new LineSegment(P(-(Half - Corner), Tip), true));
                fig.Segments.Add(new ArcSegment(
                    P(-Half, Tip - Corner),
                    new Size(Corner, Corner), 0, false,
                    SweepDirection.Clockwise, true));

                geo.Figures.Add(fig);
            }

            geo.Freeze();
            return geo;
        }

        /// <summary>
        /// Bellflower: hanging bell-shaped cup. The attach point is at the TOP
        /// of the design box (50, 0) so the bell hangs below the stem tip.
        /// </summary>
        public static PathGeometry BellCup()
        {
            var fig = new PathFigure
            {
                StartPoint = new Point(50, 0), // attach point (top)
                IsClosed = true,
                IsFilled = true
            };

            // Right side flaring out and down to the bell mouth
            fig.Segments.Add(new BezierSegment(
                new Point(58, 8),   // cp1
                new Point(78, 30),  // cp2
                new Point(82, 58),  // right bell mouth
                true));

            // Bell mouth (scalloped bottom edge) — slight upward curve in the middle
            fig.Segments.Add(new BezierSegment(
                new Point(72, 72),  // cp1
                new Point(62, 75),  // cp2
                new Point(50, 68),  // bottom center
                true));

            // Left side of the bell mouth
            fig.Segments.Add(new BezierSegment(
                new Point(38, 75),  // cp1
                new Point(28, 72),  // cp2
                new Point(18, 58),  // left bell mouth
                true));

            // Left side flaring back up to the attach point
            fig.Segments.Add(new BezierSegment(
                new Point(22, 30),  // cp1
                new Point(42, 8),   // cp2
                new Point(50, 0),   // attach point
                true));

            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            geo.Freeze();
            return geo;
        }

        // ------------------------------------------------------------------
        //  UNOPENED BUD — a small closed shape used during stage 1.
        // ------------------------------------------------------------------

        public static PathGeometry Bud()
        {
            var fig = new PathFigure
            {
                StartPoint = new Point(50, 100), // base (attaches to stem tip)
                IsClosed = true,
                IsFilled = true
            };

            // Right side rising to the tip
            fig.Segments.Add(new BezierSegment(
                new Point(60, 82),  // cp1
                new Point(60, 48),  // cp2
                new Point(50, 25),  // tip
                true));

            // Left side coming back down
            fig.Segments.Add(new BezierSegment(
                new Point(40, 48),  // cp1
                new Point(40, 82),  // cp2
                new Point(50, 100), // base
                true));

            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            geo.Freeze();
            return geo;
        }

        public static PathGeometry GetCup(CupStyle style)
        {
            switch (style)
            {
                case CupStyle.Tulip: return TulipCup();
                case CupStyle.Daisy: return DaisyCup();
                default: return BellCup();
            }
        }

        /// <summary>
        /// Point inside the 100-unit cup geometry that must coincide with the
        /// stem tip: bottom-center (50,100) for upright cups, top-center
        /// (50,0) for the hanging bell.
        /// </summary>
        public static Point GetCupAttachPoint(CupStyle style)
        {
            return style == CupStyle.Bell
                ? new Point(50, 0)
                : new Point(50, 100);
        }
    }
}
