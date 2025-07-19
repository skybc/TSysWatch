using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tulip.Utils.Code;

namespace TSysWatch.Entity
{
    internal class SensorInfo
    {
        // 自增id 
        [SqlSugar.SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
        public long Id { get; set; } = CodeHelper.CreateId();
        /// <summary>
        /// 长度为50
        /// </summary>
        [SqlSugar.SugarColumn(Length = 50)]
        public string SensorName { get; set; }
        [SqlSugar.SugarColumn(Length = 50)]

        public string HardName { get; set; }
        [SqlSugar.SugarColumn(Length = 50)]
        public string HardType { get; set; }
        [SqlSugar.SugarColumn(Length = 50)]
        public string Unit { get; set; }

    }
}
