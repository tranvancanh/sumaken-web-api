using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using WarehouseWebApi.Models;
using WarehouseWebApi.common;
using System.Data.SqlClient;
using System.Data;
using Dapper;
using System.Net;
using System.Text.Unicode;
using System.Text;
using WarehouseWebApi.Common;
//using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.Design;

namespace WarehouseWebApi.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class ReceiveController : ControllerBase
    {
        [HttpGet("{companyID}")]
        public IActionResult Get(int companyID, string receiveDate)
        {
            var companys = CompanyModel.GetCompanyByCompanyID(companyID);
            if (companys.Count != 1) return Responce.ExNotFound("データベースの取得に失敗しました");
            var databaseName = companys[0].DatabaseName;

            var receives = ReceiveModel.GetReceiveByReceiveDate(databaseName, receiveDate);
            return receives.Count() == 0 ? Responce.ExNotFound("") : Ok(receives);
        }

        // POST: api/<controller>
        [HttpPost("{companyID}")]
        public IActionResult Post(int companyID, [FromBody]List<ReceiveModel.ReceivePostBody> body)
        {
            try
            {
                var createDatetime = DateTime.Now;

                var registData = new RegistData();

                // データベース名の取得
                var companys = CompanyModel.GetCompanyByCompanyID(companyID);
                if (companys.Count != 1) return Responce.ExNotFound("データベースの取得に失敗しました");
                registData.DatabaseName = companys[0].DatabaseName;

                // 処理用のデータ詳細を取得
                var getRegistData = GetRegistData(body);
                if (getRegistData.result)
                {
                    registData.RegistDataRecords = getRegistData.registDatas;
                    registData.CreateDate = createDatetime;

                    var registScanData = Regist(registData);
                    if (registScanData.result)
                    {
                        return Ok();
                    }
                    else
                    {
                        return Responce.ExBadRequest(registScanData.message);
                    }
                }
                else
                {
                    return Responce.ExBadRequest("スキャンデータの変換に失敗しました");
                }

            }
            catch (Exception ex)
            {
                return Responce.ExServerError(ex);
            }

        }

        private class RegistData
        {
            public string DatabaseName;
            public DateTime CreateDate;
            public List<RegistDataRecord> RegistDataRecords;
        }

        private class RegistDataRecord
        {
            public ReceiveModel.ReceivePostBody PostBody;
            public QrcodeModel.QrcodeItem QrcodeItem;
        }

        private (bool result, List<RegistDataRecord> registDatas) GetRegistData(List<ReceiveModel.ReceivePostBody> bodies)
        {
            bool result = true;
            var registDatas = new List<RegistDataRecord>();

            try
            {

                for (int j = 0; j <= bodies.Count - 1; j++)
                {
                    var registData = new RegistDataRecord();
                    var qrcodeItem = new QrcodeModel.QrcodeItem();

                    // パッケージ現品票QR型の値を取得して変換

                    string[] items = bodies[j].ScanChangeData.Split(':');

                    // 納期
                    var deliveryDateString = items[0];
                    if (deliveryDateString == "")
                    {
                        qrcodeItem.DeliveryDate = "";
                    }
                    else if (DateTime.TryParse(deliveryDateString, out DateTime deliveryDate))
                    {
                        qrcodeItem.DeliveryDate = deliveryDate.ToString("yyyy/MM/dd");
                    }
                    else
                    {
                        result = false;
                        break;
                    }

                    // 便
                    qrcodeItem.DeliveryTimeClass = items[1];

                    // データ区分
                    qrcodeItem.DataClass = items[2];

                    // 発注区分
                    qrcodeItem.OrderClass = items[3];

                    // 伝票番号
                    qrcodeItem.DeliverySlipNumber = items[4];

                    // 仕入先コード
                    qrcodeItem.SupplierCode = items[5];

                    // 仕入先区分
                    qrcodeItem.SupplierClass = items[6];

                    // 製品コード
                    qrcodeItem.ProductCode = items[7];

                    // 製品略称
                    qrcodeItem.ProductAbbreviation = items[8];

                    // 発行枝番（シリアル）
                    int branchNumber;
                    if (!int.TryParse(items[9], out branchNumber))
                    {
                        result = false;
                        break;
                    }
                    else
                    {
                        qrcodeItem.ProductLabelBranchNumber = branchNumber;
                    }

                    // 数量
                    int quantity;
                    if (!int.TryParse(items[10], out quantity))
                    {
                        result = false;
                        break;
                    }
                    else
                    {
                        qrcodeItem.Quantity = quantity;
                    }

                    // 次工程1（納入先1）
                    qrcodeItem.NextProcess1 = items[11];

                    // 置き場1（受入1）
                    qrcodeItem.Location1 = items[12];

                    // 次工程2（納入先2）
                    qrcodeItem.NextProcess2 = items[13];

                    // 箱種（荷姿）
                    qrcodeItem.Packing = items[14];

                    registData.PostBody = bodies[j];
                    registData.QrcodeItem = qrcodeItem;
                    registDatas.Add(registData);
                }

            }
            catch (Exception ex)
            {
                throw;
            }

            return (result, registDatas);
        }

        private (bool result, string message) Regist(RegistData registData)
        {

            var connectionString = new GetConnectString(registData.DatabaseName).ConnectionString;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var tran = connection.BeginTransaction())
                {

                    try
                    {

                        for (int j = 0; j <= registData.RegistDataRecords.Count - 1; j++)
                        {
                            var post = registData.RegistDataRecords[j].PostBody;
                            var qrcode = registData.RegistDataRecords[j].QrcodeItem;

                            #region スキャン履歴をINSERT

                            // スキャン履歴IDを取得
                            long scanRecordID = 0;
                            string sql1 = @"
                                INSERT INTO D_ScanRecord
                                    (DepoID, HandyUserID, Device, HandyPageID, ScanString1, ScanString2, ScanChangeData, ScanTime, Latitude, Longitude, CreateDate) 
                                    OUTPUT 
                                       INSERTED.ScanRecordID
                                VALUES
                                    (@DepoID, @HandyUserID, @Device, @HandyPageID, @ScanString1, @ScanString2, @ScanChangeData, @ScanTime, @Latitude, @Longitude, @CreateDate)
                                ";

                            var param1 = new
                            {
                                post.DepoID,
                                post.HandyUserID,
                                post.Device,
                                post.HandyPageID,
                                post.ScanString1,
                                post.ScanString2,
                                post.ScanChangeData,
                                post.ScanTime,
                                post.Latitude,
                                post.Longitude,
                                registData.CreateDate
                            };
                            var result1 = connection.QuerySingle<long>(sql1, param1, tran);

                            if (result1 < 1)
                            {
                                tran.Rollback();
                                return (false, "スキャン実績の登録に失敗しました");
                            }
                            else
                            {
                                scanRecordID = result1;
                            }

                            #endregion

                            #region 入荷済みチェック

                            string sql2 = @"
                                    SELECT
                                        COUNT(*) AS DataCount
                                    FROM D_Receive AS A 
                                    WHERE
                                        A.ReceiveDate = @ReceiveDate
                                        AND A.SupplierCode = @SupplierCode
                                        AND A.SupplierClass = @SupplierClass
                                        AND A.ProductCode = @ProductCode
                                        AND A.ProductLabelBranchNumber = @ProductLabelBranchNumber
                                        AND A.Quantity = @Quantity
                                        AND A.NextProcess1 = @NextProcess1
                                        AND A.NextProcess2 = @NextProcess2
                                        AND A.Packing = @Packing
                                     ";

                            var param2 = new
                            {
                                post.ReceiveDate,
                                qrcode.SupplierCode,
                                qrcode.SupplierClass,
                                qrcode.ProductCode,
                                qrcode.ProductLabelBranchNumber,
                                qrcode.NextProcess1,
                                qrcode.NextProcess2,
                                qrcode.Quantity,
                                qrcode.Packing
                            };

                            var result2 = connection.QueryFirstOrDefault<int>(sql2, param2, tran);

                            if (result2 > 0)
                            {
                                // 既に入荷登録がある場合

                                // 現状は何もせずスキップする、D_ScanRecordのみINSERTする
                                continue;
                            }

                            #endregion

                            #region 入荷予定データの存在チェック

                            // 入荷予定詳細データのID取得
                            long receiveScheduleDetailID = 0;

                            // スキャンデータ（QR）に「納期」と「納品書番号」がある場合のみ
                            if (!String.IsNullOrEmpty(qrcode.DeliveryDate) && !String.IsNullOrEmpty(qrcode.DeliverySlipNumber))
                            {
                                string sql3 = @"
                                                SELECT
                                                    A.ReceiveScheduleDetailID
                                                FROM D_ReceiveScheduleDetail AS A
                                                LEFT OUTER JOIN D_ReceiveScheduleHeader AS B ON A.ReceiveScheduleDate = B.ReceiveScheduleDate AND A.DeliverySlipNumber = B.DeliverySlipNumber
                                                WHERE (1=1)
                                                    AND A.ReceiveScheduleDate = @ReceiveScheduleDate
                                                    AND A.DeliverySlipNumber = @DeliverySlipNumber
                                                    AND B.ReceiveTimeClass = @ReceiveTimeClass
                                                    AND A.ProductCode = @ProductCode
                                                    AND A.LotQuantity = @LotQuantity
                                                    AND A.CancelFlag = @CancelFlag
                                                    ";

                                var param3 = new
                                {
                                    ReceiveScheduleDate = qrcode.DeliveryDate,
                                    qrcode.DeliverySlipNumber,
                                    ReceiveTimeClass = qrcode.DeliveryTimeClass,
                                    qrcode.ProductCode,
                                    LotQuantity = qrcode.Quantity,
                                    CancelFlag = 0
                                };

                                var result3 = connection.Query<long>(sql3, param3, tran).ToList();

                                if (result3 == null || result3.Count == 0)
                                {
                                    // データの取得に失敗した場合
                                    tran.Rollback();
                                    return (false, "入荷予定データの取得に失敗しました");
                                }
                                else if (result3[0] > 1)
                                {
                                    // 入荷予定が複数存在した場合
                                    tran.Rollback();
                                    return (false, "入荷予定データの取得に失敗しました");
                                }
                                else if (result3[0] == 0)
                                {
                                    // 入荷予定が存在しない場合
                                }
                                else if (result3[0] == 1)
                                {
                                    // OK
                                    receiveScheduleDetailID = result3[0];
                                }
                                else
                                {
                                    // その他
                                }

                            }

                            #endregion

                            #region 入荷実績をINSERT

                            // 入荷実績IDを取得
                            int receiveID = 0;
                            string sql4 = @"
                                                INSERT INTO D_Receive 
                                                (
	                                               ScanRecordID
                                                  ,ReceiveScheduleDetailID
                                                  ,DepoID
                                                  ,DeliveryDate
                                                  ,DeliveryTimeClass
                                                  ,DeliverySlipNumber
                                                  ,DeliverySlipRowNumber
                                                  ,SupplierCode
                                                  ,SupplierClass
                                                  ,CustomerCode
                                                  ,CustomerClass
                                                  ,ReceiveDate
                                                  ,ProductCode
                                                  ,ProductAbbreviation
                                                  ,ProductManagementClass
                                                  ,ProductLabelBranchNumber
                                                  ,NextProcess1
                                                  ,Location1
                                                  ,NextProcess2
                                                  ,Location2
                                                  ,LotQuantity
                                                  ,Quantity
                                                  ,Packing
                                                  ,PackingCount
                                                  ,CreateDate
                                                  ,CreateUserID
                                                  ,UpdateDate
                                                  ,UpdateUserID
                                                )
                                                OUTPUT 
                                                   INSERTED.ReceiveID
                                                VALUES
                                                (
	                                               @ScanRecordID
                                                  ,@ReceiveScheduleDetailID
                                                  ,@DepoID
                                                  ,@DeliveryDate
                                                  ,@DeliveryTimeClass
                                                  ,@DeliverySlipNumber
                                                  ,@DeliverySlipRowNumber
                                                  ,@SupplierCode
                                                  ,@SupplierClass
                                                  ,@CustomerCode
                                                  ,@CustomerClass
                                                  ,@ReceiveDate
                                                  ,@ProductCode
                                                  ,@ProductAbbreviation
                                                  ,@ProductManagementClass
                                                  ,@ProductLabelBranchNumber
                                                  ,@NextProcess1
                                                  ,@Location1
                                                  ,@NextProcess2
                                                  ,@Location2
                                                  ,@LotQuantity
                                                  ,@Quantity
                                                  ,@Packing
                                                  ,@PackingCount
                                                  ,@CreateDate
                                                  ,@CreateUserID
                                                  ,@UpdateDate
                                                  ,@UpdateUserID
                                                )
                                        ";

                            var param4 = new
                            {
                                ScanRecordID = scanRecordID,
                                ReceiveScheduleDetailID = receiveScheduleDetailID,
                                post.DepoID,
                                qrcode.DeliveryDate,
                                qrcode.DeliveryTimeClass,
                                qrcode.DeliverySlipNumber,
                                DeliverySlipRowNumber= 0,
                                qrcode.SupplierCode,
                                qrcode.SupplierClass,
                                qrcode.CustomerCode,
                                qrcode.CustomerClass,
                                post.ReceiveDate,
                                qrcode.ProductCode,
                                qrcode.ProductAbbreviation,
                                qrcode.ProductLabelBranchNumber,
                                ProductManagementClass = "",
                                qrcode.NextProcess1,
                                qrcode.Location1,
                                qrcode.NextProcess2,
                                Location2 ="",
                                LotQuantity = qrcode.Quantity,
                                qrcode.Quantity,
                                qrcode.Packing,
                                PackingCount = 1,
                                registData.CreateDate,
                                CreateUserID = post.HandyUserID,
                                UpdateDate = registData.CreateDate,
                                UpdateUserID = post.HandyUserID
                            };

                            var result4 = connection.QuerySingle<int>(sql4, param4, tran);
                            if (result4 < 1)
                            {
                                tran.Rollback();
                                return (false, "入荷実績データの登録に失敗しました");
                            }
                            else
                            {
                                receiveID = result4;
                            }
                            
                            #endregion

                            #region 在庫入庫データをINSERT

                            // 在庫入庫フラグが1の場合のみ
                            if (post.StoreInFlag == 1)
                            {
                                string sql5 = @"
                                                INSERT INTO D_StoreIn 
                                                (
                                                       ReceiveID
                                                      ,ScanRecordID
                                                      ,DepoID
                                                      ,StoreInDate
                                                      ,ProductCode
                                                      ,Quantity
                                                      ,Packing
                                                      ,StockLocation1
                                                      ,StockLocation2
                                                      ,Remark
                                                      ,DeleteFlag
                                                      ,DeleteStoreInID
                                                      ,CreateDate
                                                      ,CreateUserID
                                                      ,UpdateDate
                                                      ,UpdateUserID
                                                )
                                                VALUES
                                                (
                                                       @ReceiveID
                                                      ,@ScanRecordID
                                                      ,@DepoID
                                                      ,@StoreInDate
                                                      ,@ProductCode
                                                      ,@Quantity
                                                      ,@Packing
                                                      ,@StockLocation1
                                                      ,@StockLocation2
                                                      ,@Remark
                                                      ,@DeleteFlag
                                                      ,@DeleteStoreInID
                                                      ,@CreateDate
                                                      ,@CreateUserID
                                                      ,@UpdateDate
                                                      ,@UpdateUserID
                                                )
                                                    ";

                                var param5 = new
                                {
                                    ReceiveID = receiveID,
                                    ScanRecordID = scanRecordID,
                                    post.DepoID,
                                    StoreInDate = post.ReceiveDate,
                                    qrcode.ProductCode,
                                    qrcode.Quantity,
                                    qrcode.Packing,
                                    StockLocation1 = qrcode.NextProcess1,
                                    StockLocation2 = qrcode.Location1,
                                    Remark = "",
                                    DeleteFlag = 0,
                                    DeleteStoreInID = 0,
                                    registData.CreateDate,
                                    CreateUserID = post.HandyUserID,
                                    UpdateDate = registData.CreateDate,
                                    UpdateUserID = post.HandyUserID
                                };

                                // 処理件数を返す
                                var result5 = connection.Execute(sql5, param5, tran);

                                if (result5 < 1)
                                {
                                    tran.Rollback();
                                    return (false, "在庫入庫データの登録に失敗しました");
                                }
                                else
                                {
                                    // スキップ
                                }

                                #endregion

                            }

                        }

                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return (false, "データの登録に失敗しました");
                    }

                    tran.Commit();
                    return (true, "入荷実績データの登録に失敗しました");
                }
            }

        }


    }
}
