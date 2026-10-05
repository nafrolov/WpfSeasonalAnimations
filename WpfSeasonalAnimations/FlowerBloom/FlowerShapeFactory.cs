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
                StartPoint = new Point(50, 100), // base center
                IsClosed = true,
                IsFilled = true
            };

            // Right edge of stem, going up
            fig.Segments.Add(new BezierSegment(
                new Point(52, 80),  // cp1
                new Point(50, 50),  // cp2
                new Point(52, 20),  // attach point right
                true));

            // Top of stem
            fig.Segments.Add(new LineSegment(new Point(48, 20), true));

            // Left edge of stem, coming back down
            fig.Segments.Add(new BezierSegment(
                new Point(50, 50),  // cp1
                new Point(48, 80),  // cp2
                new Point(48, 100), // base left
                true));

            // Close along the base
            fig.Segments.Add(new LineSegment(new Point(50, 100), true));

            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            geo.Freeze();
            return geo;
        }

        /// <summary>Small leaf attached partway up the stem.</summary>
        public static PathGeometry StemLeaf()
        {
            var fig = new PathFigure
            {
                StartPoint = new Point(48, 58), // attach to stem
                IsClosed = true,
                IsFilled = true
            };

            // Upper edge of leaf, curving out and down to the tip
            fig.Segments.Add(new BezierSegment(
                new Point(38, 50),  // cp1
                new Point(22, 55),  // cp2
                new Point(15, 65),  // tip
                true));

            // Lower edge of leaf, curving back to the stem
            fig.Segments.Add(new BezierSegment(
                new Point(25, 72),  // cp1
                new Point(40, 68),  // cp2
                new Point(48, 62),  // base right
                true));

            // Short segment closing back to the start point
            fig.Segments.Add(new LineSegment(new Point(48, 58), true));

            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            geo.Freeze();
            return geo;
        }

        // ------------------------------------------------------------------
        //  CUPS — three styles. All are authored with the stem attach point
        //  near (50, 100). At bloom time they're anchored so the base of the
        //  cup meets the top of the stem.
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
                StartPoint = new Point(50, 100), // base center
                IsClosed = true,
                IsFilled = true
            };

            // Right side up to the rightmost petal
            fig.Segments.Add(new BezierSegment(
                new Point(60, 95),  // cp1
                new Point(78, 80),  // cp2
                new Point(85, 60),  // rightmost petal tip
                true));

            // Petal ring traced counter-clockwise around the flower head center (50, 45).
            // Each pair of points is (tip, valley) — 10 petals total.
            fig.Segments.Add(new PolyLineSegment(new[]
            {
                new Point(88, 45),  // tip
                new Point(78, 40),  // valley
                new Point(82, 28),  // tip
                new Point(70, 32),  // valley
                new Point(68, 18),  // tip
                new Point(58, 26),  // valley
                new Point(50, 12),  // tip (top center)
                new Point(42, 26),  // valley
                new Point(32, 18),  // tip
                new Point(30, 32),  // valley
                new Point(18, 28),  // tip
                new Point(22, 40),  // valley
                new Point(12, 45),  // tip
                new Point(22, 50),  // valley
                new Point(18, 62),  // tip
                new Point(30, 60),  // valley
                new Point(32, 74),  // tip
                new Point(42, 66),  // valley
                new Point(50, 80),  // tip (bottom center)
                new Point(58, 66),  // valley
                new Point(68, 74),  // tip
                new Point(70, 60),  // valley
                new Point(82, 62),  // tip
                new Point(78, 55),  // valley
                new Point(85, 60),  // rightmost tip again (overlap)
            }, true));

            // Close back down to the base
            fig.Segments.Add(new BezierSegment(
                new Point(78, 80),  // cp1
                new Point(60, 95),  // cp2
                new Point(50, 100), // base center
                true));

            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            geo.Freeze();
            return geo;
        }

        /// <summary>Bellflower: hanging bell-shaped cup.</summary>
        public static PathGeometry BellCup()
        {
            var fig = new PathFigure
            {
                StartPoint = new Point(50, 20), // attach point (top)
                IsClosed = true,
                IsFilled = true
            };

            // Right side flaring out and down to the bell mouth
            fig.Segments.Add(new BezierSegment(
                new Point(58, 28),  // cp1
                new Point(78, 50),  // cp2
                new Point(82, 78),  // right bell mouth
                true));

            // Bell mouth (scalloped bottom edge) — slight upward curve in the middle
            fig.Segments.Add(new BezierSegment(
                new Point(72, 92),  // cp1
                new Point(62, 95),  // cp2
                new Point(50, 88),  // bottom center
                true));

            // Left side of the bell mouth
            fig.Segments.Add(new BezierSegment(
                new Point(38, 95),  // cp1
                new Point(28, 92),  // cp2
                new Point(18, 78),  // left bell mouth
                true));

            // Left side flaring back up to the attach point
            fig.Segments.Add(new BezierSegment(
                new Point(22, 50),  // cp1
                new Point(42, 28),  // cp2
                new Point(50, 20),  // attach point
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
                StartPoint = new Point(50, 100), // base
                IsClosed = true,
                IsFilled = true
            };

            // Right side rising to the tip
            fig.Segments.Add(new BezierSegment(
                new Point(60, 85),  // cp1
                new Point(62, 55),  // cp2
                new Point(50, 40),  // tip
                true));

            // Left side coming back down
            fig.Segments.Add(new BezierSegment(
                new Point(38, 55),  // cp1
                new Point(40, 85),  // cp2
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
    }
}
