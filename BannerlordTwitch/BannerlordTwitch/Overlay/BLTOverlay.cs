using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using BannerlordTwitch.Util;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.Logging;
using Microsoft.Owin.StaticFiles;
using Owin;
using TaleWorlds.Core;

namespace BLTOverlay
{
    public static class BLTOverlay
    {
        private static string WebRoot => Path.Combine(
            Path.GetDirectoryName(typeof(BLTOverlay).Assembly.Location) ?? ".",
            "..", "..", "web");

        private const int Port = 8087;

        public static string UrlRoot => $"http://{Dns.GetHostName()}:{Port}";
        private static string UrlBinding => $"http://*:{Port}/";

        private const string JSExtension =
#if DEBUG
                "js"
#else
                "min.js"
#endif
            ;

        public static void Start()
        {
            string indexTemplate = File.ReadAllText(Path.Combine(WebRoot, "index-template.html"));
            
            overlayProviders.Sort((l, r) => l.order.CompareTo(r.order));

            indexTemplate = indexTemplate.Replace("$custom_styles$", 
                string.Join("\n", overlayProviders
                    .Where(o => !string.IsNullOrWhiteSpace(o.css))
                    .Select(o => $"<style type=\"text/css\">\n{o.css}\n</style>")));
            indexTemplate = indexTemplate.Replace("$custom_body$", 
                string.Join("\n", overlayProviders
                    .Where(o => !string.IsNullOrWhiteSpace(o.body))
                    .Select(o => o.body)));
            indexTemplate = indexTemplate.Replace("$custom_scripts$", 
                string.Join("\n", overlayProviders
                    .Where(o => !string.IsNullOrWhiteSpace(o.script))
                    .Select(o => $"<script type=\"text/javascript\">\n{o.script}\n</script>")));

            indexTemplate = indexTemplate.Replace("$url_root$", UrlRoot);
            indexTemplate = indexTemplate.Replace("$min_js$", JSExtension);

            File.WriteAllText(Path.Combine(WebRoot, "index.html"), indexTemplate);

            try
            {
                var httpListener = new HttpListener();
                httpListener.Prefixes.Add(UrlBinding);
                httpListener.Start();
                httpListener.Stop();
            }
            catch (HttpListenerException)
            {
                OpenPort();
                return;
            }
                
            GlobalHost.Configuration.ConnectionTimeout = TimeSpan.FromDays(1);
            GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromDays(1);

            WebApp.Start(UrlBinding, app =>
            {
                app.SetLoggerFactory(new LoggerFactory());
                app.UseCors(CorsOptions.AllowAll);
                app.MapSignalR();
                var physicalFileSystem = new PhysicalFileSystem(WebRoot);
                var options = new FileServerOptions
                {
                    EnableDefaultFiles = true,
                    FileSystem = physicalFileSystem,
                    StaticFileOptions = {FileSystem = physicalFileSystem, ServeUnknownFileTypes = true},
                    DefaultFilesOptions = {DefaultFileNames = new[] {"index.html"}}
                };
                app.UseFileServer(options);
            });
            
            // Process.Start(UrlRoot);
        }
        
        private static void OpenPort()
        {
            InformationManager.ShowInquiry(
                new ("BLT Overlay",
                    $"For the BLT Overlay Browser Source to work it needs to reserve " +
                    $"port {Port}, and allow it via the Windows Firewall.\nThis requires administrator privileges, " +
                    $"which will be requested after you press Ok.\nIf successful, you won't see this popup again.",
                    true, false, "Okay", null,
                    () =>
                    {
                        // To remove them again:
                        
                        // netsh http delete urlacl url={UrlBinding}
                        // netsh advfirewall firewall delete rule name=BLTOverlay
                        
                        // netsh http delete urlacl url=http://*:8087/ & netsh advfirewall firewall delete rule name=BLTOverlay

                        try
                        {
                            // Get the translated version of the "everyone" user account
                            var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                            var account = (NTAccount) sid?.Translate(typeof(NTAccount)); 
                            
                            var proc = Process.Start(new ProcessStartInfo("cmd.exe")
                            {
                                Arguments =
                                    $"/c netsh http add urlacl url={UrlBinding} user={account?.Value ?? "everyone"} " +
                                    $"& netsh advfirewall firewall add rule name=BLTOverlay dir=in action=allow protocol=TCP localport={Port}",
                                UseShellExecute = true,
                                Verb = "runas"
                            });
                            proc?.WaitForExit(5000);
                            InformationManager.ShowInquiry(
                                new ("BLT Overlay",
                                    $"Configuration Successful!\nYou can now access the overlay at {UrlRoot}.\nYou can find this link again on the Authorize tab in the BLT Configure window.",
                                    true, false, "Okay", null,
                                    Start, () => {}), true);
                        }
                        catch(Exception e)
                        {
                            InformationManager.ShowInquiry(
                                new ("BLT Overlay",
                                    $"Configuration FAILED:\n  \"{e.Message}\"\nYou may not be able to access the overlay.\nReport this problem in the discord.",
                                    true, false, "Okay", null,
                                    () => {}, () => {}), true);
                            Log.Exception($"{nameof(BLTOverlay)}.{nameof(OpenPort)}", e, noRethrow: true);
                        }
                    }, () => {}), true);
        }

        private class OverlayProvider
        {
            public string id;
            public int order;
            public string css;
            public string body;
            public string script;
        }

        private static List<OverlayProvider> overlayProviders = new();
        
        public static void Register(string id, int order, string css, string body, string script)
        {
            overlayProviders.Add(new ()
            {
                id = id,
                order = order,
                css = css,
                body = body,
                script = script,
            });
        }
    }
}