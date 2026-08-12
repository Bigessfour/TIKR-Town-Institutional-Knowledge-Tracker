using Syncfusion.Licensing;
var key = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
if (string.IsNullOrWhiteSpace(key)) { Console.WriteLine("MISSING"); return 2; }
key = key.Trim();
SyncfusionLicenseProvider.RegisterLicense(key);
string? err = null;
var ok = SyncfusionLicenseProvider.ValidateLicense([Platform.Blazor], out err);
Console.WriteLine(ok ? "VALID" : $"INVALID:{err}");
return ok ? 0 : 1;
