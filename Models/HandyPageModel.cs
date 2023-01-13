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
    public class HandyPageModel
    {
        public class M_HandyPage
        {
            public int HandyPageID { get; set; }
            public string HandyPageName { get; set; } = string.Empty;
        }

        public static List<M_HandyPage> GetHandyPage(string databaseName, int depoID, int administratorFlag)
        {
            var selectList = new List<M_HandyPage>();

            string addWhere = "";
            if (administratorFlag == 0)  addWhere = $@" AND AdministratorOnlyFlag = @AdministratorOnlyFlag ";
            var connectionString = new GetConnectString(databaseName).ConnectionString;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                try
                {

                    var query = $@"
                                        SELECT
                                               HandyPageID
                                              ,HandyPageName
                                        FROM M_HandyPage
                                        WHERE 1=1
                                            AND DepoID = @DepoID
                                            AND NotUseFlag = @NotUseFlag
                                            {addWhere}
                                        ORDER BY SortNumber
                                                ";
                    var param = new
                    {
                        DepoID = depoID,
                        NotUseFlag = 0,
                        AdministratorOnlyFlag = 0,
                    };
                    selectList = connection.Query<M_HandyPage>(query, param).ToList();

                    return selectList;
                }
                catch (Exception e)
                {
                    throw;
                }
            }

        }

    }

}
