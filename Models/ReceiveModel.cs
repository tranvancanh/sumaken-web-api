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
    public class ReceiveModel
    {
        public class D_Receive
        {
            public string SupplierCode { get; set; } = String.Empty;
            public string SupplierClass { get; set; } = String.Empty;
            public string ProductCode { get; set; } = String.Empty;
            public int ProductLabelBranchNumber { get; set; }
            public int Quantity { get; set; }
            public string Packing { get; set; } = String.Empty;
            public string NextProcess1 { get; set; } = String.Empty;
            public string NextProcess2 { get; set; } = String.Empty;
        }

        public static List<D_Receive> GetReceiveByReceiveDate(string databaseName, string receiveDate)
        {
            var receives = new List<D_Receive>();

            try
            {
                var connectionString = new GetConnectString(databaseName).ConnectionString;
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var query = @$"
                                        SELECT
                                               SupplierCode
                                              ,SupplierClass
                                              ,ReceiveDate
                                              ,ProductCode
                                              ,NextProcess1
                                              ,NextProcess2
                                              ,Quantity
                                              ,Packing
                                              ,PackingCount
                                              ,ProductLabelBranchNumber
                                          FROM D_Receive
                                          WHERE (1=1)
                                                AND ReceiveDate = @ReceiveDate
                                                ";

                    var param = new
                    {
                        ReceiveDate = receiveDate
                    };

                    receives = connection.Query<D_Receive>(query, param).ToList();

                    return receives;

                }
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        public class ReceivePostBody
        {
            [Required]
            public int DepoID { get; set; }
            [Required]
            public int HandyPageID { get; set; }
            [Required]
            public string ReceiveDate { get; set; } = String.Empty;
            [Required]
            public int StoreInFlag { get; set; }
            [Required]
            public int HandyUserID { get; set; }
            [Required]
            public string Device { get; set; } = String.Empty;
            [Required]
            public string ScanString1 { get; set; } = String.Empty;
            public string ScanString2 { get; set; } = String.Empty;
            [Required]
            public string ScanChangeData { get; set; } = String.Empty;
            [Required]
            public DateTime ScanTime { get; set; }
            [Required]
            public double Latitude { get; set; }
            [Required]
            public double Longitude { get; set; }
        }

    }

}
