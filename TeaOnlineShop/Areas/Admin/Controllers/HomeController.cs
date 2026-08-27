using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaOnlineShop.Authorization;

namespace TeaOnlineShop.Areas.Admin.Controllers;

public sealed class HomeController : AdminBaseController
{
    [Authorize(Policy = AppPermissions.DashboardOperationalView)]
    public IActionResult Index() => View();
}
