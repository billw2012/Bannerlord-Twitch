using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BannerlordTwitch.Util;
using HarmonyLib;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ObjectSystem;
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
        
        IDocumentationGenerator Img(CharacterCode cc, string altText);
        IDocumentationGenerator Img(string css, CharacterCode cc, string altText);
    }
    
    [HarmonyPatch]
    public class DocumentationGenerator : IDocumentationGenerator
    {
        private readonly List<string> docs;

        private static string CSSFileName = "Bannerlord-Twitch-Documentation.css";
        private static string CSSFullPath => Path.Combine(Path.GetDirectoryName(typeof(Settings).Assembly.Location) ?? ".", "..", "..", CSSFileName);

        public DocumentationGenerator()
        {
            docs = new List<string>
            {
                "<!DOCTYPE html><html>",
                "<head>",
                $"<link rel=\"stylesheet\" href=\"style.css\">",
                "</head>",
                "<body>",
                "<div class=\"content\">",
                "<h1>Bannerlord-Twitch Documentation</h1>",
            };
        }

        public void Document(IDocumentable documentable)
        {
            MainThreadSync.Run(() => documentable.GenerateDocumentation(this));
        }

        private static string DocumentationRootDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Mount and Blade II Bannerlord",
            "Configs", "BLT-documentation");

        public static string DocumentationPath => Path.Combine(DocumentationRootDir, "index.html");

        public void Save()
        {
            MainThreadSync.Run(() => {
                WaitForPendingImages();
                
                docs.Add("</div></html></body>");
                try
                {
                    Directory.CreateDirectory(DocumentationRootDir);
                    File.WriteAllLines(DocumentationPath, docs);
                    string targetCSSFilePath = Path.Combine(DocumentationRootDir, "style.css");
                    if(File.Exists(targetCSSFilePath))
                        File.Delete(targetCSSFilePath);
                    File.Copy(CSSFullPath, targetCSSFilePath);
                }
                catch (Exception e)
                {
                    Log.Error($"Couldn't write documentation: {e.Message}");
                }
            });
        }

        // public void SavePdf()
        // {
        //     //var cssData = PdfGenerator.ParseStyleSheet(File.ReadAllText(CSSFullPath));
        //     Save();
        //     
        //     var pdf = PdfGenerator.GeneratePdf(string.Join("\n", docs), 
        //         new PdfGenerateConfig
        //         {
        //             //PageSize = PageSize.A0,
        //             ManualPageSize = XSize.FromSize(new (1000, 4000))
        //         },
        //         //cssData: cssData,
        //         stylesheetLoad: (sender, args) =>
        //         {
        //             args.SetStyleSheetData = PdfGenerator.ParseStyleSheet(
        //                 File.ReadAllText(Path.Combine(DocumentationRootDir, args.Src))
        //                 );
        //         }, 
        //         imageLoad: (sender, args) =>
        //         {
        //             args.Callback(Path.Combine(DocumentationRootDir, args.Src));
        //         });
        //
        //     pdf.Save(Path.Combine(DocumentationRootDir, "blt-docs.pdf"));
        // }

        // private static string LinkToAnchor(string text, string anchorTag = null)
        //     => $"<a href=\"#{text}{anchorTag ?? ""}\">{text}</a>";
        //
        // private static string MakeAnchor(string text, string anchorTag = null)
        //     => $"<a name=\"{text}{anchorTag ?? ""}\">{text}</a>";

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

        private int imageId;
        private readonly List<string> pendingImages = new();

        private void WaitForPendingImages()
        {
            for (int i = 0; i < 100 && !pendingImages.All(File.Exists); i++)
            {
                Thread.Sleep(100);
            }
        }

        public IDocumentationGenerator Img(ItemObject item) => Img(null, item);
        public IDocumentationGenerator Img(string css, ItemObject item)
        {
            string localPath = AddImage(css, item.Name.ToString());
            TableauCacheManager.Current.BeginCreateItemTexture(item, Hero.MainHero.ClanBanner.Serialize(),
                texture => TextureComplete(item.Name.ToString(), localPath, texture));
            return this;
        }
        
        public IDocumentationGenerator Img(CharacterCode cc, string altText) => Img(null, cc, altText);
        public IDocumentationGenerator Img(string css, CharacterCode cc, string altText)
        {
            string localPath = AddImage(css, altText);
            overrideRenderSettings = camera =>
            {
                //camera.SetViewVolume(false, -500, 500, 0, 1000, -500, 500);
                camera.Position -= camera.Direction * 1.2f;
                camera.Position -= Vec3.Up * 0.6f;
                camera.SetFovHorizontal(camera.GetFovHorizontal(), 120f / 256f, 0.1f, 1000f);
                return (120, 256);
            };
            TableauCacheManager.Current.BeginCreateCharacterTexture(cc, 
                texture => TextureComplete(altText, localPath, texture), true);
            return this;
        }
        
        private Bitmap SwapRedAndBlueChannels(Bitmap bitmap)
        {
            var imageAttr = new ImageAttributes();
            imageAttr.SetColorMatrix(new(
                new[]
                {
                    new[] {0.0F, 0.0F, 1.0F, 0.0F, 0.0F},
                    new[] {0.0F, 1.0F, 0.0F, 0.0F, 0.0F},
                    new[] {1.0F, 0.0F, 0.0F, 0.0F, 0.0F},
                    new[] {0.0F, 0.0F, 0.0F, 1.0F, 0.0F},
                    new[] {0.0F, 0.0F, 0.0F, 0.0F, 1.0F}
                }
            ));
            var temp = new Bitmap(bitmap.Width, bitmap.Height);
            var pixel = GraphicsUnit.Pixel;
            using var g = Graphics.FromImage(temp);
            g.DrawImage(bitmap, Rectangle.Round(bitmap.GetBounds(ref pixel)), 0, 0, 
                bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, imageAttr);
            return temp;
        }

        private async void TextureComplete(string name, string localPath, Texture texture)
        {
            try
            {
                string path = Path.Combine(DocumentationRootDir, localPath);

                pendingImages.Add(localPath);
                texture.TransformRenderTargetToResource(localPath);
                texture.SaveToFile(localPath);
                for (int i = 0; i < 100 && !File.Exists(localPath); i++)
                {
                    await Task.Delay(100);
                }

                if (File.Exists(localPath))
                {
                    Directory.CreateDirectory(DocumentationRootDir);
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    var corrected = SwapRedAndBlueChannels(new Bitmap(Path.GetFileName(path)));
                    corrected.Save(path);
                }
                else
                {
                    Log.Error($"Couldn't export image for {name} to {localPath}");
                }
            }
            catch (Exception e)
            {
                Log.Exception("Img", e);
            }
        }

        private string AddImage(string css, string name)
        {
            string localPath = $"blt_img_{++imageId}.png";
            if (File.Exists(localPath))
                File.Delete(localPath);
            docs.Add(css == null
                ? $"<img src=\"{localPath}\" alt=\"{name}\">"
                : $"<img class=\"{css}\" src=\"{localPath}\" alt=\"{name}\">");
            return localPath;
        }

        private static Func<Camera, (int, int)> overrideRenderSettings;
        
        [HarmonyPatch(typeof(ThumbnailCreatorView), nameof(ThumbnailCreatorView.RegisterEntityWithoutTexture)), HarmonyPrefix, UsedImplicitly]
        private static void RegisterEntityWithoutTexturePrefix(Camera camera, ref int width, ref int height)
        {
            if (overrideRenderSettings != null)
            {
                (width, height) = overrideRenderSettings(camera);
                overrideRenderSettings = null;
            }
        }
        
        //
        // private static GameEntity CreateCharacterBaseEntityPostfix(
        //     CharacterCode characterCode,
        //     Scene scene,
        //     ref Camera camera,
        //     bool isBig)
        // {
        //     
        // }
        // public static void BeginCreateCharacterTexture(CharacterCode characterCode, Action<Texture> setAction, bool isBig)
        // {
        //     if (MBObjectManager.Instance == null)
        //         return;
        //
        //     characterCode.BodyProperties = new (
        //         new (
        //             (int) characterCode.BodyProperties.Age, 
        //             (int) characterCode.BodyProperties.Weight, 
        //             (int) characterCode.BodyProperties.Build), 
        //         characterCode.BodyProperties.StaticProperties);
        //     string str = characterCode.CreateNewCodeString() + (isBig ? "1" : "0") + "_blt";
        //     Texture texture;
        //
        //     var _characterVisuals = (ThumbnailCache) AccessTools.Field(
        //         typeof(TableauCacheManager), "_characterVisuals").GetValue(TableauCacheManager.Current);
        //     var _renderCallbacks = (Dictionary<string, TableauCacheManager.RenderDetails>) AccessTools.Field(
        //         typeof(TableauCacheManager), "_renderCallbacks").GetValue(TableauCacheManager.Current);
        //     if (_characterVisuals.GetValue(str, out texture))
        //     {
        //         if (this._renderCallbacks.ContainsKey(str))
        //             this._renderCallbacks[str].Actions.Add(setAction);
        //         else if (setAction != null)
        //             setAction(texture);
        //         _characterVisuals.AddReference(str);
        //     }
        //     else
        //     {
        //         Camera camera = (Camera) null;
        //         int index = isBig ? 0 : 4;
        //         GameEntity characterBaseEntity = this.CreateCharacterBaseEntity(characterCode,
        //             BannerlordTableauManager.TableauCharacterScenes[index], ref camera, isBig);
        //         GameEntity entity = this.FillEntityWithPose(characterCode, characterBaseEntity,
        //             BannerlordTableauManager.TableauCharacterScenes[index]);
        //         int width = 256;
        //         int height = isBig ? 120 : 184;
        //         this._thumbnailCreatorView.RegisterEntityWithoutTexture(
        //             BannerlordTableauManager.TableauCharacterScenes[index], camera, entity, width, height,
        //             this.characterTableauGPUAllocationIndex, str,
        //             "character_tableau_" + this._characterCount.ToString());
        //         ++this._characterCount;
        //         _characterVisuals.Add(str, (Texture) null);
        //         _characterVisuals.AddReference(str);
        //         if (!this._renderCallbacks.ContainsKey(str))
        //             this._renderCallbacks.Add(str, new TableauCacheManager.RenderDetails(new List<Action<Texture>>()));
        //         this._renderCallbacks[str].Actions.Add(setAction);
        //     }
        // }
    }
}