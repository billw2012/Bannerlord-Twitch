using System;
using TaleWorlds.Core;

namespace BannerlordTwitch
{
    public interface IDocumentationGenerator
    {
        IDocumentationGenerator Div(string css, Action content);
        IDocumentationGenerator Div(Action content);
        
        IDocumentationGenerator Details(string css, Action content);
        IDocumentationGenerator Details(Action content);
        
        IDocumentationGenerator Summary(string css, Action content);
        IDocumentationGenerator Summary(Action content);
        IDocumentationGenerator Summary(string css, string content);
        IDocumentationGenerator Summary(string content);
        
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
        
        IDocumentationGenerator MakeAnchor(string tag, Action content);
        IDocumentationGenerator MakeAnchor(string tag, string content);
                
        IDocumentationGenerator LinkToAnchor(string tag, Action content);
        IDocumentationGenerator LinkToAnchor(string tag, string content);
    }
}