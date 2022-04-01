// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.Model;

namespace Microsoft.Health.Fhir.Core.Features.Security;

public static class Constants
{
    public const string SmartOAuthUriExtension = "http://fhir-registry.smarthealthit.org/StructureDefinition/oauth-uris";
    public const string SmartOAuthUriExtensionToken = "token";
    public const string SmartOAuthUriExtensionAuthorize = "authorize";
    private static readonly Coding RestfulSecurityServiceOAuthCodeableConcept = new("http://terminology.hl7.org/CodeSystem/restful-security-service", "OAuth");
    private static readonly Coding RestfulSecurityServiceStu3OAuthCodeableConcept = new("http://hl7.org/fhir/restful-security-service", "OAuth");

    public static ref readonly Coding RestfulSecurityServiceOAuth => ref RestfulSecurityServiceOAuthCodeableConcept;

    public static ref readonly Coding RestfulSecurityServiceStu3OAuth => ref RestfulSecurityServiceStu3OAuthCodeableConcept;
}
