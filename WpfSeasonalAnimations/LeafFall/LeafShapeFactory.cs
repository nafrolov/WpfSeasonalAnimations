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
            switch (Rng.Next(0, 3))
            {
                case 0: return SimpleLeaf();
                case 1: return CanadianMapleLeaf();
                default: return RoundLeaf();
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

        // ---- Templates (20x20 design box) ----

        /// <summary>Classic teardrop leaf with a central vein.</summary>
        private static PathGeometry SimpleLeaf()
        {
            var fig = new PathFigure { StartPoint = new Point(10, 0), IsClosed = true, IsFilled = true };
            // Right side curve
            fig.Segments.Add(new BezierSegment(
                new Point(20, 6), new Point(20, 14), new Point(10, 20), true));
            // Left side curve
            fig.Segments.Add(new BezierSegment(
                new Point(0, 14), new Point(0, 6), new Point(10, 0), true));

            var geo = new PathGeometry();
            geo.Figures.Add(fig);
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
            return geo;
        }

        /// <summary>Rounded oak-like leaf.</summary>
        private static PathGeometry RoundLeaf()
        {
            var fig = new PathFigure { StartPoint = new Point(10, 1), IsClosed = true, IsFilled = true };
            fig.Segments.Add(new BezierSegment(
                new Point(19, 5), new Point(19, 15), new Point(10, 19), true));
            fig.Segments.Add(new BezierSegment(
                new Point(1, 15), new Point(1, 5), new Point(10, 1), true));

            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            return geo;
        }
    }
}
