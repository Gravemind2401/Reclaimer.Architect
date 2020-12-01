using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Reclaimer.Controls
{
    /// <summary>
    /// Interaction logic for Spinner.xaml
    /// https://stackoverflow.com/a/45941268/12034691
    /// </summary>
    public partial class Spinner : UserControl
    {
        public static readonly DependencyProperty EllipseSizeProperty =
            DependencyProperty.Register(nameof(EllipseSize), typeof(int), typeof(Spinner), new PropertyMetadata(8));

        public static readonly DependencyProperty SpinnerHeightProperty =
            DependencyProperty.Register(nameof(SpinnerHeight), typeof(int), typeof(Spinner), new PropertyMetadata(64));

        public static readonly DependencyProperty SpinnerWidthProperty =
            DependencyProperty.Register(nameof(SpinnerWidth), typeof(int), typeof(Spinner), new PropertyMetadata(64));

        public int EllipseSize
        {
            get { return (int)GetValue(EllipseSizeProperty); }
            set { SetValue(EllipseSizeProperty, value); }
        }

        public int SpinnerHeight
        {
            get { return (int)GetValue(SpinnerHeightProperty); }
            set { SetValue(SpinnerHeightProperty, value); }
        }

        public int SpinnerWidth
        {
            get { return (int)GetValue(SpinnerWidthProperty); }
            set { SetValue(SpinnerWidthProperty, value); }
        }

        // start positions
        public EllipseStartPosition EllipseN { get; private set; }
        public EllipseStartPosition EllipseNE { get; private set; }
        public EllipseStartPosition EllipseE { get; private set; }
        public EllipseStartPosition EllipseSE { get; private set; }
        public EllipseStartPosition EllipseS { get; private set; }
        public EllipseStartPosition EllipseSW { get; private set; }
        public EllipseStartPosition EllipseW { get; private set; }
        public EllipseStartPosition EllipseNW { get; private set; }

        public Spinner()
        {
            InitializeComponent();
        }

        private void InitialSetup()
        {
            float horizontalCenter = SpinnerWidth / 2f;
            float verticalCenter = SpinnerHeight / 2f;
            float distance = (float)Math.Min(SpinnerHeight, SpinnerWidth) / 2;

            double angleInRadians = 44.8d;
            float cosine = (float)Math.Cos(angleInRadians);
            float sine = (float)Math.Sin(angleInRadians);

            EllipseN = GetNewPosition(left: horizontalCenter, top: verticalCenter - distance);
            EllipseNE = GetNewPosition(left: horizontalCenter + (distance * cosine), top: verticalCenter - (distance * sine));
            EllipseE = GetNewPosition(left: horizontalCenter + distance, top: verticalCenter);
            EllipseSE = GetNewPosition(left: horizontalCenter + (distance * cosine), top: verticalCenter + (distance * sine));
            EllipseS = GetNewPosition(left: horizontalCenter, top: verticalCenter + distance);
            EllipseSW = GetNewPosition(left: horizontalCenter - (distance * cosine), top: verticalCenter + (distance * sine));
            EllipseW = GetNewPosition(left: horizontalCenter - distance, top: verticalCenter);
            EllipseNW = GetNewPosition(left: horizontalCenter - (distance * cosine), top: verticalCenter - (distance * sine));
        }

        private EllipseStartPosition GetNewPosition(float left, float top)
        {
            return new EllipseStartPosition { Left = left, Top = top };
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property.Name == nameof(Height))
                SpinnerHeight = Convert.ToInt32(e.NewValue);

            if (e.Property.Name == nameof(Width))
                SpinnerWidth = Convert.ToInt32(e.NewValue);

            if (SpinnerHeight > 0 && SpinnerWidth > 0)
                InitialSetup();

            base.OnPropertyChanged(e);
        }
    }

    public struct EllipseStartPosition
    {
        public float Left { get; set; }
        public float Top { get; set; }
    }
}
