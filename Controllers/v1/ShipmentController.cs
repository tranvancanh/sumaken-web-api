using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using WarehouseWebApi.common;
using WarehouseWebApi.Common;
using WarehouseWebApi.Models;

namespace WarehouseWebApi.Controllers.v1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class ShipmentController : ControllerBase
    {

        [HttpGet("{companyID}")]
        public IActionResult Get(int companyID, int depoID, string receiveDateStart, string receiveDateEnd)
        {
            var registedData = new List<D_ShipmentModel>();
            try
            {
                var companys = CompanyModel.GetCompanyByCompanyID(companyID);
                if (companys.Count != 1) return Responce.ExBadRequest("会社情報の取得に失敗しました");
                var databaseName = companys[0].DatabaseName;
                if (string.IsNullOrEmpty(databaseName)) return Responce.ExBadRequest("データベースの取得に失敗しました");
                registedData = GetShippedDataByDeliveryDate(databaseName, depoID, receiveDateStart, receiveDateEnd);
            }
            catch (Exception ex)
            {
                return Responce.ExServerError(ex);
            }

            return Ok(registedData);
        }

        public List<D_ShipmentModel> GetShippedDataByDeliveryDate(string databaseName, int depoID, string shipmentDateStart, string shipmentDateStop)
        {
            var registedData = new List<D_ShipmentModel>();
            var connectionString = new GetConnectString(databaseName).ConnectionString;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = @$"
                                    SELECT
                                           [ShipmentID]
                                          ,[ScanRecordID]
                                          ,[ShipmentInstructionDetailID]
                                          ,[StoreOutID]
                                          ,[HandyMatchClass]
                                          ,[HandyMatchResult]
                                          ,[ShipmentDate]
                                          ,[DepoID]
                                          ,[DeliveryDate]
                                          ,[DeliveryTimeClass]
                                          ,[DeliverySlipNumber]
                                          ,[DeliverySlipRowNumber]
                                          ,[SupplierCode]
                                          ,[SupplierClass]
                                          ,[ProductCode]
                                          ,[ProductAbbreviation]
                                          ,[ProductManagementClass]
                                          ,[ProductLabelBranchNumber]
                                          ,[NextProcess1]
                                          ,[Location1]
                                          ,[NextProcess2]
                                          ,[Location2]
                                          ,[CustomerDeliveryDate]
                                          ,[CustomerDeliveryTimeClass]
                                          ,[CustomerDeliverySlipNumber]
                                          ,[CustomerDeliverySlipRowNumber]
                                          ,[CustomerCode]
                                          ,[CustomerClass]
                                          ,[CustomerName]
                                          ,[CustomerProductCode]
                                          ,[CustomerProductAbbreviation]
                                          ,[CustomerProductManagementClass]
                                          ,[CustomerProductLabelBranchNumber]
                                          ,[CustomerNextProcess1]
                                          ,[CustomerLocation1]
                                          ,[CustomerNextProcess2]
                                          ,[CustomerLocation2]
                                          ,[CustomerOrderNumber]
                                          ,[CustomerOrderClass]
                                          ,[LotQuantity]
                                          ,[FractionQuantity]
                                          ,[Quantity]
                                          ,[Packing]
                                          ,[PackingCount]
                                          ,[LotNumber]
                                          ,[InvoiceNumber]
                                          ,[ExpirationDate]
                                          ,[DeleteFlag]
                                          ,[DeleteShipmentID]
                                          ,[Remark]
                                        FROM [D_Shipment]
                                        WHERE (1=1)
                                            AND DepoID = @DepoID
                                            AND ShipmentDate >= @ShipmentDateStart
                                            AND ShipmentDate <= @ShipmentDateStop
                                        ";

                var param = new
                {
                    DepoID = depoID,
                    ShipmentDateStart = shipmentDateStart,
                    ShipmentDateStop = shipmentDateStop
                };

                registedData = connection.Query<D_ShipmentModel>(query, param).ToList();

                return registedData;
            }
        }
    }
}
