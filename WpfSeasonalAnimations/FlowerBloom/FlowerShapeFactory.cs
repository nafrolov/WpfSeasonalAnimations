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

        /// <summary>Daisy/sunflower: round petal ring with a darker center.</summary>
        public static PathGeometry DaisyCup()
        {
            var fig = new PathFigure
            {
                StartPoint = new Point(50, 100), // bottom petal tip (= attach)
                IsClosed = true,
                IsFilled = true
            };

            // Ten petals as a starburst around the flower head center (50, 62),
            // authored tip-to-valley clockwise. The bottom tip sits exactly on
            // the attach point so the head can overlap the stem tip cleanly.
            fig.Segments.Add(new PolyLineSegment(new[]
            {
                new Point(58, 87),  // valley
                new Point(72, 93),  // tip
                new Point(71, 77),  // valley
                new Point(86, 74),  // tip
                new Point(76, 62),  // valley
                new Point(86, 50),  // tip
                new Point(71, 47),  // valley
                new Point(72, 31),  // tip
                new Point(58, 37),  // valley
                new Point(50, 24),  // tip (top)
                new Point(42, 37),  // valley
                new Point(28, 31),  // tip
                new Point(29, 47),  // valley
                new Point(14, 50),  // tip
                new Point(24, 62),  // valley
                new Point(14, 74),  // tip
                new Point(29, 77),  // valley
                new Point(28, 93),  // tip
                new Point(42, 87),  // valley
            }, true));

            var geo = new PathGeometry();
            geo.Figures.Add(fig);
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
