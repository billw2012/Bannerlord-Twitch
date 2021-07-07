using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using BannerlordTwitch.Util;
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
    }
    
    public class DocumentationGenerator : IDocumentationGenerator
    {
        private readonly List<string> docs;

        public DocumentationGenerator()
        {
            docs = new List<string>
            {
                "<!DOCTYPE html><html>",
                "<head>",
                "<link rel=\"stylesheet\" href=\"Bannerlord-Twitch-Documentation.css\">",
                "</head>",
                "<body>",
                "<div class=\"content\">",
                "<h1>Bannerlord-Twitch Documentation</h1>",
            };
        }

        public void Document(IDocumentable documentable)
        {
            documentable.GenerateDocumentation(this);
            // var documentedTypes = new List<Type>();
            // docs.Add($"<h2>Reward</h2>");
            // DocumentType(docs, typeof(Reward), documentedTypes);
            // docs.Add($"<h3>Reward Actions (each is documented fully further down)</h3>");
            // HtmlTable(docs,
            //     new[] {"Name", "Description"},
            //     rewardHandlers.Values.Select(v => new[]
            //     {
            //         LinkToAnchor(v.GetType().Name, "Action"),
            //         DocStr(v.GetType()) ?? "(none)"
            //     }), "aclist");
            //
            // docs.Add($"<h2>Command</h2>");
            // DocumentType(docs, typeof(Command), documentedTypes);
            // docs.Add($"<h3>Command Handlers (each is documented fully further down)</h3>");
            // HtmlTable(docs,
            //     new[] {"Name", "Description"},
            //     commandHandlers.Values
            //         .Select(v => new[]
            //         {
            //             LinkToAnchor(v.GetType().Name, "Command"),
            //             DocStr(v.GetType()) ?? "(none)"
            //         }), "aclist");
            //
            // foreach (var action in rewardHandlers.Values)
            // {
            //     docs.Add($"<h2>{MakeAnchor(action.GetType().Name, "Action")} Action Settings</h2>");
            //     DocumentActionOrCommand(docs, action.GetType(), action.RewardConfigType, documentedTypes);
            // }
            //
            // foreach (var command in commandHandlers.Values)
            // {
            //     docs.Add($"<h2>{MakeAnchor(command.GetType().Name, "Command")} Command Handler Settings</h2>");
            //     DocumentActionOrCommand(docs, command.GetType(), command.HandlerConfigType, documentedTypes);
            // }
        }

        public static string DocumentationPath => Path.Combine(Path.GetDirectoryName(typeof(Settings).Assembly.Location), "..", "..", "Bannerlord-Twitch-Documentation.html"); 
            
        public void Save()
        {
            docs.Add("</div></html></body>");
            try
            {
                File.WriteAllLines(DocumentationPath, docs);
                // string cssFileName = Path.Combine(Path.GetDirectoryName(typeof(Settings).Assembly.Location), "..", "..",
                //     "Bannerlord-Twitch-Documentation.css");
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

        private static void DocumentActionOrCommand(ICollection<string> docs, Type objectType, Type settingsType,
            List<Type> documentedTypes)
        {
            string docStr = DocStr(objectType);
            docs.Add(docStr != null
                ? $"<p>{docStr}</p>"
                : $"<p>No documentation provided.</p>");

            if (settingsType != null)
            {
                DocumentType(docs, settingsType, documentedTypes);
            }
        }

        private static string LinkToAnchor(string text, string anchorTag = null)
            => $"<a href=\"#{text}{anchorTag ?? ""}\">{text}</a>";

        private static string MakeAnchor(string text, string anchorTag = null)
            => $"<a name=\"{text}{anchorTag ?? ""}\">{text}</a>";

        private static string DocStr(MemberInfo objectType) =>
            (objectType.GetCustomAttributes(typeof(DescriptionAttribute)).FirstOrDefault() as DescriptionAttribute)?.Description;

        private static void DocumentType(ICollection<string> docs, Type typeToDocument, List<Type> documentedTypes)
        {
            documentedTypes.Add(typeToDocument);

            string docStr = DocStr(typeToDocument);
            if (docStr != null)
            {
                docs.Add($"<p>{docStr}</p>");
            }

            static string GetSimpleTypeName(Type t)
            {
                switch (Type.GetTypeCode(t))
                {
                    case TypeCode.String: return "string";
                    case TypeCode.Boolean: return "bool";
                    case TypeCode.Byte:
                    case TypeCode.Char:
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64: return "int";
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal: return "float";
                    default: return t.Name;
                }
            }

            static string GetFieldTypeName(FieldInfo f)
            {
                return Nullable.GetUnderlyingType(f.FieldType) != null
                    ? GetSimpleTypeName(Nullable.GetUnderlyingType(f.FieldType)) + " (optional)"
                    : GetSimpleTypeName(f.FieldType);
            }

            object settingsDefaults = Activator.CreateInstance(typeToDocument);

            var documentedFieldTypes = settingsDefaults.GetType()
                .GetFields()
                .Select(fi => fi.FieldType.IsArray ? fi.FieldType.GetElementType() : fi.FieldType)
                .Where(fi => fi?.GetCustomAttributes(typeof(DescriptionAttribute)).Any() == true);

            HtmlTable(docs, new[]
                {
                    "Name", "Type", "Default Value", "Description"
                },
                settingsDefaults.GetType().GetFields()
                    .Select(f => new[]
                    {
                        f.Name,
                        $"<code>{(documentedFieldTypes.Contains(f.FieldType) ? LinkToAnchor(f.Name) : GetFieldTypeName(f))}</code>",
                        f.FieldType.IsPrimitive ? $"<code>{f.GetValue(settingsDefaults)}</code>" : "(none)",
                        DocStr(f) ?? "(none)"
                    }));

            docs.Add("<h3>Example:</h3>");
            docs.Add("<code class=\"example\">");
            docs.Add(new SerializerBuilder().Build().Serialize(settingsDefaults).Replace("\n", "<br>"));
            docs.Add("</code>");

            foreach (var sft in documentedFieldTypes.Where(t => !documentedTypes.Contains(t)))
            {
                if (!documentedTypes.Contains(sft))
                {
                    docs.Add($"<h2>{MakeAnchor(sft.Name)} Subtype</h2>");
                    DocumentType(docs, sft, documentedTypes);
                }
            }
        }

        private static void HtmlTable(ICollection<string> docs, IEnumerable<string> headers,
            IEnumerable<IEnumerable<string>> rows, string cssClass = null)
        {
            docs.Add(cssClass != null ? $"<table class=\"{cssClass}\">" : "<table>");

            docs.Add("<tr>");
            foreach (string header in headers)
            {
                docs.Add($"<th>{header}</th>");
            }

            docs.Add("</tr>");
            foreach (var row in rows)
            {
                docs.Add("<tr>");
                foreach (string cell in row)
                {
                    docs.Add($"<td>{cell}</td>");
                }

                docs.Add("</tr>");
            }

            docs.Add("</table>");
        }

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
    }
}