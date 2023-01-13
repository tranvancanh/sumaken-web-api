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

namespace WarehouseWebApi.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class QrcodeController : ControllerBase
    {
        [HttpGet("{companyID}")]
        public IActionResult Get(int companyID, int depoID, int handyPageID)
        {
            var companys = CompanyModel.GetCompanyByCompanyID(companyID);
            if (companys.Count != 1) return Responce.ExNotFound("データベースの取得に失敗しました");
            var databaseName = companys[0].DatabaseName;

            var qrcodeIndices = new List<QrcodeModel.M_QrcodeIndex>();
            try
            {
                qrcodeIndices = QrcodeModel.GetQrcodeIndex(databaseName, depoID, handyPageID);
            }
            catch (Exception ex)
            {
                return Responce.ExServerError(ex);
            }

            return qrcodeIndices.Count() == 0 ? Responce.ExNotFound("QRコードマスタが存在しません") : Ok(qrcodeIndices);
        }

    }
}
