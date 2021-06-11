using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BLTAdoptAHero.UI
{
    public class PieSlice : Shape
    {
        public double StartAngle
        {
            get => (double)GetValue(StartAngleProperty);
            set => SetValue(StartAngleProperty, value);
        }
        
        public static readonly DependencyProperty StartAngleProperty = DependencyProperty.Register(
            nameof (StartAngle), typeof (double), typeof (Shape), 
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, null, CoerceAngle));

        public double EndAngle
        {
            get => (double)GetValue(EndAngleProperty);
            set => SetValue(EndAngleProperty, value);
        }
     
        public static readonly DependencyProperty EndAngleProperty = DependencyProperty.Register(
            nameof (EndAngle), typeof (double), typeof (Shape), 
            new FrameworkPropertyMetadata(90.0, FrameworkPropertyMetadataOptions.AffectsRender, null, CoerceAngle));
     
        private static object CoerceAngle(DependencyObject depObj, object baseVal)
        {
            double angle = (double)baseVal;
            angle = Math.Min(angle, 359.9);
            angle = Math.Max(angle, 0.0);
            return angle;
        }
     
        protected override Geometry DefiningGeometry
        {
            get
            {
                double maxWidth = Math.Max(0.0, RenderSize.Width - StrokeThickness);
                double maxHeight = Math.Max(0.0, RenderSize.Height - StrokeThickness);
     
                double xStart = maxWidth / 2.0 * Math.Cos(StartAngle * Math.PI / 180.0);
                double yStart = maxHeight / 2.0 * Math.Sin(StartAngle * Math.PI / 180.0);
     
                double xEnd = maxWidth / 2.0 * Math.Cos(EndAngle * Math.PI / 180.0);
                double yEnd = maxHeight / 2.0 * Math.Sin(EndAngle * Math.PI / 180.0);
     
                var geom = new StreamGeometry();
                
                using var ctx = geom.Open();
                
                ctx.BeginFigure(
                    new Point(RenderSize.Width / 2.0 + xStart, RenderSize.Height / 2.0 - yStart),
                    true, true);

                ctx.ArcTo(
                    new Point(RenderSize.Width / 2.0 + xEnd, RenderSize.Height / 2.0 - yEnd),
                    new Size(maxWidth / 2.0, maxHeight / 2),
                    0.0,
                    EndAngle - StartAngle > 180,   // greater than 180 deg?
                    SweepDirection.Counterclockwise,
                    true, false);

                if (EndAngle - StartAngle < 359.9)
                {
                    ctx.LineTo(new Point(RenderSize.Width / 2.0, RenderSize.Height / 2.0), true, false);
                }

                return geom;
            }
        }
    }
}