using Dapper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using WarehouseWebApi.common;
using static WarehouseWebApi.Models.HandyReportLogModel;
using static WarehouseWebApi.Models.SettingModel;

namespace WarehouseWebApi.Models
{
    public class HandyReportLogModel
    {

        public class HandyReportLog
        {
            public long ScanRecordID { get; set; }
            public string HandyReport { get; set; } = string.Empty;

            public List<HandyReportLog> HandyReportLogs { get; set; } = new List<HandyReportLog>();
        }

        /// <summary>
        /// 重複のため登録できなかったデータログなどを記録
        /// </summary>
        /// <param name="databaseName"></param>
        /// <param name="handyReportLogs"></param>
        /// <returns></returns>
        public static bool InsertHandyReportLog(string databaseName, List<HandyReportLog> handyReportLogs)
        {
            try
            {
                var connectionString = new GetConnectString(databaseName).ConnectionString;
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    foreach (var report in handyReportLogs)
                    {
                        var query = @$"
                                                INSERT INTO D_HandyReportLog
                                                (
                                                       ScanRecordID
                                                      ,HandyReport
                                                )
                                                VALUES
                                                (
                                                       @ScanRecordID
                                                      ,@HandyReport
                                                )
                                                    ";

                        var param = new
                        {
                            report.ScanRecordID,
                            report.HandyReport
                        };

                        var insertResult = connection.Execute(query, param);

                        if(insertResult != 1)
                        {
                            return false;
                        }

                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                //throw;
                return false;
            }

        }

    }

}
