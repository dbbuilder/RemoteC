#!/bin/bash

# Fix Dictionary Details to be serialized as JSON strings
# This is a bit complex so we'll do it with a more sophisticated approach

# First, let's find all the patterns and fix them
perl -i -pe '
    if (/Details = new Dictionary<string, object>/) {
        $_ = "                    Details = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>\n";
    }
    if (/^(\s+)\}$/ && $prev_line =~ /\[".*"\]/) {
        $_ = "$1})\n";
    }
    $prev_line = $_;
' src/RemoteC.Api/Services/IdentityProviderService.cs

echo "Fixed Details Dictionary serialization"