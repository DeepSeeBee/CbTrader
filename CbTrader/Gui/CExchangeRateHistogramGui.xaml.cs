using CbTrader.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace CbTrader.Gui
{
    using CGetHistogramPointFunc = Func<Tuple<CExchangeRate, decimal, decimal>>;
    using CHistogramPoint = Tuple<CExchangeRate, decimal, decimal>;
    using CCanvasAndNewDataObjectsFunc = Tuple<Canvas, Func<Canvas, IEnumerable<CGuiObject>, IEnumerable<CGuiObject>>>;
    using CFrameworkElementDetail = Tuple<FrameworkElement, Canvas, CGuiObject.CLayerEnum>;
    using CDateTimeRange = Tuple<DateTime, DateTime>;

    internal abstract class CGuiObject
    {

        internal enum CLayerEnum
        {
            FillArea,
            DiagramLine,
            DataPoints,
            
            Selection,
            ValueText,
            Axis,
            XAxisStaticMarker,
            YAxisMouseOverMarker,
            XAxisMouseOverLine,
            SelectionGui,
            TrendLine,
            CrosshairSign,
        }

        internal static readonly double XAxisLineOpcacity = 0.5d;

        internal void Init()
        {
            foreach(var aFe in this.FrameworkElements)
            {
                aFe.Tag = this;
            }
        }
        internal virtual void BeginRefresh() { }
        internal virtual void Refresh() { }
        internal virtual IEnumerable<FrameworkElement> FrameworkElements => Array.Empty<FrameworkElement>();
        internal Canvas Canvas { get; set; }
        internal virtual IEnumerable<CFrameworkElementDetail> FrameworkElementDetails => this.FrameworkElements.Select(f => new CFrameworkElementDetail(f, this.Canvas, this.LayerEnum));
        internal abstract CLayerEnum LayerEnum { get; }
        internal CColors Colors => CColors.Singleton;
        #region X
        internal Func<double> XOffsetFunc = new Func<double>(() => 0d);
        internal double XOffset => this.XOffsetFunc();
        private double? XM;
        internal double X
        {
            get => (this.XM.HasValue ? this.XM.Value : this.XDefault) + this.XOffset;
            set => XM = value;
        }
        internal virtual double XDefault => double.NaN;
        #endregion
        #region Y
        internal Func<double> YOffsetFunc = new Func<double>(() => 0d);
        internal double YOffset => this.YOffsetFunc();
        private double? YM;
        internal double Y
        {
            get => (this.YM.HasValue ? this.YM.Value : this.YDefault) + this.YOffset;
            set => YM = value;
        }
        internal virtual double YDefault => double.NaN;

        #endregion

        #region Dx
        internal Func<double> DxOffsetFunc = new Func<double>(() => 0d);
        internal double DxOffset => this.DxOffsetFunc();
        private double? DxM;
        internal double Dx
        {
            get => (this.DxM.HasValue ? this.DxM.Value : this.DxDefault + this.DxOffset);
            set => DxM = value;
        }
        internal virtual double DxDefault => double.NaN;

        #endregion
        #region Dy
        internal Func<double> DyOffsetFunc = new Func<double>(() => 0d);
        internal double DyOffset => this.DyOffsetFunc();
        private double? DyM;
        internal double Dy
        {
            get => (this.DyM.HasValue ? this.DyM.Value : this.DyDefault) + this.DyOffset;
            set => DyM = value;
        }
        internal virtual double DyDefault => double.NaN;
        #endregion
    }

    internal abstract class CDataPoint : CGuiObject
    {
        internal CDataPoint(CGetHistogramPointFunc aGetHistogramPointFunc)
        {
            this.GetHistgramPointFunc = aGetHistogramPointFunc;
            this.HistogramPoint = this.GetHistgramPointFunc();
        }
        private readonly CGetHistogramPointFunc GetHistgramPointFunc;
        private CHistogramPoint HistogramPoint;
        internal CExchangeRate ExchangeRate => this.HistogramPoint.Item1;

        internal override double XDefault => (double)this.HistogramPoint.Item2;
        internal override double YDefault => (double)this.HistogramPoint.Item3;

        //internal CPriceWithCurrency CaptionAmount
        //    => this.ExchangeRate.BuyAmount.Exchange(this.ExchangeRate.BuyAmount);

        internal override void BeginRefresh()
        {
            base.BeginRefresh();
            this.HistogramPoint = this.GetHistgramPointFunc();
        }

        internal override void Refresh()
        {
            base.Refresh();
            this.HistogramPoint = this.GetHistgramPointFunc();
        }
    }

    internal sealed class CDiagramDataPoint : CDataPoint
    {
        internal CDiagramDataPoint(CGetHistogramPointFunc aGetHistogramPointFunc):base(aGetHistogramPointFunc)
        {
            this.Init();
        }
        private readonly Ellipse Ellipse = new Ellipse();
        internal override IEnumerable<FrameworkElement> FrameworkElements => Array.Empty<FrameworkElement>(); // { get { yield return this.Ellipse; } }

        internal override CLayerEnum LayerEnum => CLayerEnum.DataPoints;
        internal static readonly double RadiusConst = 1d;
        internal double Radius => RadiusConst;
        internal override void Refresh()
        {
            base.Refresh();

            var aExhangeRate = this.ExchangeRate;

            var aEllipse = this.Ellipse;
            aEllipse.Fill = this.Colors.Point;
            var aRadius = this.Radius;
            aEllipse.Width = aRadius*2;
            aEllipse.Height = aRadius*2;
            var aX = this.X - aRadius;
            var aY = this.Y  - aRadius;
            Canvas.SetLeft(aEllipse, aX);
            Canvas.SetTop(aEllipse, aY);
        }
    }

    internal abstract class CLine : CGuiObject
    {
        internal CLine(Canvas aCanvas, CDiagramDataPoint aPoint1, CDiagramDataPoint aPoint2)
        {
            this.Canvas = aCanvas;
            this.Point1 = aPoint1;
            this.Point2 = aPoint2;
        }
        internal readonly CDiagramDataPoint Point1;
        internal readonly CDiagramDataPoint Point2;
        internal readonly Line Line = new Line();
        internal override void Refresh()
        {
            this.Line.X1 = this.Point1.X;
            this.Line.Y1 = this.Point1.Y;
            this.Line.X2 = this.Point2.X;
            this.Line.Y2 = this.Point2.Y;
            this.Line.StrokeThickness = 1;
        }
        internal override IEnumerable<FrameworkElement> FrameworkElements { get { yield return this.Line; } }

    }

    internal sealed class CDiagramLine : CLine
    {
        internal CDiagramLine(Canvas aCanvas, CDiagramDataPoint aPoint1, CDiagramDataPoint aPoint2) : base(aCanvas, aPoint1, aPoint2)
        {
            this.Init();

        }
        internal override void Refresh()
        {
            base.Refresh();
            this.Line.Stroke = this.Colors.DataPointLine;
        }
        internal override CLayerEnum LayerEnum => CLayerEnum.DiagramLine;

    }

    internal sealed class CValueText : CDataPoint
    {
        internal CValueText(Canvas aCanvas, CExchangeRateHistogram aHistogram, CGetHistogramPointFunc aGetHistgramPointFunc) : base(aGetHistgramPointFunc)
        {
            this.Canvas = aCanvas;
            this.Histogram = aHistogram;
            
            this.Init();
        }
        private readonly CExchangeRateHistogram Histogram;

        internal readonly TextBlock TextBlock = new TextBlock();
        internal override IEnumerable<FrameworkElement> FrameworkElements { get { yield return this.TextBlock; } }

        internal override CLayerEnum LayerEnum => CLayerEnum.ValueText;

        //internal CPriceWithCurrency CaptionAmount
        //    => this.ExchangeRate.BuyAmount.Exchange(this.ExchangeRate.BuyAmount);
        internal double Width = double.NaN;

        internal override void Refresh()
        {
            this.TextBlock.Height = 20;
            Canvas.SetLeft(this.TextBlock, this.X);
            Canvas.SetTop(this.TextBlock, this.Y);
            this.TextBlock.Text = this.ExchangeRate.TitleWithoutDate + " ";
            this.TextBlock.Width = this.Canvas.ActualWidth;
            this.TextBlock.TextAlignment = TextAlignment.Right;
        }
    }


    internal abstract class CXyDiagramLine : CGuiObject
    {
        internal CXyDiagramLine(Canvas aCanvas, CDiagramDataPoint aDiagramDataPoint)
        {
            this.Canvas = aCanvas;
            this.DataPoint = aDiagramDataPoint;
            this.Opacity = 0d;
            this.Fill = this.Colors.Crosshair;
        }
        
        internal readonly CDiagramDataPoint DataPoint;

        private Brush FillM;
        internal Brush Fill
        {
            get => this.FillM;
            set
            {
                this.FillM = value;
                this.OnFillChanged();
            }
        }
        protected virtual void OnFillChanged()
        {
        }
        private double OpacityM = 1.0d;
        internal double Opacity
        {
            get => this.OpacityM;
            set
            {
                this.OpacityM = value;
                this.OnOpacityChanged();
            }
        }
        protected virtual void OnOpacityChanged()
        {

        }
    }

    internal sealed class CYAxisMouseOverLine : CXyDiagramLine
    {
        internal CYAxisMouseOverLine(Canvas aCanvas, CXAxisMouseOverLine aXAxisMouseOverLine) : base(aCanvas, aXAxisMouseOverLine.DataPoint)
        {
            this.XAxisMouseOverLine = aXAxisMouseOverLine;
            aXAxisMouseOverLine.VisibleChanged += AXAxisMouseOverLine_VisibleChanged;
            this.Fill = this.Colors.Crosshair;
        }

        private void AXAxisMouseOverLine_VisibleChanged(object sender, EventArgs e)
        {
            var aVisible = this.XAxisMouseOverLine.Visible;
            this.Opacity = aVisible ? XAxisLineOpcacity : 0.0d;
            if (aVisible)
            {
                this.Refresh();
            }
        }

        private readonly CXAxisMouseOverLine XAxisMouseOverLine;

        internal override IEnumerable<FrameworkElement> FrameworkElements
        {
            get
            {
                yield return this.HorizontalLine;
            }
        }
        internal override CLayerEnum LayerEnum => CLayerEnum.YAxisMouseOverMarker;
        protected override void OnFillChanged()
        {
            base.OnFillChanged();
            this.HorizontalLine.Stroke = this.Fill;
        }

        protected override void OnOpacityChanged()
        {
            base.OnOpacityChanged();
            this.HorizontalLine.Opacity = this.Opacity ;
        }

        internal readonly Line HorizontalLine = new Line();
        internal override void Refresh()
        {
            base.Refresh();
            this.HorizontalLine.X1 = 0;
            this.HorizontalLine.Y1 = this.DataPoint.Y;
            this.HorizontalLine.X2 = this.Canvas.ActualWidth;
            this.HorizontalLine.Y2 = this.DataPoint.Y;
            this.HorizontalLine.StrokeThickness = this.XAxisMouseOverLine.Dx;
            //System.Windows.Controls.Canvas.SetLeft(this.HorizontalLine, 0);
            //System.Windows.Controls.Canvas.SetTop(this.HorizontalLine, this.DataPoint.Y);
            //this.HorizontalLine.Width = this.Canvas.ActualWidth;


        }
    }

    internal sealed class CXAxisMouseOverLine : CXyDiagramLine
    {
        internal CXAxisMouseOverLine(Canvas aCanvas, CDiagramDataPoint aDiagramDataPoint, CSelection aSelection) :base(aCanvas, aDiagramDataPoint)
        {
            this.Selection = aSelection;
            this.Rectangle.IsMouseDirectlyOverChanged += Rectangle_IsMouseDirectlyOverChanged;
            this.Rectangle.MouseMove += Rectangle_MouseMove;
            this.Rectangle.MouseDown += Rectangle_MouseDown;
            this.Rectangle.MouseUp += Rectangle_MouseUp;
        }


        private void Rectangle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                //if (this.Selection.Point1Nullable is object
                //&& this.Selection.Point2Nullable is object)
                //{                   
                //}
                //else 

            }
            catch (Exception aExc)
            {
                aExc.CatchUnexpected();
            }
        }

        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.RightButton == MouseButtonState.Pressed)
                {
                    //this.Selection.Reset();
                    this.Selection.Trader.InvestmentExchangeRateHistogram = default;
                }
                else if(e.LeftButton == MouseButtonState.Pressed)
                {
                    if (this.Selection.Finished
                    && this.Selection.DateTimeRange.Contains(this.DataPoint.ExchangeRate.DateTime))
                    {
                        this.Selection.ZoomIn();
                    }
                    else
                    {
                        this.Selection.Begin(this.DataPoint);
                    }
                }
            }
            catch(Exception aExc)
            {
                aExc.CatchUnexpected();
            }
        }

        private readonly CSelection Selection;

        private void Rectangle_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                this.MousePos = e.GetPosition(this.Canvas);
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    this.Selection.Add(this.DataPoint);
                }
                else
                {
                    this.Selection.End();
                }
            }
            catch (Exception aExc)
            {
                aExc.CatchUnexpected();
            }
        }

        internal event EventHandler MousePosChanged;
        private Point MousePosM;
        internal Point MousePos
        {
            get => this.MousePosM;
            set
            {
                this.MousePosM = value;
                if (this.MousePosChanged is object)
                {
                    this.MousePosChanged(this, default);
                }
            }
        }

        protected override void OnFillChanged()
        {
            base.OnFillChanged();
            this.Rectangle.Fill = this.Fill;
        }
        protected override void OnOpacityChanged()
        {
            base.OnOpacityChanged();
            this.Rectangle.Opacity = this.Opacity;
        }

        private bool VisibleM;
        internal bool Visible
        {
            get => this.VisibleM;
            set
            {
                this.VisibleM = value;
                var aOpacity = value ? XAxisLineOpcacity : 0.0d;
                this.Opacity = aOpacity;

                if (this.VisibleChanged is object)
                {
                    this.VisibleChanged(this, default);
                }
            }
        }
        internal event EventHandler VisibleChanged;
        internal override CLayerEnum LayerEnum => CLayerEnum.XAxisMouseOverLine;
        private void Rectangle_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                var aIsMouseOver = (bool)e.NewValue;
                this.Visible = aIsMouseOver;
            }
            catch(Exception aExc)
            {
                aExc.CatchUnexpected();
            }
        }

        internal override IEnumerable<FrameworkElement> FrameworkElements
        {
            get 
            {
              //  yield return this.HorizontalLine;
                yield return this.Rectangle; 
            }
        }
        private readonly Rectangle Rectangle = new Rectangle();

        internal override void Refresh()
        {
            var aWidth = 3d;
            this.X = this.DataPoint.X + this.DataPoint.Radius - aWidth / 2d;
            this.Y = 0;
            this.Dx = aWidth;
            this.Dy = this.Canvas.ActualHeight;
            System.Windows.Controls.Canvas.SetLeft(this.Rectangle, this.X);
            System.Windows.Controls.Canvas.SetTop(this.Rectangle, this.Y);
            this.Rectangle.Width = this.Dx;
            this.Rectangle.Height = this.Dy;
         
        }
    }
    internal abstract class CXAxisMarker : CGuiObject
    {
        internal CXAxisMarker(Canvas aCanvas, CXAxisMouseOverLine aXAxisMouseOverLine)
        {
            this.Canvas = aCanvas;
            this.XAxisMouseOverLine = aXAxisMouseOverLine;
            this.StackPanel.Visibility = Visibility.Collapsed;
            this.StackPanel.Orientation = Orientation.Vertical;
            this.StackPanel.Children.Add(this.PriceTextBlock);
            this.StackPanel.Children.Add(this.DateTextBlock);
            this.Border.Child = this.StackPanel;
            this.Border.BorderThickness = new Thickness(0);
            this.Border.BorderBrush = Brushes.Black;
        }
        internal readonly CXAxisMouseOverLine XAxisMouseOverLine;
        internal readonly StackPanel StackPanel = new StackPanel();
        private readonly TextBlock PriceTextBlock = new TextBlock();
        private readonly TextBlock DateTextBlock = new TextBlock();
        private readonly Border Border = new Border();
        internal override IEnumerable<FrameworkElement> FrameworkElements { get { yield return this.Border; } }
        internal CExchangeRate ExchangeRate => this.XAxisMouseOverLine.DataPoint.ExchangeRate;
        //internal CPriceWithCurrency CaptionAmount
        //    => this.ExchangeRate.BuyAmount.Invert(this.ExchangeRate.SellCurrency);
        
        internal bool VisibleM = true;
        internal bool Visible
        {
            get => this.VisibleM;
            set
            {
                this.VisibleM = value;
                this.Refresh();
            }
        }
        internal override void Refresh()
        {
            base.Refresh();
            var aVisible = this.Visible;
            var aLine = this.XAxisMouseOverLine;
            this.StackPanel.Visibility = aVisible ? Visibility.Visible : Visibility.Collapsed;
            if (aVisible)
            {
                var aXCenter = aLine.X + aLine.Dx / 2;
                this.PriceTextBlock.Text = this.ExchangeRate.TitleWithoutDate;
                this.DateTextBlock.Text = this.ExchangeRate.DateTime.ToString("G");
                var aTextSizes = new Size[]
                {
                    this.PriceTextBlock.GetTextSize(),
                    this.DateTextBlock.GetTextSize(),
                };
                var aWidth = aTextSizes.Select(s => s.Width).Max();
                var aMaxWidth = this.Canvas.ActualWidth;
                var aX = Math.Max(0, Math.Min(aXCenter - aWidth / 2d, aMaxWidth - aWidth));
                var aHeight = aTextSizes.Select(s => s.Height).Sum();
                var aMargin = 10;
                var aTop1 = this.Y;
                var aTop2
                    = aTop1 < aHeight + aMargin
                    ? aTop1 + aMargin
                    : aTop1 - aHeight - aMargin;
                var aTop = aTop2;
                System.Windows.Controls.Canvas.SetLeft(this.Border, aX);
                System.Windows.Controls.Canvas.SetTop(this.Border, aTop);
                this.Border.Height =  aHeight;                
            }
        }

    }
    internal sealed class CXAxisStaticMarker : CXAxisMarker
    {
        internal CXAxisStaticMarker(Canvas aCanvas, CXAxisMouseOverLine aDiagramVerticalLine) : base(aCanvas, aDiagramVerticalLine)
        {
            this.Canvas = aCanvas;
        }
        internal override CLayerEnum LayerEnum => CLayerEnum.XAxisStaticMarker;
        internal override void Refresh()
        {
            base.Refresh();
        }

    }
    internal sealed class CXAxisMouseOverMarker : CXAxisMarker
    {
        internal CXAxisMouseOverMarker(Canvas aCanvas, CXAxisMouseOverLine aXAxisMouseOverLine):base(aCanvas, aXAxisMouseOverLine)
        {
            this.Canvas = aCanvas;
            this.Visible = false;
            this.XAxisMouseOverLine.VisibleChanged += this.OnDiagramLineVisibleChanged;
            aXAxisMouseOverLine.MousePosChanged += OnMouseOverLineMousePosChanged;
        }

        private void OnMouseOverLineMousePosChanged(object sender, EventArgs e)
        {
            this.Refresh();
        }

        internal override CLayerEnum LayerEnum => CLayerEnum.CrosshairSign;


        private void OnDiagramLineVisibleChanged(object aSender, EventArgs aArgs)
        {
            var aLine = this.XAxisMouseOverLine;
            var aVisible = aLine.Visible;
            this.Visible = aVisible;
        }

        internal override double YDefault => this.XAxisMouseOverLine.MousePos.Y;


        internal override void Refresh()
        {
            base.Refresh();
            this.StackPanel.Background = this.Colors.Crosshair;
        }

    }
    internal sealed class CColors
    {
        internal static readonly CColors Singleton = new CColors();
        internal readonly SolidColorBrush FillArea = Brushes.AliceBlue;
        internal readonly SolidColorBrush DataPointLine = Brushes.LightBlue;
        internal readonly SolidColorBrush Point = Brushes.DarkBlue;
        internal readonly SolidColorBrush Axis = Brushes.LightBlue;
        internal readonly SolidColorBrush Crosshair = Brushes.HotPink;
        internal readonly SolidColorBrush Selection = Brushes.HotPink;
        internal readonly SolidColorBrush TrendLine = Brushes.Gray;
    }

    internal sealed class CFillArea : CGuiObject
    {
        internal CFillArea (Canvas aCanvas, CDiagramDataPoint aPoint1, CDiagramDataPoint aPoint2)
        {
            this.Canvas = aCanvas;
            this.Point1 = aPoint1;
            this.Point2 = aPoint2;
            this.Init();
        }
        internal override CLayerEnum LayerEnum => CLayerEnum.FillArea;
        
        internal override IEnumerable<FrameworkElement> FrameworkElements { get { yield return this.Polygon; } } 
        internal readonly CDiagramDataPoint Point1;
        internal readonly CDiagramDataPoint Point2;
        private readonly Polygon Polygon = new Polygon();
        internal override void Refresh()
        {
            this.Point1.Refresh();
            this.Point2.Refresh();
            var aPoints = new Point[]
            {
                new Point(this.Point1.X, this.Canvas.ActualHeight),
                new Point(this.Point2.X, this.Canvas.ActualHeight),
                new Point(this.Point2.X, this.Point2.Y),
                new Point(this.Point1.X, this.Point1.Y),
            };
            foreach (var aPoint in aPoints)
            {
                this.Polygon.Points.Add(aPoint);
            }
            this.Polygon.Fill = this.Colors.FillArea;
        }
    }

    internal sealed class CAxis : CGuiObject
    {
        internal CAxis(Canvas aCanvas) : base()
        {
            this.Canvas = aCanvas;
            this.Init();
        }
        
        internal readonly Line Line = new Line();
        internal override IEnumerable<FrameworkElement> FrameworkElements => Array.Empty<FrameworkElement>();

        internal override CLayerEnum LayerEnum => CLayerEnum.Axis;
        internal override void Refresh()
        {
            this.Canvas.Background = this.Colors.Axis;
            //this.Line.X1 = 0;
            //this.Line.Y1 = 0;
            //this.Line.X2 = 0;
            //this.Line.Y2 = this.Canvas.ActualHeight;
            //this.Line.StrokeThickness = this.Canvas.ActualWidth;
            //this.Line.Stroke = this.Colors.Axis;
        }
    }

    internal sealed class CTrendLine : CLine
    {
        internal CTrendLine(Canvas aCanvas, CExchangeRateHistogramGui aExchangeRateHistogramGui, CDiagramDataPoint aPoint1, CDiagramDataPoint aPoint2) : base(aCanvas, aPoint1, aPoint2)
        {
            this.ExchangeRateHistogramGui = aExchangeRateHistogramGui;
            this.Init();
        }
        internal readonly CExchangeRateHistogramGui ExchangeRateHistogramGui;

        internal override void BeginRefresh()
        {
            base.BeginRefresh();
            
        }
        internal override void Refresh()
        {
            base.Refresh();

            this.Point1.Refresh();
            this.Point2.Refresh();

            this.Line.Stroke = this.Colors.TrendLine;
            this.Line.StrokeThickness = 3;
            this.Line.StrokeDashOffset = 10;
            this.Line.StrokeDashArray.Add(5);
            this.Line.ToolTip = this.ExchangeRateHistogramGui.Histogram.VmTrendLinePitchTitle;
        }
        internal override CLayerEnum LayerEnum => CLayerEnum.TrendLine;
    }

    internal sealed class CSelection : CGuiObject
    {
        internal CSelection(CExchangeRateHistogramGui aExchangeRateHistogramGui, Canvas aCanvas)
        {
            this.ExchangeRateHistogramGui = aExchangeRateHistogramGui;
            this.Canvas = aCanvas;
            this.HistogramSelectionGui.Selection = this;
            this.Rectangle.Opacity = 0.2d;
            this.Rectangle.Fill = this.Colors.Selection;
            this.Rectangle.Visibility = Visibility.Hidden;
            this.HistogramSelectionGui.Visibility = Visibility.Collapsed;
        }
        internal readonly CExchangeRateHistogramGui ExchangeRateHistogramGui;
        internal CCbTrader Trader => this.ExchangeRateHistogramGui.Histogram.Trader;
        internal readonly CHistogramSelectionGui HistogramSelectionGui = new CHistogramSelectionGui();
        private readonly Rectangle Rectangle = new Rectangle();
        internal override IEnumerable<CFrameworkElementDetail> FrameworkElementDetails
        {
            get
            {
                yield return new CFrameworkElementDetail(this.Rectangle, this.Canvas, CLayerEnum.Selection);
                yield return new CFrameworkElementDetail(this.HistogramSelectionGui, this.Canvas, CLayerEnum.SelectionGui);
            }
        }
        internal override CLayerEnum LayerEnum => CLayerEnum.Selection;
        #region Point1
        private CDataPoint Point1NullableM;
        public CDataPoint Point1Nullable
        {
            get => this.Point1NullableM;
            private set
            {
                this.Point1NullableM = value;
                this.Refresh();
            }
        }
        #endregion
        #region Point2
        private CDataPoint Point2NullableM;
        public CDataPoint Point2Nullable
        {
            get => this.Point2NullableM;
            private set
            {
                this.Point2NullableM = value;
                this.Refresh();
            }
        }
        #endregion
        private bool FinishedM;
        internal bool Finished { get => this.FinishedM;set { this.FinishedM = value; this.Refresh(); } }

        internal void ZoomIn()
        {
            var aTrader = this.Trader;
            var aStartDate = this.MinPoint.ExchangeRate.DateTime;
            var aEndDate = this.MaxPoint.ExchangeRate.DateTime;
            this.ExchangeRateHistogramGui.Select(new CDateTimeRange(aStartDate, aEndDate), true);
        }
        internal void Select(CDataPoint aMin, CDataPoint aMax)
        {
            this.Point1Nullable = aMin;
            this.Point2Nullable = aMax;
            this.Finished = true;
        }
        internal bool Flip => this.Point1Nullable.X > this.Point2Nullable.X;
        internal CDataPoint MinPoint=> this.Flip ? this.Point2Nullable : this.Point1Nullable;
        internal CDataPoint MaxPoint=> this.Flip ? this.Point1Nullable : this.Point2Nullable;

        internal CDateTimeRange DateTimeRange => new CDateTimeRange(this.MinPoint.ExchangeRate.DateTime, this.MaxPoint.ExchangeRate.DateTime);
        internal override void Refresh()
        {
            base.Refresh();
            var aPoint1 = this.Point1Nullable;
            var aPoint2 = this.Point2Nullable;
            var aVisible = aPoint1 is object && aPoint2 is object;
            var aRectangle = this.Rectangle; 
            if (aVisible)
            {
                var aMinPoint = this.MinPoint;
                var aMaxPoint = this.MaxPoint;
                System.Windows.Controls.Canvas.SetLeft(aRectangle, aMinPoint.X);
                System.Windows.Controls.Canvas.SetTop(aRectangle, 0);
                aRectangle.Height = this.Canvas.ActualHeight;
                aRectangle.Width = aMaxPoint.X - aMinPoint.X;
                var aSelectionGui = this.HistogramSelectionGui;
                this.HistogramSelectionGui.Visibility = this.Finished ? Visibility.Visible : Visibility.Collapsed;
                var aSelectionGuiX = Math.Min(aMinPoint.X, this.Canvas.ActualWidth - aSelectionGui.Width);
                System.Windows.Controls.Canvas.SetLeft(aRectangle, aMinPoint.X);
                System.Windows.Controls.Canvas.SetTop(aRectangle, 0);
                System.Windows.Controls.Canvas.SetLeft(aSelectionGui, aSelectionGuiX);
                System.Windows.Controls.Canvas.SetTop(aSelectionGui, 0);
            }
            var aVisibility = aVisible ? Visibility.Visible : Visibility.Collapsed;
            aRectangle.Visibility = aVisibility;
        }



        internal void Reset()
        {
            this.ExchangeRateHistogramGui.Selection = default;
           //this.Trader.InvestmentExchangeRateHistogram = default;
            this.Point1Nullable = default;
            this.Point2Nullable = default;
            this.Finished = false;
        }

        internal void Begin(CDiagramDataPoint aDataPoint)
        {
            this.Finished = false;
            this.Point2Nullable = default;
            this.Point1Nullable = aDataPoint;
        }

        internal void Add(CDiagramDataPoint aDataPoint)
        {
            if (this.Point1Nullable is object)
            {
                this.Point2Nullable = aDataPoint;
            }
            else
            {
                this.Point1Nullable = aDataPoint;
            }
        }

        internal void End()
        {
            if (!(this.Point1Nullable is object))
            {
                this.Point2Nullable = default;
            }
            else if (!(this.Point2Nullable is object))
            {
                this.Point1Nullable = default;
            }
            else if(!this.Finished)
            {
             //   this.Zoom();
                this.Finished = true;
            }
        }

        internal void Truncate()
        {
            this.Trader.Truncate(this.DateTimeRange);
        }
    }

    /// <summary>
    /// Interaktionslogik für CExchangeRateHistogramGuixaml.xaml
    /// </summary>
    public partial class CExchangeRateHistogramGui : UserControl
    {
        public CExchangeRateHistogramGui()
        {
            InitializeComponent();

            this.DataContextChanged += this.OnDataContextChanged;
            this.SizeChanged += this.OnSizeChanged;
        }
        private void OnDataContextChanged(object aSender, DependencyPropertyChangedEventArgs aArgs)
        {
            this.GuiObjects = default;
            this.RefreshCanvas();
        }
        private void OnSizeChanged(object aSender, SizeChangedEventArgs aArgs)
        {
            this.RefreshCanvas();
            //Dispatcher.BeginInvoke(new Action(this.RefreshCanvas));
        }
        internal CExchangeRateHistogram Histogram => this.DataContext as CExchangeRateHistogram;

        private void Clear(Canvas aCanvas)
        {
            while (aCanvas.Children.Count > 0)
            {
                aCanvas.Children.RemoveAt(0);
            }
        }
        private IEnumerable<CGetHistogramPointFunc> GetDataPointFuncs(Canvas aCanvas, params CExchangeRate[] aExchangeRates)
            => this.Histogram.GetPointFuncs
                (
                    new Func<decimal>(() => (decimal)aCanvas.ActualWidth),
                    new Func<decimal>(() => (decimal)aCanvas.ActualHeight),
                    aExchangeRates
                );
        private IEnumerable<CGuiObject> NewYAxisLeftCaptionGuiObjects(Canvas aCanvas, IEnumerable<CGuiObject> aRecentObjects)
        {
            var aGetWidth = new Func<decimal>(() => (decimal)aCanvas.ActualWidth);
            var aGetHeight = new Func<decimal>(() => (decimal)aCanvas.ActualHeight);
            var aHistogram = this.Histogram;
            if (aHistogram.ContainsOneOrMoreValues)
            {
                var aMinPoint = aHistogram.GetPointFuncs(aGetWidth, aGetHeight, aHistogram.MinY).Single();
                var aMinValue = new CValueText(aCanvas, aHistogram, aMinPoint);              
                var aMaxPoint = aHistogram.GetPointFuncs(aGetWidth, aGetHeight, aHistogram.MaxY).Single();
                var aMaxValue = new CValueText(aCanvas, aHistogram, aMaxPoint);

                aMaxValue.YOffsetFunc = new Func<double>(() => -aMinValue.TextBlock.Height);
                aMinValue.X = 0d;
                aMaxValue.X = 0d;
                var aGuiObjects = new CGuiObject[]
                {
                    aMinValue,
                    aMaxValue,
                };
                return aGuiObjects;
            }
            else
            {
                return new CGuiObject[] { };
            }
        }
        private IEnumerable<CGuiObject> NewTrendLines(Canvas aCanvas, IEnumerable<CGuiObject> aRecentObjects)
        {
            var aHistogram = this.Histogram;
            var aStart = aHistogram.TrendLineStart;
            var aEnd = aHistogram.TrendLineEnd;
            var aDataPoints = aRecentObjects.OfType<CDiagramDataPoint>();
            var aFirst = this.NewDiagramDataPoints(aCanvas, aStart).Single(); //  aDataPoints.Where(d => d.ExchangeRate.DateTime.CompareTo(aStart.DateTime) >= 0).FirstOrDefault();
            var aLast = this.NewDiagramDataPoints(aCanvas, aEnd).Single(); // aDataPoints.Where(d => d.ExchangeRate.DateTime.CompareTo(aEnd.DateTime) >= 0).FirstOrDefault();
            if(aFirst is object
            && aLast is object)
            { 
                var aTrendLine = new CTrendLine(aCanvas, this, aFirst, aLast);
                yield return aTrendLine;
            }
        }
        internal IEnumerable <CGuiObject> NewXAxisGuiObjects(Canvas aCanvas, IEnumerable<CGuiObject> aRecentObjects)
        {
            var aXAxis = new CAxis(aCanvas);
            return new CGuiObject[] { aXAxis };
        }
        private IEnumerable<CGuiObject> NewYAxisGuiObjects(Canvas aCanvas, IEnumerable<CGuiObject> aRecentObjects)
        {
            var aYAxis = new CAxis(aCanvas);
            return new CGuiObject[] { aYAxis };
        }
        private IEnumerable<Canvas> Canvass => this.CanvasAndNewDataObjectsFuncs.Select(t => t.Item1);


        private void ClearCanvass()
        {
            var aSelection = this.GuiObjects.OfType<CSelection>().FirstOrDefault();
            if(aSelection is object)
            {
                aSelection.Reset();
            }
            foreach (var aCanvas in this.Canvass)
            {
                this.Clear(aCanvas);
            }
        }
        private IEnumerable<CGuiObject> GuiObjectsM;
        private IEnumerable<CGuiObject> GuiObjects
        {
            get => CLazyLoad.Get(ref this.GuiObjectsM, () => this.NewGuiObjects()); 
            set
            {
                this.ClearCanvass();
                this.GuiObjectsM = default;
            }
        }

        private IEnumerable<CGuiObject> NewGuiObjects()
        {
            var aObjects = new List<CGuiObject>();
            var aHistogram = this.Histogram;
            if (aHistogram is object)
            {
                foreach (var aCanvasAndNewFunc in this.CanvasAndNewDataObjectsFuncs)
                {
                    var aCanvas = aCanvasAndNewFunc.Item1;
                    var aNewFunc = aCanvasAndNewFunc.Item2;
                    var aObjects2 = aNewFunc(aCanvas, aObjects); // this.AddCanvasObjects(aCanvasAndNewFunc.Item1, aObjects, aCanvasAndNewFunc.Item2);
                    aObjects.AddRange(aObjects2);
                }
            }
            this.AddCanvasObjects(aObjects);
            return aObjects.ToArray();
        }


        private IEnumerable<CDiagramDataPoint> NewDiagramDataPoints(Canvas aCanvas, params CExchangeRate[] aExchangeRates)
            => this.GetDataPointFuncs(aCanvas, aExchangeRates).Select(f => new CDiagramDataPoint(f)).ToArray();
        private IEnumerable<CGuiObject> NewDiagramDataPoints(Canvas aCanvas, IEnumerable<CGuiObject> aRecentObjects)
        {
            //var aGetWidth = new Func<decimal>(() => (decimal)aCanvas.ActualWidth);
            //var aGetHeight = new Func<decimal>(() => (decimal)aCanvas.ActualHeight);
            var aHistogram = this.Histogram;
            //var aGetPoints = this.GetDataPointFuncs(aCanvas, aHistogram.Items); // aHistogram.GetPointFuncs(aGetWidth, aGetHeight);
            //var aPoints1 = aGetPoints;
            var aPoints1 = this.NewDiagramDataPoints(aCanvas, aHistogram.Items).ToArray();
            var aPoints2 = new List<CDiagramDataPoint>(aPoints1.Length);
            var aPrevPoint = aPoints1.FirstOrDefault();
            if(aPrevPoint is object)
            {
                aPoints2.Add(aPrevPoint);
                foreach(var aPoint in aPoints1.Skip(1))
                {
                    if(aPoint.X -aPrevPoint.X > CDiagramDataPoint.RadiusConst)
                    {
                        aPoints2.Add(aPoint);
                        aPrevPoint = aPoint;
                    }                    
                }
                if(!object.ReferenceEquals(aPrevPoint, aPoints2.Last()))
                {
                    aPoints2.Add(aPoints2.Last());
                }
            }
            return aPoints2;
        }

        private IEnumerable<CGuiObject> NewDiagramPointConnectors(Canvas aCanvas, IEnumerable<CGuiObject> aRecentObjects)
        {
            var aConnectors = new List<CGuiObject>();
            var aPoints = aRecentObjects.OfType<CDiagramDataPoint>();
            var aPreviousPoint = aPoints.FirstOrDefault();
            if(aPreviousPoint is object)
            {
                foreach(var aPoint in aPoints.Skip(1))
                {
                    var aLine = new CDiagramLine(aCanvas, aPreviousPoint, aPoint);
                    aConnectors.Add(aLine);

                    var aUseFillAreas = false; // MS.Bug
                    if(aUseFillAreas)
                    { 
                        var aFillArea = new CFillArea(aCanvas, aPreviousPoint, aPoint);
                        aConnectors.Add(aFillArea);
                    }
                    aPreviousPoint = aPoint;
                }
            }
            return aConnectors;
        }

        private IEnumerable<CGuiObject> NewDiagramAreaVerticalLines(Canvas aCanvas, IEnumerable<CGuiObject> aRecentObjects)
        {
            var aSelection = aRecentObjects.OfType<CSelection>().Single();
            var aDigramDataPoints = aRecentObjects.OfType<CDiagramDataPoint>();
            var aXAxisLines = aDigramDataPoints.Select(dp => new CXAxisMouseOverLine(aCanvas, dp, aSelection)).ToArray();
            var aYAxisLines = aXAxisLines.Select(l => new CYAxisMouseOverLine(aCanvas, l)).ToArray();
            var aLines = aXAxisLines.AsEnumerable<CGuiObject>().Concat(aYAxisLines.AsEnumerable<CGuiObject>());
            return aLines;
        }
        private IEnumerable<CGuiObject> NewXAxisMouseOverMarkers(Canvas aCanvas, IEnumerable<CGuiObject> aRecentObjects)
        {
            var aLines = aRecentObjects.OfType<CXAxisMouseOverLine>();
            var aCaptions = aLines.Select(l => new CXAxisMouseOverMarker(aCanvas, l)).ToArray();
            return aCaptions;
        }
        private void AddCanvasObjects(IEnumerable<CGuiObject> aObjects)
        {
            var aFes =
                from o in aObjects
                from f in o.FrameworkElementDetails
                select new Tuple<CGuiObject, CFrameworkElementDetail>(o, f);
            var aOrdered = aFes.OrderBy(f => f.Item2.Item3).ToArray(); 
            foreach (var aFe in aOrdered)
            {
                var aCanvas = aFe.Item2.Item2;
                aCanvas.Children.Add(aFe.Item2.Item1);
            }
        }

        private IEnumerable<CGuiObject> NewXAxisStaticMarkers(Canvas aCanvas, IEnumerable<CGuiObject> aRecentObjects)
        {
            var aVerticalLine = aRecentObjects.OfType<CXAxisMouseOverLine>();
            var aFirst = aVerticalLine.FirstOrDefault();
            var aLast = aVerticalLine.LastOrDefault();
            if(aFirst is object)
                yield return new CXAxisStaticMarker(aCanvas, aFirst);
            if (aLast is object
            && !object.ReferenceEquals(aFirst, aLast))
                yield return new CXAxisStaticMarker(aCanvas, aLast);
        }

        private IEnumerable<CGuiObject> NewSelections(Canvas aCanvas, IEnumerable<CGuiObject> aRecentObjects)
        {
            var aSelection = new CSelection(this, aCanvas);
            var aGuiObjects = new CGuiObject[] { aSelection };
            return aGuiObjects;
        }
        private IEnumerable<CCanvasAndNewDataObjectsFunc> CanvasAndNewDataObjectsFuncs
        {
            get
            {
                yield return new CCanvasAndNewDataObjectsFunc(this.DataPointsCanvas, this.NewSelections);
                yield return new CCanvasAndNewDataObjectsFunc(this.DataPointsCanvas, this.NewDiagramDataPoints);
                yield return new CCanvasAndNewDataObjectsFunc(this.DataPointsCanvas, this.NewDiagramPointConnectors);
                yield return new CCanvasAndNewDataObjectsFunc(this.DataPointsCanvas, this.NewTrendLines);
                yield return new CCanvasAndNewDataObjectsFunc(this.DataPointsCanvas, this.NewDiagramAreaVerticalLines);
                yield return new CCanvasAndNewDataObjectsFunc(this.XAxisCaptionCanvas, this.NewXAxisStaticMarkers);
                yield return new CCanvasAndNewDataObjectsFunc(this.DataPointsCanvas, this.NewXAxisMouseOverMarkers);
                yield return new CCanvasAndNewDataObjectsFunc(this.YAxisLeftCaptionCanvas, this.NewYAxisLeftCaptionGuiObjects);




                //yield return new CCanvasAndNewDataObjectsFunc(this.YAxisCanvas, this.NewYAxisGuiObjects);
                //yield return new CCanvasAndNewDataObjectsFunc(this.XAxisCanvas, this.NewXAxisGuiObjects);
            }
        }



        private void RefreshCanvas()
        {
            var aGuiObjects = this.GuiObjects;
            foreach (var aGuiObject in aGuiObjects)
            {
                aGuiObject.BeginRefresh();
            }
            foreach (var aGuiObject in aGuiObjects)
            {
                aGuiObject.Refresh();
            }
            if (this.Selection is object)
            {
                this.Select(this.Selection, false);
            }
        }

        internal void Select(CDateTimeRange aRange, bool aZoomIn)
        {
            var aStartDate = aRange.Item1;
            var aEndDate = aRange.Item2;
            var aDataPoints = this.GuiObjects.OfType<CDiagramDataPoint>();
            var aStart = aDataPoints.Where(p => p.ExchangeRate.DateTime.CompareTo(aStartDate) >= 0).FirstOrDefault();
            var aEnd = aDataPoints.Where(p => p.ExchangeRate.DateTime.CompareTo(aEndDate) >= 0).FirstOrDefault();
            //var aSelection = this.GuiObjects.OfType<CSelection>().Single();
            //aSelection.Select(aStart, aEnd);
            //this.Selection = new CDateTimeRange(aStartDate, aEndDate);
            if(aZoomIn)
            {
                this.Histogram.Trader.Zoom(aStartDate, aEndDate);
            }
        }
        internal CDateTimeRange Selection;

        internal void Select(CSelection aSelection, bool aZoom)
        {
            this.Select(aSelection.DateTimeRange, aZoom);
        }
    }
}
