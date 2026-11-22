using System.Linq;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Extensions.Alerts;
using Markdig.Renderers.Docx;

namespace Adliance.QmDoc.Converter.Docx;

/// <summary>
/// Renders GitHub style alerts ("> [!NOTE]") as callouts, like the "{{!}}" blocks.
/// </summary>
internal sealed class AlertBlockRenderer(DocxRenderContext context) : DocxObjectRenderer<AlertBlock>
{
    protected override void WriteObject(DocxDocumentRenderer renderer, AlertBlock alert)
    {
        renderer.ForceCloseParagraph();

        var kind = Callout.FromAlertKind(alert.Kind.ToString());
        var container = new Body();
        DocxRenderContext.RenderInto(renderer, container, () => renderer.WriteChildren(alert));

        // consecutive paragraphs with the same borders are rendered as one box
        foreach (var element in container.ChildElements.ToList())
        {
            element.Remove();
            if (element is Paragraph paragraph) Callout.Apply(paragraph, kind, context.Theme);
            DocxRenderContext.WriteBlock(renderer, element);
        }
    }
}
