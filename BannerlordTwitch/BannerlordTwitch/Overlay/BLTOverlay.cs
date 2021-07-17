using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.StaticFiles;
using Owin;

namespace BLTOverlay
{
    public static class BLTOverlay
    {
        private static string WebRoot => Path.Combine(
            Path.GetDirectoryName(typeof(BLTOverlay).Assembly.Location) ?? ".",
            "..", "..", "web");

        private const int Port = 8087;

        private static string UrlRoot => $"http://{Dns.GetHostName()}:{Port}";
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
            }
                
            WebApp.Start(UrlBinding, app =>
            {
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
            
            Process.Start(UrlRoot);
        }
        
        private static void OpenPort()
        {
            MessageBox.Show($"For the BLT Overlay browser source to work it needs to reserve " +
                            $"port {Port}.\nThis requires administrator privileges, " +
                            $"so they will be requested after you press Ok.\nIf successful, " +
                            $"this will only appear once.", "BLT Overlay");
            
            string netsh = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "netsh.exe");

            var startInfo = new ProcessStartInfo(netsh)
            {
                Arguments = $"http add urlacl url={UrlBinding} user=everyone",
                UseShellExecute = true,
                Verb = "runas"
            };

            try
            {
                Process.Start(startInfo)?.WaitForExit(5000);
            }
            catch(FileNotFoundException)
            {
                // netsh.exe was missing?
            }
            catch(Win32Exception)
            {
                // user may have aborted the action, or doesn't have access
            }
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
    
    // public class MyHub : Hub
    // {
    //     public void Send(string message)
    //     {
    //         Clients.All.addMessage(name, message);
    //     }
    // }
}