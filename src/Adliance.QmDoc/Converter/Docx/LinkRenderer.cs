using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Renderers.Docx;
using Markdig.Renderers.Docx.Inlines;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace Adliance.QmDoc.Converter.Docx;

/// <summary>
/// Embeds images and renders links to chapters ("#heading-id") as links to a bookmark of the heading. All other links are rendered as usual.
/// </summary>
internal sealed class LinkRenderer(DocxRenderContext context) : DocxObjectRenderer<LinkInline>
{
    private const long EmusPerTwip = 635;
    private readonly LinkInlineRenderer _defaultRenderer = new();

    protected override void WriteObject(DocxDocumentRenderer renderer, LinkInline link)
    {
        if (link.IsImage)
        {
            if (!TryWriteImage(renderer, link)) renderer.WriteChildren(link); // the alternative text
            return;
        }

        if (link.Url?.StartsWith('#') == true)
        {
            WriteChapterLink(renderer, link);
            return;
        }

        _defaultRenderer.Write(renderer, link);
    }

    private void WriteChapterLink(DocxDocumentRenderer renderer, LinkInline link)
    {
        var id = Uri.UnescapeDataString(link.Url![1..]);
        var heading = context.FindHeading(id);
        if (heading == null)
        {
            context.AddError($"Unable to find an element \"{id}\", but there's a link to it.");
            renderer.WriteChildren(link);
            return;
        }

        // like in the HTML/PDF, the link shows the title of the heading (including its numbering)
        var run = new Run(
            new RunProperties(new RunStyle { Val = renderer.Styles.Hyperlink }),
            new Text(heading.Text) { Space = SpaceProcessingModeValues.Preserve });
        renderer.Cursor.Write(new Hyperlink(run) { Anchor = heading.BookmarkName, History = true });
    }

    private bool TryWriteImage(DocxDocumentRenderer renderer, LinkInline link)
    {
        var url = link.Url ?? "";
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("//", StringComparison.Ordinal) || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            context.AddError($"Image '{url}' is not embedded into the DOCX, only local images are supported.", true);
            return false;
        }

        var imagePath = Uri.UnescapeDataString(url);
        var fullFilePath = Path.Combine(Path.GetDirectoryName(context.SourceFilePath) ?? "", imagePath);
        if (!File.Exists(fullFilePath))
        {
            context.AddError($"Unable to find image '{imagePath}'.", true);
            return false;
        }

        var data = File.ReadAllBytes(fullFilePath);
        var info = ImageInfo.Read(data);
        if (info == null || info.Width <= 0 || info.Height <= 0)
        {
            context.AddError($"Image '{imagePath}' is not embedded into the DOCX, only PNG, JPEG and GIF images are supported.", true);
            return false;
        }

        var classes = link.GetAttributes().Classes;
        var floating = classes?.Contains("left") == true ? "left" : classes?.Contains("right") == true ? "right" : null;

        // images keep their size (scaled like in the PDF), but are never wider than the available space (max. 40% for floating images)
        var theme = context.Theme;
        var maxWidth = floating == null ? context.MaxImageWidth : Math.Min(context.MaxImageWidth, ThemeStylesProcessor.TextWidth * 4 / 10);
        var width = theme.ToEmus(info.Width);
        var height = theme.ToEmus(info.Height);
        var factor = Math.Min(1, Math.Min(maxWidth * EmusPerTwip / (double)width, ThemeStylesProcessor.TextHeight * 0.9 * EmusPerTwip / height));
        width = (long)(width * factor);
        height = (long)(height * factor);

        var mainPart = renderer.Document.MainDocumentPart!;
        var fullPath = Path.GetFullPath(fullFilePath);
        if (!context.EmbeddedImages.TryGetValue(fullPath, out var relationshipId))
        {
            var imagePart = mainPart.AddImagePart(info.PartType);
            using (var stream = new MemoryStream(data)) imagePart.FeedData(stream);
            relationshipId = mainPart.GetIdOfPart(imagePart);
            context.EmbeddedImages[fullPath] = relationshipId;
        }

        var id = context.NextDrawingId();
        var name = Path.GetFileName(imagePath);
        var alt = string.Concat(link.Descendants<LiteralInline>().Select(l => l.Content.ToString()));
        var graphic = Graphic(relationshipId, id, name, width, height);
        var docProperties = new DW.DocProperties { Id = id, Name = name, Description = alt };
        var frameProperties = new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true });

        OpenXmlElement drawing;
        if (floating == null)
        {
            drawing = new DW.Inline(
                new DW.Extent { Cx = width, Cy = height },
                new DW.EffectExtent { LeftEdge = 0, TopEdge = 0, RightEdge = 0, BottomEdge = 0 },
                docProperties, frameProperties, graphic)
            {
                DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U
            };
        }
        else
        {
            // CSS: margin 0 .5rem .1rem 0 for ".left" (and the other way round for ".right")
            var gap = (uint)theme.ToEmus(8);
            drawing = new DW.Anchor(
                new DW.SimplePosition { X = 0, Y = 0 },
                new DW.HorizontalPosition(new DW.HorizontalAlignment(floating)) { RelativeFrom = DW.HorizontalRelativePositionValues.Margin },
                new DW.VerticalPosition(new DW.PositionOffset("0")) { RelativeFrom = DW.VerticalRelativePositionValues.Paragraph },
                new DW.Extent { Cx = width, Cy = height },
                new DW.EffectExtent { LeftEdge = 0, TopEdge = 0, RightEdge = 0, BottomEdge = 0 },
                new DW.WrapSquare { WrapText = DW.WrapTextValues.BothSides },
                docProperties, frameProperties, graphic)
            {
                DistanceFromTop = 0U,
                DistanceFromBottom = (uint)theme.ToEmus(2),
                DistanceFromLeft = floating == "right" ? gap : 0U,
                DistanceFromRight = floating == "left" ? gap : 0U,
                SimplePos = false,
                RelativeHeight = id,
                BehindDoc = false,
                Locked = false,
                LayoutInCell = true,
                AllowOverlap = true
            };
        }

        renderer.Cursor.Write(new Run(new Drawing(drawing)));
        return true;
    }

    private static A.Graphic Graphic(string relationshipId, uint id, string name, long width, long height) =>
        new(new A.GraphicData(
            new PIC.Picture(
                new PIC.NonVisualPictureProperties(
                    new PIC.NonVisualDrawingProperties { Id = id, Name = name },
                    new PIC.NonVisualPictureDrawingProperties()),
                new PIC.BlipFill(
                    new A.Blip { Embed = relationshipId },
                    new A.Stretch(new A.FillRectangle())),
                new PIC.ShapeProperties(
                    new A.Transform2D(new A.Offset { X = 0, Y = 0 }, new A.Extents { Cx = width, Cy = height }),
                    new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }))
        ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" });
}
