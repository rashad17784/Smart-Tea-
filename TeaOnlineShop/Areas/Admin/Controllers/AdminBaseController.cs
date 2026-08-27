using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaOnlineShop.Authorization;

namespace TeaOnlineShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = AppPermissions.AdminAccess)]
    public abstract class AdminBaseController : Controller
    {
        // Common admin controller functionality can be added here
    }
} 
