using System;
using System.Collections.Generic;
using System.Drawing;


namespace Tet.GeoSymbol
{
    public sealed class SymbolTypes
    {
        public const byte Lithology = 0;


    }

    internal struct IDFMT
    {
        public string ID_Head;      // 编码的前缀 如：SY
        public string ID_Format;    // 编码格式字符 如：######
        public string ID_fGf;       // 编码分隔字符 包括：" ", ".", ":", "-", ","
        public string ID_FullFormat;// 编码格式字符 如：#.#.#.###
        public int ID_Len;       // 格式字符的长度 如：###### ＝ 6
        public int[] IDfmt;        // 数组大小9
        public int[] IDfmt_New;    // 数组大小9
        public int ID_fgf;       // 旧的分隔符序号
        public int ID_fgf_New;   // 新的分隔符序号
    };

    // 岩性符号中单个符号
    public struct SymChar
    {
        public byte CID;         // 岩性基本符号的序号（0-255）
        public sbyte Left;        // 符号偏移
        public byte Width;       // 符号宽度
    };

    // 符号
    public struct SymData
    {
        public string ID;           // 代码
        public  byte Type;         // 符号的类型  0-岩性  1 - 矢量填充  2 - 单个符号   3 - 位图
        public byte SysFlag;      // 是否系统符号
        public float Size;         // 符号尺寸
        public Color[] Colors; // 符号颜色表
        public Color m_bgCol;

        public ushort Index;        // 填充符号的序号
        public ushort Index2;       // 填充符号的序号(附加序号 type＝2时处理)
        public byte PenWidth;     // 填充符号的线宽参数

        public byte SepInd;       // 岩性符号分隔线的字符编码 0-None
        public sbyte SepTop;       // 岩性符号分隔线的起始位置
        public byte BaseMode;     // 岩性符号基本模式(0-双排符号,1-单排符号,2-单个符号)
        public byte DrawMode;     // 岩性符号绘制模式(0:岩性剖面,1:图例,2:平面区域,3:单个符号,4:单排岩性)
        public sbyte RowSpace;     // 岩性符号行间距
        public byte Width;        // 岩性符号总宽度
        public byte nTTF;         // 岩性符号每排基本符号的个数
        public SymChar[] TTFs;     // 岩性符号基本符号序列(每排最多7个，最多两排)

        public bool isUsed;       // 是否使用
        public bool isOutput;     // 是否允许输出
        public string NameCN;       // 中文名称(理解成locale名称）
        public string Name;         // 英文名称(理解成标识）
        public bool isSmooth;     // 光滑参数(反走样标记)
        public byte lithgrain;    // 岩性符号宽度
        public int res;          //  保留字节
    };

    // 符号基本图元
    public struct SymElem
    {
        public byte E_Type;             // 类型(ElemType)
        public bool BX;                 // 变形标记
        public bool UseBk;              // 是否使用背景颜色
        public byte Level;            // 级别参数，用于表示图元集合
        public int Top;                // 顶边界
        public int Left;               // 左边界
        public int Width;              // 宽度
        public int Height;             // 高度

        public int Right;              // 右边界
        public int Bottom;             // 底边界

        public byte PenStyle;           // 线型PenStyle { psSolid, psDash, psDot, psDashDot, psDashDotDot, psClear, psInsideFrame };
        public Color PenColor;           // 线颜色
        public int PenWidth;           // 线宽（像素）
        public byte PenEndCap;          // PenEndCap { pecFlat方头, pecSquare方头, pecRound 圆头};
        public byte PenJoin;            // PenJoin { pjBevel方头, pjMiter尖头, pjRound圆弧形 };

        public byte FillMode;           // 填充模式  TBrushMethod { bmHatch, bmGradient, bmBitmap };
        public byte BrushStyle;         // 普通填充的类型 BrushStyle { bsSolid, bsClear, bsHorizontal, bsVertical,
                                        // bsFDiagonal, bsBDiagonal, bsCross, bsDiagCross };
        public Color GradColor1;         // 过渡色的BeginColor
        public Color GradColor2;         // 过渡色的EndColor

        public bool IsSolid;            // 闭合标记

        public string DashStr;            // 自定义虚线的数据

        public byte GradientStyle;      // 渐变填充的类型Gradient
                                        // GradientStyle { gsHorizontal, gsVertical, gsSquare, gsElliptic,
                                        // gsTopLeft, gsTopRight, gsBottomLeft, gsBottomRight };
        public string FontName;           // 字体名称
        public byte FontSet;             // 字符集
        public int FontSize;             // 字体大小(像素）
        public byte FontStyle;           // 字体风格
        public Color FontColor;          // 字体颜色
        public string TextStr;           // 文字内容

        public PointF[] Points;
        public byte[] Flags;
    };


    // 矢量填充符号
    public struct SymFill
    {
        public bool IsDel;      // 是否删除
        public int No;         // 矢量符号内部的ID码
        public byte nUsed;      // 该符号被使用的次数

        public string Ctype;      // 沉积相分类
        public string Name;       // 符号名称
        public bool Type;       // 符号类型 true-ColorSign  false－Fill_Sign    1-单个， 0-填充， 2-位图
        public int Size;       // 符号大小

        public bool Lock;       // 符号整体比例锁定标记
        public bool Flag;       // 矢量填充边角处理标记

        public int Width;      //符号文档的宽度
        public int Height;     //符号文档的高度

        public int Top;        //符号的坐标宽度
        public int Left;       //符号的坐标高度
        public int Right;      //符号的坐标宽度
        public int Bottom;     //符号的坐标高度

        public Color[] Colors;// 颜色表

        public int nElem;       // 基本图元个数
        public SymElem[] Elems; // 基本图元个数列表
        public int res;        //  保留字节
    };


    // 岩性符号基本图元(400*400的图框内制作,均为单色符号)
    public struct TTFShape
    {
        /// <summary>
        /// E_TYPE Line
        /// </summary>
        public const byte ET_LINE = 1;
        /// <summary>
        /// E_TYPE RECTANGLE
        /// </summary>
        public const byte ET_RECTANGLE = 2;

        /// <summary>
        /// E_TYPE_ELLIPSE
        /// </summary>
        public const byte ET_ELLIPSE = 4;

        public const byte ET_POLYLINE = 8;
        public const byte ET_POLYGON = 9;
        public const byte ET_BEIZER = 14;
        public const byte ET_PATH = 25;
        
        public byte E_Type;         // 类型
        public ushort Top;            // 顶边界
        public ushort Left;           // 边界
        public ushort Right;          // 右边界
        public ushort Bottom;         // 下边界
        public ushort Width;          // 图元宽度
        public ushort Height;         // 图元高度
        public byte PenStyle;       // 线类型
        public float PenWidth;       // 线宽
        public byte Fill;           // 是否填充
        public bool Closed;         // 是否闭合

        public PointF[] Points;         // 坐标列表
        public byte[] Flags;          // 当前点标志(参考QPaintPath)
    };


    // 岩性基本符号数据(相当于一个TTF字符,最大总数为255)
    public struct SymTTF             // 岩性符号的总数为255
    {
        public byte No;     // 代码
        public ushort nShape; // 符号基本符号元素个数
        public TTFShape[] Shapes; // 形状列表
    };





}
