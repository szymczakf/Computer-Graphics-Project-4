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
using Microsoft.Win32;
using Xceed.Wpf.Toolkit;
using System.IO;

namespace CG_Project3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WriteableBitmap writeableBitmap;
        bool drawing;
        bool editing;
        Color buttColor;
        List<Line> lines;
        List<Circle> circles;
        List<Polygon> polygons;
        List<MyRectangle> rectangles;

        Line currLine;
        Circle currCircle;
        Polygon currPolygon;
        MyRectangle currRectangle;

        MyRectangle clipRect;

        object edited;

        int currX;
        int currY;
        int init;
        int index;

        int imgWidth;
        int imgHeight;

        int winWidth;
        int winHeight;

        bool created;
        bool anti;

        bool caps;
        int nextX;
        int nextY;

        bool clip;

        List<AET> ActiveEdgeTable;
        List<ET> EdgeTable;

        BitmapSource pattern;
        Uri pattUri;

        public MainWindow()
        {
            InitializeComponent();
            drawing = false;
            editing = false;
            created = false;
            caps = false;
            anti = false;
            clip = false;
            init = 0;
            lines = new List<Line>();
            circles = new List<Circle>();
            polygons = new List<Polygon>();
            rectangles = new List<MyRectangle>();
            buttColor = Color.FromArgb(255, 0, 0, 0);
            clrPicker.SelectedColor = buttColor;
            winWidth = (int)this.Width;
            winHeight = (int)this.Height;
            pattern = null;
        }

        private void newImage(object sender, RoutedEventArgs e)
        {
            created = true;
            int width = Int32.Parse(drawWidth.Text);
            int height = Int32.Parse(drawHeight.Text);

            imgWidth = width;
            imgHeight = height;

            writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            drawSpace.Source = writeableBitmap;

            try {
                writeableBitmap.Lock();

                unsafe
                {
                    IntPtr pBackBuffer = writeableBitmap.BackBuffer;

                    for(int i = 0; i < height; i++ )
                    {
                        for (int j = 0; j < width; j++)
                        {
                            int color_data = 255 << 24; // A
                            color_data |= 255 << 16; // R
                            color_data |= 255 << 8;  // G
                            color_data |= 255 << 0;  // B

                            *((int*)pBackBuffer) = color_data;

                            pBackBuffer += 4;
                        }
                    }
                    writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
                }
            }
            finally
            {
                writeableBitmap.Unlock();
            }

            writeableBitmap.Lock();

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    DrawPixel(x, y, Colors.White);

            writeableBitmap.Unlock();
        }

        private void mouseClick(object sender, MouseButtonEventArgs e)
        {
            int thickness = 1;
            if (thickBox.SelectedIndex == 0)
                thickness = 1;
            if (thickBox.SelectedIndex == 1)
                thickness = 3;
            if (thickBox.SelectedIndex == 2)
                thickness = 5;
            if (thickBox.SelectedIndex == 3)
                thickness = 7;

            int posX = (int)e.GetPosition(drawSpace).X;
            int posY = (int)e.GetPosition(drawSpace).Y;

            if(toolBox.SelectedIndex == 0 && shapeBox.SelectedIndex == 0)
            {
                if(!drawing)
                {
                    currLine = new Line();
                    currLine.initX = posX;
                    currLine.initY = posY;
                    currLine.color = buttColor;
                    currLine.thickness = thickness;
                    drawing = true;
                }
                else
                {
                    currLine.endX = posX;
                    currLine.endY = posY;
                    lines.Add(currLine);
                    if (!anti)
                        DrawLine(currLine);
                    else
                        WuLine(currLine);
                    drawing = false;
                }
            }

            else if(toolBox.SelectedIndex == 0 && shapeBox.SelectedIndex == 1)
            {
                if (!drawing)
                {
                    currCircle = new Circle();
                    currCircle.x = posX;
                    currCircle.y = posY;
                    currCircle.color = buttColor;
                    currCircle.thickness = thickness;
                    drawing = true;
                }
                else
                {
                    int endx = posX;
                    int endy = posY;
                    double radius = Math.Sqrt(Math.Pow(currCircle.x - endx, 2) + Math.Pow(currCircle.y - endy, 2));
                    currCircle.radius = (int)radius;
                    circles.Add(currCircle);
                    if (!anti)
                        DrawCircle(currCircle);
                    else
                        WuCircle(currCircle);
                    drawing = false;
                }
            }

            else if(toolBox.SelectedIndex == 0 && shapeBox.SelectedIndex == 2)
            {
                if (!drawing)
                {
                    currPolygon = new Polygon();
                    currPolygon.xs = new List<int>();
                    currPolygon.ys = new List<int>();
                    currPolygon.color = buttColor;
                    currPolygon.xs.Add(posX);
                    currPolygon.ys.Add(posY);
                    currX = posX;
                    currY = posY;
                    currPolygon.thickness = thickness;
                    currPolygon.sfill = false;
                    currPolygon.pfill = false;
                    drawing = true;
                }
                else
                {
                    if(PointDistance(posX, posY, currPolygon.xs[0], currPolygon.ys[0]) > 10)
                    {
                        currPolygon.xs.Add(posX);
                        currPolygon.ys.Add(posY);
                        if (!anti)
                            DrawLine(new Line() { initX = currX, initY = currY, endX = posX, endY = posY, color = currPolygon.color, thickness = thickness });
                        else
                            WuLine(new Line() { initX = currX, initY = currY, endX = posX, endY = posY, color = currPolygon.color, thickness = thickness });
                        currX = posX;
                        currY = posY;
                    }
                    else
                    {
                        if (!anti)
                            DrawLine(new Line() { initX = currX, initY = currY, endX = currPolygon.xs[0], endY = currPolygon.ys[0], color = currPolygon.color, thickness = thickness });
                        else
                            WuLine(new Line() { initX = currX, initY = currY, endX = currPolygon.xs[0], endY = currPolygon.ys[0], color = currPolygon.color, thickness = thickness });
                        drawing = false;
                        polygons.Add(currPolygon);
                    }
                }
            }

            else if(toolBox.SelectedIndex == 0 && shapeBox.SelectedIndex == 3)
            {
                if(!drawing)
                {
                    currRectangle = new MyRectangle();
                    currRectangle.initX = posX;
                    currRectangle.initY = posY;
                    currRectangle.color = buttColor;
                    currRectangle.thickness = thickness;
                    currRectangle.sfill = false;
                    currRectangle.pfill = false;
                    drawing = true;
                }
                else if (drawing)
                {
                    currRectangle.endX = posX;
                    currRectangle.endY = posY;
                    rectangles.Add(currRectangle);
                    if (!anti)
                        DrawRectangle(currRectangle);
                    else
                        DrawRectangleAnti(currRectangle);
                    drawing = false;
                }
            }

            else if(toolBox.SelectedIndex == 0 && shapeBox.SelectedIndex == 4)
            {
                if (!drawing && !caps)
                {
                    currX = posX;
                    currY = posY;
                    drawing = true;
                }
                else if (drawing && !caps)
                {
                    nextX = posX;
                    nextY = posY;
                    caps = true;
                }
                else if (drawing && caps)
                {
                    double radius = Math.Sqrt(Math.Pow(nextX - posX, 2) + Math.Pow(nextY - posY, 2));
                    double vectX = nextX - currX;
                    double vectY = nextY - currY;

                    double veclength = Math.Sqrt(Math.Pow(vectX, 2) + Math.Pow(vectY, 2));

                    vectX /= veclength;
                    vectY /= veclength;

                    vectX *= radius;
                    vectY *= radius;

                    int Ex1 = currX + (int)vectY;
                    int Ey1 = currY - (int)vectX;
                    int Ex2 = nextX + (int)vectY;
                    int Ey2 = nextY - (int)vectX;

                    radius = Math.Floor(radius);

                    DrawLine(new Line() { initX = currX + (int)vectY, initY = currY - (int)vectX, endX = nextX + (int)vectY, endY = nextY - (int)vectX, color = buttColor, thickness = 1 });
                    DrawLine(new Line() { initX = currX - (int)vectY, initY = currY + (int)vectX, endX = nextX - (int)vectY, endY = nextY + (int)vectX, color = buttColor, thickness = 1 });

                    DrawSemiCircle(new Circle() { x = currX, y = currY, radius = (int)radius, color = buttColor, thickness = 1 }, Ex1, Ey1, -1);
                    DrawSemiCircle(new Circle() { x = nextX, y = nextY, radius = (int)radius, color = buttColor, thickness = 1 }, Ex2, Ey2, 1);

                    drawing = false;
                    caps = false;
                }
            }

            else if (toolBox.SelectedIndex == 1 && shapeBox.SelectedIndex == 0)
            {
                if (!editing)
                {
                    int distance = 20;
                    foreach(var line in lines)
                    {
                        if(PointDistance(line.initX, line.initY, posX, posY) < distance)
                        {
                            distance = (int)PointDistance(line.initX, line.initY, posX, posY);
                            currLine = line;
                            edited = line;
                            currX = line.initX;
                            currY = line.initY;
                            editing = true;
                            warningLabel.Visibility = Visibility.Visible;
                            init = 1;
                            break;
                        }
                        else if(PointDistance(line.endX, line.endY, posX, posY) < distance)
                        {
                            distance = (int)PointDistance(line.endX, line.endY, posX, posY);
                            currLine = line;
                            edited = line;
                            currX = line.endX;
                            currY = line.endY;
                            editing = true;
                            warningLabel.Visibility = Visibility.Visible;
                            init = 2;
                            break;
                        }
                    }
                }
                else
                {
                    if(init == 1)
                    {
                        currLine.initX = posX;
                        currLine.initY = posY;
                        ClearImg();
                        RedrawFigures();
                        init = 0;
                    }
                    else if (init == 2)
                    {
                        currLine.endX = posX;
                        currLine.endY = posY;
                        ClearImg();
                        RedrawFigures();
                        init = 0;
                    }
                    editing = false;
                    warningLabel.Visibility = Visibility.Hidden;
                }
            }

            else if (toolBox.SelectedIndex == 1 && shapeBox.SelectedIndex == 1)
            {
                if (!editing)
                {
                    int distance = 20;
                    foreach (var circle in circles)
                    {
                        if (PointDistance(circle.x, circle.y, posX, posY) < distance)
                        {
                            distance = (int)PointDistance(circle.x, circle.y, posX, posY);
                            currCircle = circle;
                            edited = circle;
                            editing = true;
                            warningLabel.Visibility = Visibility.Visible;
                            break;
                        }
                    }
                }
                else
                {
                    double radius = Math.Sqrt(Math.Pow(currCircle.x - posX, 2) + Math.Pow(currCircle.y - posY, 2));
                    currCircle.radius = (int)radius;
                    ClearImg();
                    RedrawFigures();
                    editing = false;
                    warningLabel.Visibility = Visibility.Hidden;
                }
            }

            else if (toolBox.SelectedIndex == 1 && shapeBox.SelectedIndex == 2)
            {
                if(!editing)
                {
                    int distance = 20;
                    foreach(var polygon in polygons)
                    {
                        int length = polygon.xs.Count;
                        for(int i = 0; i < length; i++)
                        {
                            if (PointDistance(polygon.xs[i], polygon.ys[i], posX, posY) < distance)
                            {
                                distance = (int)PointDistance(polygon.xs[i], polygon.ys[i], posX, posY);
                                currPolygon = polygon;
                                edited = polygon;
                                currX = polygon.xs[i];
                                currY = polygon.ys[i];
                                index = i;
                                editing = true;
                                warningLabel.Visibility = Visibility.Visible;
                                init = 1;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    currPolygon.xs[index] = posX;
                    currPolygon.ys[index] = posY;
                    ClearImg();
                    RedrawFigures();
                    editing = false;
                    warningLabel.Visibility = Visibility.Hidden;
                }
            }

            else if (toolBox.SelectedIndex == 1 && shapeBox.SelectedIndex == 3)
            {
                if(!editing)
                {
                    int distance = 10;
                    foreach(var rect in rectangles)
                    {
                        if (PointDistance(rect.initX, rect.initY, posX, posY) < distance)
                        {
                            distance = (int)PointDistance(rect.initX, rect.initY, posX, posY);
                            currRectangle = rect;
                            edited = rect;
                            currX = rect.initX;
                            currY = rect.initY;
                            editing = true;
                            warningLabel.Visibility = Visibility.Visible;
                            init = 1;
                            break;
                        }
                        else if (PointDistance(rect.endX, rect.endY, posX, posY) < distance)
                        {
                            distance = (int)PointDistance(rect.endX, rect.endY, posX, posY);
                            currRectangle = rect;
                            edited = rect;
                            currX = rect.endX;
                            currY = rect.endY;
                            editing = true;
                            warningLabel.Visibility = Visibility.Visible;
                            init = 2;
                            break;
                        }
                        else if (PointDistance(rect.initX, rect.endY, posX, posY) < distance)
                        {
                            distance = (int)PointDistance(rect.initX, rect.endY, posX, posY);
                            currRectangle = rect;
                            edited = rect;
                            currX = rect.initX;
                            currY = rect.endY;
                            editing = true;
                            warningLabel.Visibility = Visibility.Visible;
                            init = 3;
                            break;
                        }
                        else if (PointDistance(rect.endX, rect.initY, posX, posY) < distance)
                        {
                            distance = (int)PointDistance(rect.endX, rect.initY, posX, posY);
                            currRectangle = rect;
                            edited = rect;
                            currX = rect.endX;
                            currY = rect.initY;
                            editing = true;
                            warningLabel.Visibility = Visibility.Visible;
                            init = 4;
                            break;
                        }
                    }
                }
                else
                {
                    if (init == 1)
                    {
                        currRectangle.initX = posX;
                        currRectangle.initY = posY;
                        ClearImg();
                        RedrawFigures();
                        init = 0;
                    }
                    else if (init == 2)
                    {
                        currRectangle.endX = posX;
                        currRectangle.endY = posY;
                        ClearImg();
                        RedrawFigures();
                        init = 0;
                    }
                    else if (init == 3)
                    {
                        currRectangle.initX = posX;
                        currRectangle.endY = posY;
                        ClearImg();
                        RedrawFigures();
                        init = 0;
                    }
                    else if (init == 4)
                    {
                        currRectangle.endX = posX;
                        currRectangle.initY = posY;
                        ClearImg();
                        RedrawFigures();
                        init = 0;
                    }
                    editing = false;
                    warningLabel.Visibility = Visibility.Hidden;
                }
            }

            else if (toolBox.SelectedIndex == 1 && shapeBox.SelectedIndex == 4)
            {
                System.Windows.MessageBox.Show("Option not supported", "Error");
            }

            else if (toolBox.SelectedIndex == 2 && shapeBox.SelectedIndex == 0)
            {
                int distance = 20;
                foreach (var line in lines)
                {
                    if (PointDistance(line.initX, line.initY, posX, posY) < distance)
                    {
                        distance = (int)PointDistance(line.initX, line.initY, posX, posY);
                        currLine = line;
                        break;
                    }
                    else if (PointDistance(line.endX, line.endY, posX, posY) < distance)
                    {
                        distance = (int)PointDistance(line.endX, line.endY, posX, posY);
                        currLine = line;
                        break;
                    }
                }

                lines.Remove(currLine);
                ClearImg();
                if (!anti)
                    RedrawFigures();
                else
                    RedrawAnti();
            }

            else if (toolBox.SelectedIndex == 2 && shapeBox.SelectedIndex == 1)
            {
                int distance = 20;
                foreach (var circle in circles)
                {
                    if (PointDistance(circle.x, circle.y, posX, posY) < distance)
                    {
                        distance = (int)PointDistance(circle.x, circle.y, posX, posY);
                        currCircle = circle;
                        break;
                    }
                }
                circles.Remove(currCircle);
                ClearImg();
                if (!anti)
                    RedrawFigures();
                else
                    RedrawAnti();
            }

            else if (toolBox.SelectedIndex == 2 && shapeBox.SelectedIndex == 2)
            {
                int distance = 20;
                foreach (var polygon in polygons)
                {
                    int length = polygon.xs.Count;
                    for (int i = 0; i < length; i++)
                    {
                        if (PointDistance(polygon.xs[i], polygon.ys[i], posX, posY) < distance)
                        {
                            distance = (int)PointDistance(polygon.xs[i], polygon.ys[i], posX, posY);
                            currPolygon = polygon;
                            break;
                        }
                    }
                }
                polygons.Remove(currPolygon);
                ClearImg();
                if (!anti)
                    RedrawFigures();
                else
                    RedrawAnti();
            }

            else if (toolBox.SelectedIndex == 2 && shapeBox.SelectedIndex == 3)
            {
                int distance = 10;
                foreach (var rect in rectangles)
                {
                    if (PointDistance(rect.initX, rect.initY, posX, posY) < distance)
                    {
                        distance = (int)PointDistance(rect.initX, rect.initY, posX, posY);
                        currRectangle = rect;
                        break;
                    }
                    else if (PointDistance(rect.endX, rect.endY, posX, posY) < distance)
                    {
                        distance = (int)PointDistance(rect.endX, rect.endY, posX, posY);
                        currRectangle = rect;
                        break;
                    }
                    else if (PointDistance(rect.initX, rect.endY, posX, posY) < distance)
                    {
                        distance = (int)PointDistance(rect.initX, rect.endY, posX, posY);
                        currRectangle = rect;
                        break;
                    }
                    else if (PointDistance(rect.endX, rect.initY, posX, posY) < distance)
                    {
                        distance = (int)PointDistance(rect.endX, rect.initY, posX, posY);
                        currRectangle = rect;
                        break;
                    }
                }

                rectangles.Remove(currRectangle);
                ClearImg();
                if (!anti)
                    RedrawFigures();
                else
                    RedrawAnti();
            }

            else if (toolBox.SelectedIndex == 2 && shapeBox.SelectedIndex == 4)
            {
                System.Windows.MessageBox.Show("Option not supported", "Error");
            }

            else if (toolBox.SelectedIndex == 3 && shapeBox.SelectedIndex == 0)
            {
                if (!clip)
                {
                    int distance = 20;
                    foreach (var line in lines)
                    {
                        if (PointDistance(line.initX, line.initY, posX, posY) < distance)
                        {
                            distance = (int)PointDistance(line.initX, line.initY, posX, posY);
                            currLine = line;
                            init = 1;
                            break;
                        }
                        else if (PointDistance(line.endX, line.endY, posX, posY) < distance)
                        {
                            distance = (int)PointDistance(line.endX, line.endY, posX, posY);
                            currLine = line;
                            init = 1;
                            break;
                        }
                    }
                    warningLabel.Visibility = Visibility.Visible;
                    clip = true;
                }
                else if(clip && init == 1)
                {
                    clipRect = new MyRectangle();
                    clipRect.initX = posX;
                    clipRect.initY = posY;
                    clipRect.color = Colors.Black;
                    clipRect.thickness = 1;
                    init = 2;
                }
                else if(clip && init == 2)
                {
                    clipRect.endX = posX;
                    clipRect.endY = posY;
                    DrawRectangle(clipRect);
                    Point p1 = new Point(currLine.initX, currLine.initY);
                    Point p2 = new Point(currLine.endX, currLine.endY);
                    LiangBarsky(p1, p2, clipRect);
                    warningLabel.Visibility = Visibility.Hidden;
                    clip = false;
                    init = 0;
                }
            }

            else if (toolBox.SelectedIndex == 3 && shapeBox.SelectedIndex == 1)
            {
                System.Windows.MessageBox.Show("Option not supported", "Error");
            }

            else if (toolBox.SelectedIndex == 3 && shapeBox.SelectedIndex == 2)
            {
                if (!clip)
                {
                    int distance = 20;
                    foreach (var polygon in polygons)
                    {
                        int length = polygon.xs.Count;
                        for (int i = 0; i < length; i++)
                        {
                            if (PointDistance(polygon.xs[i], polygon.ys[i], posX, posY) < distance)
                            {
                                distance = (int)PointDistance(polygon.xs[i], polygon.ys[i], posX, posY);
                                currPolygon = polygon;
                                init = 1;
                                break;
                            }
                        }
                    }
                    warningLabel.Visibility = Visibility.Visible;
                    clip = true;
                }
                else if (clip && init == 1)
                {
                    clipRect = new MyRectangle();
                    clipRect.initX = posX;
                    clipRect.initY = posY;
                    clipRect.color = Colors.Black;
                    clipRect.thickness = 1;
                    init = 2;
                }
                else if (clip && init == 2)
                {
                    clipRect.endX = posX;
                    clipRect.endY = posY;
                    DrawRectangle(clipRect);

                    int length = currPolygon.xs.Count;
                    for(int i = 0; i < length; i++)
                    {
                        if (i != length - 1)
                        {
                            Point p1 = new Point();
                            Point p2 = new Point();
                            p1.X = currPolygon.xs[i];
                            p1.Y = currPolygon.ys[i];
                            p2.X = currPolygon.xs[i + 1];
                            p2.Y = currPolygon.ys[i + 1];
                            LiangBarsky(p1, p2, clipRect);
                        }
                        else
                        {
                            Point p1 = new Point();
                            Point p2 = new Point();
                            p1.X = currPolygon.xs[i];
                            p1.Y = currPolygon.ys[i];
                            p2.X = currPolygon.xs[0];
                            p2.Y = currPolygon.ys[0];
                            LiangBarsky(p1, p2, clipRect);
                        }
                    }
                    warningLabel.Visibility = Visibility.Hidden;
                    clip = false;
                    init = 0;
                }
            }

            else if (toolBox.SelectedIndex == 3 && shapeBox.SelectedIndex == 3)
            {
                if (!clip)
                {
                    int distance = 10;
                    foreach (var rect in rectangles)
                    {
                        if (PointDistance(rect.initX, rect.initY, posX, posY) < distance)
                        {
                            distance = (int)PointDistance(rect.initX, rect.initY, posX, posY);
                            currRectangle = rect;
                            init = 1;
                            break;
                        }
                        else if (PointDistance(rect.endX, rect.endY, posX, posY) < distance)
                        {
                            distance = (int)PointDistance(rect.endX, rect.endY, posX, posY);
                            currRectangle = rect;
                            init = 1;
                            break;
                        }
                        else if (PointDistance(rect.initX, rect.endY, posX, posY) < distance)
                        {
                            distance = (int)PointDistance(rect.initX, rect.endY, posX, posY);
                            currRectangle = rect;
                            init = 1;
                            break;
                        }
                        else if (PointDistance(rect.endX, rect.initY, posX, posY) < distance)
                        {
                            distance = (int)PointDistance(rect.endX, rect.initY, posX, posY);
                            currRectangle = rect;
                            init = 1;
                            break;
                        }
                    }
                    warningLabel.Visibility = Visibility.Visible;
                    clip = true;
                }
                else if (clip && init == 1)
                {
                    clipRect = new MyRectangle();
                    clipRect.initX = posX;
                    clipRect.initY = posY;
                    clipRect.color = Colors.Black;
                    clipRect.thickness = 1;
                    init = 2;
                }
                else if (clip && init == 2)
                {
                    clipRect.endX = posX;
                    clipRect.endY = posY;
                    DrawRectangle(clipRect);

                    Point p1 = new Point(currRectangle.initX, currRectangle.initY);
                    Point p2 = new Point(currRectangle.initX, currRectangle.endY);
                    LiangBarsky(p1, p2, clipRect);

                    p1.X = currRectangle.initX; p1.Y = currRectangle.initY;
                    p2.X = currRectangle.endX; p2.Y = currRectangle.initY;
                    LiangBarsky(p1, p2, clipRect);

                    p1.X = currRectangle.endX; p1.Y = currRectangle.endY;
                    p2.X = currRectangle.initX; p2.Y = currRectangle.endY;
                    LiangBarsky(p1, p2, clipRect);

                    p1.X = currRectangle.endX; p1.Y = currRectangle.endY;
                    p2.X = currRectangle.endX; p2.Y = currRectangle.initY;
                    LiangBarsky(p1, p2, clipRect);

                    warningLabel.Visibility = Visibility.Hidden;
                    clip = false;
                    init = 0;
                }
            }

            else if (toolBox.SelectedIndex == 3 && shapeBox.SelectedIndex == 4)
            {
                System.Windows.MessageBox.Show("Option not supported", "Error");
            }

            else if (toolBox.SelectedIndex == 4 && shapeBox.SelectedIndex == 0)
            {
                System.Windows.MessageBox.Show("Option not supported", "Error");
            }

            else if (toolBox.SelectedIndex == 4 && shapeBox.SelectedIndex == 1)
            {
                System.Windows.MessageBox.Show("Option not supported", "Error");
            }

            else if (toolBox.SelectedIndex == 4 && shapeBox.SelectedIndex == 2)
            {
                int distance = 20;
                foreach (var polygon in polygons)
                {
                    int length = polygon.xs.Count;
                    for (int i = 0; i < length; i++)
                    {
                        if (PointDistance(polygon.xs[i], polygon.ys[i], posX, posY) < distance)
                        {
                            distance = (int)PointDistance(polygon.xs[i], polygon.ys[i], posX, posY);
                            currPolygon = polygon;
                            break;
                        }
                    }
                }
                FillPolygon(currPolygon);
                currPolygon.sfill = true;
                currPolygon.fillColor = buttColor;
            }

            else if (toolBox.SelectedIndex == 4 && shapeBox.SelectedIndex == 3)
            {
                int distance = 10;
                foreach (var rect in rectangles)
                {
                    if (PointDistance(rect.initX, rect.initY, posX, posY) < distance)
                    {
                        distance = (int)PointDistance(rect.initX, rect.initY, posX, posY);
                        currRectangle = rect;
                        break;
                    }
                    else if (PointDistance(rect.endX, rect.endY, posX, posY) < distance)
                    {
                        distance = (int)PointDistance(rect.endX, rect.endY, posX, posY);
                        currRectangle = rect;
                        break;
                    }
                    else if (PointDistance(rect.initX, rect.endY, posX, posY) < distance)
                    {
                        distance = (int)PointDistance(rect.initX, rect.endY, posX, posY);
                        currRectangle = rect;
                        break;
                    }
                    else if (PointDistance(rect.endX, rect.initY, posX, posY) < distance)
                    {
                        distance = (int)PointDistance(rect.endX, rect.initY, posX, posY);
                        currRectangle = rect;
                        break;
                    }
                }
                FillRectangle(currRectangle);
                currRectangle.sfill = true;
                currRectangle.fillColor = buttColor;
            }

            else if (toolBox.SelectedIndex == 4 && shapeBox.SelectedIndex == 4)
            {
                System.Windows.MessageBox.Show("Option not supported", "Error");
            }

            else if (toolBox.SelectedIndex == 5 && shapeBox.SelectedIndex == 0)
            {
                System.Windows.MessageBox.Show("Option not supported", "Error");
            }

            else if (toolBox.SelectedIndex == 5 && shapeBox.SelectedIndex == 1)
            {
                System.Windows.MessageBox.Show("Option not supported", "Error");
            }

            else if (toolBox.SelectedIndex == 5 && shapeBox.SelectedIndex == 2)
            {
                if (pattern == null)
                    return;
                int distance = 20;
                foreach (var polygon in polygons)
                {
                    int length = polygon.xs.Count;
                    for (int i = 0; i < length; i++)
                    {
                        if (PointDistance(polygon.xs[i], polygon.ys[i], posX, posY) < distance)
                        {
                            distance = (int)PointDistance(polygon.xs[i], polygon.ys[i], posX, posY);
                            currPolygon = polygon;
                            break;
                        }
                    }
                }
                FillPolygonPatt(currPolygon);
                currPolygon.pfill = true;
                currPolygon.pUri = pattUri;
            }

            else if (toolBox.SelectedIndex == 5 && shapeBox.SelectedIndex == 3)
            {
                if (pattern == null)
                    return;
                int distance = 10;
                foreach (var rect in rectangles)
                {
                    if (PointDistance(rect.initX, rect.initY, posX, posY) < distance)
                    {
                        distance = (int)PointDistance(rect.initX, rect.initY, posX, posY);
                        currRectangle = rect;
                        break;
                    }
                    else if (PointDistance(rect.endX, rect.endY, posX, posY) < distance)
                    {
                        distance = (int)PointDistance(rect.endX, rect.endY, posX, posY);
                        currRectangle = rect;
                        break;
                    }
                    else if (PointDistance(rect.initX, rect.endY, posX, posY) < distance)
                    {
                        distance = (int)PointDistance(rect.initX, rect.endY, posX, posY);
                        currRectangle = rect;
                        break;
                    }
                    else if (PointDistance(rect.endX, rect.initY, posX, posY) < distance)
                    {
                        distance = (int)PointDistance(rect.endX, rect.initY, posX, posY);
                        currRectangle = rect;
                        break;
                    }
                }
                FillRectanglePatt(currRectangle);
                currRectangle.pfill = true;
                currRectangle.pUri = pattUri;
            }

            else if (toolBox.SelectedIndex == 5 && shapeBox.SelectedIndex == 4)
            {
                System.Windows.MessageBox.Show("Option not supported", "Error");
            }

            else if (toolBox.SelectedIndex == 6)
            {
                Color c = GetColor(posX, posY);
                FloodFill(posX, posY, c, buttColor);
            }
        }

        int Sign(int Dx, int Dy, int Ex, int Ey, int Fx, int Fy)
        {
            return Math.Sign((Ex - Dx) * (Fy - Dy) - (Ey - Dy) * (Fx - Dx));
        }

        private void mouseRClick(object sender, MouseButtonEventArgs e)
        {
            if(editing && edited.GetType() == typeof(Line))
            {
                int posX = (int)e.GetPosition(drawSpace).X;
                int posY = (int)e.GetPosition(drawSpace).Y;

                int diffX = posX - currX;
                int diffY = posY - currY;

                currLine.initX += diffX;
                currLine.initY += diffY;
                currLine.endX += diffX;
                currLine.endY += diffY;

                ClearImg();
                if (!anti)
                    RedrawFigures();
                else
                    RedrawAnti();

                editing = false;
                warningLabel.Visibility = Visibility.Hidden;
            }

            if(editing && edited.GetType() == typeof(Circle))
            {
                int posX = (int)e.GetPosition(drawSpace).X;
                int posY = (int)e.GetPosition(drawSpace).Y;

                currCircle.x = posX;
                currCircle.y = posY;
                ClearImg();
                if (!anti)
                    RedrawFigures();
                else
                    RedrawAnti();
                editing = false;
                warningLabel.Visibility = Visibility.Hidden;
            }

            if (editing && edited.GetType() == typeof(Polygon))
            {
                int posX = (int)e.GetPosition(drawSpace).X;
                int posY = (int)e.GetPosition(drawSpace).Y;

                int diffX = posX - currX;
                int diffY = posY - currY;

                for (int i = 0; i < currPolygon.xs.Count; i++)
                    currPolygon.xs[i] += diffX;
                for (int i = 0; i < currPolygon.ys.Count; i++)
                    currPolygon.ys[i] += diffY;

                ClearImg();
                if (!anti)
                    RedrawFigures();
                else
                    RedrawAnti();
                editing = false;
                warningLabel.Visibility = Visibility.Hidden;
            }

            if (editing && edited.GetType() == typeof(MyRectangle))
            {
                int posX = (int)e.GetPosition(drawSpace).X;
                int posY = (int)e.GetPosition(drawSpace).Y;

                int diffX = posX - currX;
                int diffY = posY - currY;

                currRectangle.initX += diffX;
                currRectangle.initY += diffY;
                currRectangle.endX += diffX;
                currRectangle.endY += diffY;

                ClearImg();
                if (!anti)
                    RedrawFigures();
                else
                    RedrawAnti();

                editing = false;
                warningLabel.Visibility = Visibility.Hidden;
            }
        }

        private double PointDistance(int x1, int y1, int x2, int y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        private void selectedColor(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            buttColor = (Color)clrPicker.SelectedColor;
            if(editing)
            {
                if(edited.GetType() == typeof(Line))
                {
                    (edited as Line).color = buttColor;
                    ClearImg();
                    RedrawFigures();
                }
                if (edited.GetType() == typeof(Circle))
                {
                    (edited as Circle).color = buttColor;
                    ClearImg();
                    RedrawFigures();
                }
                if (edited.GetType() == typeof(Polygon))
                {
                    (edited as Polygon).color = buttColor;
                    ClearImg();
                    RedrawFigures();
                }
                if (edited.GetType() == typeof(MyRectangle))
                {
                    (edited as MyRectangle).color = buttColor;
                    ClearImg();
                    RedrawFigures();
                }
            }
        }

        private void redrawClick(object sender, RoutedEventArgs e)
        {
            if (!anti)
                RedrawFigures();
            else
                RedrawAnti();
        }

        private void clearClick(object sender, RoutedEventArgs e)
        {
            lines = new List<Line>();
            circles = new List<Circle>();
            polygons = new List<Polygon>();
            ClearImg();
        }

        public void DrawPixel(int x, int y, Color color)
        {
            int column = x;
            int row = y;

            if(x < drawSpace.Source.Width && x > 0 && y < drawSpace.Source.Height && y > 0)
            {

                    unsafe
                    {
                        IntPtr pBackBuffer = writeableBitmap.BackBuffer;

                        pBackBuffer += row * writeableBitmap.BackBufferStride;
                        pBackBuffer += column * 4;

                        int color_data = color.A << 24;
                        color_data |= color.R << 16; // R
                        color_data |= color.G << 8;   // G
                        color_data |= color.B << 0;   // B

                        *((int*)pBackBuffer) = color_data;
                    }

                    writeableBitmap.AddDirtyRect(new Int32Rect(column, row, 1, 1));
            }
        }

        void DrawBrush(int x, int y, Color color, int thickness)
        {
            writeableBitmap.Lock();

            if (thickness == 1)
                DrawPixel(x, y, color);

            if (thickness == 3)
            {
                DrawPixel(x, y, color);
                DrawPixel(x+1, y, color);
                DrawPixel(x, y+1, color);
                DrawPixel(x-1, y, color);
                DrawPixel(x, y-1, color);
            }

            if(thickness == 5)
            {
                for(int i = -1; i < 2; i++)
                    DrawPixel(x + i, y-2, color);
                for (int j = -1; j < 2; j++)                    
                {
                    for (int i = -2; i < 3; i++)
                        DrawPixel(x + i, y + j, color);
                }
                for (int i = -1; i < 2; i++)
                    DrawPixel(x + i, y + 2, color);
            }

            if(thickness == 7)
            {
                for (int i = -1; i < 2; i++)
                    DrawPixel(x + i, y - 3, color);
                for (int i = -2; i < 3; i++)
                    DrawPixel(x + i, y - 2, color);
                for (int j = -1; j < 2; j++)
                {
                    for (int i = -3; i < 4; i++)
                        DrawPixel(x + i, y + j, color);
                }
                for (int i = -2; i < 3; i++)
                    DrawPixel(x + i, y + 2, color);
                for (int i = -1; i < 2; i++)
                    DrawPixel(x + i, y + 3, color);
            }

            writeableBitmap.Unlock();
        }

        void DrawLine(Line line)
        {
            writeableBitmap.Lock();

            int thick = line.thickness;
            int dx = line.endX - line.initX;
            int dy = line.endY - line.initY;

            if(dx >= 0 && dy >= 0)
            {
                if(Math.Abs(dx) > Math.Abs(dy))
                {
                    int d = 2 * dy - dx;
                    int dH = 2 * dy;
                    int dV = 2 * (dy - dx);
                    int x = line.initX, y = line.initY;
                    DrawBrush(x, y, line.color, thick);
                    while (x < line.endX)
                    {
                        if (d < 0)
                        {
                            d += dH;
                            x++;
                        }
                        else
                        {
                            d += dV;
                            x++;
                            y++;
                        }
                        DrawBrush(x, y, line.color, thick);
                    }
                }
                else
                {
                    int d = 2 * dx - dy;
                    int dH = 2 * dx;
                    int dV = 2 * (dx - dy);
                    int x = line.initX, y = line.initY;
                    DrawBrush(x, y, line.color, thick);
                    while (y < line.endY)
                    {
                        if (d < 0)
                        {
                            d += dH;
                            y++;
                        }
                        else
                        {
                            d += dV;
                            y++;
                            x++;
                        }
                        DrawBrush(x, y, line.color, thick);
                    }
                }
            }
            if (dx < 0 && dy >= 0)
            {
                if (Math.Abs(dx) > Math.Abs(dy))
                {
                    int d = 2 * dy + dx;
                    int dH = 2 * dy;
                    int dV = 2 * (dy + dx);
                    int x = line.initX, y = line.initY;
                    DrawBrush(x, y, line.color, thick);
                    while (x > line.endX)
                    {
                        if (d < 0)
                        {
                            d += dH;
                            x--;
                        }
                        else
                        {
                            d += dV;
                            x--;
                            y++;
                        }
                        DrawBrush(x, y, line.color, thick);
                    }
                }
                else
                {
                    int d = 2 * -dx - dy;
                    int dH = 2 * -dx;
                    int dV = 2 * (-dx - dy);
                    int x = line.initX, y = line.initY;
                    DrawBrush(x, y, line.color, thick);
                    while (y < line.endY)
                    {
                        if (d < 0)
                        {
                            d += dH;
                            y++;
                        }
                        else
                        {
                            d += dV;
                            y++;
                            x--;
                        }
                        DrawBrush(x, y, line.color, thick);
                    }
                }
            }
            if (dx < 0 && dy < 0)
            {
                if (Math.Abs(dx) > Math.Abs(dy))
                {
                    int d = 2 * -dy + dx;
                    int dH = 2 * -dy;
                    int dV = 2 * (-dy + dx);
                    int x = line.initX, y = line.initY;
                    DrawBrush(x, y, line.color, thick);
                    while (x > line.endX)
                    {
                        if (d < 0)
                        {
                            d += dH;
                            x--;
                        }
                        else
                        {
                            d += dV;
                            x--;
                            y--;
                        }
                        DrawBrush(x, y, line.color, thick);
                    }
                }
                else
                {
                    int d = 2 * -dx + dy;
                    int dH = 2 * -dx;
                    int dV = 2 * (-dx + dy);
                    int x = line.initX, y = line.initY;
                    DrawBrush(x, y, line.color, thick);
                    while (y > line.endY)
                    {
                        if (d < 0)
                        {
                            d += dH;
                            y--;
                        }
                        else
                        {
                            d += dV;
                            y--;
                            x--;
                        }
                        DrawBrush(x, y, line.color, thick);
                    }
                }
            }
            if (dx >= 0 && dy < 0)
            {
                if (Math.Abs(dx) > Math.Abs(dy))
                {
                    int d = 2 * -dy - dx;
                    int dH = 2 * -dy;
                    int dV = 2 * (-dy - dx);
                    int x = line.initX, y = line.initY;
                    DrawBrush(x, y, line.color, thick);
                    while (x < line.endX)
                    {
                        if (d < 0)
                        {
                            d += dH;
                            x++;
                        }
                        else
                        {
                            d += dV;
                            x++;
                            y--;
                        }
                        DrawBrush(x, y, line.color, thick);
                    }
                }
                else
                {
                    int d = 2 * dx + dy;
                    int dH = 2 * dx;
                    int dV = 2 * (dx + dy);
                    int x = line.initX, y = line.initY;
                    DrawBrush(x, y, line.color, thick);
                    while (y > line.endY)
                    {
                        if (d < 0)
                        {
                            d += dH;
                            y--;
                        }
                        else
                        {
                            d += dV;
                            y--;
                            x++;
                        }
                        DrawBrush(x, y, line.color, thick);
                    }
                }
            }

            writeableBitmap.Unlock();
        }

        void DrawCircle(Circle circle)
        {
            writeableBitmap.Lock();

            int thick = circle.thickness; 
            int dE = 3;
            int dSE = 5 - 2 * circle.radius;
            int d = 1 - circle.radius;
            int x = 0;
            int y = circle.radius;
            DrawBrush(x + circle.x, y + circle.y, circle.color, thick);
            DrawBrush(-x + circle.x, y + circle.y, circle.color, thick);
            DrawBrush(-x + circle.x, -y + circle.y, circle.color, thick);
            DrawBrush(x + circle.x, -y + circle.y, circle.color, thick);
            DrawBrush(y + circle.x, x + circle.y, circle.color, thick);
            DrawBrush(-y + circle.x, x + circle.y, circle.color, thick);
            DrawBrush(-y + circle.x, -x + circle.y, circle.color, thick);
            DrawBrush(y + circle.x, -x + circle.y, circle.color, thick);
            while (y > x)
            {
                if(d < 0)
                {
                    d += dE;
                    dE += 2;
                    dSE += 2;
                }
                else
                {
                    d += dSE;
                    dE += 2;
                    dSE += 4;
                    --y;
                }
                ++x;
                DrawBrush(x + circle.x, y + circle.y, circle.color, thick);
                DrawBrush(-x + circle.x, y + circle.y, circle.color, thick);
                DrawBrush(-x + circle.x, -y + circle.y, circle.color, thick);
                DrawBrush(x + circle.x, -y + circle.y, circle.color, thick);
                DrawBrush(y + circle.x, x + circle.y, circle.color, thick);
                DrawBrush(-y + circle.x, x + circle.y, circle.color, thick);
                DrawBrush(-y + circle.x, -x + circle.y, circle.color, thick);
                DrawBrush(y + circle.x, -x + circle.y, circle.color, thick);
            }

            writeableBitmap.Unlock();
        }

        void DrawSemiCircle(Circle circle, int Ex, int Ey, int sign)
        {
            writeableBitmap.Lock();

            int thick = circle.thickness;
            int dE = 3;
            int dSE = 5 - 2 * circle.radius;
            int d = 1 - circle.radius;
            int x = 0;
            int y = circle.radius;

            if (Sign(circle.x, circle.y, Ex, Ey, x + circle.x, y + circle.y) == sign)
                DrawBrush(x + circle.x, y + circle.y, circle.color, thick);
            if (Sign(circle.x, circle.y, Ex, Ey, -x + circle.x, y + circle.y) == sign)
                DrawBrush(-x + circle.x, y + circle.y, circle.color, thick);
            if (Sign(circle.x, circle.y, Ex, Ey, -x + circle.x, -y + circle.y) == sign)
                DrawBrush(-x + circle.x, -y + circle.y, circle.color, thick);
            if (Sign(circle.x, circle.y, Ex, Ey, x + circle.x, -y + circle.y) == sign)
                DrawBrush(x + circle.x, -y + circle.y, circle.color, thick);
            if (Sign(circle.x, circle.y, Ex, Ey, y + circle.x, x + circle.y) == sign)
                DrawBrush(y + circle.x, x + circle.y, circle.color, thick);
            if (Sign(circle.x, circle.y, Ex, Ey, -y + circle.x, x + circle.y) == sign)
                DrawBrush(-y + circle.x, x + circle.y, circle.color, thick);
            if (Sign(circle.x, circle.y, Ex, Ey, -y + circle.x, -x + circle.y) == sign)
                DrawBrush(-y + circle.x, -x + circle.y, circle.color, thick);
            if (Sign(circle.x, circle.y, Ex, Ey, y + circle.x, -x + circle.y) == sign)
                DrawBrush(y + circle.x, -x + circle.y, circle.color, thick);
            while (y > x)
            {
                if (d < 0)
                {
                    d += dE;
                    dE += 2;
                    dSE += 2;
                }
                else
                {
                    d += dSE;
                    dE += 2;
                    dSE += 4;
                    --y;
                }
                ++x;

                if (Sign(circle.x, circle.y, Ex, Ey, x + circle.x, y + circle.y) == sign)
                    DrawBrush(x + circle.x, y + circle.y, circle.color, thick);
                if (Sign(circle.x, circle.y, Ex, Ey, -x + circle.x, y + circle.y) == sign)
                    DrawBrush(-x + circle.x, y + circle.y, circle.color, thick);
                if (Sign(circle.x, circle.y, Ex, Ey, -x + circle.x, -y + circle.y) == sign)
                    DrawBrush(-x + circle.x, -y + circle.y, circle.color, thick);
                if (Sign(circle.x, circle.y, Ex, Ey, x + circle.x, -y + circle.y) == sign)
                    DrawBrush(x + circle.x, -y + circle.y, circle.color, thick);
                if (Sign(circle.x, circle.y, Ex, Ey, y + circle.x, x + circle.y) == sign)
                    DrawBrush(y + circle.x, x + circle.y, circle.color, thick);
                if (Sign(circle.x, circle.y, Ex, Ey, -y + circle.x, x + circle.y) == sign)
                    DrawBrush(-y + circle.x, x + circle.y, circle.color, thick);
                if (Sign(circle.x, circle.y, Ex, Ey, -y + circle.x, -x + circle.y) == sign)
                    DrawBrush(-y + circle.x, -x + circle.y, circle.color, thick);
                if (Sign(circle.x, circle.y, Ex, Ey, y + circle.x, -x + circle.y) == sign)
                    DrawBrush(y + circle.x, -x + circle.y, circle.color, thick);
            }

            writeableBitmap.Unlock();
        }

        void DrawPolygon(Polygon polygon)
        {
            writeableBitmap.Lock();

            int length = polygon.xs.Count;
            for (int i = 0; i < length; i++)
            {
                if(i != length - 1)
                {
                    Line line = new Line();
                    line.initX = polygon.xs[i];
                    line.initY = polygon.ys[i];
                    line.endX = polygon.xs[i+1];
                    line.endY = polygon.ys[i+1];
                    line.color = polygon.color;
                    line.thickness = polygon.thickness;
                    DrawLine(line);
                }
                else
                {
                    Line line = new Line();
                    line.initX = polygon.xs[i];
                    line.initY = polygon.ys[i];
                    line.endX = polygon.xs[0];
                    line.endY = polygon.ys[0];
                    line.color = polygon.color;
                    line.thickness = polygon.thickness;
                    DrawLine(line);
                }
            }
            if (polygon.sfill)
            {
                Color oldCol = buttColor;
                buttColor = polygon.fillColor;
                FillPolygon(polygon);
                buttColor = oldCol;
            }
            if (polygon.pfill)
            {
                BitmapSource olsource = pattern;
                pattern = new BitmapImage(polygon.pUri);
                FillPolygonPatt(polygon);
                pattern = olsource;
            }

            writeableBitmap.Unlock();
        }

        void DrawRectangle(MyRectangle rectangle)
        {
            writeableBitmap.Lock();

            int thick = rectangle.thickness;
            int inX = rectangle.initX;
            int inY = rectangle.initY;
            int enX = rectangle.endX;
            int enY = rectangle.endY;
            Color col = rectangle.color;
            DrawLine(new Line() { initX = inX, initY = inY, endX = enX, endY = inY, color = col, thickness = thick });
            DrawLine(new Line() { initX = inX, initY = inY, endX = inX, endY = enY, color = col, thickness = thick });
            DrawLine(new Line() { initX = enX, initY = enY, endX = enX, endY = inY, color = col, thickness = thick });
            DrawLine(new Line() { initX = enX, initY = enY, endX = inX, endY = enY, color = col, thickness = thick });
            if (rectangle.sfill)
            {
                Color oldCol = buttColor;
                buttColor = rectangle.fillColor;
                FillRectangle(rectangle);
                buttColor = oldCol;
            }
            if (rectangle.pfill)
            {
                BitmapSource olsource = pattern;
                pattern = new BitmapImage(rectangle.pUri);
                FillRectanglePatt(rectangle);
                pattern = olsource;
            }

            writeableBitmap.Unlock();
        }

        void ClearImg()
        {
            int width = imgWidth;
            int height = imgHeight;

            //writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            //drawSpace.Source = writeableBitmap;

            try
            {
                writeableBitmap.Lock();

                unsafe
                {
                    IntPtr pBackBuffer = writeableBitmap.BackBuffer;

                    for (int i = 0; i < height; i++)
                    {
                        for (int j = 0; j < width; j++)
                        {
                            int color_data = 255 << 24; // A
                            color_data |= 255 << 16; // R
                            color_data |= 255 << 8;  // G
                            color_data |= 255 << 0;  // B

                            *((int*)pBackBuffer) = color_data;

                            pBackBuffer += 4;
                        }
                    }
                    writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
                }
            }
            finally
            {
                writeableBitmap.Unlock();
            }
        }

        void RedrawFigures()
        {
            foreach (var line in lines)
                DrawLine(line);
            foreach (var circle in circles)
                DrawCircle(circle);
            foreach (var polygon in polygons)
                DrawPolygon(polygon);
            foreach (var rect in rectangles)
                DrawRectangle(rect);
        }

        private void antialiasingClick(object sender, RoutedEventArgs e)
        {
            if(!anti)
            {
                antiLabel.Content = "ON";
                ClearImg();
                RedrawAnti();
                anti = true;
            }
            else
            {
                antiLabel.Content = "OFF";
                ClearImg();
                RedrawFigures();
                anti = false;
            }
        }

        void WuLine(Line line)
        {
            writeableBitmap.Lock();

            int x1 = line.initX;
            int x2 = line.endX;
            int y1 = line.initY;
            int y2 = line.endY;

            Color col = line.color;
            bool steep = Math.Abs(y2 - y1) > Math.Abs(x2 - x1);

            double dx, dy, m;
            int xf, xb, yf, yb;
            int L = col.A;
            int B = 0;

            if (steep)
            {
                double x;
                if (y1 < y2)
                {
                    yf = y1;
                    yb = y2;
                    x = x1;
                    dx = x2 - x1;
                    dy = y2 - y1;
                }
                else
                {
                    yf = y2;
                    yb = y1;
                    x = x2;
                    dx = x1 - x2;
                    dy = y1 - y2;
                }
                m = dx / dy;
                for (int y = yf; y <= yb; ++y)
                {
                    double c1 = L * (1 - (x - Math.Floor(x))) + B * (x - Math.Floor(x));
                    double c2 = L * (x - Math.Floor(x)) + B * (1 - (x - Math.Floor(x)));
                    if (c1 > 255) c1 = 255;
                    else if (c1 < 0) c1 = 0;
                    if (c2 > 255) c2 = 255;
                    else if (c2 < 0) c2 = 0;
                    DrawPixel((int)Math.Floor(x), y, Color.FromArgb((byte)c1, col.R, col.G, col.B));
                    DrawPixel((int)Math.Floor(x) + 1, y, Color.FromArgb((byte)c2, col.R, col.G, col.B));
                    x += m;
                }
            }
            else
            {
                double y;
                if (x1 < x2)
                {
                    xf = x1;
                    xb = x2;
                    y = y1;
                    dx = x2 - x1;
                    dy = y2 - y1;
                }
                else
                {
                    xf = x2;
                    xb = x1;
                    y = y2;
                    dx = x1 - x2;
                    dy = y1 - y2;
                }
                m = dy / dx;
                for (int x = xf; x <= xb; ++x)
                {
                    double c1 = L * (1 - (y - Math.Floor(y))) + B * (y - Math.Floor(y));
                    double c2 = L * (y - Math.Floor(y)) + B * (1 - (y - Math.Floor(y)));
                    if (c1 > 255) c1 = 255;
                    else if (c1 < 0) c1 = 0;
                    if (c2 > 255) c2 = 255;
                    else if (c2 < 0) c2 = 0;
                    DrawPixel(x, (int)Math.Floor(y), Color.FromArgb((byte)c1, col.R, col.G, col.B));
                    DrawPixel(x, (int)Math.Floor(y) + 1, Color.FromArgb((byte)c2, col.R, col.G, col.B));
                    y += m;
                }
            }

            writeableBitmap.Unlock();
        }

        double D(int R, int y)
        {
            return Math.Ceiling(Math.Sqrt(Math.Pow(R, 2) - Math.Pow(y, 2))) - Math.Sqrt(Math.Pow(R, 2) - Math.Pow(y, 2));
        }

        void WuCircle(Circle circle)
        {
            writeableBitmap.Lock();

            Color c = circle.color;
            int cx = circle.x;
            int cy = circle.y;
            int R = circle.radius;

            int L = c.A;
            int B = 0;
            int x = R;
            int y = 0;
            DrawPixel(cx + x, cy, c);
            DrawPixel(cx - x, cy, c);
            DrawPixel(cx, cy + x, c);
            DrawPixel(cx, cy - x, c);
            while (x > y)
            {
                ++y;
                x = (int)Math.Ceiling(Math.Sqrt(R * R - y * y));
                double T = D(R, y);
                double c2 = L * (1 - T) + B * T;
                double c1 = L * T + B * (1 - T);
                DrawPixel(cx + x, cy + y, Color.FromArgb((byte)c2, c.R, c.G, c.B));
                DrawPixel(cx + x - 1, cy + y, Color.FromArgb((byte)c1, c.R, c.G, c.B));
                DrawPixel(cx + x, cy - y, Color.FromArgb((byte)c2, c.R, c.G, c.B));
                DrawPixel(cx + x - 1, cy - y, Color.FromArgb((byte)c1, c.R, c.G, c.B));

                DrawPixel(cx - x + 1, cy - y + 1, Color.FromArgb((byte)c1, c.R, c.G, c.B));
                DrawPixel(cx - x, cy - y + 1, Color.FromArgb((byte)c2, c.R, c.G, c.B));
                DrawPixel(cx - x + 1, cy + y, Color.FromArgb((byte)c1, c.R, c.G, c.B));
                DrawPixel(cx - x, cy + y, Color.FromArgb((byte)c2, c.R, c.G, c.B));

                DrawPixel(cx + y, cy + x, Color.FromArgb((byte)c2, c.R, c.G, c.B));
                DrawPixel(cx + y, cy + x - 1, Color.FromArgb((byte)c1, c.R, c.G, c.B));
                DrawPixel(cx + y, cy - x, Color.FromArgb((byte)c2, c.R, c.G, c.B));
                DrawPixel(cx + y, cy - x + 1, Color.FromArgb((byte)c1, c.R, c.G, c.B));

                DrawPixel(cx - y, cy + x - 1, Color.FromArgb((byte)c1, c.R, c.G, c.B));
                DrawPixel(cx - y, cy + x, Color.FromArgb((byte)c2, c.R, c.G, c.B));
                DrawPixel(cx - y + 1, cy - x + 1, Color.FromArgb((byte)c1, c.R, c.G, c.B));
                DrawPixel(cx - y + 1, cy - x, Color.FromArgb((byte)c2, c.R, c.G, c.B));
            }

            writeableBitmap.Unlock();
        }

        void DrawPolygonAnti(Polygon polygon)
        {
            writeableBitmap.Lock();

            int length = polygon.xs.Count;
            for (int i = 0; i < length; i++)
            {
                if (i != length - 1)
                {
                    Line line = new Line();
                    line.initX = polygon.xs[i];
                    line.initY = polygon.ys[i];
                    line.endX = polygon.xs[i + 1];
                    line.endY = polygon.ys[i + 1];
                    line.color = polygon.color;
                    line.thickness = polygon.thickness;
                    WuLine(line);
                }
                else
                {
                    Line line = new Line();
                    line.initX = polygon.xs[i];
                    line.initY = polygon.ys[i];
                    line.endX = polygon.xs[0];
                    line.endY = polygon.ys[0];
                    line.color = polygon.color;
                    line.thickness = polygon.thickness;
                    WuLine(line);
                }
            }

            writeableBitmap.Unlock();
        }

        void DrawRectangleAnti(MyRectangle rectangle)
        {
            writeableBitmap.Lock();

            int thick = rectangle.thickness;
            int inX = rectangle.initX;
            int inY = rectangle.initY;
            int enX = rectangle.endX;
            int enY = rectangle.endY;
            Color col = rectangle.color;
            WuLine(new Line() { initX = inX, initY = inY, endX = enX, endY = inY, color = col, thickness = thick });
            WuLine(new Line() { initX = inX, initY = inY, endX = inX, endY = enY, color = col, thickness = thick });
            WuLine(new Line() { initX = enX, initY = enY, endX = enX, endY = inY, color = col, thickness = thick });
            WuLine(new Line() { initX = enX, initY = enY, endX = inX, endY = enY, color = col, thickness = thick });

            writeableBitmap.Unlock();
        }

        void RedrawAnti()
        {
            foreach (var line in lines)
                WuLine(line);
            foreach (var circle in circles)
                WuCircle(circle);
            foreach (var polygon in polygons)
                DrawPolygonAnti(polygon);
            foreach (var rect in rectangles)
                DrawRectangleAnti(rect);
        }

        bool Clip(float denom, float numer, ref float tE, ref float tL)
        {
            if (denom == 0) // Parallel line
            {
                if (numer < 0)
                    return false;
                return true;
            }
            float t = numer / denom;
            if (denom < 0) // PE
            {
                if (t > tL)
                    return false;
                if (t > tE)
                    tE = t;
            }
            else // PL
            {
                if (t < tE)
                    return false;
                if (t < tL)
                    tL = t;
            }
            return true;
        }

        void LiangBarsky(Point p1, Point p2, MyRectangle clip)
        {
            writeableBitmap.Lock();

            float dx = (float)(p2.X - p1.X), dy = (float)(p2.Y - p1.Y);
            float tE = 0, tL = 1;

            int left;
            int right;
            int top;
            int bottom;
            if (clip.initX < clip.endX)
            {
                left = clip.initX;
                right = clip.endX;
            }
            else
            {
                left = clip.endX;
                right = clip.initX;
            }
            if (clip.initY < clip.endY)
            {
                bottom = clip.initY;
                top = clip.endY;
            }
            else
            {
                bottom = clip.endY;
                top = clip.initY;
            }

            if(Clip(-dx, (float)(p1.X - left), ref tE, ref tL))
            {
                if (Clip(dx, (float)(right - p1.X), ref tE, ref tL))
                {
                    if (Clip(-dy, (float)(p1.Y - bottom), ref tE, ref tL))
                    {
                        if (Clip(dy, (float)(top - p1.Y), ref tE, ref tL))
                        {
                            if(tL < 1)
                            {
                                p2.X = p1.X + dx * tL;
                                p2.Y = p1.Y + dy * tL;
                            }
                            if(tE > 0)
                            {
                                p1.X += dx * tE;
                                p1.Y += dy * tE;
                            }
                            DrawLine(new Line() { initX = (int)p1.X, initY = (int)p1.Y, endX = (int)p2.X, endY = (int)p2.Y, color = Colors.Red, thickness = 3 });
                        }
                    }
                }
            }

            writeableBitmap.Unlock();
        }

        void FillRectangle(MyRectangle rect)
        {
            writeableBitmap.Lock();

            int maxy, miny;
            if (rect.initY < rect.endY)
            {
                maxy = rect.endY;
                miny = rect.initY;
            }
            else
            {
                maxy = rect.initY;
                miny = rect.endY;
            }
               
            int minx, maxx;
            if(rect.initX < rect.endX)
            {
                minx = rect.initX;
                maxx = rect.endX;
            }
            else
            {
                maxx = rect.initX;
                minx = rect.endX;
            }

            EdgeTable = new List<ET>();
            ET eT = new ET();
            eT.y = miny;
            eT.aETs = new List<AET>();
            eT.aETs.Add(new AET() { ymax = maxy, x = minx, oneOverM = 0 });
            eT.aETs.Add(new AET() { ymax = maxy, x = maxx, oneOverM = 0 });
            EdgeTable.Add(eT);

            int y = miny;
            ActiveEdgeTable = new List<AET>();
            foreach(var item in EdgeTable)
            {
                foreach (var it in item.aETs)
                    ActiveEdgeTable.Add(it);
            }

            for (int i = y; i < maxy; i++)
            {
                y = i;
                DrawLine(new Line() { initX = (int)ActiveEdgeTable[0].x, initY = y, endX = (int)ActiveEdgeTable[1].x, endY = y, thickness = 1, color = buttColor });
            }

            writeableBitmap.Unlock();
        }

        private static int CompareEdges(AET e1, AET e2)
        {
            if (e1.x < e2.x)
                return -1;
            else if (e1.x > e2.x)
                return 1;
            return 0;
        }

        void FillPolygon(Polygon polygon)
        {
            writeableBitmap.Lock();

            int maxy = 0;
            int miny = Int32.MaxValue;

            foreach(var y in polygon.ys)
            {
                if (y > maxy)
                    maxy = y;
                if (y < miny)
                    miny = y;
            }

            List<int> ys = new List<int>();
            foreach(var y in polygon.ys)
            {
                if (!ys.Contains(y))
                    ys.Add(y);
            }
            ys.Sort();

            EdgeTable = new List<ET>();
            foreach (var y in ys)
            {
                ET eT = new ET();
                eT.y = y;
                eT.aETs = new List<AET>();
                EdgeTable.Add(eT);
            }

            int length = polygon.ys.Count;
            for(int i = 0; i < length ; i++)
            {
                if(i != length - 1)
                {
                    double dx = polygon.xs[i + 1] - polygon.xs[i];
                    double dy = polygon.ys[i + 1] - polygon.ys[i];

                    int xmin, ymax, ymin;
                    if (polygon.ys[i] < polygon.ys[i + 1])
                    {
                        xmin = polygon.xs[i];
                        ymax = polygon.ys[i + 1];
                        ymin = polygon.ys[i];
                    }                       
                    else
                    {
                        xmin = polygon.xs[i + 1];
                        ymax = polygon.ys[i];
                        ymin = polygon.ys[i + 1];
                    }

                    if(dy != 0)
                    {
                        AET edge = new AET();
                        edge.x = xmin;
                        edge.ymax = ymax;
                        edge.oneOverM = dx / dy;

                        EdgeTable.Find(x => x.y == ymin).aETs.Add(edge);
                    }
                }
                else
                {
                    double dx = polygon.xs[0] - polygon.xs[i];
                    double dy = polygon.ys[0] - polygon.ys[i];

                    int xmin, ymax, ymin;
                    if (polygon.ys[i] < polygon.ys[0])
                    {
                        xmin = polygon.xs[i];
                        ymax = polygon.ys[0];
                        ymin = polygon.ys[i];
                    }
                    else
                    {
                        xmin = polygon.xs[0];
                        ymax = polygon.ys[i];
                        ymin = polygon.ys[0];
                    }

                    AET edge = new AET();
                    edge.x = xmin;
                    edge.ymax = ymax;
                    edge.oneOverM = dx / dy;

                    EdgeTable.Find(x => x.y == ymin).aETs.Add(edge);
                }
            }

            ActiveEdgeTable = new List<AET>();
            int iter = 0;
            int iterY;
            for(int i = miny; i < maxy; i++)
            {
                iterY = i;
                ActiveEdgeTable.RemoveAll(edge => edge.ymax <= iterY);
                if(EdgeTable[iter].y == iterY)
                {
                    foreach (var it in EdgeTable[iter].aETs)
                        ActiveEdgeTable.Add(it);
                    iter++;
                }
                ActiveEdgeTable.Sort(CompareEdges);
                int len = ActiveEdgeTable.Count;
                for (int j = 0; j < len; j += 2)
                    DrawLine(new Line() { color=buttColor, thickness=1, initX = (int)ActiveEdgeTable[j].x, initY = iterY, endX = (int)ActiveEdgeTable[j+1].x, endY=iterY });

                foreach (var item in ActiveEdgeTable)
                    item.Step();
            }

            writeableBitmap.Unlock();
        }

        void FillRectanglePatt(MyRectangle rect)
        {
            writeableBitmap.Lock();

            int maxy, miny;
            if (rect.initY < rect.endY)
            {
                maxy = rect.endY;
                miny = rect.initY;
            }
            else
            {
                maxy = rect.initY;
                miny = rect.endY;
            }

            int minx, maxx;
            if (rect.initX < rect.endX)
            {
                minx = rect.initX;
                maxx = rect.endX;
            }
            else
            {
                maxx = rect.initX;
                minx = rect.endX;
            }

            EdgeTable = new List<ET>();
            ET eT = new ET();
            eT.y = miny;
            eT.aETs = new List<AET>();
            eT.aETs.Add(new AET() { ymax = maxy, x = minx, oneOverM = 0 });
            eT.aETs.Add(new AET() { ymax = maxy, x = maxx, oneOverM = 0 });
            EdgeTable.Add(eT);

            int y = miny;
            ActiveEdgeTable = new List<AET>();
            foreach (var item in EdgeTable)
            {
                foreach (var it in item.aETs)
                    ActiveEdgeTable.Add(it);
            }

            int pwidth = pattern.PixelWidth;
            int pheight = pattern.PixelHeight;

            System.Drawing.Bitmap bmp = BitmapFromSource(pattern);

            System.Drawing.Rectangle rect1 = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(rect1, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);

            IntPtr ptr = bmpData.Scan0;

            int stride = bmpData.Stride;

            int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
            byte[] rgbValues = new byte[bytes];

            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            for (int i = y; i < maxy; i++)
            {
                y = i;
                for(int j = (int)ActiveEdgeTable[0].x; j < (int)ActiveEdgeTable[1].x; j++)
                {
                    Color col = new Color();
                    col.A = 255;
                    col.R = rgbValues[(y % pheight) * stride + (j % pwidth) * 4 + 2];
                    col.G = rgbValues[(y % pheight) * stride + (j % pwidth) * 4 + 1];
                    col.B = rgbValues[(y % pheight) * stride + (j % pwidth) * 4];
                    DrawPixel(j, y, col);
                }
            }

            writeableBitmap.Unlock();
        }

        void FillPolygonPatt(Polygon polygon)
        {
            writeableBitmap.Lock();

            int maxy = 0;
            int miny = Int32.MaxValue;

            foreach (var y in polygon.ys)
            {
                if (y > maxy)
                    maxy = y;
                if (y < miny)
                    miny = y;
            }

            List<int> ys = new List<int>();
            foreach (var y in polygon.ys)
            {
                if (!ys.Contains(y))
                    ys.Add(y);
            }
            ys.Sort();

            EdgeTable = new List<ET>();
            foreach (var y in ys)
            {
                ET eT = new ET();
                eT.y = y;
                eT.aETs = new List<AET>();
                EdgeTable.Add(eT);
            }

            int length = polygon.ys.Count;
            for (int i = 0; i < length; i++)
            {
                if (i != length - 1)
                {
                    double dx = polygon.xs[i + 1] - polygon.xs[i];
                    double dy = polygon.ys[i + 1] - polygon.ys[i];

                    int xmin, ymax, ymin;
                    if (polygon.ys[i] < polygon.ys[i + 1])
                    {
                        xmin = polygon.xs[i];
                        ymax = polygon.ys[i + 1];
                        ymin = polygon.ys[i];
                    }
                    else
                    {
                        xmin = polygon.xs[i + 1];
                        ymax = polygon.ys[i];
                        ymin = polygon.ys[i + 1];
                    }

                    if(dy != 0)
                    {
                        AET edge = new AET();
                        edge.x = xmin;
                        edge.ymax = ymax;
                        edge.oneOverM = dx / dy;

                        EdgeTable.Find(x => x.y == ymin).aETs.Add(edge);
                    }
                }
                else
                {
                    double dx = polygon.xs[0] - polygon.xs[i];
                    double dy = polygon.ys[0] - polygon.ys[i];

                    int xmin, ymax, ymin;
                    if (polygon.ys[i] < polygon.ys[0])
                    {
                        xmin = polygon.xs[i];
                        ymax = polygon.ys[0];
                        ymin = polygon.ys[i];
                    }
                    else
                    {
                        xmin = polygon.xs[0];
                        ymax = polygon.ys[i];
                        ymin = polygon.ys[0];
                    }

                    AET edge = new AET();
                    edge.x = xmin;
                    edge.ymax = ymax;
                    edge.oneOverM = dx / dy;

                    EdgeTable.Find(x => x.y == ymin).aETs.Add(edge);
                }
            }

            int pwidth = pattern.PixelWidth;
            int pheight = pattern.PixelHeight;

            System.Drawing.Bitmap bmp = BitmapFromSource(pattern);

            System.Drawing.Rectangle rect1 = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(rect1, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);

            IntPtr ptr = bmpData.Scan0;

            int stride = bmpData.Stride;

            int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
            byte[] rgbValues = new byte[bytes];

            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            ActiveEdgeTable = new List<AET>();
            int iter = 0;
            int iterY;
            for (int i = miny; i < maxy; i++)
            {
                iterY = i;
                ActiveEdgeTable.RemoveAll(edge => edge.ymax <= iterY);
                if (EdgeTable[iter].y == iterY)
                {
                    foreach (var it in EdgeTable[iter].aETs)
                        ActiveEdgeTable.Add(it);
                    iter++;
                }
                ActiveEdgeTable.Sort(CompareEdges);
                int len = ActiveEdgeTable.Count;
                for (int j = 0; j < len; j += 2)
                {
                    for(int k = (int)ActiveEdgeTable[j].x; k < (int)ActiveEdgeTable[j+1].x; k++)
                    {
                        Color col = new Color();
                        col.A = 255;
                        col.R = rgbValues[(iterY % pheight) * stride + (k % pwidth) * 4 + 2];
                        col.G = rgbValues[(iterY % pheight) * stride + (k % pwidth) * 4 + 1];
                        col.B = rgbValues[(iterY % pheight) * stride + (k % pwidth) * 4];
                        DrawPixel(k, iterY, col);
                    }
                }

                foreach (var item in ActiveEdgeTable)
                    item.Step();
            }

            writeableBitmap.Unlock();
        }

        private System.Drawing.Bitmap BitmapFromSource(BitmapSource source)
        {
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(source.PixelWidth, source.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            System.Drawing.Imaging.BitmapData data = bmp.LockBits(new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp.Size), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            source.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }

        Color GetColor(int x, int y)
        {
            var color = new Color();
            unsafe
            {
                IntPtr pBackBuffer = writeableBitmap.BackBuffer;

                pBackBuffer += y * writeableBitmap.BackBufferStride;
                pBackBuffer += x * 4;

                int color_data = *((int*)pBackBuffer);
                color.B = (byte)((color_data & 0x000000FF) >> 0);
                color.G = (byte)((color_data & 0x0000FF00) >> 8);
                color.R = (byte)((color_data & 0x00FF0000) >> 16);
                color.A = (byte)((color_data & 0xFF000000) >> 24);
            }
            return color;
        }

        void FloodFill(int x, int y, Color old, Color newCol)
        {
            writeableBitmap.Lock();

            Stack<(int, int)> stack = new Stack<(int, int)>();

            stack.Push((x, y));

            while(stack.Count != 0)
            {
                var elem = stack.Pop();

                int ox = elem.Item1;
                int oy = elem.Item2;

                if(GetColor(ox, oy) == old)
                {
                    stack.Push((ox + 1, oy));
                    stack.Push((ox, oy + 1));
                    stack.Push((ox - 1, oy));
                    stack.Push((ox, oy - 1));
                    DrawPixel(ox, oy, newCol);
                }
            }

            writeableBitmap.Unlock();
        }

        class Line
        {
            public int initX;
            public int initY;
            public int endX;
            public int endY;
            public Color color;
            public int thickness;
        }

        class Circle
        {
            public int x;
            public int y;
            public int radius;
            public Color color;
            public int thickness;
        }

        class Polygon
        {
            public List<int> xs;
            public List<int> ys;
            public Color color;
            public int thickness;
            public bool sfill;
            public Color fillColor;
            public bool pfill;
            public Uri pUri;
        }

        class MyRectangle
        {
            public int initX;
            public int initY;
            public int endX;
            public int endY;
            public Color color;
            public int thickness;
            public bool sfill;
            public Color fillColor;
            public bool pfill;
            public Uri pUri;
        }

        public class AET
        {
            public int ymax;
            public double x;
            public double oneOverM;

            public void Step()
            {
                x += oneOverM;
            }
        }

        public struct ET
        {
            public int y;
            public List<AET> aETs;
        }

        private void saveClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (saveFileDialog.ShowDialog() == true)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(saveFileDialog.FileName, true))
                {
                    foreach(var line in lines)
                    {
                        file.WriteLine("line");
                        file.WriteLine(line.initX.ToString());
                        file.WriteLine(line.initY.ToString());
                        file.WriteLine(line.endX.ToString());
                        file.WriteLine(line.endY.ToString());
                        file.WriteLine(line.color.ToString());
                        file.WriteLine(line.thickness.ToString());
                        file.WriteLine();
                    }

                    foreach(var circle in circles)
                    {
                        file.WriteLine("circle");
                        file.WriteLine(circle.x.ToString());
                        file.WriteLine(circle.y.ToString());
                        file.WriteLine(circle.radius.ToString());
                        file.WriteLine(circle.color.ToString());
                        file.WriteLine(circle.thickness.ToString());
                        file.WriteLine();
                    }

                    foreach(var polygon in polygons)
                    {
                        file.WriteLine("polygon");
                        for(int i = 0; i < polygon.xs.Count; i++)
                        {
                            file.Write(polygon.xs[i].ToString());
                            if (i != polygon.xs.Count - 1)
                                file.Write(',');
                        }
                        file.WriteLine();
                        for (int i = 0; i < polygon.ys.Count; i++)
                        {
                            file.Write(polygon.ys[i].ToString());
                            if (i != polygon.ys.Count - 1)
                                file.Write(',');
                        }
                        file.WriteLine();
                        file.WriteLine(polygon.color.ToString());
                        file.WriteLine(polygon.thickness.ToString());
                        file.WriteLine(polygon.sfill);
                        if (!polygon.sfill)
                            file.WriteLine("none");
                        else
                            file.WriteLine(polygon.fillColor.ToString());
                        file.WriteLine(polygon.pfill);
                        if (!polygon.pfill)
                            file.WriteLine("none");
                        else
                            file.WriteLine(polygon.pUri.ToString());
                        file.WriteLine();
                    }

                    foreach (var rect in rectangles)
                    {
                        file.WriteLine("rectangle");
                        file.WriteLine(rect.initX.ToString());
                        file.WriteLine(rect.initY.ToString());
                        file.WriteLine(rect.endX.ToString());
                        file.WriteLine(rect.endY.ToString());
                        file.WriteLine(rect.color.ToString());
                        file.WriteLine(rect.thickness.ToString());
                        file.WriteLine(rect.sfill);
                        if (!rect.sfill)
                            file.WriteLine("none");
                        else
                            file.WriteLine(rect.fillColor.ToString());
                        file.WriteLine(rect.pfill);
                        if (!rect.pfill)
                            file.WriteLine("none");
                        else
                            file.WriteLine(rect.pUri.ToString());
                        file.WriteLine();
                    }
                }
            }
        }

        private void loadClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                this.lines = new List<Line>();
                this.circles = new List<Circle>();
                this.polygons = new List<Polygon>();
                this.rectangles = new List<MyRectangle>();
                string[] lines = System.IO.File.ReadAllLines(openFileDialog.FileName);
                int length = lines.Count();
                int i = 0;
                while (i < length)
                {
                    if(lines[i] == "line")
                    {
                        Line line = new Line();
                        line.initX = Int32.Parse(lines[i + 1]);
                        line.initY = Int32.Parse(lines[i + 2]);
                        line.endX = Int32.Parse(lines[i + 3]);
                        line.endY = Int32.Parse(lines[i + 4]);
                        line.color = (Color)ColorConverter.ConvertFromString(lines[i + 5]);
                        line.thickness = Int32.Parse(lines[i + 6]);
                        this.lines.Add(line);
                        i = i + 8;
                    }

                    else if (lines[i] == "circle")
                    {
                        Circle circle = new Circle();
                        circle.x = Int32.Parse(lines[i + 1]);
                        circle.y = Int32.Parse(lines[i + 2]);
                        circle.radius = Int32.Parse(lines[i + 3]);
                        circle.color = (Color)ColorConverter.ConvertFromString(lines[i + 4]);
                        circle.thickness = Int32.Parse(lines[i + 5]);
                        this.circles.Add(circle);
                        i = i + 7;
                    }

                    else if(lines[i] == "polygon")
                    {
                        Polygon polygon = new Polygon();
                        polygon.xs = new List<int>();
                        polygon.ys = new List<int>();
                        string[] xs = lines[i + 1].Split(',');
                        foreach (var x in xs)
                            polygon.xs.Add(Int32.Parse(x));
                        string[] ys = lines[i + 2].Split(',');
                        foreach (var y in ys)
                            polygon.ys.Add(Int32.Parse(y));
                        polygon.color = (Color)ColorConverter.ConvertFromString(lines[i + 3]);
                        polygon.thickness = Int32.Parse(lines[i + 4]);
                        polygon.sfill = Convert.ToBoolean(lines[i + 5]);
                        if (polygon.sfill)
                            polygon.fillColor = (Color)ColorConverter.ConvertFromString(lines[i + 6]);
                        polygon.pfill = Convert.ToBoolean(lines[i + 7]);
                        if (polygon.pfill)
                            polygon.pUri = new Uri(lines[i + 8]);
                        this.polygons.Add(polygon);
                        i = i + 10;
                    }

                    else if (lines[i] == "rectangle")
                    {
                        MyRectangle rect = new MyRectangle();
                        rect.initX = Int32.Parse(lines[i + 1]);
                        rect.initY = Int32.Parse(lines[i + 2]);
                        rect.endX = Int32.Parse(lines[i + 3]);
                        rect.endY = Int32.Parse(lines[i + 4]);
                        rect.color = (Color)ColorConverter.ConvertFromString(lines[i + 5]);
                        rect.thickness = Int32.Parse(lines[i + 6]);
                        rect.sfill = Convert.ToBoolean(lines[i + 7]);
                        if (rect.sfill)
                            rect.fillColor = (Color)ColorConverter.ConvertFromString(lines[i + 8]);
                        rect.pfill = Convert.ToBoolean(lines[i + 9]);
                        if (rect.pfill)
                            rect.pUri = new Uri(lines[i + 10]);
                        this.rectangles.Add(rect);
                        i = i + 12;
                    }
                }
                ClearImg();
                if (!anti)
                    RedrawFigures();
                else
                    RedrawAnti();
            }
        }

        private void sizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!created)
                return;

            int width = (int)this.Width;
            int height = (int)this.Height;

            int widthDiff = width - winWidth;
            int heightDiff = height - winHeight;

            imgWidth += widthDiff;
            imgHeight += heightDiff;

            writeableBitmap = new WriteableBitmap(imgWidth, imgHeight, 96, 96, PixelFormats.Bgra32, null);
            drawSpace.Source = writeableBitmap;

            try
            {
                writeableBitmap.Lock();

                unsafe
                {
                    IntPtr pBackBuffer = writeableBitmap.BackBuffer;

                    for (int i = 0; i < height; i++)
                    {
                        for (int j = 0; j < width; j++)
                        {
                            int color_data = 255 << 24; // A
                            color_data |= 255 << 16; // R
                            color_data |= 255 << 8;  // G
                            color_data |= 255 << 0;  // B

                            *((int*)pBackBuffer) = color_data;

                            pBackBuffer += 4;
                        }
                    }
                    writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
                }
            }
            finally
            {
                writeableBitmap.Unlock();
            }
            RedrawFigures();
        }

        private void thickChanged(object sender, SelectionChangedEventArgs e)
        {
            if(editing)
            {
                int thickness = 1;
                if (thickBox.SelectedIndex == 0)
                    thickness = 1;
                if (thickBox.SelectedIndex == 1)
                    thickness = 3;
                if (thickBox.SelectedIndex == 2)
                    thickness = 5;

                if (edited.GetType() == typeof(Line))
                {
                    (edited as Line).thickness = thickness;
                    ClearImg();
                    RedrawFigures();
                }
                if (edited.GetType() == typeof(Circle))
                {
                    (edited as Circle).thickness = thickness;
                    ClearImg();
                    RedrawFigures();
                }
                if (edited.GetType() == typeof(Polygon))
                {
                    (edited as Polygon).thickness = thickness;
                    ClearImg();
                    RedrawFigures();
                }
                if (edited.GetType() == typeof(MyRectangle))
                {
                    (edited as MyRectangle).thickness = thickness;
                    ClearImg();
                    RedrawFigures();
                }
            }
        }

        private void readmeClick(object sender, RoutedEventArgs e)
        {
            /*string text = File.ReadAllText("..//..//ReadMe.txt");
            System.Windows.MessageBox.Show(text, "ReadMe");*/
            ReadMeWin win = new ReadMeWin();
            win.Show();
        }

        private void loadPatt(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                Uri fileUri = new Uri(openFileDialog.FileName);
                pattern = new BitmapImage(fileUri);
                pattUri = fileUri;
            }
        }
    }

}

