using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tulip.Utils.Code;

namespace TSysWatch.Entity
{
    /// <summary>
    /// 
    /// </summary>
    [SplitTable(SplitType.Day)]
    [SugarTable("RealTimeData_{year}{month}{day}")]
    public class RealTimeData
    {
        [SqlSugar.SugarColumn(IsPrimaryKey = true)]
        public long Id { get; set; } = CodeHelper.CreateId();
        [SplitField]
        [SqlSugar.SugarColumn(IndexGroupNameList = ["create_time_type_index"])]
        public DateTime CreateTime { get; set; } = DateTime.Now;
        /// <summary>
        /// 索引
        /// </summary>
        [SqlSugar.SugarColumn(IndexGroupNameList = ["real_time_data_type_id_index"])]
        public long TypeId { get; set; }


        public double Value { get; set; }


    }
}
