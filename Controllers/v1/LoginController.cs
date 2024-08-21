using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WarehouseWebApi.common;
using WarehouseWebApi.Common;
using WarehouseWebApi.Models;
//using Microsoft.AspNetCore.Http.HttpResults;
using static WarehouseWebApi.Models.LoginModel;

namespace WarehouseWebApi.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // POST: api/<controller>
        [HttpPost]
        public IActionResult Post([FromBody] LoginModel.LoginPostBody input)
        {
            // 会社情報の取得
            var companys = new List<CompanyModel.M_Company>();
            var company = new CompanyModel.M_Company();
            var databaseName = "";
            try
            {
                companys = CompanyModel.GetCompanyByCompanyID(input.CompanyID);
            }
            catch (Exception ex)
            {
                return Responce.ExServerError(ex);
            }
            if (companys.Count != 1) return Responce.ExBadRequest("会社情報の取得に失敗しました");
            if (String.IsNullOrEmpty(companys[0].DatabaseName)) return Responce.ExBadRequest("データベースの取得に失敗しました");
            company = companys[0];
            databaseName = companys[0].DatabaseName;

            if (company.HandyAppMinVersion > input.HandyAppVersion) return Responce.ExBadRequest("アプリを更新してください");

            // ユーザー情報の取得
            var handyUser = new HandyUserModel.M_HandyUser();
            var handyUsers = new List<HandyUserModel.M_HandyUser>();
            try
            {
                if (input.PasswordMode == 0)
                {
                    // ハンディユーザーパスワード不要
                    handyUsers = HandyUserModel.GetHandyUserByHandyUserIDAndCode(databaseName, input.HandyUserID, input.HandyUserCode);
                }
                else
                {
                    handyUsers = HandyUserModel.GetHandyUserByHandyUserIDAndCodeAndPassword(databaseName, input.HandyUserID, input.HandyUserCode, input.HandyUserPassword);
                }
            }
            catch (Exception ex)
            {
                return Responce.ExServerError(ex);
            }
            if (handyUsers.Count != 1) return Responce.ExBadRequest("ログインに失敗しました");
            handyUser = handyUsers[0];

            // デポ情報の取得
            var depo = new DepoModel.M_Depo();
            var depos = new List<DepoModel.M_Depo>();
            try
            {
                depos = DepoModel.GetDepoByHandyUserID(databaseName, input.HandyUserID);
            }
            catch (Exception ex)
            {
                return Responce.ExServerError(ex);
            }
            if (depos.Count != 1) return Responce.ExBadRequest("デポ情報の取得に失敗しました");
            depo = depos[0];

            // デバイスチェック
            // 設定で登録した最新のデバイスと一致しているか
            var latestDevice = "";
            var connectionString = new GetConnectString(databaseName).ConnectionString;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                try
                {
                    var query = @"
                                        SELECT
                                           Device
                                        FROM D_HandyDevice 
                                        WHERE 1=1
                                            AND HandyUserID = @HandyUserID
                                        ORDER BY UpdateDate Desc
                                ";
                    var param = new
                    {
                        input.HandyUserID
                    };
                    latestDevice = connection.QueryFirstOrDefault<string>(query, param);
                    if (latestDevice != input.Device) return Responce.ExBadRequest("登録されたデバイスと一致しません");

                    var versionUpdateTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                    var SQL_UPDATE_Version = $@"
                                                UPDATE [M_HandyUser]
                                                SET [CurrentVersion] = @CurrentVersion, [VersionUpdateTime] = @VersionUpdateTime
                                                WHERE [HandyUserID] = @HandyUserID
                                                AND [CurrentVersion] != @CurrentVersion
                                             ;";
                    var param_update_version = new
                    {
                        HandyUserID = input.HandyUserID,
                        CurrentVersion = input.HandyAppVersion,
                        VersionUpdateTime = versionUpdateTime
                    };
                    var result = connection.Execute(SQL_UPDATE_Version, param_update_version);
                }
                catch (Exception ex)
                {
                    return Responce.ExServerError(ex);
                }
            }

            // ログイン成功
            var outPut = new LoginModel.LoginPostBackContent();
            outPut.CompanyName = company.CompanyName;
            outPut.HandyUserName = handyUser.HandyUserName;
            outPut.AdministratorFlag = handyUser.AdministratorFlag;
            outPut.DefaultHandyPageID = handyUser.DefaultHandyPageID;
            outPut.DepoID = depo.DepoID;
            outPut.DepoCode = depo.DepoCode;
            outPut.DepoName = depo.DepoName;
            outPut.TokenString = GenerateAccessToken(input, outPut);
            return Ok(outPut);

        }

        private string GenerateAccessToken(LoginPostBody loginPost, LoginPostBackContent loginPostContent)
        {
            // Create user claims
            var claims = new List<Claim>
            {
                new Claim("CompanyID", loginPost.CompanyID.ToString()),
                new Claim("CompanyName", loginPostContent.CompanyName),
                new Claim("HandyUserID", loginPost.HandyUserID.ToString()),
                new Claim("HandyUserCode", loginPost.HandyUserCode),
                new Claim("HandyUserName", loginPostContent.HandyUserName),
                new Claim("AdministratorFlag", loginPostContent.AdministratorFlag.ToString()),
                new Claim("DepoID", loginPostContent.DepoID.ToString()),
                new Claim("DepoCode", loginPostContent.DepoCode),
                new Claim("DepoName", loginPostContent.DepoName),
                new Claim("HandyAppVersion", loginPost.HandyAppVersion.ToString())
            };

            // Create a JWT
            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1), // Token expiration time
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"])),
                    SecurityAlgorithms.HmacSha256)
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            return accessToken;
        }

    }
}
