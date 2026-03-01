using DavidHome.Optimizely.VirtualText.TestWebsite.Models.Pages;
using DavidHome.Optimizely.VirtualText.TestWebsite.Models.ViewModels;
using EPiServer.Applications;
using EPiServer.Data;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace DavidHome.Optimizely.VirtualText.TestWebsite.Business;

[ServiceConfiguration]
public class PageViewContextFactory
{
    private readonly IContentLoader _contentLoader;
    private readonly UrlResolver _urlResolver;
    private readonly IDatabaseMode _databaseMode;
    private readonly CookieAuthenticationOptions _cookieAuthenticationOptions;
    private readonly IApplicationResolver _applicationResolver;
    private readonly SystemDefinition _systemDefinition;

    public PageViewContextFactory(
        IContentLoader contentLoader,
        UrlResolver urlResolver,
        IDatabaseMode databaseMode,
        IOptionsMonitor<CookieAuthenticationOptions> optionMonitor,
        IApplicationResolver applicationResolver,
        SystemDefinition systemDefinition)
    {
        _contentLoader = contentLoader;
        _urlResolver = urlResolver;
        _databaseMode = databaseMode;
        _cookieAuthenticationOptions = optionMonitor.Get(IdentityConstants.ApplicationScheme);
        _applicationResolver = applicationResolver;
        _systemDefinition = systemDefinition;
    }

    public virtual LayoutModel CreateLayoutModel(ContentReference currentContentLink, HttpContext httpContext)
    {
        var startPageContentLink = (_applicationResolver.GetByContent(currentContentLink, false) as IRoutableApplication)?.RoutingEntryPoint;

        // Use the content link with version information when editing the startpage,
        // otherwise the published version will be used when rendering the props below.
        if (currentContentLink.CompareToIgnoreWorkID(startPageContentLink))
        {
            startPageContentLink = currentContentLink;
        }

        var startPage = _contentLoader.Get<StartPage>(startPageContentLink);

        return new LayoutModel
        {
            Logotype = startPage.SiteLogotype,
            LogotypeLinkUrl = new HtmlString(_urlResolver.GetUrl(startPageContentLink)),
            ProductPages = startPage.ProductPageLinks,
            CompanyInformationPages = startPage.CompanyInformationPageLinks,
            NewsPages = startPage.NewsPageLinks,
            CustomerZonePages = startPage.CustomerZonePageLinks,
            LoggedIn = httpContext.User.Identity.IsAuthenticated,
            LoginUrl = new HtmlString(GetLoginUrl(currentContentLink)),
            SearchActionUrl = new HtmlString(_urlResolver.GetUrl(startPage.SearchPageLink)),
            IsInReadonlyMode = _databaseMode.DatabaseMode == DatabaseMode.ReadOnly
        };
    }

    private string GetLoginUrl(ContentReference returnToContentLink)
    {
        return $"{_cookieAuthenticationOptions?.LoginPath.Value ?? Globals.LoginPath}?ReturnUrl={_urlResolver.GetUrl(returnToContentLink)}";
    }

    public virtual IContent GetSection(ContentReference contentLink)
    {
        var currentContent = _contentLoader.Get<IContent>(contentLink);
        var startPage = (_applicationResolver.GetByContent(contentLink, false) as IRoutableApplication)?.RoutingEntryPoint;

        if (IsSectionRoot(currentContent.ParentLink))
        {
            return currentContent;
        }

        return _contentLoader.GetAncestors(contentLink)
            .OfType<PageData>()
            .SkipWhile(x => !IsSectionRoot(x.ParentLink))
            .FirstOrDefault();

        bool IsSectionRoot(ContentReference contentReference) =>
            ContentReference.IsNullOrEmpty(contentReference) ||
            contentReference.Equals(startPage) ||
            contentReference.Equals(_systemDefinition.RootPage);
    }
}
