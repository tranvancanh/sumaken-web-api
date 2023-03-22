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
using WarehouseWebApi.Common;
using System.ComponentModel.Design;
//using Microsoft.AspNetCore.Http.HttpResults;
using System.Linq.Expressions;
using static WarehouseWebApi.Models.SettingModel;

namespace WarehouseWebApi.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class TemporaryStoreAddressController : ControllerBase
    {
        [HttpGet("{companyID}")]
        public IActionResult Get(int companyID, int depoID)
        {
            var companys = CompanyModel.GetCompanyByCompanyID(companyID);
            if (companys.Count != 1) return Responce.ExNotFound("データベースの取得に失敗しました");
            var databaseName = companys[0].DatabaseName;

            var temporaryStoreAddresses = new List<TemporaryStoreAddressModel.M_TemporaryStoreAddress>();
            try
            {
                temporaryStoreAddresses = TemporaryStoreAddressModel.GetTemporaryStoreAddress(databaseName, depoID);
            }
            catch (Exception ex)
            {
                return Responce.ExServerError(ex);
            }

            return temporaryStoreAddresses.Count() == 0 ? Responce.ExNotFound("仮番地情報の取得に失敗しました") : Ok(temporaryStoreAddresses);
        }

    }
}
