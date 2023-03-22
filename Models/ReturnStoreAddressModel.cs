using Dapper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using WarehouseWebApi.common;

namespace WarehouseWebApi.Models
{
    public class ReturnStoreAddresseModel
    {
        public class ReturnStoreAddressePostBackBody
        {
            /// <summary>
            /// 移動登録成功データカウント
            /// </summary>
            public int SuccessDataCount { get; set; }
            /// <summary>
            /// 移動元データが存在しなかったデータカウント
            /// </summary>
            public int StoreInNotFoundDataCount { get; set; }
            /// <summary>
            /// 移動元データが存在しなかったデータ
            /// </summary>
            public List<QrcodeModel.QrcodeItem> StoreInNotFoundDatas { get; set; } = new List<QrcodeModel.QrcodeItem>();
        }

    }

}
