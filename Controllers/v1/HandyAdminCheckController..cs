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
using static WarehouseWebApi.Models.HandyAdminCheckModel;

namespace WarehouseWebApi.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class HandyAdminCheckController : ControllerBase
    {
        // POST: api/<controller>
        [HttpPost]
        public IActionResult Post([FromBody] HandyAdminCheckPostBody input)
        {
            var handyAdminCount = 0;
            try
            {
                handyAdminCount = HandyAdminCheckModel.HandyAdminCheck(input.CompanyID, input.HandyAdminPassword);
            }
            catch (Exception ex)
            {
                return Responce.ExServerError(ex);
            }
            if (handyAdminCount != 1) return Responce.ExBadRequest("管理者パスワードが不正です");

            return Ok();

        }

      }
}
