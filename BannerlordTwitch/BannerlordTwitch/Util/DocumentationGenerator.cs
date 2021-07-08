using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BannerlordTwitch.Util;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using YamlDotNet.Serialization;
using Path = System.IO.Path;

namespace BannerlordTwitch
{
    public interface IDocumentable
    {
        void GenerateDocumentation(IDocumentationGenerator generator);
    }
    
    public interface IDocumentationGenerator
    {
        IDocumentationGenerator Div(string css, Action content);
        IDocumentationGenerator Div(Action content);
        IDocumentationGenerator H1(string css, string content);
        IDocumentationGenerator H1(string content);
        IDocumentationGenerator H2(string css, string content);
        IDocumentationGenerator H2(string content);
        IDocumentationGenerator H3(string css, string content);
        IDocumentationGenerator H3(string content);
        IDocumentationGenerator Table(string css, Action content);
        IDocumentationGenerator Table(Action content);
        
        IDocumentationGenerator TR(string css, Action content);
        IDocumentationGenerator TR(Action content);
        IDocumentationGenerator TR(string css, string content);
        IDocumentationGenerator TR(string content);
        
        IDocumentationGenerator TH(string css, Action content);
        IDocumentationGenerator TH(Action content);
        IDocumentationGenerator TH(string css, string content);
        IDocumentationGenerator TH(string content);
        
        IDocumentationGenerator TD(string css, Action content);
        IDocumentationGenerator TD(Action content);
        IDocumentationGenerator TD(string css, string content);
        IDocumentationGenerator TD(string content);

        IDocumentationGenerator P(string css, string content);
        IDocumentationGenerator P(string content);
        
        IDocumentationGenerator Br();
        IDocumentationGenerator Img(ItemObject item);
        IDocumentationGenerator Img(string css, ItemObject item);
    }
    
    public class DocumentationGenerator : IDocumentationGenerator
    {
        private readonly List<string> docs;

        private static string CSSFileName = "Bannerlord-Twitch-Documentation.css";
        public DocumentationGenerator()
        {
            docs = new List<string>
            {
                "<!DOCTYPE html><html>",
                "<head>",
                $"<link rel=\"stylesheet\" href=\"{CSSFileName}\">",
                "</head>",
                "<body>",
                "<div class=\"content\">",
                "<h1>Bannerlord-Twitch Documentation</h1>",
            };
        }

        public void Document(IDocumentable documentable)
        {
            documentable.GenerateDocumentation(this);
         }

        public static string DocumentationRootDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Mount and Blade II Bannerlord",
            "Configs", "BLT-documentation");

        //Path.Combine(Path.GetDirectoryName(typeof(Settings).Assembly.Location), "..", "..", fileName);
        
        public static string DocumentationPath => Path.Combine(DocumentationRootDir, "Bannerlord-Twitch-Documentation.html"); 
            
        public void Save()
        {
            docs.Add("</div></html></body>");
            try
            {
                Directory.CreateDirectory(DocumentationRootDir);
                File.WriteAllLines(DocumentationPath, docs);
                string cssFileName = Path.Combine(Path.GetDirectoryName(typeof(Settings).Assembly.Location),
                    "..", "..", CSSFileName);
                string targetCSSFilePath = Path.Combine(DocumentationRootDir, CSSFileName);
                if(File.Exists(targetCSSFilePath))
                    File.Delete(targetCSSFilePath);
                File.Copy(cssFileName, Path.Combine(DocumentationRootDir, CSSFileName));
                // string css = File.ReadAllText(cssFileName);
                // FileSystem.SaveFileString(FileSystem.GetConfigPath("Bannerlord-Twitch-Documentation.css"), css);
                // FileSystem.SaveFileString(FileSystem.GetConfigPath(fileName),
                //     string.Join("\n", docs.Select(s => s + "  ")));
            }
            catch (Exception e)
            {
                Log.Error($"Couldn't write documentation: {e.Message}");
            }
        }

        private static string LinkToAnchor(string text, string anchorTag = null)
            => $"<a href=\"#{text}{anchorTag ?? ""}\">{text}</a>";

