using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Tet.GeoSymbol.DPSymbolUI
{
    public enum SymbolType
    {
        Payzon,
        Lithology,
        Facies
    }

    public class SymbolDataManager
    {
        private static SymbolData _SymbolData;
        public static SymbolData SymbolData
        {
            get
            {
                if(_SymbolData == null)
                {
                    _SymbolData = new SymbolData();
                    _SymbolData.PayzoneItems = ReadAndConvertFromFile(SymbolType.Payzon);
                    _SymbolData.LithologyItems = ReadAndConvertFromFile(SymbolType.Lithology);
                    _SymbolData.FaciesItems = ReadAndConvertFromFile(SymbolType.Facies);


                }
                return _SymbolData;
            }
        }


        /// <summary>
        /// 从文件读取JSON数据并转换为对象列表，同时将Base64转换为BitmapImage
        /// </summary>
        /// <param name="filePath">JSON文件路径</param>
        /// <returns>转换后的LithologyItem列表</returns>
        public static List<SymbolItem> ReadAndConvertFromFile(SymbolType symbolType)
        {
            List<SymbolItem> res = new List<SymbolItem>();
            string filePath = System.AppDomain.CurrentDomain.BaseDirectory+ "Configs\\";
            if(symbolType == SymbolType.Payzon)
            {
                filePath = filePath + "Payzon.json";
            }
            else if(symbolType == SymbolType.Lithology)
            {
                filePath = filePath + "Lithology.json";
            }
            else if (symbolType == SymbolType.Facies)
            {
                filePath = filePath + "Facies.json";
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"文件不存在: {filePath}");
            }

            // 1. 读取JSON文件
            string jsonContent = File.ReadAllText(filePath);

            // 2. 反序列化为对象列表
            List<SymbolItem> items = JsonConvert.DeserializeObject<List<SymbolItem>>(jsonContent);

            if (items == null || items.Count == 0)
            {
                return new List<SymbolItem>();
            }

            // 3. 为每个对象转换Base64为BitmapImage
            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.EngImageContent))
                {
                    item.ImageBitmap = ConvertBase64ToBitmapImage(item.EngImageContent);
                }
            }

            return items;
        }

        /// <summary>
        /// 将Base64字符串转换为BitmapImage
        /// </summary>
        /// <param name="base64String">Base64编码的图片字符串</param>
        /// <returns>BitmapImage对象</returns>
        public static BitmapImage ConvertBase64ToBitmapImage(string base64String)
        {
            try
            {
                // 移除可能的Base64前缀（如"data:image/png;base64,"）
                string cleanBase64 = base64String;
                if (base64String.Contains(","))
                {
                    cleanBase64 = base64String.Substring(base64String.IndexOf(",") + 1);
                }

                // 将Base64字符串转换为字节数组
                byte[] imageBytes = Convert.FromBase64String(cleanBase64);

                // 创建BitmapImage
                BitmapImage bitmapImage = new BitmapImage();

                using (MemoryStream memoryStream = new MemoryStream(imageBytes))
                {
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = memoryStream;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze(); // 冻结对象以便在UI线程外使用
                }

                return bitmapImage;
            }
            catch (Exception ex)
            {
                // 记录错误或返回null
                Console.WriteLine($"转换Base64为BitmapImage失败: {ex.Message}");
                return null;
            }
        }

    }
    public class SymbolData
    {
        public List<SymbolItem> PayzoneItems { get; set; }
        public List<SymbolItem> LithologyItems { get; set; }
        public List<SymbolItem> FaciesItems { get; set; }
    }

    public class SymbolItem
    {
        [JsonProperty("categoryType")]
        public string CategoryType { get; set; }

        [JsonProperty("createUserId")]
        public int CreateUserId { get; set; }

        [JsonProperty("engName")]
        public string EngName { get; set; }

        [JsonProperty("createTime")]
        public long CreateTime { get; set; }

        [JsonProperty("categoryPath")]
        public string CategoryPath { get; set; }

        [JsonProperty("chineseName")]
        public string ChineseName { get; set; }

        [JsonProperty("updateTime")]
        public long UpdateTime { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("engImageContent")]
        public string EngImageContent { get; set; }

        // 新增的BitmapImage字段（不参与JSON序列化/反序列化）
        [JsonIgnore]
        public BitmapImage ImageBitmap { get; set; }
    }

}
