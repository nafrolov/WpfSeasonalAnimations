using System;
using System.Windows;
using System.Windows.Media;

namespace WpfSeasonalAnimations.LeafFall
{
    /// <summary>
    /// Provides a small set of leaf PathGeometry templates and autumn colors.
    /// All geometries are authored inside a nominal 20x20 box, so scaling
    /// is handled by the particle's Scale property.
    /// </summary>
    public static class LeafShapeFactory
    {
        private static readonly Random Rng = new Random();

        /// <summary>
        /// Returns a randomly chosen leaf geometry.
        /// </summary>
        public static PathGeometry GetRandomGeometry()
        {
            switch (Rng.Next(0, 4))
            {
                case 0: return SimpleLeaf();
                case 1: return CanadianMapleLeaf();
                case 2: return SquashedLeaf();
                default: return OakLeaf();
            }
        }

        /// <summary>
        /// Returns a random autumn color.
        /// </summary>
        public static Brush GetRandomBrush()
        {
            // Warm palette: golds, oranges, reds, browns
            Color[] palette =
            {
                Color.FromRgb(0xC0, 0x39, 0x2B), // brick red
                Color.FromRgb(0xD3, 0x54, 0x00), // burnt orange
                Color.FromRgb(0xE6, 0x7E, 0x22), // amber
                Color.FromRgb(0xF4, 0xA2, 0x60), // light orange
                Color.FromRgb(0xD9, 0xB3, 0x8C), // tan
                Color.FromRgb(0x8B, 0x5A, 0x2B), // brown
                Color.FromRgb(0xA0, 0x52, 0x2D), // rust
                Color.FromRgb(0xC9, 0x7B, 0x2B), // goldenrod
            };
            var c = palette[Rng.Next(palette.Length)];
            return new SolidColorBrush(c);
        }

        // ---- Templates (100x100 design box) ----

        /// <summary>Classic teardrop leaf with a central vein.</summary>
        private static PathGeometry SimpleLeaf()
        {
            var fig = new PathFigure { StartPoint = new Point(50, 0), IsClosed = true, IsFilled = true };
            // Right side curve
            fig.Segments.Add(new BezierSegment(
                new Point(71, 42), new Point(89, 70), new Point(50, 100), true));
            // Left side curve
            fig.Segments.Add(new BezierSegment(
                new Point(11, 70), new Point(29, 42), new Point(50, 0), true));

            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            geo.Freeze();
            return geo;
        }

        /// <summary>Canadian maple-like leaf.</summary>
        private static PathGeometry CanadianMapleLeaf()
        {
            var fig = new PathFigure { StartPoint = new Point(50, 5), IsClosed = true, IsFilled = true };
            fig.Segments.Add(new PolyLineSegment(new[]
            {
                new Point(60, 18),
                new Point(68, 15),
                new Point(65, 39),
                new Point(78, 27),
                new Point(80, 35),
                new Point(95, 32),
                new Point(90, 48),
                new Point(98, 50),
                new Point(74, 69),
                new Point(76, 77),
                new Point(54, 74),
                new Point(54, 99),
                new Point(46, 99),
                new Point(46, 74),
                new Point(24, 77),
                new Point(26, 69),
                new Point(12, 50),
                new Point(10, 48),
                new Point(5, 32),
                new Point(20, 35),
                new Point(22, 27),
                new Point(35, 39),
                new Point(32, 15),
                new Point(40, 18),
                new Point(50, 5)
            }, true));

            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            geo.Freeze();
            return geo;
        }

        /// <summary>Oak-like leaf.</summary>
        private static PathGeometry OakLeaf()
        {
            var fig = new PathFigure
            {
                StartPoint = new Point(50, 6),
                IsClosed = true,
                IsFilled = true
            };

            // ----- Right half: top apex → down the right side → stem tip -----
            fig.Segments.Add(new BezierSegment(new Point(54, 10), new Point(52, 15), new Point(55, 18), true));
            fig.Segments.Add(new BezierSegment(new Point(59, 19), new Point(62, 16), new Point(64, 18), true));
            fig.Segments.Add(new BezierSegment(new Point(65, 24), new Point(59, 31), new Point(60, 33), true));
            fig.Segments.Add(new BezierSegment(new Point(66, 34), new Point(69, 29), new Point(74, 31), true));
            fig.Segments.Add(new BezierSegment(new Point(73, 39), new Point(62, 44), new Point(60, 51), true));
            fig.Segments.Add(new BezierSegment(new Point(65, 53), new Point(69, 47), new Point(74, 50), true));
            fig.Segments.Add(new BezierSegment(new Point(73, 57), new Point(61, 60), new Point(58, 66), true));
            fig.Segments.Add(new BezierSegment(new Point(62, 64), new Point(66, 64), new Point(67, 66), true));
            fig.Segments.Add(new BezierSegment(new Point(65, 73), new Point(53, 77), new Point(51, 81), true));

            // Stem (right side down to tip)
            fig.Segments.Add(new LineSegment(new Point(51, 92), true));
            fig.Segments.Add(new LineSegment(new Point(50, 92), true));
            fig.Segments.Add(new LineSegment(new Point(49, 92), true));

            // ----- Left half: stem tip → up the left side → top apex -----
            fig.Segments.Add(new LineSegment(new Point(49, 81), true));
            fig.Segments.Add(new BezierSegment(new Point(47, 77), new Point(35, 73), new Point(33, 66), true));
            fig.Segments.Add(new BezierSegment(new Point(34, 64), new Point(38, 64), new Point(42, 66), true));
            fig.Segments.Add(new BezierSegment(new Point(39, 60), new Point(27, 57), new Point(26, 50), true));
            fig.Segments.Add(new BezierSegment(new Point(31, 47), new Point(35, 53), new Point(40, 51), true));
            fig.Segments.Add(new BezierSegment(new Point(38, 44), new Point(27, 39), new Point(26, 31), true));
            fig.Segments.Add(new BezierSegment(new Point(31, 29), new Point(34, 34), new Point(40, 33), true));
            fig.Segments.Add(new BezierSegment(new Point(41, 31), new Point(35, 24), new Point(36, 18), true));
            fig.Segments.Add(new BezierSegment(new Point(38, 16), new Point(41, 19), new Point(45, 18), true));
            fig.Segments.Add(new BezierSegment(new Point(48, 15), new Point(46, 10), new Point(50, 6), true));

            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            geo.Freeze();
            return geo;
        }

        /// <summary>Squashed-like leaf.</summary>
        private static PathGeometry SquashedLeaf()
        {
            var fig = new PathFigure
            {
                StartPoint = new Point(50, 0),
                IsClosed = true,
                IsFilled = true
            };

            fig.Segments.Add(new BezierSegment(
                new Point(30, 35),
                new Point(75, 75),
                new Point(50, 100),
                true));

            fig.Segments.Add(new BezierSegment(
                new Point(10, 70), 
                new Point(0, 30),
                new Point(50, 0),
                true));

            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            geo.Freeze();
            return geo;
        }
    }
}
