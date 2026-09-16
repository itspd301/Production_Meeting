using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProductionMeeting.Controllers;

// No Login/Logout here: authentication is Windows Authentication (Negotiate). The browser
// answers the server's 401 challenge with the signed-in user's Windows credentials
// automatically, so there is no form for the user to submit and nothing for this app to
// "log out" of.
[AllowAnonymous]
public class AccountController : Controller
{
    [HttpGet]
    public IActionResult AccessDenied(string? statusCode = null)
    {
        ViewBag.StatusCode = statusCode;
        return View();
    }
}
