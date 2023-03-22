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
    public class HandyPageController : ControllerBase
    {
        [HttpGet("{companyID}")]
        public IActionResult Get(int companyID, int depoID, int administratorFlag, int handyUserID = 0)
        {
            var companys = CompanyModel.GetCompanyByCompanyID(companyID);
            if (companys.Count != 1) return Responce.ExNotFound("データベースの取得に失敗しました");
            var databaseName = companys[0].DatabaseName;

            var handyPages = new List<HandyPageModel.M_HandyPage>();
            try
            {
                handyPages = HandyPageModel.GetHandyPage(databaseName, depoID, administratorFlag, handyUserID);
            }
            catch (Exception ex)
            {
                return Responce.ExServerError(ex);
            }

            return handyPages.Count() == 0 ? Responce.ExNotFound("表示可能なメニューがありません") : Ok(handyPages);
        }

    }
}
