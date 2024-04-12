using Dapper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using WarehouseWebApi.common;
using static WarehouseWebApi.Models.SettingModel;

namespace WarehouseWebApi.Models
{
    public class CompanyModel
    {
        public class M_Company
        {
            public int CompanyID { get; set; }
            public string CompanyCode { get; set; } = string.Empty;
            public string CompanyName { get; set; } = string.Empty;
            public string DatabaseName { get; set; } = string.Empty;
            public decimal HandyAppMinVersion { get; set; }
        }

        public static List<M_Company> GetCompanyByCompanyID(int companyID)
        {
            var companys = new List<M_Company>();
            var connectionString = new GetMasterConnectString().ConnectionString;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                try
                {

                    var query = @"
                                        SELECT
                                            CompanyID, CompanyCode, CompanyName, DatabaseName, HandyAppMinVersion
                                        FROM M_Company
                                        WHERE (1=1)
                                        AND CompanyID = @CompanyID
                                        AND NotUseFlag = @NotUseFlag
                                                ";
                    var param = new
                    {
                        CompanyID = companyID,
                        NotUseFlag = 0
                    };
                    companys = connection.Query<M_Company>(query, param).ToList();
                    return companys;
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// /酒倉デポAGFハンディの接続先を取得 
        /// </summary>
        /// <param name="companyID"></param>
        /// <returns></returns>
        public static string GetCompanyByCompanyID_AGF(string databaseName, string companyCode)
        {
            var handyApiUrl = string.Empty;
            var connectionString = new GetConnectString(databaseName).ConnectionString;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                try
                {

                    var query = @"
                                 SELECT TOP (1) [AGFApiUrl]
                                 FROM [M_AGF_WebAPIURL]
                                 WHERE [CompanyCode] = @CompanyCode
                                ";
                    var param = new
                    {
                        CompanyCode = companyCode
                    };
                    handyApiUrl = connection.Query<string>(query, param).FirstOrDefault();
                    return handyApiUrl;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public static List<M_Company> GetCompanyByCompanyCodeAndPassword(string companyCode, string companyPassword)
        {
            var companys = new List<M_Company>();
            var connectionString = new GetMasterConnectString().ConnectionString;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                try
                {

                    var query = @"
                                        SELECT
                                            CompanyID, CompanyCode, CompanyName, DatabaseName
                                        FROM M_Company
                                        WHERE 1=1
                                            AND CompanyCode = @CompanyCode
                                            AND CompanyPassword = @CompanyPassword
                                            AND NotUseFlag = @NotUseFlag
                                                ";
                    var param = new
                    {
                        CompanyCode = companyCode,
                        CompanyPassword = companyPassword,
                        NotUseFlag = 0
                    };
                    companys = connection.Query<M_Company>(query, param).ToList();
                    return companys;
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }

    }

}
