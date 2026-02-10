using DevExpress.Internal;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace Tet.GeoSymbol
{

    internal struct ElemColors
    {
        internal Color m_lc;
        internal Color m_fc;
        internal Color m_bc;
    }

    internal struct ElemBrushs
    {
        internal Brush m_HatchBrush;
        internal Brush m_Fill_SolidBrush;
        internal Brush m_linearGradientBrush;
        internal Brush m_radialGradientBrush;
    }

    public class GeoSymbolRepository
    {
        protected List<SymData> _symbols;
        protected List<SymFill> _symFills;
        protected List<SymTTF>  _symTTFs;
        protected List<SymbolNode> _symbolNodes;
        protected Color _defaultForegroundColor = Color.Black;
        protected Color _defaultBackgroundColor = Color.Red;

        

        
        public List<SymData> Symbols
        {
            get
            {
                return _symbols;
            }
            set
            {
                _symbols = value;
            }
        }

        protected Color DefaultForegroundColor
        {
            get
            {
                return _defaultForegroundColor;
            }
        }
        protected Color DefaultBackgroundColor
        {
            get
            {
                return _defaultBackgroundColor;
            }
        }

        public List<SymFill> SymFills
        {
            get
            {
                return _symFills;
            }
            set
            {
                _symFills = value;
            }
        }


        public  List<SymTTF> SymTTFs
        {
            get
            {
                return _symTTFs;
            }
            set
            {
                _symTTFs = value;
            }
        }

        public List<SymbolNode> SymbolNodes
        {

            get
            {
                return _symbolNodes;
            }
            internal set
            {
                _symbolNodes = value;
            }
        }

        protected int FindSymbolIndex(string symCode)
        {
            int result = -1;
            for(int i= 0; i<_symbols.Count; i++)
            {
                if (string.Equals(_symbols[i].ID, symCode))
                {
                    result = i;
                    break;
                }
            }
            return result;
        }

        protected SymData? GetSymbol(string symCode)
        {
            SymData? result = null;
            int index = FindSymbolIndex(symCode);
            if(index >= 0)
            {
                result = this.Symbols[index];
                
            }
            return result;
        }


        /// <summary>
        /// 获得符号中自定义的背景色
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public Color? GetSymbolBuildinBackColor(string code)
        {
            SymData? symData = GetSymbol(code);
            if (symData == null)
            {
                return null;
            }
            Color? color = symData.Value.m_bgCol;
            return color;
        }


       protected Color? GetDefaultSymbolBackgroudColor(int index)
       {
            SymData symData = this.Symbols[index];
            Color? result = symData.m_bgCol;
            return result;
       }


       protected Image CreateLithologyImage(ref SymData symbol,int appMode,double width,Color? symColor,bool bOpaque,Color? backColor,bool antialias)
       {
            double x1, y1,x2,y2, w, h;
            SymTTF LT;
           
            double Size = width / (symbol.Width * 400 / 120.0);

            int M_Width = (int)width;    //岩性符号的宽度  当size=1 位图宽度= FHS[ID]->Syms_Width
            double fhjj = Size * (symbol.RowSpace * 400 / 120.0);            //符号第1行与第2行之间的间距
            int M_Height = (int)(Size * ((30 + symbol.RowSpace) * 400 / 120.0));    //每行岩性符号的高度 缺省值30

            if (M_Height < 1)
                M_Height = 1;

            if (symbol.BaseMode == 0 && appMode != 4)
            {
                M_Height = 2 * M_Height;                            // 双排符号的总高度
            }

            double[] BaseX = new double[9];
            double[] BaseY = new double[9];
            float factor = 3.0f; 
            int kk = 0;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    BaseX[kk] = j * M_Width;
                    BaseY[kk] = i * M_Height;
                    kk++;
                }
            }

            float imageWidth =  factor * M_Width;
            float imageHeight = factor * M_Height;
            // 定义3倍的范围宽度的QPixmap
            Bitmap bitMap = new Bitmap((int)(imageWidth), (int)(imageWidth));

            Graphics g = Graphics.FromImage(bitMap);
            {
                if (antialias)
                {
                    g.TextRenderingHint = TextRenderingHint.AntiAlias;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                }
                RectangleF rect = new RectangleF(0, 0, imageWidth, imageHeight);
                Brush brush;
                if (backColor == null)
                {
                    backColor = DefaultBackgroundColor; 
                }
                if(symColor == null)
                {
                    symColor = DefaultForegroundColor;
                }
                if (bOpaque)
                    brush = new SolidBrush(backColor.Value);
                else
                    brush = new SolidBrush(Color.FromArgb(0, 255, 255, 255));
                g.FillRectangle(brush, rect);
              
            }

            g.TranslateTransform(0, 0);
            g.ScaleTransform(1, 1);

            Color LC = symColor.Value; //Qt::black;    // 线条\填充颜色
            Color FC = symColor.Value; //Qt::black;    //填充的前景色

            double Scale = Size * ((30.0f) * 400.0f / 120.0f) / 500.0f;//确定符号的大小
            int x0 = (int)BaseX[4];
            int y0 = (int)BaseY[4];

            int nDrawRowNums = 2;
            if (symbol.BaseMode == 1)
            {
                nDrawRowNums = 1;
            }

            for (int p = 0; p < nDrawRowNums * symbol.nTTF; p++)
            {
                int k2 = p;
                y0 = (int)BaseY[4];
                if (p >= symbol.nTTF)
                {
                    k2 = p - symbol.nTTF + 7;            //绘制第2排的符号
                    y0 = (int)(BaseY[4] + M_Height / 2.0);
                }
                x0 = (int)(BaseX[4] + (M_Width * symbol.TTFs[k2].Left) / 120);
                int char_ID = symbol.TTFs[k2].CID;        //字符的序号
                if (char_ID <= 0 || char_ID > 255)        //当 Base_Mode == 0 双排符号，且 App_Mode = 4单排符号，绘制第二排的符号
                    continue;

                LT = SymTTFs[char_ID - 1];

                //绘制岩性符号 -------------------------------------------------
                TTFShape LM;
                for (int j = 0; j < LT.Shapes.Length; j++)
                {
                    LM = LT.Shapes[j];
                    byte Brush_Transparency = 255;  // 填充透明参数
                    if (LM.Fill == 0)            //不填充
                    {
                        Brush_Transparency = 0;
                    }
                    //Hatch刷子 = 实心刷子
                    SolidBrush m_Fill_SolidBrush = new SolidBrush(FC);

                    // pen
                    double Pen_Width = Size * (LM.PenWidth / 10.0 / 5.0);    //   笔宽参数
                    if (Pen_Width <= 0)
                        Pen_Width = 0.1;

                    byte Pen_Transparency = 255;    // 线条的透明度  255－不透明
                    if (LM.PenWidth == 0)            // 无线条
                        Pen_Transparency = 0;

                    Pen m_pen =  new Pen(LC, (float)Pen_Width);

                    // 线条连接方式参数的设置  ===============================================
                    //m_pen.setJoinStyle(Qt::RoundJoin);          // new TPenJoin { 1-pjBevel方头, 0-pjMiter尖头, 2-pjRound圆弧形 };
                    m_pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                    m_pen.Alignment = PenAlignment.Inset;                 // 闭合曲线内对齐  ????

                    int L = LM.Left;
                    int T = LM.Top;

                    x1 = x0 + Scale * LM.Left;       // _x
                    y1 = y0 + Scale * LM.Top;       // _y
                    w = Scale * LM.Width;
                    h = Scale * LM.Height;
                    RectangleF rect = new RectangleF((float)x1, (float)y1, (float)w, (float)h);
                    
                    if (LM.E_Type == TTFShape.ET_LINE)            // Line
                    {
                        this.DrawShapeLine(g,LM,m_pen,Scale, L, T, x0, y0);
                    }
                    else if (LM.E_Type == TTFShape.ET_RECTANGLE)    // Rectangle
                    {
                        this.DrawShapeRectangle(g,LM,m_pen,m_Fill_SolidBrush,rect);
                    }
                    else if (LM.E_Type == TTFShape.ET_ELLIPSE)       // Ellipse
                    {
                        this.DrawShapeEllipse(g, LM, m_pen,m_Fill_SolidBrush,rect);
                    }
                    else                            // 以下包括 polyline, polygon, bezier, path 的绘制
                    {
                        this.DrawShapePoly(g, LM, m_pen, m_Fill_SolidBrush, Scale, L, T, x0, y0);
                    }
                    g.TranslateTransform(0.0f, 0.0f);
                }
            }


            //绘制分隔线 ===============================================================
            if (symbol.SepInd > 0 && symbol.BaseMode == 0)              //绘制分隔线标记
            {
                int char_ID = symbol.SepInd;
                LT = this.SymTTFs[char_ID - 1];
                TTFShape LM;

                for (int j = 0; j < LT.Shapes.Length; j++)
                {
                    LM = LT.Shapes[j];
                    int T = LM.Top;
                    double Pen_Width = Size * (LM.PenWidth / 10.0 / 5.0);    //   笔宽参数
                    if (Pen_Width < 0.1)
                        Pen_Width = 0.1;
                    Pen L_pen  = new Pen(LC);
                    L_pen.Width =(float)Pen_Width;

                    //线条连接方式参数的设置  ===============================================
                    L_pen.LineJoin = LineJoin.Round;         // new TPenJoin { 1-pjBevel方头, 0-pjMiter尖头, 2-pjRound圆弧形 };

                    if (LM.E_Type == 1)                // Line
                    {
                        x1 = BaseX[4];
                        y1 = BaseY[4] + BaseY[4] / 2.0 + Size * (symbol.SepTop * 400 / 120.0);     //符号第1行与第2行之间的间距 ;  //   Scale * (LM.XY[0].y + T) +
                        y2 = y1;

                        if (char_ID == 1)    //直接拉伸
                        {
                            x2 = BaseX[5];
                            
                            g.DrawLine(L_pen,(int)x1, (int)y1, (int)x2, (int)y2);//  第1分隔线
                            if (appMode != 4)
                            {
                                y1 = y1 + (M_Height + fhjj) / 2.0;
                                y2 = y1;
                                g.DrawLine(L_pen,(int)x1, (int)y1, (int)x2, (int)y2); //  第2分隔线
                            }
                        }
                    }
                    else    // 以下包括 polyline, polygon, bezier, path 的绘制
                    {
                        int Plot_Num = 2;
                        do
                        {
                            if (LM.Points == null)
                                break;
                            int N = LM.Points.Length;
                            //QPainterPath path;        //定义路径
                            List<PointF> Points = new List<PointF>(N);

                            double ya = BaseY[4] + BaseY[4] / 4.0 + Size * (symbol.SepTop * 400 / 120.0);
                            if (Plot_Num == 2)
                            {
                                for (int z = 0; z < N; z++)
                                {
                                    double yy = ya + Scale * (LM.Points[z].Y + T);            //_y    //polygon的控制点X,Y座标
                                    double xx = BaseX[4] + (M_Width * LM.Points[z].X / 500);    //_x
                                    Points.Add(new PointF((float)xx, (float)yy));
                                }
                            }

                            if (appMode != 4 && Plot_Num == 1)
                            {
                                double yb = ya + BaseY[4] / 2.0 ;
                                for (int z = 0; z < N; z++)
                                {
                                    double yy = yb + Scale * (LM.Points[z].Y + T);            //_y    //polygon的控制点X,Y座标
                                    double xx = BaseX[4] + (M_Width * LM.Points[z].X / 500);    //_x
                                    Points.Add(new PointF((float)xx, (float)yy));
                                }
                            }

                            if (LM.E_Type == 8)    // 8- Polyline
                            {
                                
                                g.DrawLines(L_pen,Points.ToArray());        // 绘制曲线 0 - 张力系数
                            }
                            else
                            {
                                if (LM.E_Type == 9)          //  9-polygon
                                {

                                    //Points << Points[0];
                                    g.DrawPolygon(L_pen, Points.ToArray());
                                    //path.addPolygon(QPolygonF(Points));
                                }
                                else if (LM.E_Type == 14)     // 14 - Bezier
                                {
                                    //path.moveTo(Points[0]);
                                    //for (int LineNum = 0; LineNum < Points.count() / 3; LineNum++)
                                    //{
                                    //    path.cubicTo(Points[LineNum * 3 + 1], Points[LineNum * 3 + 2], Points[LineNum * 3 + 3]);
                                    //}
                                    g.DrawBeziers(L_pen,Points.ToArray());
                                }
                                else if (LM.E_Type == 25)     // 25-Path
                                {
                                    int Fg1, Fg2;
                                    bool BP = true;  // 曲线起点标记
                                    PointF? Begin_Point = null;
                                    PointF? curPoint= null;
                                    GraphicsPath path = new GraphicsPath();
                                    for (int z = 0; z < N - 1; z++)
                                    {
                                        Fg1 = LM.Flags[z];
                                        Fg2 = LM.Flags[z + 1];

                                        if (BP == true)    //  曲线起点标记
                                        {
                                            BP = false;
                                            Begin_Point = Points[z]; // 记录曲线的第一个点，曲线闭合时使用
                                            curPoint = Begin_Point;
                                        }

                                        if (Fg1 == 1)        // 不闭合当前图形即开始一个新图形
                                        {
                                            BP = true;
                                        }
                                        else if (Fg1 == 2)  // 闭合当前图形即开始一个新图形
                                        {
                                            if (Fg2 == 3)       // 闭合点后续点是 Bezier 曲线的控制点
                                            {
                                                path.AddBezier(curPoint.Value,Points[z + 1], Points[z + 2], Begin_Point.Value);
                                                z += 2;
                                            }
                                            path.CloseFigure();
                                            BP = true;
                                        }
                                        else if (Fg1 == 0)
                                        {
                                            if (Fg2 == 3) // 后续点是 Bezier 曲线的控制点
                                            {
                                                path.AddBezier(curPoint.Value,Points[z + 1], Points[z + 2], Points[z + 3]);
                                                curPoint = Points[z + 3];
                                                z += 2;
                                            }
                                            else
                                            {
                                                path.AddLine(curPoint.Value,Points[z + 1]);//在Path中加一条直线
                                                curPoint = Points[z + 1];
                                            }
                                        }
                                    }  //for(int z=0

                                    if ((int)LM.Flags[N - 1] == 2)
                                    {
                                        path.CloseFigure();
                                    }
                                    g.DrawPath(L_pen, path);
                                } //   E_Type == 25
                            }
                            Plot_Num--;
                        }
                        while (Plot_Num > 0);
                    }
                    g.TranslateTransform(0.0f, 0.0f);
                }
            }
            //===================================================================================

            RectangleF FG = new RectangleF((float)BaseX[4], (float)BaseY[4], (float)(BaseX[4] + M_Width), (float)(BaseY[4] + M_Height));
            Bitmap brushBmp = new Bitmap((int)M_Width, (int)M_Height); //定义返回图像
            
            Graphics painter = Graphics.FromImage(brushBmp);
            painter.FillRectangle(new SolidBrush(Color.FromArgb(0, 0, 0, 0)), 0, 0, brushBmp.Width, brushBmp.Height);

            painter.DrawImage(bitMap,0,0,FG,GraphicsUnit.Pixel);

            //边角处理
            int[] PA = { 1, 3, 5, 7 };              
            if (appMode != 3)
            {
                for (int i = 0; i < 4; i++)
                {
                    int k = PA[i];
                    RectangleF BaseRect = new RectangleF((float)BaseX[k], (float)BaseY[k], (float)(BaseX[k] + M_Width), (float)(BaseY[k] + M_Height));
                    painter.DrawImage(bitMap,0,0,BaseRect,GraphicsUnit.Pixel);
                }
                RectangleF rect = new RectangleF(0, 0, M_Width, M_Height);
                painter.DrawImage(bitMap,0,0,FG,GraphicsUnit.Pixel);
            }

            bitMap.Dispose();
            return brushBmp;
       }

       protected void DrawShapeLine(Graphics g, TTFShape shape, Pen pen, double scale,double L,double T,double x0,double y0)
       {

            double x1, y1, x2, y2;
            x1 = x0 + scale * (shape.Points[0].X + L);  // _x
            y1 = y0 + scale * (shape.Points[0].Y + T);  // _y
            x2 = x0 + scale * (shape.Points[1].X+  L);  // _x
            y2 = y0 + scale * (shape.Points[1].Y + T);  // _y
            g.DrawLine(pen, (float)x1, (float)y1, (float)x2, (float)y2);  
        }

        protected void DrawShapeRectangle(Graphics g,TTFShape shape, Pen pen, Brush brush,RectangleF rectF)
        {
            if (shape.Fill == 1)
            {
                g.FillRectangle(brush, rectF);
            }
            g.DrawRectangle(pen,rectF.X,rectF.Y,rectF.Width,rectF.Height);
        }

        protected void DrawShapeEllipse(Graphics g, TTFShape shape, Pen pen, Brush brush,RectangleF rect)
        {
            if (shape.Fill ==1)
            {
                g.FillRectangle(brush, rect);
            }
            g.DrawEllipse(pen, rect);
        }

        protected void DrawShapePoly(Graphics g, TTFShape shape,Pen pen, Brush bush,double scale,double L,double T, double x0,double y0)
        {
            int nPoints = shape.Points.Length;
            List<PointF> points = new List<PointF>(nPoints);
            for(int i=0; i<nPoints; i++)
            {
                double x = x0 + scale * (shape.Points[i].X + L);
                double y = y0 + scale * (shape.Points[i].Y + T);
                points.Add(new PointF((float)x, (float)y));
            }
            if(shape.E_Type == TTFShape.ET_POLYLINE)
            {
                g.DrawLines(pen, points.ToArray());

            }else if (shape.E_Type == TTFShape.ET_POLYGON)
            {
                g.DrawPolygon(pen, points.ToArray());
            }else if (shape.E_Type == TTFShape.ET_BEIZER)
            {
                g.DrawBeziers(pen, points.ToArray()); 
            }else if(shape.E_Type == TTFShape.ET_PATH)
            {
                GraphicsPath path = new GraphicsPath();
                int  Fg1, Fg2;
                bool BP = true;
                PointF? beginPoint = null;
                PointF? curPoint= null;
                for(int z=0; z<nPoints-1; z++)
                {
                   
                    Fg1 = shape.Flags[z];
                    Fg2 = shape.Flags[z + 1];
                    if (BP)
                    {
                        BP = false;
                        curPoint = points[z];
                        beginPoint = points[z];
                    }

                    if(Fg1 == 1)
                    {
                        BP = true;
                    }else if(Fg1 == 2)
                    {
                        if(Fg2 == 3)
                        {
                           
                            path.AddBezier(curPoint.Value, points[z + 1], points[z + 2], beginPoint.Value);
                            z += 2;
                        }
                        path.CloseFigure();
                        BP = true;

                    }else if(Fg1 == 0)
                    {
                        if(Fg2 == 3)
                        {
                            path.AddBezier(curPoint.Value, points[z + 1], points[z + 2], points[z + 3]);
                            curPoint = points[z + 3];
                            z += 2;
                        }
                        else
                        {
                            path.AddLine(curPoint.Value,points[z + 1]);
                            curPoint = points[z + 1];
                        }
                    }
                }
             
                if(shape.Flags[nPoints-1] == 2)
                {
                    path.CloseFigure();
                }
                g.DrawPath(pen, path);
                if (shape.Closed)
                {
                    if (shape.Fill == 1)
                    {
                        path.FillMode = FillMode.Winding;
                        g.FillPath(bush, path);
                    }
                }
            }
        }

        public List<string> SearchSymbolCode(string code)
        {
            List<string> result = new List<string>();
            if(this.Symbols == null)
            {
                return result;
            }

            for(int i=0; i<this.Symbols.Count; i++)
            {
                SymData symData = this.Symbols[i];
                string id = symData.ID;
                if (id.StartsWith(code))
                {
                    result.Add(id);
                }
            }
            result.Sort();
            return result;
        }

        public SymData? FindSymbol(string code)
        {
            int symIndex = FindSymbolIndex(code);
            if (symIndex < 0)
                return null;
            return Symbols[symIndex];
        }


        /// <summary>
        /// 创建符号图片，指定宽度，否是描边，背景色
        /// </summary>
        /// <param name="symcode"></param>
        /// <param name="width"></param>
        /// <param name="drawRect"></param>
        /// <param name="backColor">如果为空，使用系统默认背景色</param>
        /// <returns></returns>
        public  Image CreateSymbolImage(string symcode, int width,bool drawRect = false,Color? backColor=null)
        {
            int height = width;
            if (symcode.StartsWith("2"))
                height = width / 2;

            if(backColor == null)
              backColor = this.GetSymbolBuildinBackColor(symcode);

            Image image = this.CreateSymbolImage(symcode, width, null, backColor, true, true, true);
            Graphics g = null;
            if (image == null)
            {
                image = new Bitmap(width, height);
                g = Graphics.FromImage(image);
                g.FillRectangle(new SolidBrush(Color.White), 0, 0, width, height);
            }
            if (g == null)
            {
                g = Graphics.FromImage(image);
            };
            if (drawRect)
            {
                Pen pen = new Pen(Color.Black);
                g.DrawRectangle(pen, 0, 0, image.Width - 1, image.Height - 1);
            }
            return image;
        }

        /// <summary>
        /// 创建32x32的符号图片
        /// </summary>
        /// <param name="symcode"></param>
        /// <param name="drawRect"></param>
        /// <returns></returns>
        public Image CreateDefaultSymbolImage(string symcode,bool drawRect = false)
        {
            return this.CreateSymbolImage(symcode, 32,drawRect);
        }

        public Image CreateTransparentSymbolImage(string symcode,bool drawRect = false)
        {
            return this.CreateSymbolImage(symcode, 32, drawRect, Color.FromArgb(0, 255, 255, 255));
        }

       


       public  Image  CreateSymbolImage(string symCode, int size, Color? foreColor,Color? backColor,bool oPaque=true,bool predefined=true,bool antilias=true)
       {
            int symIndex = FindSymbolIndex(symCode);
            if (symIndex < 0)
               return null;

            if (backColor==null&&predefined)
            {
                Color? color = this.GetSymbolBuildinBackColor(symCode);
                if (color!=null)
                {
                    backColor = color.Value;
                }
                oPaque = true;
            }
            SymData symbol = this.Symbols[symIndex];
            byte symbolType = symbol.Type;
            if (string.Equals(symCode, "2180010"))
            {
                System.Console.WriteLine(symCode);
            }
            if(symbolType == SymbolTypes.Lithology)
            {
                return CreateLithologyImage(ref symbol, 0, size, foreColor, oPaque, backColor, antilias);
            }
            else
            {
                int iIndex2 = symbol.Index;
                int Box_In_SymIndex = this.FindSymFill(iIndex2);
                if (Box_In_SymIndex < 0)
                    return null;
                List<Color> newColorList = new List<Color>();
                if (foreColor != null)
                {
                    newColorList.Add(foreColor.Value);
                }
                else
                {
                    for (int i = 0; i < symbol.Colors.Length; i++)
                    {
                        newColorList.Add(symbol.Colors[i]);
                    }
                }
                SymFill symFill = this.SymFills[Box_In_SymIndex];
                return CreatePixmapSymbol(size, false, symFill.Flag, true, antilias, symFill, symIndex, newColorList, oPaque, backColor);

            }

       }

       private void DrawElemLine(Graphics g,SymElem symElem, ElemBrushs elemBrush,Pen pen, ElemColors elemColors, double Scale,double L,double T)
       {
            double x1, y1, x2, y2;
            x1 = symElem.Points[0].X * Scale + L;  // _x
            y1 = symElem.Points[0].Y * Scale + T;  // _y
            x2 = symElem.Points[1].X * Scale + L;  // _x
            y2 = symElem.Points[1].Y*  Scale + T;  // _y
            g.DrawLine(pen, (float)x1, (float)y1, (float)x2, (float)y2);
        }

        private void DrawElemRect(Graphics g, SymElem symElem,ElemBrushs elemBrushes,Pen pen,ElemColors elemColors,RectangleF rect)
        {
            if (symElem.FillMode == 1)    // 渐变填充
            {
                if (symElem.BrushStyle < 2)
                {
                    g.FillRectangle(elemBrushes.m_linearGradientBrush, rect);
                }
                else
                {
                    g.FillRectangle(elemBrushes.m_radialGradientBrush, rect);
                }
       
            }
            else                                // 纹理填充
            {
                if (symElem.BrushStyle > 1)
                {
                    g.FillRectangle(elemBrushes.m_HatchBrush, rect.X, rect.Y, rect.Width, rect.Height);
                }
                else if (symElem.BrushStyle == 1)
                {
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                }
                else
                {
                    g.FillRectangle(elemBrushes.m_Fill_SolidBrush, rect.X, rect.Y, rect.Width, rect.Height);
                }
            }
            g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);

        }

        private void DrawElemEllipse(Graphics g, SymElem symElem, ElemBrushs elemBrushes, Pen pen, ElemColors elemColors, RectangleF rect)
        {
           
            if (symElem.FillMode == 1)
            {
                if (symElem.BrushStyle < 2)
                {
                    g.FillEllipse(elemBrushes.m_linearGradientBrush,rect);
                }
                else
                {
                    g.FillEllipse(elemBrushes.m_radialGradientBrush,rect);
                }
                
            }
            else
            {
                if (symElem.BrushStyle > 1)
                {
                    g.FillEllipse(elemBrushes.m_HatchBrush,rect);
                }
                else if (symElem.BrushStyle == 1)
                {
                    g.FillEllipse(elemBrushes.m_Fill_SolidBrush, rect);
                }
                else
                {
                    g.FillEllipse(elemBrushes.m_Fill_SolidBrush,rect);
                }
            }
            g.DrawEllipse(pen,rect);

        }


        private void DrawElemPoly(Graphics painter, SymElem symElem,ElemBrushs elemBrushs, Pen pen, ElemColors elemColors,double Scale,double L,double T)
        {
            if (symElem.Points == null)
                return;

            int N = symElem.Points.Length;
           
            List<PointF> points = new List<PointF>(N);
            for (int z = 0; z < N; z++)
            {
                double  xx = (symElem.Points[z].X* Scale + L);  //_x
                double  yy = (symElem.Points[z].Y* Scale + T);  //_y    //polygon的控制点X,Y座标
                PointF pt = new PointF((float)xx, (float)yy);
                points.Add(pt);
            }
            if(symElem.E_Type == 8)
            {
                painter.DrawLines(pen, points.ToArray());

            }
            else if(symElem.E_Type == 9)
            {
                //painter.DrawPolygon(pen, points.ToArray());
                GraphicsPath path = new GraphicsPath();
                path.AddPolygon(points.ToArray());
                path.CloseFigure();

                if (symElem.IsSolid == true)
                {
                    if (symElem.FillMode == 1)
                    {
                        if (symElem.BrushStyle != 2 || symElem.BrushStyle != 3)
                        {
                            painter.FillPath(elemBrushs.m_linearGradientBrush, path);
                        }
                        else
                        {
                            painter.FillPath(elemBrushs.m_radialGradientBrush, path);
                        }
                    }
                    else
                    {
                        if (symElem.BrushStyle > 1)
                        {
                            painter.FillPath(elemBrushs.m_HatchBrush, path);
                        }
                        else if (symElem.BrushStyle == 1)
                        {
                            //painter->fillPath(path, Qt::NoBrush);
                        }
                        else
                        {
                            painter.FillPath(elemBrushs.m_Fill_SolidBrush, path);
                        }
                    }
                }

                painter.DrawPath(pen, path);

            }else if(symElem.E_Type == 14)
            {
               
                GraphicsPath path = new GraphicsPath();
                if (points != null && points.Count >= 4)
                {
                    path.AddBeziers(points.ToArray());
                    path.CloseFigure();

                    if (symElem.IsSolid == true)
                    {
                        if (symElem.FillMode == 1)
                        {
                            if (symElem.BrushStyle != 2 || symElem.BrushStyle != 3)
                            {
                                painter.FillPath(elemBrushs.m_linearGradientBrush, path);
                            }
                            else
                            {
                                painter.FillPath(elemBrushs.m_radialGradientBrush, path);
                            }
                        }
                        else
                        {
                            if (symElem.BrushStyle > 1)
                            {
                                painter.FillPath(elemBrushs.m_HatchBrush, path);
                            }
                            else if (symElem.BrushStyle == 1)
                            {
                                //painter->fillPath(path, Qt::NoBrush);
                            }
                            else
                            {
                                painter.FillPath(elemBrushs.m_Fill_SolidBrush, path);
                            }
                        }
                    }
                    painter.DrawPath(pen, path);
                }

            }
            else if(symElem.E_Type == 25)
            {
                GraphicsPath path = new GraphicsPath();
                int Fg1, Fg2;
                bool BP = true;  // 曲线起点标记
                PointF? beginPoint = null;
                PointF? curPoint = null;
                for(int z=0; z < N - 1; z++)
                {

                    Fg1 = symElem.Flags[z];
                    Fg2 = symElem.Flags[z + 1];

                    if(BP == true)
                    {
                        BP = false;
                        beginPoint = points[z];
                        curPoint = beginPoint;
                    }
                    if (Fg1 == 1)
                    {
                        BP = true;
                    }
                    else if (Fg1 == 2)
                    {
                        if (Fg2 == 3)
                        {

                            path.AddBezier(curPoint.Value, points[z + 1], points[z + 2], beginPoint.Value);
                            z += 2;
                        }
                        path.CloseFigure();
                        BP = true;

                    }
                    else if (Fg1 == 0)
                    {
                        if (Fg2 == 3)
                        {
                            path.AddBezier(curPoint.Value, points[z + 1], points[z + 2], points[z + 3]);
                            curPoint = points[z + 3];
                            z += 2;
                        }
                        else
                        {
                            path.AddLine(curPoint.Value, points[z + 1]);
                            curPoint = points[z + 1];
                        }
                    }
                }

                if ((int)symElem.Flags[N - 1] == 2)
                {
                    path.CloseAllFigures();
                }
                
                //Fill
                if (symElem.IsSolid == true)
                {
                    if (symElem.FillMode == 1)
                    {
                        if (symElem.BrushStyle != 2 || symElem.BrushStyle != 3)
                        {
                            painter.FillPath(elemBrushs.m_linearGradientBrush,path);
                        }
                        else
                        {
                            painter.FillPath(elemBrushs.m_radialGradientBrush,path);
                        }
                    }
                    else
                    {
                        if (symElem.BrushStyle > 1)
                        {
                            painter.FillPath(elemBrushs.m_HatchBrush,path);
                        }
                        else if (symElem.BrushStyle == 1)
                        {
                            //painter->fillPath(path, Qt::NoBrush);
                        }
                        else
                        {
                            painter.FillPath(elemBrushs.m_Fill_SolidBrush, path);
                        }
                    }
                }

                painter.DrawPath(pen, path);
            }
        }
       

        private void DrawElemText(Graphics g, SymElem symElem, ElemBrushs elemBrushes, Pen pen, ElemColors elemColors,double scale,RectangleF rect)
        {
            
            string fontFamily = symElem.FontName;
            float fontSize = (float)(symElem.FontSize * scale);
            if (fontSize <= 0) 
                fontSize = 1;

           
            pen.Color = symElem.FontColor;
            //            font.setPointSizeF(TSYM->Elems[j]->FontSize *Scale);
          
            int F_Style = symElem.FontStyle;
            FontStyle style = FontStyle.Regular;
            if ((F_Style & 1)>0)
            {

                style |= FontStyle.Bold;
            }
            if ((F_Style & 2) > 0)
            {
                style |= FontStyle.Italic;
            }
            if ((F_Style & 4)>0)
            {
                style |= FontStyle.Underline;
            }
            if ((F_Style & 8)>0)
            {
                style |= FontStyle.Strikeout;
            }
            Font font = new Font(fontFamily, fontSize,style);

            //painter->setFont(font);
            //painter->setBrush(Qt::NoBrush);
            //painter->setBackgroundMode(Qt::TransparentMode);

            //painter->drawText(rect, Qt::AlignCenter, pElem->TextStr);
            g.DrawString(symElem.TextStr, font, elemBrushes.m_Fill_SolidBrush, rect);
        }


        protected Bitmap CreatePixmapSymbol(double TrackWidth,bool BX,bool Flag_BJ, bool Use_Color, bool Smooth_Mode,SymFill TSYM,
            int FHSIndex,List<Color> NEW_Color_List,
            bool bOpaque,Color? BColor)
        {

            if (TSYM.Elems.Length < 1)
            {
                return null;
            }
            double Scale = TrackWidth * 1.0 / TSYM.Width;
            double x1, y1, x2, y2, w, h;
            double Doc_Width = TrackWidth;
            double Doc_Height = TSYM.Height * Scale;
            int W = (int)Doc_Width;
            int H = (int)Doc_Height;
            double[] BaseX = new double[9];
            double[] BaseY = new double[9];
            double Factor = 3;
            int k = 0;


            // 计算3×3的范围
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    BaseX[k] = j * Doc_Width;
                    BaseY[k] = i * Doc_Height;
                    k++;
                }
            }

            Bitmap bitmap = new Bitmap((int)Math.Round(Factor * Doc_Width), (int)Math.Round(Factor * Doc_Height));
            Graphics g = Graphics.FromImage(bitmap);
            Rectangle imageRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            if(BColor == null)
            {
                BColor = DefaultBackgroundColor;
            }
            if (bOpaque)
            {
                SolidBrush brush = new SolidBrush(BColor.Value);
                g.FillRectangle(brush, imageRect);
            }
            else
            {
                Color color = Color.FromArgb(0, 0, 0,0);
                SolidBrush brush = new SolidBrush(color);
                g.FillRectangle(brush, imageRect);
            }

            g.TranslateTransform(0, 0);
            g.ScaleTransform(1, 1);

            if (Smooth_Mode)
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.SmoothingMode = SmoothingMode.AntiAlias;
            }

            for(int j=0; j<TSYM.Elems.Length; j++)
            {
                int Px = (int)BaseX[4]; //Doc_Width
                int Py = (int)BaseY[4]; //Doc_Height
                int L = (int)((TSYM.Elems[j].Left * Scale) + Px);
                int T = (int)((TSYM.Elems[j].Top * Scale) + Py);
                x1 = TSYM.Elems[j].Left * Scale + Px;       // _x
                y1 = TSYM.Elems[j].Top * Scale + Py;       // _y
                x2 = TSYM.Elems[j].Right * Scale + Px;       // _x
                y2 = TSYM.Elems[j].Bottom * Scale + Py;       // _y
                w = TSYM.Elems[j].Width * Scale;
                h = TSYM.Elems[j].Height * Scale;

                RectangleF rectF = new RectangleF((float)x1, (float)y1, (float)w, (float)h);
                ElemColors elemColors  =  GenerateElemColor(TSYM.Elems[j], TSYM, NEW_Color_List);
                ElemBrushs elemBrushed =  GenerateElemBrush(TSYM.Elems[j], elemColors, x1, y1, w, h);
                Pen pen = GeneratePen(TSYM.Elems[j], elemColors, Scale);

                SymElem symElem = TSYM.Elems[j];
                if(symElem.E_Type == 1)
                {
                    this.DrawElemLine(g, symElem, elemBrushed, pen, elemColors, Scale, L, T);
                }else if(symElem.E_Type == 2)
                {
                    this.DrawElemRect(g, symElem, elemBrushed, pen, elemColors, rectF);
                }else if(symElem.E_Type == 4)
                {
                    this.DrawElemEllipse(g, symElem, elemBrushed, pen, elemColors, rectF);
                }else if(symElem.E_Type == 10)
                {
                    this.DrawElemText(g, symElem, elemBrushed, pen, elemColors, Scale, rectF);
                }
                else
                {
                    this.DrawElemPoly(g, symElem, elemBrushed, pen, elemColors, Scale, L, T);
                }
                g.TranslateTransform(0, 0);
            }

           


            RectangleF OR = new RectangleF();
            Bitmap brushBmp = new Bitmap(W, H);
            Graphics painter = Graphics.FromImage(brushBmp);
            painter.FillRectangle(new SolidBrush(Color.FromArgb(0, 255, 255, 255)), new RectangleF(0, 0, W, H));
            if (Flag_BJ)
            {
                for (int i = 0; i < 9; i++)
                {
                    if (i != 4)
                    {
                        int left = ((int)BaseX[i]);
                        int top = ((int)BaseY[i]);
                        int right = ((int)OR.Left + W);
                        int bottom = ((int)OR.Top + H);
                        OR = new RectangleF(left, top, W, H);
                        painter.DrawImage(bitmap, 0, 0, OR, GraphicsUnit.Pixel);
                    }
                }
            }
           

            RectangleF centerRectF = new RectangleF((float)BaseX[4], (float)BaseY[4], W, H);
            painter.DrawImage(bitmap, 0, 0, centerRectF, GraphicsUnit.Pixel);

            bitmap.Dispose();
            return brushBmp;

  
        }


        internal ElemBrushs GenerateElemBrush(SymElem pElem,ElemColors elemColors,double x1,double y1,double w,double h)
        {
            ElemBrushs elemBrush = new ElemBrushs();
            Color Color_FC, Color_BC, Center_Color;
            Color[] colors = new Color[4];

            PointF P1, P2;
            RectangleF Rect_Brush;

            byte Brush_Transparency = elemColors.m_fc.A;
            if(pElem.BrushStyle == 1&& pElem.FillMode == 0)
            {
                Brush_Transparency = 0;
            }

            Color_FC = Color.FromArgb(Brush_Transparency,elemColors.m_fc.R,elemColors.m_fc.G,elemColors.m_fc.B);
            Color_BC = elemColors.m_bc;
            if(pElem.FillMode==0&&pElem.BrushStyle > 1)
            {
                Color_BC = Color.FromArgb(0, Color_BC);
            }
            else
            {
                Color_BC = Color.FromArgb(255,Color_BC);
            }


            if(pElem.FillMode == 1)
            {
                Rect_Brush = new RectangleF((float)x1, (float)y1, (float)w, (float)h);
                P1 = new PointF((float)x1, (float)y1);

                for (int z=0; z<4; z++)
                {
                    colors[z] = Color.FromArgb(255, elemColors.m_bc);
                }
                Center_Color = Color.FromArgb(255, elemColors.m_bc);

                if (pElem.BrushStyle == 0)
                {
                    P2 = new PointF((float)(x1 + w), (float)y1);
                    elemBrush.m_linearGradientBrush = new LinearGradientBrush(P1, P2, elemColors.m_fc, elemColors.m_bc);

                }else if(pElem.BrushStyle == 1)
                {
                    P2 = new PointF((float)x1, (float)(y1 + h));
                    elemBrush.m_linearGradientBrush = new LinearGradientBrush(P1, P2, elemColors.m_fc, elemColors.m_bc);

                }else if(pElem.BrushStyle == 2)
                {
                    PointF pt = new PointF((float)(x1 + w / 2.0f), (float)(y1 + h / 2.0f));

                    GraphicsPath rectPath = new GraphicsPath();
                    rectPath.AddRectangle(Rect_Brush);
                    PathGradientBrush brush = new PathGradientBrush(rectPath);
                    brush.CenterPoint = pt;
                    Color[] surroundColors = { Color.FromArgb(255, elemColors.m_bc) };
                    brush.SurroundColors = surroundColors;
                    elemBrush.m_radialGradientBrush = brush;

                }else if (pElem.BrushStyle == 3) {

                    PointF pt = new PointF((float)(x1 + w / 2.0f), (float)(y1 + h / 2.0f));

                    GraphicsPath rectPath = new GraphicsPath();
                    rectPath.AddEllipse(Rect_Brush);
                    PathGradientBrush brush = new PathGradientBrush(rectPath);
                    brush.CenterPoint = pt;
                    Color[] surroundColors = { Color.FromArgb(255, elemColors.m_bc) };
                    brush.SurroundColors = surroundColors;
                    elemBrush.m_radialGradientBrush = brush;

                }else if( pElem.BrushStyle > 3)
                {
                    if (pElem.BrushStyle == 4)        // gsTopLeft
                    {
                        P1 = new PointF((float)x1,(float)y1);
                        P2 = new PointF((float)(x1 + w), (float)(y1 + h));
                    }
                    else if (pElem.BrushStyle == 5)// gsTopRight
                    {
                        P1 = new PointF((float)(x1 + w), (float)y1);
                        P2 = new PointF((float)(x1), (float)(y1 + h));
                    }
                    else if (pElem.BrushStyle == 6)// gsBottomLeft
                    {
                        P1 = new PointF((float)(x1), (float)(y1 + h));
                        P2 = new PointF((float)(x1 + w), (float)y1);
                    }
                    else // gsBottomRight
                    {
                        P1 = new PointF((float)(x1 + w),(float)(y1 + h));
                        P2 = new PointF((float)x1, (float)y1);
                    }
                    elemBrush.m_linearGradientBrush = new LinearGradientBrush(P1, P2, elemColors.m_fc, elemColors.m_bc);
                }
            }
            elemBrush.m_HatchBrush = new HatchBrush(HatchStyle.DiagonalBrick, Color_FC);
            elemBrush.m_Fill_SolidBrush = new SolidBrush(Color_FC);
            return elemBrush;
        }


        internal Pen  GeneratePen(SymElem symElem,ElemColors elemColor,double scale)
        {
            byte penTransparency = 255;
            byte brushTransparency = 255;

            if(symElem.BrushStyle == 1&&symElem.FillMode==0)
            {
                brushTransparency = 0;
            }
            if(symElem.PenStyle == 5)
            {
                penTransparency = 0;
            }

            double Pen_Width = symElem.PenWidth / 10.0 * scale;  //   笔宽参数
            if (Pen_Width < 0.1)
            {
                Pen_Width = 0.1;
            }
            Color lineColor = Color.FromArgb(penTransparency, elemColor.m_lc);
            Pen pen = new Pen(new SolidBrush(lineColor));
            pen.Width = (float)Pen_Width;
            int lineCap = symElem.PenEndCap;
            LineCap lcap = ConvertCap(lineCap);
            pen.EndCap = lcap;
            if(symElem.PenStyle <= 5)
            {
                DashStyle? dashStyle = ConvertDashStyle(symElem.PenStyle);
                if(dashStyle!= null)
                {
                    pen.DashStyle = dashStyle.Value;
                }
            }else if (symElem.PenStyle >= 6)
            {
                string dashStr = symElem.DashStr;
                string[] sValues = dashStr.Split(',');
                float[] values = new float[sValues.Length];
                for(int i=0; i<sValues.Length; i++)
                {
                    values[i] = float.Parse(sValues[i]);
                }
                pen.DashPattern = values;
            }
            pen.LineJoin = ConvertLineJoin(symElem.PenJoin);
            return pen;
        }


        internal LineJoin ConvertLineJoin(int join)
        {
            LineJoin lj = LineJoin.Bevel;
            if(join == 0)
            {
                lj = LineJoin.Miter;
                return lj;
            }
            if(join == 1)
            {
                lj = LineJoin.Bevel;
                return lj;
            }
            lj = LineJoin.Round;
            return lj;
        }



        internal DashStyle? ConvertDashStyle(int style)
        {
            DashStyle? result = null;
            if(style == 5)
            {
                result = null;
                return result;
            }
            if(style == 0)
            {
                result = DashStyle.Solid;
                return result;
            }
            if(style == 1)
            {
                result = DashStyle.Dash;
                return result;
            }
            if(style == 2)
            {
                result = DashStyle.Dot;
                return result;
            }
            if(style == 3)
            {
                result = DashStyle.DashDot;
                return result;
            }
            if(style == 4)
            {
                result = DashStyle.DashDotDot;
                return result;
            }
            if(style == 6)
            {
                result = DashStyle.Custom;
                return result;
            }
            return result;
        }

        internal LineCap ConvertCap(int lineCap)
        {
            //qt 定义转化成LineCap
            LineCap ccap = LineCap.Flat;
            if(lineCap == 0x00)
            {
                ccap = LineCap.Flat;
                return ccap;
            }
            if(lineCap == 0x10)
            {
                ccap = LineCap.Square;
                return ccap;
            }
            if(lineCap == 0x20)
            {
                ccap = LineCap.Round;
                return ccap;
            }
            return ccap;
        }




        internal ElemColors GenerateElemColor(SymElem pElem,SymFill symFill,List<Color> colors)
        {
            ElemColors result = new ElemColors();
            result.m_lc = pElem.PenColor;
            result.m_fc = pElem.GradColor1;
            result.m_bc = pElem.GradColor2;
            if(colors.Count == 1)
            {
                result.m_lc = colors[0];
                result.m_bc = colors[0];
                result.m_fc = colors[0];

            }else if(colors.Count > 1)
            {
                result.m_lc = GetUserColor(symFill, result.m_lc, colors);
                result.m_fc = GetUserColor(symFill, result.m_fc, colors);
                result.m_bc = GetUserColor(symFill, result.m_bc, colors);
            }
            return result;
        }

        Color  GetUserColor(SymFill TSYM, Color oldColor,List<Color> newcolors)
        {
            Color cc = oldColor;
            if (newcolors.Count > 0 && newcolors.Count == TSYM.Colors.Length)
            {
                for (int i = 0; i < TSYM.Colors.Length; i++)
                {
                    if (cc == (TSYM.Colors[i]) && cc != newcolors[i])
                    {
                        cc = newcolors[i];
                        break;
                    }
                }
            }
            return cc;
        }
  

        protected int FindSymFill(int no)
        {
            int result = -1;
            for (int i = 0; i < this.SymFills.Count; i++)
            {
                if (!this.SymFills[i].IsDel)
                {
                    if (no == this.SymFills[i].No)
                    {
                        result = i;
                        break;
                    }
                }
            }
            return result;
        }
        
    }

    public struct GeoSymbolFileHeader
    {
        public const int MAGICNUMBER = ~0x4c444f43;

        public int magic;
        public int ver;
        public int endian;
        public int dbid;
        public int dbver;
        public int count;
    }


    public class SymbolNode
    {
        public bool   m_bDir;         // 是否目录节点
        public int    m_iLevel;       // 节点级别(0-4)
        public string m_sCode;        // 节点代码
        public string m_sName;        // 节点名称
        public string m_sNameCN;      // 节点中文名称

        public SymbolNode    m_parent;
        public List<SymbolNode> m_children;

        public SymbolNode()
        {
            m_children = new List<SymbolNode>();
        }
    }

    public class GeoSymbolRepositoryLoader
    {
        private GeoSymbolFileHeader _fileHeader;
        public  GeoSymbolRepository LoadFromFile(string pathFileName,Encoding encoding)
        {
            GeoSymbolRepository result = null;
            if (!File.Exists(pathFileName))
            {
                return null;
            }

            using (FileStream s = new FileStream(pathFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (BinaryReader reader = new BinaryReader(s, encoding))
                {
                    result = LoadFrom(reader,encoding);
                }
            }
            return result;
        }

        protected GeoSymbolRepository LoadFrom(BinaryReader reader,Encoding encoding)
        {
            GeoSymbolRepository repository = null;
            _fileHeader = ReadHeader(reader,encoding);
            IDFMT idFmt = ReadIDFMT(reader, encoding);
            SymData[] symDatas = this.ReadSymDataArray(reader, encoding);
            SymbolNode[] symbolNodes = this.ReadSymNodes(symDatas, reader, encoding);
            List<SymFill> symFills = this.ReadGeoFillSignData(symDatas,reader, encoding);
            List<SymTTF> symTTFs = this.ReadTTFs(reader, encoding);


            repository = new GeoSymbolRepository();
            repository.Symbols = new List<SymData>(symDatas);
            repository.SymFills = symFills;
            repository.SymTTFs = symTTFs;
            repository.SymbolNodes = new List<SymbolNode>(symbolNodes);
            
            return repository;
        }

        protected Color ReadColor(BinaryReader reader,Encoding encoding)
        {
            uint iColor = reader.ReadUInt32();  
            int a = (int)(iColor >> 24);
            int r = (int)((iColor >> 16) & 0xFF);
            int g = (int)((iColor >> 8) & 0xFF);
            int b = (int)(iColor & 0xFF);
            if(_fileHeader.dbver == 1)
            {
                a = 255;
            }
            Color color = Color.FromArgb(a, r, g, b);
            return color;
        }


        protected List<SymTTF> ReadTTFs(BinaryReader reader,Encoding encoding)
        {
            List<SymTTF> symTTFs = new List<SymTTF>();

            byte Num;
            int ptx, pty;
            SymTTF pTTF;
            TTFShape pTTFBase;

            Num = reader.ReadByte();
            for(int i=0; i<255; i++)
            {
                pTTF = new SymTTF();
                pTTF.No = reader.ReadByte();
                pTTF.nShape = reader.ReadUInt16();
                List<TTFShape> shapes = new List<TTFShape>(pTTF.nShape);
                for(int j=0; j<pTTF.nShape; j++)
                {
                    pTTFBase = new TTFShape();
                    pTTFBase.E_Type = reader.ReadByte();
                    pTTFBase.Top = reader.ReadUInt16();
                    pTTFBase.Left = reader.ReadUInt16();
                    pTTFBase.Right = reader.ReadUInt16();
                    pTTFBase.Bottom = reader.ReadUInt16();
                    pTTFBase.Width = (ushort)(pTTFBase.Right - pTTFBase.Left);
                    pTTFBase.Height = (ushort)(pTTFBase.Bottom - pTTFBase.Top);

                    pTTFBase.PenStyle = reader.ReadByte();
                    pTTFBase.PenWidth = reader.ReadSingle();
                    pTTFBase.Fill = reader.ReadByte();
                   
                    switch (pTTFBase.E_Type)
                    {
                        case 1:   // 1 Line
                        case 8:   // 8 Polyline
                        case 9:   // 9 Polygon
                        case 14:  // 14 Bezier
                        case 25:   // path
                            {
                                pTTFBase.Closed = reader.ReadBoolean();

                                int ptNum = reader.ReadInt32();
                                List<PointF> points = new List<PointF>(ptNum);
                                List<byte> Flags = new List<byte>(ptNum);

                                for (int k = 0; k < ptNum; k++)
                                {
                                    ptx = reader.ReadInt32();
                                    pty = reader.ReadInt32();
                                    points.Add(new PointF(ptx, pty));

                                    byte flag = reader.ReadByte();
                                    Flags.Add(flag);
                                }
                                pTTFBase.Points = points.ToArray();
                                pTTFBase.Flags = Flags.ToArray();
                                break;
                            }
                        default:
                            break;
                    }
                    shapes.Add(pTTFBase);
                }
                pTTF.Shapes = shapes.ToArray();
                symTTFs.Add(pTTF);
            }
            return symTTFs;
        }


        protected int FindIndex(string code,SymData[] symDataList)
        {
            int result = -1;
            for(int index = 0; index < symDataList.Length; index++)
            {
                if (string.Equals(symDataList[index].ID, code,StringComparison.OrdinalIgnoreCase))
                {
                    result = index;
                    break;
                }
            }
            return result;
        }

        protected SymbolNode[] ReadSymNodes(SymData[] symDataList,BinaryReader reader, Encoding encoding)
        {
            List<SymbolNode> symbolNodes = new List<SymbolNode>();
            byte bExist = reader.ReadByte();
            if (!(bExist >0))
            {
                return symbolNodes.ToArray();
            }
            int nNodeCount = reader.ReadInt32();

            SymbolNode[] symLevelNodes = { null, null, null };

            for(int i=0; i < nNodeCount; i++)
            {
                string id = this.ReadString(reader, encoding);
                string mc = this.ReadString(reader, encoding);
                string me = this.ReadString(reader, encoding);
                byte nodeLevel = reader.ReadByte();
                byte nodeType = reader.ReadByte();

                SymbolNode node = new SymbolNode();
                node.m_sCode = id;
                node.m_sName = me;
                node.m_sNameCN = mc;
                node.m_iLevel = nodeLevel;
                node.m_bDir = (nodeType == 0);

                int index = FindIndex(id, symDataList);

                if (node.m_bDir)
                {
                    if(node.m_iLevel == 0)
                    {
                        symbolNodes.Add(node);
                        symLevelNodes[0] = node;
                        node.m_parent = null;
                    } else if (node.m_iLevel == 1)
                    {
                        symLevelNodes[0].m_children.Add(node);
                        symLevelNodes[1] = node;
                        node.m_parent = symLevelNodes[0];
                    }
                    else if(node.m_iLevel == 2)
                    {
                        symLevelNodes[1].m_children.Add(node);
                        symLevelNodes[2] = node;
                        node.m_parent = symLevelNodes[1];
                    }
                    else
                    {
                        System.Console.WriteLine(String.Format("bad node,{0}", node.m_sNameCN));
                    }

                }
                else
                {
                    if(index!= -1)
                    {
                        symLevelNodes[node.m_iLevel - 1].m_children.Add(node);
                        node.m_parent = symLevelNodes[node.m_iLevel - 1];
                    }
                }

                if(index!= -1)
                {
                    symDataList[index].NameCN = node.m_sNameCN;
                    symDataList[index].Name = node.m_sName;
                    symDataList[index].isUsed = true;
                }
            }

            return symbolNodes.ToArray();

        }

        protected SymData[] ReadSymDataArray(BinaryReader reader,Encoding encoding)
        {
            List<SymData> result = new List<SymData>();
            int symCount = reader.ReadInt32();
            for(int i=0; i<symCount; i++)
            {
                SymData symData = new SymData();

                symData.ID = this.ReadString(reader, encoding).Trim();
                symData.Type = reader.ReadByte();
                symData.SysFlag = reader.ReadByte();
                symData.Size = reader.ReadSingle();

                byte nColor = reader.ReadByte();
                List<Color> colors = new List<Color>();
                for (sbyte j = 0; j < nColor; j++)
                {
                    Color color =  ReadColor(reader, encoding);
                    colors.Add(color);
                }
                symData.Colors = colors.ToArray();
                symData.m_bgCol = Color.FromArgb(0, 0, 0);

                if(_fileHeader.dbver > 1)
                {
                    symData.m_bgCol = ReadColor(reader,encoding);
                }
                if(symData.Type > 0)
                {
                    symData.Index = reader.ReadUInt16();
                    symData.Index2 = reader.ReadUInt16();
                    symData.PenWidth = reader.ReadByte();
                    symData.BaseMode = 2;
                    if(symData.Type == 1)    // 矢量
                    {
                        symData.DrawMode = 2;
                    }
                    else if(symData.Type == 2) //单个符号
                    {
                        symData.DrawMode = 3;
                    }
                    else if (symData.Type == 3)  //位图符号
                    {
                        symData.DrawMode = 2; 
                    }
                }
                else
                {
                    symData.SepInd = reader.ReadByte();
                    symData.SepTop = reader.ReadSByte();
                    symData.BaseMode = reader.ReadByte();
                    symData.DrawMode = reader.ReadByte();
                    symData.RowSpace = reader.ReadSByte();
                    symData.Width = reader.ReadByte();
                    symData.nTTF = reader.ReadByte();
                    List<SymChar> ttfs = new List<SymChar>();
                    for(int j=0; j<14; j++)
                    {
                        ttfs.Add(new SymChar());
                    }
                    for(int j=0; j<symData.nTTF; j++)
                    {
                        SymChar char1 = new SymChar();
                        char1.CID = reader.ReadByte();
                        char1.Left = reader.ReadSByte();
                        char1.Width = reader.ReadByte();

                        SymChar char2 = new SymChar();
                        char2.CID = reader.ReadByte();
                        char2.Left = reader.ReadSByte();
                        char2.Width = reader.ReadByte();
                        ttfs[j] = char1;
                        ttfs[j + 7] = char2;
                    }
                    symData.TTFs = ttfs.ToArray();
                }
                if (_fileHeader.dbver >= 3)
                {
                    symData.lithgrain = reader.ReadByte();
                    symData.res = reader.ReadInt32();
                }
                else
                {
                    symData.lithgrain = 30;
                    symData.res = 0;
                }

                result.Add(symData);
            }
            return result.ToArray();
        }

        private int statUseFrequency(bool type,int no,SymData[] symDataList)
        {
            int counter = 0;
            for(int i=0; i < symDataList.Length; i++)
            {
                if (symDataList[i].Index == no)
                    counter++;
            }
            return counter;
        }


        protected List<SymFill> ReadGeoFillSignData(SymData[] symDataList,BinaryReader reader,Encoding encoding){

            List<SymFill> result = new List<SymFill>();

            int nSym, Num, E_Num, ptx, pty;
            SymFill pFill;
            SymElem pElem;
            Color color;
            byte byteTmp;

            int numStart = 0;   // 符号起始索引(未用)
            nSym = reader.ReadInt32();
            numStart = reader.ReadInt32();
            
            //printf("number of fill symbols : %d\n", nSym);
            int cnt = 0;
            for(int i=0; i<nSym; i++)
            {
                pFill = new SymFill();

                pFill.IsDel = false;
                pFill.No = reader.ReadInt32();
                pFill.nUsed = reader.ReadByte();

                pFill.Ctype = this.ReadString(reader, encoding);

                pFill.Type = reader.ReadBoolean();
                pFill.Size = reader.ReadInt32();
                pFill.Lock = reader.ReadBoolean();
                pFill.Flag = reader.ReadBoolean();
                pFill.Width = reader.ReadInt32();
                pFill.Height = reader.ReadInt32();
                pFill.nUsed = (byte)statUseFrequency(pFill.Type, pFill.No, symDataList);

                Num = reader.ReadInt32();
                List<Color> colors = new List<Color>();
                for(int j=0; j<Num; j++)
                {
                   color = ReadColor(reader, encoding);
                   colors.Add(color);
                }
                pFill.Colors = colors.ToArray() ;

                Num = reader.ReadInt32();
                List<SymElem> elemets = new List<SymElem>(Num);
                for(int j=0; j<Num; j++)
                {
                    pElem = new SymElem();
                    pElem.E_Type = reader.ReadByte();
                    pElem.BX = reader.ReadBoolean();
                    pElem.UseBk = reader.ReadBoolean();
                    pElem.Level = reader.ReadByte();
                    pElem.Top = reader.ReadInt32();
                    pElem.Left = reader.ReadInt32();
                    pElem.Width = reader.ReadInt32();
                    pElem.Height = reader.ReadInt32();

                    pElem.Right = pElem.Left + pElem.Width;
                    pElem.Bottom = pElem.Top + pElem.Height;


                    switch (pElem.E_Type)
                    {
                        case 2:
                        case 4:
                            pElem.PenStyle = reader.ReadByte();
                            pElem.PenColor = this.ReadColor(reader, encoding);
                            pElem.PenWidth = reader.ReadInt32();
                            pElem.PenEndCap = reader.ReadByte();
                            pElem.PenJoin = reader.ReadByte();

                            pElem.FillMode = reader.ReadByte();
                            pElem.BrushStyle = reader.ReadByte();

                            pElem.GradColor1 = ReadColor(reader, encoding);
                            pElem.GradColor2 = ReadColor(reader, encoding);
                            break;
                        case 1: //line
                        case 8: //ployline
                        case 9: //ploygon
                        case 14://bezier
                        case 25: //path
                            {
                                pElem.PenStyle = reader.ReadByte();
                                pElem.PenColor = this.ReadColor(reader, encoding);

                                pElem.PenWidth = reader.ReadInt32();
                                pElem.PenEndCap = reader.ReadByte();
                                pElem.PenJoin = reader.ReadByte();

                                pElem.FillMode = reader.ReadByte();
                                pElem.BrushStyle = reader.ReadByte();

                                pElem.GradColor1 = this.ReadColor(reader, encoding);
                                pElem.GradColor2 = this.ReadColor(reader, encoding);
                                pElem.IsSolid = reader.ReadBoolean();


                                if (pFill.No == 72 || pFill.No == 113 || pFill.No == 99 || pFill.No == 491 || pFill.No == 516 || pFill.No == 524) 
                                    pElem.IsSolid = false;

                                if(pElem.IsSolid== false)
                                {
                                    pElem.FillMode = 0;
                                    pElem.BrushStyle = 1;
                                }
                                pElem.DashStr = this.ReadString(reader, encoding);

                                E_Num = reader.ReadInt32();
                                List<PointF> points = new List<PointF>();
                                List<byte> flags = new List<byte>();
                                for(int k=0; k<E_Num; k++)
                                {
                                    int px = reader.ReadInt32();
                                    int py = reader.ReadInt32();
                                    PointF p = new PointF(px, py);
                                    points.Add(p);
                                    byteTmp = reader.ReadByte();
                                    flags.Add(byteTmp);
                                   
                                }
                                pElem.Points = points.ToArray();
                                pElem.Flags = flags.ToArray();
                                break;
                            }

                        case 10:
                            {
                                pElem.FontName = this.ReadString(reader, encoding);
                                pElem.FontSet = reader.ReadByte();
                                pElem.FontSize = reader.ReadInt32();
                                pElem.FontStyle = reader.ReadByte();
                                pElem.FontColor = this.ReadColor(reader, encoding);
                                pElem.TextStr = this.ReadString(reader, encoding);
                                break;
                            }
                    }

                    elemets.Add(pElem);
                }
                pFill.Elems = elemets.ToArray();
                if (_fileHeader.dbver >= 3)
                {
                    pFill.res = reader.ReadInt32();
                }
                else
                {
                    pFill.res = 0;
                }
                result.Add(pFill);
            }
            return result;
        }


        protected string ReadString(BinaryReader reader, Encoding encoding)
        {

            //int length = reader.ReadInt16();
            //StringBuilder sb = new StringBuilder(length);
            //for(uint i=0; i<length; i++)
            //{
            //    sb.Append(reader.ReadByte());
            //}
            //return sb.ToString();
            string result = reader.ReadString();
            if (!string.IsNullOrEmpty(result))
            {
                if (result.Length > 0) {
                    if (result[result.Length - 1] == 0)
                    {
                        result = result.Substring(0, result.Length - 1);
                    }
                }
            }
            return result;
        }

        internal IDFMT ReadIDFMT(BinaryReader reader, Encoding encoding)
        {
            IDFMT idFmt= new IDFMT();
            idFmt.ID_Head = this.ReadString(reader, encoding);
            idFmt.ID_Format = this.ReadString(reader, encoding);
            byte[] bytes = reader.ReadBytes(24);
            return idFmt;
        }

        protected GeoSymbolFileHeader ReadHeader(BinaryReader reader,Encoding encoding)
        {
            //little-endian read,but dot know how;

            int headSize = 32;
            byte[] buf = reader.ReadBytes(headSize);
            if (buf.Length != headSize)
                throw new FormatException(String.Format("Format Error,header size is insufficient"));

            int[] values = new int[6];
            int[] four = new int[4];

            for(int i=0; i<values.Length; i++)
            {
                four[0] = (buf[4 * i + 0] & 0xff);
                four[1] = (buf[4 * i + 1] & 0xff);
                four[2] = (buf[4 * i + 2] & 0xff);
                four[3] = (buf[4 * i + 3] & 0xff);
                int v1 = four[0] << 24;
                int v2 = four[1] << 16;
                int v3 = four[2] << 8;
                int val = v1|v2|v3|four[3];
                values[i] = val;
            }
            GeoSymbolFileHeader header = new GeoSymbolFileHeader();
            header.magic = values[0];
            header.ver = values[1];
            header.endian = values[2];
            header.dbid = values[3];
            header.dbver = values[4];
            header.count = values[5];

            if(header.magic!= GeoSymbolFileHeader.MAGICNUMBER)
            {
                throw new FormatException(String.Format("Format Error, invalid geo symbol File"));
            }
            if(header.ver!= 1)
            {
                throw new FormatException(String.Format("Format Error, not supported version({0})",header.ver));
            }
            if (header.dbid != 32)
            {
                throw new FormatException(String.Format("Format Error, dbid({0} is not correct",header.dbid));
            }
            if (header.dbver > 7 || header.dbver <= 0)
            {
                throw new FormatException(String.Format("Format Error,dbver version {0} not supported",header.dbver));
            }
            return header;
        }
    }


}
