#!/bin/bash

# Fix EntityType to ResourceType and EntityId to ResourceId in IdentityProviderService
sed -i 's/EntityType = /ResourceType = /g' src/RemoteC.Api/Services/IdentityProviderService.cs
sed -i 's/EntityId = /ResourceId = /g' src/RemoteC.Api/Services/IdentityProviderService.cs

echo "Fixed EntityType/EntityId references"