        private static string MakeAnchor(string text, string anchorTag = null)
            => $"<a name=\"{text}{anchorTag ?? ""}\">{text}</a>";

        private IDocumentationGenerator ScopedTag(string tag, string css, Action content)
        {
            docs.Add(css != null ? $"<{tag} class=\"{css}\">" : $"<{tag}>");
            content();
            docs.Add($"</{tag}>");
            return this;
        }
        
        private IDocumentationGenerator Tag(string tag, string css, string content)
        {
            docs.Add(
                css != null 
                    ? $"<{tag} class={css}>{content}</{tag}>"
                    : $"<{tag}>{content}</{tag}>"
                );
            return this;
        }
        
        public IDocumentationGenerator Div(string css, Action content) => ScopedTag("div", css, content);
        public IDocumentationGenerator Div(Action content) => Div(null, content);
        public IDocumentationGenerator H1(string css, string content) => Tag("h1", css, content);
        public IDocumentationGenerator H1(string content) => H1(null, content);
        public IDocumentationGenerator H2(string css, string content) => Tag("h2", css, content);
        public IDocumentationGenerator H2(string content) => H2(null, content);
        public IDocumentationGenerator H3(string css, string content) => Tag("h3", css, content);
        public IDocumentationGenerator H3(string content) => H3(null, content);

        public IDocumentationGenerator Table(string css, Action content) => ScopedTag("table", css, content);
        public IDocumentationGenerator Table(Action content) => Table(null, content);

        public IDocumentationGenerator TR(string css, Action content) => ScopedTag("tr", css, content);
        public IDocumentationGenerator TR(Action content) => TR(null, content);
        public IDocumentationGenerator TR(string css, string content) => Tag("tr", css, content);
        public IDocumentationGenerator TR(string content) => TR(null, content);

        public IDocumentationGenerator TH(string css, Action content) => ScopedTag("th", css, content);
        public IDocumentationGenerator TH(Action content) => TH(null, content);
        public IDocumentationGenerator TH(string css, string content) => Tag("th", css, content);
        public IDocumentationGenerator TH(string content) => TH(null, content);
        
        public IDocumentationGenerator TD(string css, Action content) => ScopedTag("td", css, content);
        public IDocumentationGenerator TD(Action content) => TD(null, content);
        public IDocumentationGenerator TD(string css, string content) => Tag("td", css, content);
        public IDocumentationGenerator TD(string content) => TD(null, content);

        public IDocumentationGenerator P(string css, string content) => Tag("p", css, content);
        public IDocumentationGenerator P(string content) => P(null, content);

        public IDocumentationGenerator Br()
        {
            docs.Add("<br>");
            return this;
        }

        private int imageId = 0;
        // private Scene drawScene;

        public IDocumentationGenerator Img(ItemObject item) => Img(null, item);
        public IDocumentationGenerator Img(string css, ItemObject item)
        {
            string localPath = $"blt_img_{++imageId}.png";
            if (File.Exists(localPath))
                File.Delete(localPath);
            docs.Add(css == null
                ? $"<img src=\"images\\{localPath}\" alt=\"{item.Name}\">"
                : $"<img class=\"{css}\" src=\"images\\{localPath}\" alt=\"{item.Name}\">");
            TableauCacheManager.Current.BeginCreateItemTexture(item, Hero.MainHero.ClanBanner.Serialize(),
                async texture =>
                {
                    try
                    {
                        texture.TransformRenderTargetToResource(localPath);
                        texture.SaveToFile(localPath);
                        for (int i = 0; i < 10 && !File.Exists(localPath); i++)
                        {
                            await Task.Delay(1000);
                        }

                        if (File.Exists(localPath))
                        {
                            string path = Path.Combine(DocumentationRootDir, "images", localPath);
                            Directory.CreateDirectory(Path.Combine(DocumentationRootDir, "images"));
                            if (File.Exists(path))
                                File.Delete(path);
                            File.Move(Path.GetFileName(path), path);
                        }
                        else
                        {
                            Log.Error($"Couldn't export image for {item.Name} to {localPath}");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Exception("Img", e);
                    }
                });
            return this;
        }
    }
}