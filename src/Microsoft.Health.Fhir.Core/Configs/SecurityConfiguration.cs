// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License (MIT).See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Fhir.Core.Configs;

public class SecurityConfiguration
{
    public bool Enabled { get; set; }

    public bool EnableAadSmartOnFhirProxy { get; set; }

    public AuthenticationConfiguration Authentication { get; set; } = new();

    public virtual HashSet<string> PrincipalClaims { get; } = new(StringComparer.Ordinal);

    public AuthorizationConfiguration Authorization { get; set; } = new();
}
