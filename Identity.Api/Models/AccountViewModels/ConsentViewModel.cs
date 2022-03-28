using System.Collections.Generic;
using System.Linq;
using IdentityServer4.Models;
using IdentityServer4.Validation;

namespace Identity.Api.Models.AccountViewModels
{
    public class ConsentViewModel : ConsentInputModel
    {
        public ConsentViewModel(ConsentInputModel model, string returnUrl, AuthorizationRequest request, ResourceValidationResult resources)
        {
            RememberConsent = model?.RememberConsent ?? true;
            ScopesConsented = model?.ScopesConsented ?? Enumerable.Empty<string>();

            ReturnUrl = returnUrl;

            ClientName = request.Client.ClientName;
            ClientUrl = request.Client.ClientUri;
            ClientLogoUrl = request.Client.LogoUri;
            AllowRememberConsent = request.Client.AllowRememberConsent;

            IdentityScopes = resources.Resources.IdentityResources.Select(x => new ScopeViewModel(x, ScopesConsented.Contains(x.Name) || model == null)).ToArray();
            var apiScopes = new List<ScopeViewModel>();
            foreach (var parsedScope in request.ValidatedResources.ParsedScopes)
            {
                var apiScope = request.ValidatedResources.Resources.FindApiScope(parsedScope.ParsedName);
                if (apiScope != null)
                {
                    var scopeVm = new ScopeViewModel(parsedScope, apiScope, ScopesConsented.Contains(parsedScope.RawValue) || model == null);
                    apiScopes.Add(scopeVm);
                }
            }
            if (ConsentOptions.EnableOfflineAccess && request.ValidatedResources.Resources.OfflineAccess)
            {
                apiScopes.Add(new ScopeViewModel(ScopesConsented.Contains(IdentityServer4.IdentityServerConstants.StandardScopes.OfflineAccess) || model == null));
            }
            ApiScopes = apiScopes;
        }

        public string ClientName { get; set; }
        public string ClientUrl { get; set; }
        public string ClientLogoUrl { get; set; }
        public bool AllowRememberConsent { get; set; }

        public IEnumerable<ScopeViewModel> IdentityScopes { get; set; }
        public IEnumerable<ScopeViewModel> ApiScopes { get; set; }
    }

    public class ScopeViewModel
    {
        public ScopeViewModel(ParsedScopeValue parsedScopeValue, ApiScope scope, bool check)
        {
            Name = parsedScopeValue.RawValue;
            DisplayName = scope.DisplayName ?? scope.Name;
            Description = scope.Description;
            Emphasize = scope.Emphasize;
            Required = scope.Required;
            Checked = check || scope.Required;
        }

        public ScopeViewModel(IdentityResource identity, bool check)
        {
            Name = identity.Name;
            DisplayName = identity.DisplayName ?? identity.Name;
            Description = identity.Description;
            Emphasize = identity.Emphasize;
            Required = identity.Required;
            Checked = check || identity.Required;
        }

        public ScopeViewModel(bool check)
        {
            Name = IdentityServer4.IdentityServerConstants.StandardScopes.OfflineAccess;
            DisplayName = ConsentOptions.OfflineAccessDisplayName;
            Description = ConsentOptions.OfflineAccessDescription;
            Emphasize = true;
            Checked = check;
        }

        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public bool Emphasize { get; set; }
        public bool Required { get; set; }
        public bool Checked { get; set; }
    }
}
