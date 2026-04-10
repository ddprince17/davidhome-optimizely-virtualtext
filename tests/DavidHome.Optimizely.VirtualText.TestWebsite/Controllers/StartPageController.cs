using DavidHome.Optimizely.VirtualText.TestWebsite.Models.Pages;
using DavidHome.Optimizely.VirtualText.TestWebsite.Models.ViewModels;
using EPiServer.Applications;
using EPiServer.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace DavidHome.Optimizely.VirtualText.TestWebsite.Controllers;

public class StartPageController : PageControllerBase<StartPage>
{
    private readonly IApplicationResolver _applicationResolver;

    public StartPageController(IApplicationResolver applicationResolver)
    {
        _applicationResolver = applicationResolver;
    }

    public IActionResult Index(StartPage currentPage)
    {
        var model = PageViewModel.Create(currentPage);
        var startPage = (_applicationResolver.GetByContent(currentPage.ContentLink, false) as IRoutableApplication)?.EntryPoint;

        // Check if it is the StartPage or just a page of the StartPage type.
        if (startPage?.CompareToIgnoreWorkID(currentPage.ContentLink) ?? false)
        {
            // Connect the view models logotype property to the start page's to make it editable
            var editHints = ViewData.GetEditHints<PageViewModel<StartPage>, StartPage>();
            editHints.AddConnection(m => m.Layout.Logotype, p => p.SiteLogotype);
            editHints.AddConnection(m => m.Layout.ProductPages, p => p.ProductPageLinks);
            editHints.AddConnection(m => m.Layout.CompanyInformationPages, p => p.CompanyInformationPageLinks);
            editHints.AddConnection(m => m.Layout.NewsPages, p => p.NewsPageLinks);
            editHints.AddConnection(m => m.Layout.CustomerZonePages, p => p.CustomerZonePageLinks);
        }

        return View(model);
    }
}
