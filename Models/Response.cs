using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KindomDataAPIServer.Models
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class WellOperationResult
    {
        /// <summary>
        /// 单个井操作结果列表
        /// </summary>
        [JsonProperty("results")]
        public List<WellOperationDetail> Results { get; set; }

        /// <summary>
        /// 操作摘要统计
        /// </summary>
        [JsonProperty("summary")]
        public OperationSummary Summary { get; set; }
    }

    public class WellOperationDetail
    {
        /// <summary>
        /// 输入的井名
        /// </summary>
        [JsonProperty("inputWellName")]
        public string InputWellName { get; set; }

        /// <summary>
        /// 最终的井名（可能经过重命名）
        /// </summary>
        [JsonProperty("finalWellName")]
        public string FinalWellName { get; set; }



        public long wellId { get; set; }
        public string wellName { get; set; }

        public string formationName { get; set; }


        public long dataSetId { get; set; }    
        public string curveName { get; set; }

        
        /// <summary>
        /// 执行的操作 (如: created, updated, ignored, renamed)
        /// </summary>
        [JsonProperty("action")]
        public string Action { get; set; }

        /// <summary>
        /// 操作消息或说明
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }
        public int errorCode { get; set; }
    }

    public class OperationSummary
    {
        /// <summary>
        /// 创建的数量
        /// </summary>
        [JsonProperty("created")]
        public int Created { get; set; }

        /// <summary>
        /// 更新的数量
        /// </summary>
        [JsonProperty("updated")]
        public int Updated { get; set; }

        public int failed { get; set; }

        /// <summary>
        /// 忽略的数量
        /// </summary>
        [JsonProperty("ignored")]
        public int Ignored { get; set; }
        public int renamed { get; set; }
        
        /// <summary>
        /// 重命名后创建的数量
        /// </summary>
        [JsonProperty("renamedCreated")]
        public int RenamedCreated { get; set; }

        /// <summary>
        /// 获取操作总数
        /// </summary>
        public int TotalOperations => Created + Updated + Ignored + RenamedCreated;
    }


    public class LogSetInfo
    {
        /// <summary>
        /// 唯一标识符
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 日志集类型
        /// </summary>
        public string LogSetType { get; set; }

        /// <summary>
        /// 井数量
        /// </summary>
        public int WellCount { get; set; }

        /// <summary>
        /// 特征数量
        /// </summary>
        public int FeatureCount { get; set; }

        /// <summary>
        /// 特征类型数量
        /// </summary>
        public int FeatureTypeCount { get; set; }

        /// <summary>
        /// 更新时间（UTC时间）
        /// </summary>
        public DateTime UpdateTime { get; set; }
    }
}
