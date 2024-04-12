using Microsoft.AspNetCore.Mvc;
using WarehouseWebApi.Common;
using WarehouseWebApi.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SakaguraAGFWebApi.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class ReturnAgfUrlController : ControllerBase
    {
        // GET: api/<AGFShipmentController>
        [HttpGet("{companyID}")]
        public IActionResult Get(int companyID, string companyCode)
        {
            try
            {
                var companys = CompanyModel.GetCompanyByCompanyID(companyID);
                if (companys.Count != 1) return Responce.ExNotFound("データベースの取得に失敗しました");
                var databaseName = companys[0].DatabaseName;

                var handyApiUrl = CompanyModel.GetCompanyByCompanyID_AGF(databaseName, companyCode);
                if (handyApiUrl == null) return Responce.ExNotFound("データベースの取得に失敗しました");
                return Ok(handyApiUrl);
            }
            catch (Exception ex)
            {
                return Responce.ExServerError(ex);
            }
        }


    }
}
