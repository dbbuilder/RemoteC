# PowerShell script to disable Hangfire in Program.cs
$programPath = "D:\dev2\remotec\src\RemoteC.Api\Program.cs"

Write-Host "Disabling Hangfire in Program.cs..." -ForegroundColor Yellow

# Backup the original file
Copy-Item $programPath "$programPath.original" -Force

# Read the file content
$content = Get-Content $programPath -Raw

# Replace Hangfire configuration with conditional logic
$modifiedContent = $content -replace `
    '// Add Hangfire for background jobs\s*\n\s*builder\.Services\.AddHangfire\(configuration => configuration[\s\S]*?builder\.Services\.AddHangfireServer\(\);', `
    @'
// Add Hangfire for background jobs (conditionally)
            var hangfireEnabled = builder.Configuration.GetValue<bool>("Hangfire:Enabled", true);
            if (hangfireEnabled && !string.IsNullOrEmpty(builder.Configuration.GetConnectionString("DefaultConnection")) && !builder.Configuration.GetConnectionString("DefaultConnection").Contains(".db"))
            {
                builder.Services.AddHangfire(configuration => configuration
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

                builder.Services.AddHangfireServer();
            }
'@

# Also update the Hangfire dashboard configuration
$modifiedContent = $modifiedContent -replace `
    '// Add Hangfire Dashboard\s*\n\s*app\.UseHangfireDashboard\("/hangfire"[\s\S]*?\}\);', `
    @'
// Add Hangfire Dashboard (conditionally)
            if (app.Services.GetService<Hangfire.IGlobalConfiguration>() != null)
            {
                app.UseHangfireDashboard("/hangfire", new DashboardOptions
                {
                    Authorization = new[] { new HangfireAuthorizationFilter() }
                });
            }
'@

# Write the modified content back
$modifiedContent | Out-File $programPath -Encoding UTF8

Write-Host "Hangfire has been disabled!" -ForegroundColor Green
Write-Host "The original file has been backed up as Program.cs.original" -ForegroundColor Yellow