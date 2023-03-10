// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Localization;

/// <summary>
/// Determines the culture information for a request via the value of a cookie.
/// </summary>
public class CookieRequestCultureProvider : RequestCultureProvider
{
    private const char _cookieSeparator = '|';
    private const string _culturePrefix = "c=";
    private const string _uiCulturePrefix = "uic=";

    /// <summary>
    /// Represent the default cookie name used to track the user's preferred culture information, which is ".AspNetCore.Culture".
    /// </summary>
    public static readonly string DefaultCookieName = ".AspNetCore.Culture";

    /// <summary>
    /// The name of the cookie that contains the user's preferred culture information.
    /// Defaults to <see cref="DefaultCookieName"/>.
    /// </summary>
    public string CookieName { get; set; } = DefaultCookieName;

    /// <inheritdoc />
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var cookie = httpContext.Request.Cookies[CookieName];

        if (string.IsNullOrEmpty(cookie))
        {
            return NullProviderCultureResult;
        }

        var providerResultCulture = ParseCookieValue(cookie);

        return Task.FromResult<ProviderCultureResult?>(providerResultCulture);
    }

    /// <summary>
    /// Creates a string representation of a <see cref="RequestCulture"/> for placement in a cookie.
    /// </summary>
    /// <param name="requestCulture">The <see cref="RequestCulture"/>.</param>
    /// <returns>The cookie value.</returns>
    public static string MakeCookieValue(RequestCulture requestCulture)
    {
        ArgumentNullException.ThrowIfNull(requestCulture);

        return string.Join(_cookieSeparator,
            $"{_culturePrefix}{requestCulture.Culture.Name}",
            $"{_uiCulturePrefix}{requestCulture.UICulture.Name}");
    }

    /// <summary>
    /// Parses a <see cref="RequestCulture"/> from the specified cookie value.
    /// Returns <c>null</c> if parsing fails.
    /// </summary>
    /// <param name="value">The cookie value to parse.</param>
    /// <returns>The <see cref="RequestCulture"/> or <c>null</c> if parsing fails.</returns>
    public static ProviderCultureResult? ParseCookieValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var parts = value.Split(_cookieSeparator, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2)
        {
            return null;
        }

        var potentialCultureName = parts[0];
        var potentialUICultureName = parts[1];

        if (!potentialCultureName.StartsWith(_culturePrefix, StringComparison.Ordinal) || !
            potentialUICultureName.StartsWith(_uiCulturePrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var cultureName = potentialCultureName.Substring(_culturePrefix.Length);
        var uiCultureName = potentialUICultureName.Substring(_uiCulturePrefix.Length);

        if (cultureName == null && uiCultureName == null)
        {
            // No values specified for either so no match
            return null;
        }

        if (cultureName != null && uiCultureName == null)
        {
            // Value for culture but not for UI culture so default to culture value for both
            uiCultureName = cultureName;
        }
        else if (cultureName == null && uiCultureName != null)
        {
            // Value for UI culture but not for culture so default to UI culture value for both
            cultureName = uiCultureName;
        }

        return new ProviderCultureResult(cultureName, uiCultureName);
    }
}
