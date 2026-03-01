using System.Globalization;
using DavidHome.Optimizely.VirtualText.TestWebsite.Models.Pages;
using EPiServer.Filters;
using EPiServer.Applications;
using EPiServer.ServiceLocation;
using EPiServer.Shell.Configuration;

namespace DavidHome.Optimizely.VirtualText.TestWebsite.Business;

[ServiceConfiguration(Lifecycle = ServiceInstanceScope.Singleton)]
public class ContentLocator
{
    private readonly IContentLoader _contentLoader;
    private readonly IContentProviderManager _providerManager;
    private readonly IPageCriteriaQueryService _pageCriteriaQueryService;
    private readonly IApplicationResolver _applicationResolver;

    public ContentLocator(
        IContentLoader contentLoader,
        IContentProviderManager providerManager,
        IPageCriteriaQueryService pageCriteriaQueryService,
        IApplicationResolver applicationResolver)
    {
        _contentLoader = contentLoader;
        _providerManager = providerManager;
        _pageCriteriaQueryService = pageCriteriaQueryService;
        _applicationResolver = applicationResolver;
    }

    public virtual IEnumerable<T> GetAll<T>(ContentReference rootLink)
        where T : PageData
    {
        var children = _contentLoader.GetChildren<PageData>(rootLink);
        foreach (var child in children)
        {
            if (child is T childOfRequestedTyped)
            {
                yield return childOfRequestedTyped;
            }

            foreach (var descendant in GetAll<T>(child.ContentLink))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Returns pages of a specific page type
    /// </summary>
    /// <param name="pageLink"></param>
    /// <param name="recursive"></param>
    /// <param name="pageTypeId">ID of the page type to filter by</param>
    /// <returns></returns>
    public IEnumerable<PageData> FindPagesByPageType(ContentReference pageLink, bool recursive, int pageTypeId)
    {
        if (ContentReference.IsNullOrEmpty(pageLink))
        {
            throw new ArgumentNullException(nameof(pageLink), "No page link specified, unable to find pages");
        }

        var pages = recursive
            ? FindPagesByPageTypeRecursively(pageLink, pageTypeId)
            : _contentLoader.GetChildren<PageData>(pageLink);

        return pages;
    }

    // Type specified through page type ID
    private PageDataCollection FindPagesByPageTypeRecursively(ContentReference pageLink, int pageTypeId)
    {
        var criteria = new PropertyCriteriaCollection
        {
            new PropertyCriteria
            {
                Name = "PageTypeID",
                Type = PropertyDataType.PageType,
                Condition = CompareCondition.Equal,
                Value = pageTypeId.ToString(CultureInfo.InvariantCulture)
            }
        };

        // Include content providers serving content beneath the page link specified for the search
        if (_providerManager.ProviderMap.CustomProvidersExist)
        {
            var contentProvider = _providerManager.ProviderMap.GetProvider(pageLink);

            if (contentProvider.HasCapability(ContentProviderCapabilities.Search))
            {
                criteria.Add(new PropertyCriteria
                {
                    Name = "EPI:MultipleSearch",
                    Value = contentProvider.ProviderKey
                });
            }
        }

        return _pageCriteriaQueryService.FindPagesWithCriteria(pageLink, criteria);
    }

    /// <summary>
    /// Returns all contact pages beneath the main contacts container
    /// </summary>
    /// <returns></returns>
    public IEnumerable<ContactPage> GetContactPages()
    {
        var startPageLink = (_applicationResolver.GetByContext() as IRoutableApplication)?.RoutingEntryPoint;
        var contactsRootPageLink = _contentLoader.Get<StartPage>(startPageLink).ContactsPageLink;

        if (ContentReference.IsNullOrEmpty(contactsRootPageLink))
        {
            throw new MissingConfigurationException("No contact page root specified in site settings, unable to retrieve contact pages");
        }

        return _contentLoader.GetChildren<ContactPage>(contactsRootPageLink).OrderBy(p => p.PageName);
    }
}
