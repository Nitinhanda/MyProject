using DocumentFormat.OpenXml.Packaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using FontFamily = System.Drawing;

namespace OpenXMLPowerTools
{
    public static class PtOpenXmlExtensions
    {
        public static XDocument GetXDocument(this OpenXmlPart part)
        {
            if (part == null) throw new ArgumentNullException("part");

            XDocument partXDocument = part.Annotation<XDocument>();
            if (partXDocument != null) return partXDocument;

            using (Stream partStream = part.GetStream())
            {
                if (partStream.Length == 0)
                {
                    partXDocument = new XDocument();
                    partXDocument.Declaration = new XDeclaration("1.0", "UTF-8", "yes");
                }
                else
                {
                    using (XmlReader partXmlReader = XmlReader.Create(partStream))
                        partXDocument = XDocument.Load(partXmlReader);
                }
            }

            part.AddAnnotation(partXDocument);
            return partXDocument;
        }

        public static IEnumerable<OpenXmlPart> ContentParts(this WordprocessingDocument doc)
        {
            yield return doc.MainDocumentPart;

            foreach (var hdr in doc.MainDocumentPart.HeaderParts)
                yield return hdr;

            foreach (var ftr in doc.MainDocumentPart.FooterParts)
                yield return ftr;

            if (doc.MainDocumentPart.FootnotesPart != null)
                yield return doc.MainDocumentPart.FootnotesPart;

            if (doc.MainDocumentPart.EndnotesPart != null)
                yield return doc.MainDocumentPart.EndnotesPart;
        }

        public static void PutXDocument(this OpenXmlPart part)
        {
            if (part == null) throw new ArgumentNullException("part");

            XDocument partXDocument = part.GetXDocument();
            if (partXDocument != null)
            {
#if true
                using (Stream partStream = part.GetStream(FileMode.Create, FileAccess.Write))
                using (XmlWriter partXmlWriter = XmlWriter.Create(partStream))
                    partXDocument.Save(partXmlWriter);
#else
                byte[] array = Encoding.UTF8.GetBytes(partXDocument.ToString(SaveOptions.DisableFormatting));
                using (MemoryStream ms = new MemoryStream(array))
                    part.FeedData(ms);
#endif
            }
        }

        public static XElement ToXElement<T>(this object obj)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (TextWriter streamWriter = new StreamWriter(memoryStream))
                {
                    var xmlSerializer = new XmlSerializer(typeof(T));
                    xmlSerializer.Serialize(streamWriter, obj);
                    return XElement.Parse(Encoding.UTF8.GetString(memoryStream.ToArray()));
                }
            }
        }
    }

    public static class WordprocessingMLUtil
    {
        private static HashSet<string> UnknownFonts = new HashSet<string>();
        private static HashSet<string> KnownFamilies = null;

        //public static int CalcWidthOfRunInTwips(XElement r)
        //{
        //    if (KnownFamilies == null)
        //    {
        //        KnownFamilies = new HashSet<string>();
        //        var families = FontFamily.Families;
        //        foreach (var fam in families)
        //            KnownFamilies.Add(fam.Name);
        //    }

        //    var fontName = (string)r.Attribute(PtOpenXml.pt + "FontName");
        //    if (fontName == null)
        //        fontName = (string)r.Ancestors(W.p).First().Attribute(PtOpenXml.pt + "FontName");
        //    if (fontName == null)
        //        throw new OpenXmlPowerToolsException("Internal Error, should have FontName attribute");
        //    if (UnknownFonts.Contains(fontName))
        //        return 0;

        //    var rPr = r.Element(W.rPr);
        //    if (rPr == null)
        //        throw new OpenXmlPowerToolsException("Internal Error, should have run properties");
        //    var languageType = (string)r.Attribute(PtOpenXml.LanguageType);
        //    decimal? szn = null;
        //    if (languageType == "bidi")
        //        szn = (decimal?)rPr.Elements(W.szCs).Attributes(W.val).FirstOrDefault();
        //    else
        //        szn = (decimal?)rPr.Elements(W.sz).Attributes(W.val).FirstOrDefault();
        //    if (szn == null)
        //        szn = 22m;

        //    var sz = szn.GetValueOrDefault();

        //    // unknown font families will throw ArgumentException, in which case just return 0
        //    if (!KnownFamilies.Contains(fontName))
        //        return 0;
        //    // in theory, all unknown fonts are found by the above test, but if not...
        //    FontFamily ff;
        //    try
        //    {
        //        ff = new FontFamily(fontName);
        //    }
        //    catch (ArgumentException)
        //    {
        //        UnknownFonts.Add(fontName);

        //        return 0;
        //    }
        //    FontStyle fs = FontStyle.Regular;
        //    var bold = GetBoolProp(rPr, W.b) || GetBoolProp(rPr, W.bCs);
        //    var italic = GetBoolProp(rPr, W.i) || GetBoolProp(rPr, W.iCs);
        //    if (bold && !italic)
        //        fs = FontStyle.Bold;
        //    if (italic && !bold)
        //        fs = FontStyle.Italic;
        //    if (bold && italic)
        //        fs = FontStyle.Bold | FontStyle.Italic;

        //    var runText = r.DescendantsTrimmed(W.txbxContent)
        //        .Where(e => e.Name == W.t)
        //        .Select(t => (string)t)
        //        .StringConcatenate();

        //    var tabLength = r.DescendantsTrimmed(W.txbxContent)
        //        .Where(e => e.Name == W.tab)
        //        .Select(t => (decimal)t.Attribute(PtOpenXml.TabWidth))
        //        .Sum();

        //    if (runText.Length == 0 && tabLength == 0)
        //        return 0;

        //    int multiplier = 1;
        //    if (runText.Length <= 2)
        //        multiplier = 100;
        //    else if (runText.Length <= 4)
        //        multiplier = 50;
        //    else if (runText.Length <= 8)
        //        multiplier = 25;
        //    else if (runText.Length <= 16)
        //        multiplier = 12;
        //    else if (runText.Length <= 32)
        //        multiplier = 6;
        //    if (multiplier != 1)
        //    {
        //        StringBuilder sb = new StringBuilder();
        //        for (int i = 0; i < multiplier; i++)
        //            sb.Append(runText);
        //        runText = sb.ToString();
        //    }

        //    var w = MetricsGetter.GetTextWidth(ff, fs, sz, runText);

        //    return (int)(w / 96m * 1440m / multiplier + tabLength * 1440m);
        //}

        public static bool GetBoolProp(XElement runProps, XName xName)
        {
            var p = runProps.Element(xName);
            if (p == null)
                return false;
            var v = p.Attribute(W.val);
            if (v == null)
                return true;
            var s = v.Value.ToLower();
            if (s == "0" || s == "false")
                return false;
            if (s == "1" || s == "true")
                return true;
            return false;
        }

        private static readonly List<XName> AdditionalRunContainerNames = new List<XName>
        {
            W.w + "bdo",
            W.customXml,
            W.dir,
            W.fldSimple,
            W.hyperlink,
            W.moveFrom,
            W.moveTo,
            W.sdtContent
        };

        public static XElement CoalesceAdjacentRunsWithIdenticalFormatting(XElement runContainer)
        {
            const string dontConsolidate = "DontConsolidate";

            IEnumerable<IGrouping<string, XElement>> groupedAdjacentRunsWithIdenticalFormatting =
                runContainer
                    .Elements()
                    .GroupAdjacent(ce =>
                    {
                        if (ce.Name == W.r)
                        {
                            if (ce.Elements().Count(e => e.Name != W.rPr) != 1)
                                return dontConsolidate;

                            XElement rPr = ce.Element(W.rPr);
                            string rPrString = rPr != null ? rPr.ToString(SaveOptions.None) : string.Empty;

                            if (ce.Element(W.t) != null)
                                return "Wt" + rPrString;

                            if (ce.Element(W.instrText) != null)
                                return "WinstrText" + rPrString;

                            return dontConsolidate;
                        }

                        if (ce.Name == W.ins)
                        {
                            if (ce.Elements(W.del).Any())
                            {
                                return dontConsolidate;
#if false
                                // for w:ins/w:del/w:r/w:delText
                                if ((ce.Elements(W.del).Elements(W.r).Elements().Count(e => e.Name != W.rPr) != 1) ||
                                    !ce.Elements().Elements().Elements(W.delText).Any())
                                    return dontConsolidate;

                                XAttribute dateIns = ce.Attribute(W.date);
                                XElement del = ce.Element(W.del);
                                XAttribute dateDel = del.Attribute(W.date);

                                string authorIns = (string) ce.Attribute(W.author) ?? string.Empty;
                                string dateInsString = dateIns != null
                                    ? ((DateTime) dateIns).ToString("s")
                                    : string.Empty;
                                string authorDel = (string) del.Attribute(W.author) ?? string.Empty;
                                string dateDelString = dateDel != null
                                    ? ((DateTime) dateDel).ToString("s")
                                    : string.Empty;

                                return "Wins" +
                                       authorIns +
                                       dateInsString +
                                       authorDel +
                                       dateDelString +
                                       ce.Elements(W.del)
                                           .Elements(W.r)
                                           .Elements(W.rPr)
                                           .Select(rPr => rPr.ToString(SaveOptions.None))
                                           .StringConcatenate();
#endif
                            }

                            // w:ins/w:r/w:t
                            if ((ce.Elements().Elements().Count(e => e.Name != W.rPr) != 1) ||
                                !ce.Elements().Elements(W.t).Any())
                                return dontConsolidate;

                            XAttribute dateIns2 = ce.Attribute(W.date);

                            string authorIns2 = (string)ce.Attribute(W.author) ?? string.Empty;
                            string dateInsString2 = dateIns2 != null
                                ? ((DateTime)dateIns2).ToString("s")
                                : string.Empty;

                            string idIns2 = (string)ce.Attribute(W.id);

                            return "Wins2" +
                                   authorIns2 +
                                   dateInsString2 +
                                   idIns2 +
                                   ce.Elements()
                                       .Elements(W.rPr)
                                       .Select(rPr => rPr.ToString(SaveOptions.None))
                                       .StringConcatenate();
                        }

                        if (ce.Name == W.del)
                        {
                            if ((ce.Elements(W.r).Elements().Count(e => e.Name != W.rPr) != 1) ||
                                !ce.Elements().Elements(W.delText).Any())
                                return dontConsolidate;

                            XAttribute dateDel2 = ce.Attribute(W.date);

                            string authorDel2 = (string)ce.Attribute(W.author) ?? string.Empty;
                            string dateDelString2 = dateDel2 != null ? ((DateTime)dateDel2).ToString("s") : string.Empty;

                            return "Wdel" +
                                   authorDel2 +
                                   dateDelString2 +
                                   ce.Elements(W.r)
                                       .Elements(W.rPr)
                                       .Select(rPr => rPr.ToString(SaveOptions.None))
                                       .StringConcatenate();
                        }

                        return dontConsolidate;
                    });

            var runContainerWithConsolidatedRuns = new XElement(runContainer.Name,
                runContainer.Attributes(),
                groupedAdjacentRunsWithIdenticalFormatting.Select(g =>
                {
                    if (g.Key == dontConsolidate)
                        return (object)g;

                    string textValue = g
                        .Select(r =>
                            r.Descendants()
                                .Where(d => (d.Name == W.t) || (d.Name == W.delText) || (d.Name == W.instrText))
                                .Select(d => d.Value)
                                .StringConcatenate())
                        .StringConcatenate();
                    XAttribute xs = XmlUtil.GetXmlSpaceAttribute(textValue);

                    if (g.First().Name == W.r)
                    {
                        if (g.First().Element(W.t) != null)
                        {
                            IEnumerable<IEnumerable<XAttribute>> statusAtt =
                                g.Select(r => r.Descendants(W.t).Take(1).Attributes(PtOpenXml.Status));
                            return new XElement(W.r,
                                g.First().Elements(W.rPr),
                                new XElement(W.t, statusAtt, xs, textValue));
                        }

                        if (g.First().Element(W.instrText) != null)
                            return new XElement(W.r,
                                g.First().Elements(W.rPr),
                                new XElement(W.instrText, xs, textValue));
                    }

                    if (g.First().Name == W.ins)
                    {
#if false
                        if (g.First().Elements(W.del).Any())
                            return new XElement(W.ins,
                                g.First().Attributes(),
                                new XElement(W.del,
                                    g.First().Elements(W.del).Attributes(),
                                    new XElement(W.r,
                                        g.First().Elements(W.del).Elements(W.r).Elements(W.rPr),
                                        new XElement(W.delText, xs, textValue))));
#endif
                        return new XElement(W.ins,
                            g.First().Attributes(),
                            new XElement(W.r,
                                g.First().Elements(W.r).Elements(W.rPr),
                                new XElement(W.t, xs, textValue)));
                    }

                    if (g.First().Name == W.del)
                        return new XElement(W.del,
                            g.First().Attributes(),
                            new XElement(W.r,
                                g.First().Elements(W.r).Elements(W.rPr),
                                new XElement(W.delText, xs, textValue)));

                    return g;
                }));

            // Process w:txbxContent//w:p
            foreach (XElement txbx in runContainerWithConsolidatedRuns.Descendants(W.txbxContent))
                foreach (XElement txbxPara in txbx.DescendantsTrimmed(W.txbxContent).Where(d => d.Name == W.p))
                {
                    XElement newPara = CoalesceAdjacentRunsWithIdenticalFormatting(txbxPara);
                    txbxPara.ReplaceWith(newPara);
                }

            // Process additional run containers.
            List<XElement> runContainers = runContainerWithConsolidatedRuns
                .Descendants()
                .Where(d => AdditionalRunContainerNames.Contains(d.Name))
                .ToList();
            foreach (XElement container in runContainers)
            {
                XElement newContainer = CoalesceAdjacentRunsWithIdenticalFormatting(container);
                container.ReplaceWith(newContainer);
            }

            return runContainerWithConsolidatedRuns;
        }

        private static Dictionary<XName, int> Order_settings = new Dictionary<XName, int>
        {
            { W.writeProtection, 10},
            { W.view, 20},
            { W.zoom, 30},
            { W.removePersonalInformation, 40},
            { W.removeDateAndTime, 50},
            { W.doNotDisplayPageBoundaries, 60},
            { W.displayBackgroundShape, 70},
            { W.printPostScriptOverText, 80},
            { W.printFractionalCharacterWidth, 90},
            { W.printFormsData, 100},
            { W.embedTrueTypeFonts, 110},
            { W.embedSystemFonts, 120},
            { W.saveSubsetFonts, 130},
            { W.saveFormsData, 140},
            { W.mirrorMargins, 150},
            { W.alignBordersAndEdges, 160},
            { W.bordersDoNotSurroundHeader, 170},
            { W.bordersDoNotSurroundFooter, 180},
            { W.gutterAtTop, 190},
            { W.hideSpellingErrors, 200},
            { W.hideGrammaticalErrors, 210},
            { W.activeWritingStyle, 220},
            { W.proofState, 230},
            { W.formsDesign, 240},
            { W.attachedTemplate, 250},
            { W.linkStyles, 260},
            { W.stylePaneFormatFilter, 270},
            { W.stylePaneSortMethod, 280},
            { W.documentType, 290},
            { W.mailMerge, 300},
            { W.revisionView, 310},
            { W.trackRevisions, 320},
            { W.doNotTrackMoves, 330},
            { W.doNotTrackFormatting, 340},
            { W.documentProtection, 350},
            { W.autoFormatOverride, 360},
            { W.styleLockTheme, 370},
            { W.styleLockQFSet, 380},
            { W.defaultTabStop, 390},
            { W.autoHyphenation, 400},
            { W.consecutiveHyphenLimit, 410},
            { W.hyphenationZone, 420},
            { W.doNotHyphenateCaps, 430},
            { W.showEnvelope, 440},
            { W.summaryLength, 450},
            { W.clickAndTypeStyle, 460},
            { W.defaultTableStyle, 470},
            { W.evenAndOddHeaders, 480},
            { W.bookFoldRevPrinting, 490},
            { W.bookFoldPrinting, 500},
            { W.bookFoldPrintingSheets, 510},
            { W.drawingGridHorizontalSpacing, 520},
            { W.drawingGridVerticalSpacing, 530},
            { W.displayHorizontalDrawingGridEvery, 540},
            { W.displayVerticalDrawingGridEvery, 550},
            { W.doNotUseMarginsForDrawingGridOrigin, 560},
            { W.drawingGridHorizontalOrigin, 570},
            { W.drawingGridVerticalOrigin, 580},
            { W.doNotShadeFormData, 590},
            { W.noPunctuationKerning, 600},
            { W.characterSpacingControl, 610},
            { W.printTwoOnOne, 620},
            { W.strictFirstAndLastChars, 630},
            { W.noLineBreaksAfter, 640},
            { W.noLineBreaksBefore, 650},
            { W.savePreviewPicture, 660},
            { W.doNotValidateAgainstSchema, 670},
            { W.saveInvalidXml, 680},
            { W.ignoreMixedContent, 690},
            { W.alwaysShowPlaceholderText, 700},
            { W.doNotDemarcateInvalidXml, 710},
            { W.saveXmlDataOnly, 720},
            { W.useXSLTWhenSaving, 730},
            { W.saveThroughXslt, 740},
            { W.showXMLTags, 750},
            { W.alwaysMergeEmptyNamespace, 760},
            { W.updateFields, 770},
            { W.footnotePr, 780},
            { W.endnotePr, 790},
            { W.compat, 800},
            { W.docVars, 810},
            { W.rsids, 820},
            { M.mathPr, 830},
            { W.attachedSchema, 840},
            { W.themeFontLang, 850},
            { W.clrSchemeMapping, 860},
            { W.doNotIncludeSubdocsInStats, 870},
            { W.doNotAutoCompressPictures, 880},
            { W.forceUpgrade, 890}, 
            //{W.captions, 900}, 
            { W.readModeInkLockDown, 910},
            { W.smartTagType, 920}, 
            //{W.sl:schemaLibrary, 930}, 
            { W.doNotEmbedSmartTags, 940},
            { W.decimalSymbol, 950},
            { W.listSeparator, 960},
        };

#if false
// from the schema in the standard
        
writeProtection
view
zoom
removePersonalInformation
removeDateAndTime
doNotDisplayPageBoundaries
displayBackgroundShape
printPostScriptOverText
printFractionalCharacterWidth
printFormsData
embedTrueTypeFonts
embedSystemFonts
saveSubsetFonts
saveFormsData
mirrorMargins
alignBordersAndEdges
bordersDoNotSurroundHeader
bordersDoNotSurroundFooter
gutterAtTop
hideSpellingErrors
hideGrammaticalErrors
activeWritingStyle
proofState
formsDesign
attachedTemplate
linkStyles
stylePaneFormatFilter
stylePaneSortMethod
documentType
mailMerge
revisionView
trackRevisions
doNotTrackMoves
doNotTrackFormatting
documentProtection
autoFormatOverride
styleLockTheme
styleLockQFSet
defaultTabStop
autoHyphenation
consecutiveHyphenLimit
hyphenationZone
doNotHyphenateCaps
showEnvelope
summaryLength
clickAndTypeStyle
defaultTableStyle
evenAndOddHeaders
bookFoldRevPrinting
bookFoldPrinting
bookFoldPrintingSheets
drawingGridHorizontalSpacing
drawingGridVerticalSpacing
displayHorizontalDrawingGridEvery
displayVerticalDrawingGridEvery
doNotUseMarginsForDrawingGridOrigin
drawingGridHorizontalOrigin
drawingGridVerticalOrigin
doNotShadeFormData
noPunctuationKerning
characterSpacingControl
printTwoOnOne
strictFirstAndLastChars
noLineBreaksAfter
noLineBreaksBefore
savePreviewPicture
doNotValidateAgainstSchema
saveInvalidXml
ignoreMixedContent
alwaysShowPlaceholderText
doNotDemarcateInvalidXml
saveXmlDataOnly
useXSLTWhenSaving
saveThroughXslt
showXMLTags
alwaysMergeEmptyNamespace
updateFields
footnotePr
endnotePr
compat
docVars
rsids
m:mathPr
attachedSchema
themeFontLang
clrSchemeMapping
doNotIncludeSubdocsInStats
doNotAutoCompressPictures
forceUpgrade
captions
readModeInkLockDown
smartTagType
sl:schemaLibrary
doNotEmbedSmartTags
decimalSymbol
listSeparator
#endif

        private static Dictionary<XName, int> Order_pPr = new Dictionary<XName, int>
        {
            { W.pStyle, 10 },
            { W.keepNext, 20 },
            { W.keepLines, 30 },
            { W.pageBreakBefore, 40 },
            { W.framePr, 50 },
            { W.widowControl, 60 },
            { W.numPr, 70 },
            { W.suppressLineNumbers, 80 },
            { W.pBdr, 90 },
            { W.shd, 100 },
            { W.tabs, 120 },
            { W.suppressAutoHyphens, 130 },
            { W.kinsoku, 140 },
            { W.wordWrap, 150 },
            { W.overflowPunct, 160 },
            { W.topLinePunct, 170 },
            { W.autoSpaceDE, 180 },
            { W.autoSpaceDN, 190 },
            { W.bidi, 200 },
            { W.adjustRightInd, 210 },
            { W.snapToGrid, 220 },
            { W.spacing, 230 },
            { W.ind, 240 },
            { W.contextualSpacing, 250 },
            { W.mirrorIndents, 260 },
            { W.suppressOverlap, 270 },
            { W.jc, 280 },
            { W.textDirection, 290 },
            { W.textAlignment, 300 },
            { W.textboxTightWrap, 310 },
            { W.outlineLvl, 320 },
            { W.divId, 330 },
            { W.cnfStyle, 340 },
            { W.rPr, 350 },
            { W.sectPr, 360 },
            { W.pPrChange, 370 },
        };

        private static Dictionary<XName, int> Order_rPr = new Dictionary<XName, int>
        {
            { W.ins, 10 },
            { W.del, 20 },
            { W.rStyle, 30 },
            { W.rFonts, 40 },
            { W.b, 50 },
            { W.bCs, 60 },
            { W.i, 70 },
            { W.iCs, 80 },
            { W.caps, 90 },
            { W.smallCaps, 100 },
            { W.strike, 110 },
            { W.dstrike, 120 },
            { W.outline, 130 },
            { W.shadow, 140 },
            { W.emboss, 150 },
            { W.imprint, 160 },
            { W.noProof, 170 },
            { W.snapToGrid, 180 },
            { W.vanish, 190 },
            { W.webHidden, 200 },
            { W.color, 210 },
            { W.spacing, 220 },
            { W._w, 230 },
            { W.kern, 240 },
            { W.position, 250 },
            { W.sz, 260 },
            { W14.wShadow, 270 },
            { W14.wTextOutline, 280 },
            { W14.wTextFill, 290 },
            { W14.wScene3d, 300 },
            { W14.wProps3d, 310 },
            { W.szCs, 320 },
            { W.highlight, 330 },
            { W.u, 340 },
            { W.effect, 350 },
            { W.bdr, 360 },
            { W.shd, 370 },
            { W.fitText, 380 },
            { W.vertAlign, 390 },
            { W.rtl, 400 },
            { W.cs, 410 },
            { W.em, 420 },
            { W.lang, 430 },
            { W.eastAsianLayout, 440 },
            { W.specVanish, 450 },
            { W.oMath, 460 },
        };

        private static Dictionary<XName, int> Order_tblPr = new Dictionary<XName, int>
        {
            { W.tblStyle, 10 },
            { W.tblpPr, 20 },
            { W.tblOverlap, 30 },
            { W.bidiVisual, 40 },
            { W.tblStyleRowBandSize, 50 },
            { W.tblStyleColBandSize, 60 },
            { W.tblW, 70 },
            { W.jc, 80 },
            { W.tblCellSpacing, 90 },
            { W.tblInd, 100 },
            { W.tblBorders, 110 },
            { W.shd, 120 },
            { W.tblLayout, 130 },
            { W.tblCellMar, 140 },
            { W.tblLook, 150 },
            { W.tblCaption, 160 },
            { W.tblDescription, 170 },
        };

        private static Dictionary<XName, int> Order_tblBorders = new Dictionary<XName, int>
        {
            { W.top, 10 },
            { W.left, 20 },
            { W.start, 30 },
            { W.bottom, 40 },
            { W.right, 50 },
            { W.end, 60 },
            { W.insideH, 70 },
            { W.insideV, 80 },
        };

        private static Dictionary<XName, int> Order_tcPr = new Dictionary<XName, int>
        {
            { W.cnfStyle, 10 },
            { W.tcW, 20 },
            { W.gridSpan, 30 },
            { W.hMerge, 40 },
            { W.vMerge, 50 },
            { W.tcBorders, 60 },
            { W.shd, 70 },
            { W.noWrap, 80 },
            { W.tcMar, 90 },
            { W.textDirection, 100 },
            { W.tcFitText, 110 },
            { W.vAlign, 120 },
            { W.hideMark, 130 },
            { W.headers, 140 },
        };

        private static Dictionary<XName, int> Order_tcBorders = new Dictionary<XName, int>
        {
            { W.top, 10 },
            { W.start, 20 },
            { W.left, 30 },
            { W.bottom, 40 },
            { W.right, 50 },
            { W.end, 60 },
            { W.insideH, 70 },
            { W.insideV, 80 },
            { W.tl2br, 90 },
            { W.tr2bl, 100 },
        };

        private static Dictionary<XName, int> Order_pBdr = new Dictionary<XName, int>
        {
            { W.top, 10 },
            { W.left, 20 },
            { W.bottom, 30 },
            { W.right, 40 },
            { W.between, 50 },
            { W.bar, 60 },
        };

        public static object WmlOrderElementsPerStandard(XNode node)
        {
            XElement element = node as XElement;
            if (element != null)
            {
                if (element.Name == W.pPr)
                    return new XElement(element.Name,
                        element.Attributes(),
                        element.Elements().Select(e => (XElement)WmlOrderElementsPerStandard(e)).OrderBy(e =>
                        {
                            if (Order_pPr.ContainsKey(e.Name))
                                return Order_pPr[e.Name];
                            return 999;
                        }));

                if (element.Name == W.rPr)
                    return new XElement(element.Name,
                        element.Attributes(),
                        element.Elements().Select(e => (XElement)WmlOrderElementsPerStandard(e)).OrderBy(e =>
                        {
                            if (Order_rPr.ContainsKey(e.Name))
                                return Order_rPr[e.Name];
                            return 999;
                        }));

                if (element.Name == W.tblPr)
                    return new XElement(element.Name,
                        element.Attributes(),
                        element.Elements().Select(e => (XElement)WmlOrderElementsPerStandard(e)).OrderBy(e =>
                        {
                            if (Order_tblPr.ContainsKey(e.Name))
                                return Order_tblPr[e.Name];
                            return 999;
                        }));

                if (element.Name == W.tcPr)
                    return new XElement(element.Name,
                        element.Attributes(),
                        element.Elements().Select(e => (XElement)WmlOrderElementsPerStandard(e)).OrderBy(e =>
                        {
                            if (Order_tcPr.ContainsKey(e.Name))
                                return Order_tcPr[e.Name];
                            return 999;
                        }));

                if (element.Name == W.tcBorders)
                    return new XElement(element.Name,
                        element.Attributes(),
                        element.Elements().Select(e => (XElement)WmlOrderElementsPerStandard(e)).OrderBy(e =>
                        {
                            if (Order_tcBorders.ContainsKey(e.Name))
                                return Order_tcBorders[e.Name];
                            return 999;
                        }));

                if (element.Name == W.tblBorders)
                    return new XElement(element.Name,
                        element.Attributes(),
                        element.Elements().Select(e => (XElement)WmlOrderElementsPerStandard(e)).OrderBy(e =>
                        {
                            if (Order_tblBorders.ContainsKey(e.Name))
                                return Order_tblBorders[e.Name];
                            return 999;
                        }));

                if (element.Name == W.pBdr)
                    return new XElement(element.Name,
                        element.Attributes(),
                        element.Elements().Select(e => (XElement)WmlOrderElementsPerStandard(e)).OrderBy(e =>
                        {
                            if (Order_pBdr.ContainsKey(e.Name))
                                return Order_pBdr[e.Name];
                            return 999;
                        }));

                if (element.Name == W.p)
                {
                    var newP = new XElement(element.Name,
                        element.Attributes(),
                        element.Elements(W.pPr).Select(e => (XElement)WmlOrderElementsPerStandard(e)),
                        element.Elements().Where(e => e.Name != W.pPr).Select(e => (XElement)WmlOrderElementsPerStandard(e)));
                    return newP;
                }

                if (element.Name == W.r)
                    return new XElement(element.Name,
                        element.Attributes(),
                        element.Elements(W.rPr).Select(e => (XElement)WmlOrderElementsPerStandard(e)),
                        element.Elements().Where(e => e.Name != W.rPr).Select(e => (XElement)WmlOrderElementsPerStandard(e)));

                if (element.Name == W.settings)
                    return new XElement(element.Name,
                        element.Attributes(),
                        element.Elements().Select(e => (XElement)WmlOrderElementsPerStandard(e)).OrderBy(e =>
                        {
                            if (Order_settings.ContainsKey(e.Name))
                                return Order_settings[e.Name];
                            return 999;
                        }));

                return new XElement(element.Name,
                    element.Attributes(),
                    element.Nodes().Select(n => WmlOrderElementsPerStandard(n)));
            }
            return node;
        }

        public static WmlDocument BreakLinkToTemplate(WmlDocument source)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(source.DocumentByteArray, 0, source.DocumentByteArray.Length);
                using (WordprocessingDocument wDoc = WordprocessingDocument.Open(ms, true))
                {
                    var efpp = wDoc.ExtendedFilePropertiesPart;
                    if (efpp != null)
                    {
                        var xd = efpp.GetXDocument();
                        var template = xd.Descendants(EP.Template).FirstOrDefault();
                        if (template != null)
                            template.Value = "";
                        efpp.PutXDocument();
                    }
                }
                var result = new WmlDocument(source.FileName, ms.ToArray());
                return result;
            }
        }
    }

    public class InvalidOpenXmlDocumentException : Exception
    {
        public InvalidOpenXmlDocumentException(string message) : base(message) { }
    }

    public class OpenXmlPowerToolsException : Exception
    {
        public OpenXmlPowerToolsException(string message) : base(message) { }
    }

    public static class W
    {
        public static readonly XNamespace w =
            "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        public static readonly XName abstractNum = w + "abstractNum";
        public static readonly XName abstractNumId = w + "abstractNumId";
        public static readonly XName accent1 = w + "accent1";
        public static readonly XName accent2 = w + "accent2";
        public static readonly XName accent3 = w + "accent3";
        public static readonly XName accent4 = w + "accent4";
        public static readonly XName accent5 = w + "accent5";
        public static readonly XName accent6 = w + "accent6";
        public static readonly XName activeRecord = w + "activeRecord";
        public static readonly XName activeWritingStyle = w + "activeWritingStyle";
        public static readonly XName actualPg = w + "actualPg";
        public static readonly XName addressFieldName = w + "addressFieldName";
        public static readonly XName adjustLineHeightInTable = w + "adjustLineHeightInTable";
        public static readonly XName adjustRightInd = w + "adjustRightInd";
        public static readonly XName after = w + "after";
        public static readonly XName afterAutospacing = w + "afterAutospacing";
        public static readonly XName afterLines = w + "afterLines";
        public static readonly XName algIdExt = w + "algIdExt";
        public static readonly XName algIdExtSource = w + "algIdExtSource";
        public static readonly XName alias = w + "alias";
        public static readonly XName aliases = w + "aliases";
        public static readonly XName alignBordersAndEdges = w + "alignBordersAndEdges";
        public static readonly XName alignment = w + "alignment";
        public static readonly XName alignTablesRowByRow = w + "alignTablesRowByRow";
        public static readonly XName allowPNG = w + "allowPNG";
        public static readonly XName allowSpaceOfSameStyleInTable = w + "allowSpaceOfSameStyleInTable";
        public static readonly XName altChunk = w + "altChunk";
        public static readonly XName altChunkPr = w + "altChunkPr";
        public static readonly XName altName = w + "altName";
        public static readonly XName alwaysMergeEmptyNamespace = w + "alwaysMergeEmptyNamespace";
        public static readonly XName alwaysShowPlaceholderText = w + "alwaysShowPlaceholderText";
        public static readonly XName anchor = w + "anchor";
        public static readonly XName anchorLock = w + "anchorLock";
        public static readonly XName annotationRef = w + "annotationRef";
        public static readonly XName applyBreakingRules = w + "applyBreakingRules";
        public static readonly XName appName = w + "appName";
        public static readonly XName ascii = w + "ascii";
        public static readonly XName asciiTheme = w + "asciiTheme";
        public static readonly XName attachedSchema = w + "attachedSchema";
        public static readonly XName attachedTemplate = w + "attachedTemplate";
        public static readonly XName attr = w + "attr";
        public static readonly XName author = w + "author";
        public static readonly XName autofitToFirstFixedWidthCell = w + "autofitToFirstFixedWidthCell";
        public static readonly XName autoFormatOverride = w + "autoFormatOverride";
        public static readonly XName autoHyphenation = w + "autoHyphenation";
        public static readonly XName autoRedefine = w + "autoRedefine";
        public static readonly XName autoSpaceDE = w + "autoSpaceDE";
        public static readonly XName autoSpaceDN = w + "autoSpaceDN";
        public static readonly XName autoSpaceLikeWord95 = w + "autoSpaceLikeWord95";
        public static readonly XName b = w + "b";
        public static readonly XName background = w + "background";
        public static readonly XName balanceSingleByteDoubleByteWidth = w + "balanceSingleByteDoubleByteWidth";
        public static readonly XName bar = w + "bar";
        public static readonly XName basedOn = w + "basedOn";
        public static readonly XName bCs = w + "bCs";
        public static readonly XName bdr = w + "bdr";
        public static readonly XName before = w + "before";
        public static readonly XName beforeAutospacing = w + "beforeAutospacing";
        public static readonly XName beforeLines = w + "beforeLines";
        public static readonly XName behavior = w + "behavior";
        public static readonly XName behaviors = w + "behaviors";
        public static readonly XName between = w + "between";
        public static readonly XName bg1 = w + "bg1";
        public static readonly XName bg2 = w + "bg2";
        public static readonly XName bibliography = w + "bibliography";
        public static readonly XName bidi = w + "bidi";
        public static readonly XName bidiVisual = w + "bidiVisual";
        public static readonly XName blockQuote = w + "blockQuote";
        public static readonly XName body = w + "body";
        public static readonly XName bodyDiv = w + "bodyDiv";
        public static readonly XName bookFoldPrinting = w + "bookFoldPrinting";
        public static readonly XName bookFoldPrintingSheets = w + "bookFoldPrintingSheets";
        public static readonly XName bookFoldRevPrinting = w + "bookFoldRevPrinting";
        public static readonly XName bookmarkEnd = w + "bookmarkEnd";
        public static readonly XName bookmarkStart = w + "bookmarkStart";
        public static readonly XName bordersDoNotSurroundFooter = w + "bordersDoNotSurroundFooter";
        public static readonly XName bordersDoNotSurroundHeader = w + "bordersDoNotSurroundHeader";
        public static readonly XName bottom = w + "bottom";
        public static readonly XName bottomFromText = w + "bottomFromText";
        public static readonly XName br = w + "br";
        public static readonly XName cachedColBalance = w + "cachedColBalance";
        public static readonly XName calcOnExit = w + "calcOnExit";
        public static readonly XName calendar = w + "calendar";
        public static readonly XName cantSplit = w + "cantSplit";
        public static readonly XName caps = w + "caps";
        public static readonly XName category = w + "category";
        public static readonly XName cellDel = w + "cellDel";
        public static readonly XName cellIns = w + "cellIns";
        public static readonly XName cellMerge = w + "cellMerge";
        public static readonly XName chapSep = w + "chapSep";
        public static readonly XName chapStyle = w + "chapStyle";
        public static readonly XName _char = w + "char";
        public static readonly XName characterSpacingControl = w + "characterSpacingControl";
        public static readonly XName charset = w + "charset";
        public static readonly XName charSpace = w + "charSpace";
        public static readonly XName checkBox = w + "checkBox";
        public static readonly XName _checked = w + "checked";
        public static readonly XName checkErrors = w + "checkErrors";
        public static readonly XName checkStyle = w + "checkStyle";
        public static readonly XName citation = w + "citation";
        public static readonly XName clear = w + "clear";
        public static readonly XName clickAndTypeStyle = w + "clickAndTypeStyle";
        public static readonly XName clrSchemeMapping = w + "clrSchemeMapping";
        public static readonly XName cnfStyle = w + "cnfStyle";
        public static readonly XName code = w + "code";
        public static readonly XName col = w + "col";
        public static readonly XName colDelim = w + "colDelim";
        public static readonly XName colFirst = w + "colFirst";
        public static readonly XName colLast = w + "colLast";
        public static readonly XName color = w + "color";
        public static readonly XName cols = w + "cols";
        public static readonly XName column = w + "column";
        public static readonly XName combine = w + "combine";
        public static readonly XName combineBrackets = w + "combineBrackets";
        public static readonly XName comboBox = w + "comboBox";
        public static readonly XName comment = w + "comment";
        public static readonly XName commentRangeEnd = w + "commentRangeEnd";
        public static readonly XName commentRangeStart = w + "commentRangeStart";
        public static readonly XName commentReference = w + "commentReference";
        public static readonly XName comments = w + "comments";
        public static readonly XName compat = w + "compat";
        public static readonly XName compatSetting = w + "compatSetting";
        public static readonly XName connectString = w + "connectString";
        public static readonly XName consecutiveHyphenLimit = w + "consecutiveHyphenLimit";
        public static readonly XName contentPart = w + "contentPart";
        public static readonly XName contextualSpacing = w + "contextualSpacing";
        public static readonly XName continuationSeparator = w + "continuationSeparator";
        public static readonly XName control = w + "control";
        public static readonly XName convMailMergeEsc = w + "convMailMergeEsc";
        public static readonly XName count = w + "count";
        public static readonly XName countBy = w + "countBy";
        public static readonly XName cr = w + "cr";
        public static readonly XName cryptAlgorithmClass = w + "cryptAlgorithmClass";
        public static readonly XName cryptAlgorithmSid = w + "cryptAlgorithmSid";
        public static readonly XName cryptAlgorithmType = w + "cryptAlgorithmType";
        public static readonly XName cryptProvider = w + "cryptProvider";
        public static readonly XName cryptProviderType = w + "cryptProviderType";
        public static readonly XName cryptProviderTypeExt = w + "cryptProviderTypeExt";
        public static readonly XName cryptProviderTypeExtSource = w + "cryptProviderTypeExtSource";
        public static readonly XName cryptSpinCount = w + "cryptSpinCount";
        public static readonly XName cs = w + "cs";
        public static readonly XName csb0 = w + "csb0";
        public static readonly XName csb1 = w + "csb1";
        public static readonly XName cstheme = w + "cstheme";
        public static readonly XName customMarkFollows = w + "customMarkFollows";
        public static readonly XName customStyle = w + "customStyle";
        public static readonly XName customXml = w + "customXml";
        public static readonly XName customXmlDelRangeEnd = w + "customXmlDelRangeEnd";
        public static readonly XName customXmlDelRangeStart = w + "customXmlDelRangeStart";
        public static readonly XName customXmlInsRangeEnd = w + "customXmlInsRangeEnd";
        public static readonly XName customXmlInsRangeStart = w + "customXmlInsRangeStart";
        public static readonly XName customXmlMoveFromRangeEnd = w + "customXmlMoveFromRangeEnd";
        public static readonly XName customXmlMoveFromRangeStart = w + "customXmlMoveFromRangeStart";
        public static readonly XName customXmlMoveToRangeEnd = w + "customXmlMoveToRangeEnd";
        public static readonly XName customXmlMoveToRangeStart = w + "customXmlMoveToRangeStart";
        public static readonly XName customXmlPr = w + "customXmlPr";
        public static readonly XName dataBinding = w + "dataBinding";
        public static readonly XName dataSource = w + "dataSource";
        public static readonly XName dataType = w + "dataType";
        public static readonly XName date = w + "date";
        public static readonly XName dateFormat = w + "dateFormat";
        public static readonly XName dayLong = w + "dayLong";
        public static readonly XName dayShort = w + "dayShort";
        public static readonly XName ddList = w + "ddList";
        public static readonly XName decimalSymbol = w + "decimalSymbol";
        public static readonly XName _default = w + "default";
        public static readonly XName defaultTableStyle = w + "defaultTableStyle";
        public static readonly XName defaultTabStop = w + "defaultTabStop";
        public static readonly XName defLockedState = w + "defLockedState";
        public static readonly XName defQFormat = w + "defQFormat";
        public static readonly XName defSemiHidden = w + "defSemiHidden";
        public static readonly XName defUIPriority = w + "defUIPriority";
        public static readonly XName defUnhideWhenUsed = w + "defUnhideWhenUsed";
        public static readonly XName del = w + "del";
        public static readonly XName delInstrText = w + "delInstrText";
        public static readonly XName delText = w + "delText";
        public static readonly XName description = w + "description";
        public static readonly XName destination = w + "destination";
        public static readonly XName dir = w + "dir";
        public static readonly XName dirty = w + "dirty";
        public static readonly XName displacedByCustomXml = w + "displacedByCustomXml";
        public static readonly XName display = w + "display";
        public static readonly XName displayBackgroundShape = w + "displayBackgroundShape";
        public static readonly XName displayHangulFixedWidth = w + "displayHangulFixedWidth";
        public static readonly XName displayHorizontalDrawingGridEvery = w + "displayHorizontalDrawingGridEvery";
        public static readonly XName displayText = w + "displayText";
        public static readonly XName displayVerticalDrawingGridEvery = w + "displayVerticalDrawingGridEvery";
        public static readonly XName distance = w + "distance";
        public static readonly XName div = w + "div";
        public static readonly XName divBdr = w + "divBdr";
        public static readonly XName divId = w + "divId";
        public static readonly XName divs = w + "divs";
        public static readonly XName divsChild = w + "divsChild";
        public static readonly XName dllVersion = w + "dllVersion";
        public static readonly XName docDefaults = w + "docDefaults";
        public static readonly XName docGrid = w + "docGrid";
        public static readonly XName docLocation = w + "docLocation";
        public static readonly XName docPart = w + "docPart";
        public static readonly XName docPartBody = w + "docPartBody";
        public static readonly XName docPartCategory = w + "docPartCategory";
        public static readonly XName docPartGallery = w + "docPartGallery";
        public static readonly XName docPartList = w + "docPartList";
        public static readonly XName docPartObj = w + "docPartObj";
        public static readonly XName docPartPr = w + "docPartPr";
        public static readonly XName docParts = w + "docParts";
        public static readonly XName docPartUnique = w + "docPartUnique";
        public static readonly XName document = w + "document";
        public static readonly XName documentProtection = w + "documentProtection";
        public static readonly XName documentType = w + "documentType";
        public static readonly XName docVar = w + "docVar";
        public static readonly XName docVars = w + "docVars";
        public static readonly XName doNotAutoCompressPictures = w + "doNotAutoCompressPictures";
        public static readonly XName doNotAutofitConstrainedTables = w + "doNotAutofitConstrainedTables";
        public static readonly XName doNotBreakConstrainedForcedTable = w + "doNotBreakConstrainedForcedTable";
        public static readonly XName doNotBreakWrappedTables = w + "doNotBreakWrappedTables";
        public static readonly XName doNotDemarcateInvalidXml = w + "doNotDemarcateInvalidXml";
        public static readonly XName doNotDisplayPageBoundaries = w + "doNotDisplayPageBoundaries";
        public static readonly XName doNotEmbedSmartTags = w + "doNotEmbedSmartTags";
        public static readonly XName doNotExpandShiftReturn = w + "doNotExpandShiftReturn";
        public static readonly XName doNotHyphenateCaps = w + "doNotHyphenateCaps";
        public static readonly XName doNotIncludeSubdocsInStats = w + "doNotIncludeSubdocsInStats";
        public static readonly XName doNotLeaveBackslashAlone = w + "doNotLeaveBackslashAlone";
        public static readonly XName doNotOrganizeInFolder = w + "doNotOrganizeInFolder";
        public static readonly XName doNotRelyOnCSS = w + "doNotRelyOnCSS";
        public static readonly XName doNotSaveAsSingleFile = w + "doNotSaveAsSingleFile";
        public static readonly XName doNotShadeFormData = w + "doNotShadeFormData";
        public static readonly XName doNotSnapToGridInCell = w + "doNotSnapToGridInCell";
        public static readonly XName doNotSuppressBlankLines = w + "doNotSuppressBlankLines";
        public static readonly XName doNotSuppressIndentation = w + "doNotSuppressIndentation";
        public static readonly XName doNotSuppressParagraphBorders = w + "doNotSuppressParagraphBorders";
        public static readonly XName doNotTrackFormatting = w + "doNotTrackFormatting";
        public static readonly XName doNotTrackMoves = w + "doNotTrackMoves";
        public static readonly XName doNotUseEastAsianBreakRules = w + "doNotUseEastAsianBreakRules";
        public static readonly XName doNotUseHTMLParagraphAutoSpacing = w + "doNotUseHTMLParagraphAutoSpacing";
        public static readonly XName doNotUseIndentAsNumberingTabStop = w + "doNotUseIndentAsNumberingTabStop";
        public static readonly XName doNotUseLongFileNames = w + "doNotUseLongFileNames";
        public static readonly XName doNotUseMarginsForDrawingGridOrigin = w + "doNotUseMarginsForDrawingGridOrigin";
        public static readonly XName doNotValidateAgainstSchema = w + "doNotValidateAgainstSchema";
        public static readonly XName doNotVertAlignCellWithSp = w + "doNotVertAlignCellWithSp";
        public static readonly XName doNotVertAlignInTxbx = w + "doNotVertAlignInTxbx";
        public static readonly XName doNotWrapTextWithPunct = w + "doNotWrapTextWithPunct";
        public static readonly XName drawing = w + "drawing";
        public static readonly XName drawingGridHorizontalOrigin = w + "drawingGridHorizontalOrigin";
        public static readonly XName drawingGridHorizontalSpacing = w + "drawingGridHorizontalSpacing";
        public static readonly XName drawingGridVerticalOrigin = w + "drawingGridVerticalOrigin";
        public static readonly XName drawingGridVerticalSpacing = w + "drawingGridVerticalSpacing";
        public static readonly XName dropCap = w + "dropCap";
        public static readonly XName dropDownList = w + "dropDownList";
        public static readonly XName dstrike = w + "dstrike";
        public static readonly XName dxaOrig = w + "dxaOrig";
        public static readonly XName dyaOrig = w + "dyaOrig";
        public static readonly XName dynamicAddress = w + "dynamicAddress";
        public static readonly XName eastAsia = w + "eastAsia";
        public static readonly XName eastAsianLayout = w + "eastAsianLayout";
        public static readonly XName eastAsiaTheme = w + "eastAsiaTheme";
        public static readonly XName ed = w + "ed";
        public static readonly XName edGrp = w + "edGrp";
        public static readonly XName edit = w + "edit";
        public static readonly XName effect = w + "effect";
        public static readonly XName element = w + "element";
        public static readonly XName em = w + "em";
        public static readonly XName embedBold = w + "embedBold";
        public static readonly XName embedBoldItalic = w + "embedBoldItalic";
        public static readonly XName embedItalic = w + "embedItalic";
        public static readonly XName embedRegular = w + "embedRegular";
        public static readonly XName embedSystemFonts = w + "embedSystemFonts";
        public static readonly XName embedTrueTypeFonts = w + "embedTrueTypeFonts";
        public static readonly XName emboss = w + "emboss";
        public static readonly XName enabled = w + "enabled";
        public static readonly XName encoding = w + "encoding";
        public static readonly XName endnote = w + "endnote";
        public static readonly XName endnotePr = w + "endnotePr";
        public static readonly XName endnoteRef = w + "endnoteRef";
        public static readonly XName endnoteReference = w + "endnoteReference";
        public static readonly XName endnotes = w + "endnotes";
        public static readonly XName enforcement = w + "enforcement";
        public static readonly XName entryMacro = w + "entryMacro";
        public static readonly XName equalWidth = w + "equalWidth";
        public static readonly XName equation = w + "equation";
        public static readonly XName evenAndOddHeaders = w + "evenAndOddHeaders";
        public static readonly XName exitMacro = w + "exitMacro";
        public static readonly XName family = w + "family";
        public static readonly XName ffData = w + "ffData";
        public static readonly XName fHdr = w + "fHdr";
        public static readonly XName fieldMapData = w + "fieldMapData";
        public static readonly XName fill = w + "fill";
        public static readonly XName first = w + "first";
        public static readonly XName firstColumn = w + "firstColumn";
        public static readonly XName firstLine = w + "firstLine";
        public static readonly XName firstLineChars = w + "firstLineChars";
        public static readonly XName firstRow = w + "firstRow";
        public static readonly XName fitText = w + "fitText";
        public static readonly XName flatBorders = w + "flatBorders";
        public static readonly XName fldChar = w + "fldChar";
        public static readonly XName fldCharType = w + "fldCharType";
        public static readonly XName fldData = w + "fldData";
        public static readonly XName fldLock = w + "fldLock";
        public static readonly XName fldSimple = w + "fldSimple";
        public static readonly XName fmt = w + "fmt";
        public static readonly XName followedHyperlink = w + "followedHyperlink";
        public static readonly XName font = w + "font";
        public static readonly XName fontKey = w + "fontKey";
        public static readonly XName fonts = w + "fonts";
        public static readonly XName fontSz = w + "fontSz";
        public static readonly XName footer = w + "footer";
        public static readonly XName footerReference = w + "footerReference";
        public static readonly XName footnote = w + "footnote";
        public static readonly XName footnoteLayoutLikeWW8 = w + "footnoteLayoutLikeWW8";
        public static readonly XName footnotePr = w + "footnotePr";
        public static readonly XName footnoteRef = w + "footnoteRef";
        public static readonly XName footnoteReference = w + "footnoteReference";
        public static readonly XName footnotes = w + "footnotes";
        public static readonly XName forceUpgrade = w + "forceUpgrade";
        public static readonly XName forgetLastTabAlignment = w + "forgetLastTabAlignment";
        public static readonly XName format = w + "format";
        public static readonly XName formatting = w + "formatting";
        public static readonly XName formProt = w + "formProt";
        public static readonly XName formsDesign = w + "formsDesign";
        public static readonly XName frame = w + "frame";
        public static readonly XName frameLayout = w + "frameLayout";
        public static readonly XName framePr = w + "framePr";
        public static readonly XName frameset = w + "frameset";
        public static readonly XName framesetSplitbar = w + "framesetSplitbar";
        public static readonly XName ftr = w + "ftr";
        public static readonly XName fullDate = w + "fullDate";
        public static readonly XName gallery = w + "gallery";
        public static readonly XName glossaryDocument = w + "glossaryDocument";
        public static readonly XName grammar = w + "grammar";
        public static readonly XName gridAfter = w + "gridAfter";
        public static readonly XName gridBefore = w + "gridBefore";
        public static readonly XName gridCol = w + "gridCol";
        public static readonly XName gridSpan = w + "gridSpan";
        public static readonly XName group = w + "group";
        public static readonly XName growAutofit = w + "growAutofit";
        public static readonly XName guid = w + "guid";
        public static readonly XName gutter = w + "gutter";
        public static readonly XName gutterAtTop = w + "gutterAtTop";
        public static readonly XName h = w + "h";
        public static readonly XName hAnchor = w + "hAnchor";
        public static readonly XName hanging = w + "hanging";
        public static readonly XName hangingChars = w + "hangingChars";
        public static readonly XName hAnsi = w + "hAnsi";
        public static readonly XName hAnsiTheme = w + "hAnsiTheme";
        public static readonly XName hash = w + "hash";
        public static readonly XName hdr = w + "hdr";
        public static readonly XName hdrShapeDefaults = w + "hdrShapeDefaults";
        public static readonly XName header = w + "header";
        public static readonly XName headerReference = w + "headerReference";
        public static readonly XName headerSource = w + "headerSource";
        public static readonly XName helpText = w + "helpText";
        public static readonly XName hidden = w + "hidden";
        public static readonly XName hideGrammaticalErrors = w + "hideGrammaticalErrors";
        public static readonly XName hideMark = w + "hideMark";
        public static readonly XName hideSpellingErrors = w + "hideSpellingErrors";
        public static readonly XName highlight = w + "highlight";
        public static readonly XName hint = w + "hint";
        public static readonly XName history = w + "history";
        public static readonly XName hMerge = w + "hMerge";
        public static readonly XName horzAnchor = w + "horzAnchor";
        public static readonly XName hps = w + "hps";
        public static readonly XName hpsBaseText = w + "hpsBaseText";
        public static readonly XName hpsRaise = w + "hpsRaise";
        public static readonly XName hRule = w + "hRule";
        public static readonly XName hSpace = w + "hSpace";
        public static readonly XName hyperlink = w + "hyperlink";
        public static readonly XName hyphenationZone = w + "hyphenationZone";
        public static readonly XName i = w + "i";
        public static readonly XName iCs = w + "iCs";
        public static readonly XName id = w + "id";
        public static readonly XName ignoreMixedContent = w + "ignoreMixedContent";
        public static readonly XName ilvl = w + "ilvl";
        public static readonly XName imprint = w + "imprint";
        public static readonly XName ind = w + "ind";
        public static readonly XName initials = w + "initials";
        public static readonly XName inkAnnotations = w + "inkAnnotations";
        public static readonly XName ins = w + "ins";
        public static readonly XName insDel = w + "insDel";
        public static readonly XName insideH = w + "insideH";
        public static readonly XName insideV = w + "insideV";
        public static readonly XName instr = w + "instr";
        public static readonly XName instrText = w + "instrText";
        public static readonly XName isLgl = w + "isLgl";
        public static readonly XName jc = w + "jc";
        public static readonly XName keepLines = w + "keepLines";
        public static readonly XName keepNext = w + "keepNext";
        public static readonly XName kern = w + "kern";
        public static readonly XName kinsoku = w + "kinsoku";
        public static readonly XName lang = w + "lang";
        public static readonly XName lastColumn = w + "lastColumn";
        public static readonly XName lastRenderedPageBreak = w + "lastRenderedPageBreak";
        public static readonly XName lastValue = w + "lastValue";
        public static readonly XName lastRow = w + "lastRow";
        public static readonly XName latentStyles = w + "latentStyles";
        public static readonly XName layoutRawTableWidth = w + "layoutRawTableWidth";
        public static readonly XName layoutTableRowsApart = w + "layoutTableRowsApart";
        public static readonly XName leader = w + "leader";
        public static readonly XName left = w + "left";
        public static readonly XName leftChars = w + "leftChars";
        public static readonly XName leftFromText = w + "leftFromText";
        public static readonly XName legacy = w + "legacy";
        public static readonly XName legacyIndent = w + "legacyIndent";
        public static readonly XName legacySpace = w + "legacySpace";
        public static readonly XName lid = w + "lid";
        public static readonly XName line = w + "line";
        public static readonly XName linePitch = w + "linePitch";
        public static readonly XName lineRule = w + "lineRule";
        public static readonly XName lines = w + "lines";
        public static readonly XName lineWrapLikeWord6 = w + "lineWrapLikeWord6";
        public static readonly XName link = w + "link";
        public static readonly XName linkedToFile = w + "linkedToFile";
        public static readonly XName linkStyles = w + "linkStyles";
        public static readonly XName linkToQuery = w + "linkToQuery";
        public static readonly XName listEntry = w + "listEntry";
        public static readonly XName listItem = w + "listItem";
        public static readonly XName listSeparator = w + "listSeparator";
        public static readonly XName lnNumType = w + "lnNumType";
        public static readonly XName _lock = w + "lock";
        public static readonly XName locked = w + "locked";
        public static readonly XName lsdException = w + "lsdException";
        public static readonly XName lvl = w + "lvl";
        public static readonly XName lvlJc = w + "lvlJc";
        public static readonly XName lvlOverride = w + "lvlOverride";
        public static readonly XName lvlPicBulletId = w + "lvlPicBulletId";
        public static readonly XName lvlRestart = w + "lvlRestart";
        public static readonly XName lvlText = w + "lvlText";
        public static readonly XName mailAsAttachment = w + "mailAsAttachment";
        public static readonly XName mailMerge = w + "mailMerge";
        public static readonly XName mailSubject = w + "mailSubject";
        public static readonly XName mainDocumentType = w + "mainDocumentType";
        public static readonly XName mappedName = w + "mappedName";
        public static readonly XName marBottom = w + "marBottom";
        public static readonly XName marH = w + "marH";
        public static readonly XName markup = w + "markup";
        public static readonly XName marLeft = w + "marLeft";
        public static readonly XName marRight = w + "marRight";
        public static readonly XName marTop = w + "marTop";
        public static readonly XName marW = w + "marW";
        public static readonly XName matchSrc = w + "matchSrc";
        public static readonly XName maxLength = w + "maxLength";
        public static readonly XName mirrorIndents = w + "mirrorIndents";
        public static readonly XName mirrorMargins = w + "mirrorMargins";
        public static readonly XName monthLong = w + "monthLong";
        public static readonly XName monthShort = w + "monthShort";
        public static readonly XName moveFrom = w + "moveFrom";
        public static readonly XName moveFromRangeEnd = w + "moveFromRangeEnd";
        public static readonly XName moveFromRangeStart = w + "moveFromRangeStart";
        public static readonly XName moveTo = w + "moveTo";
        public static readonly XName moveToRangeEnd = w + "moveToRangeEnd";
        public static readonly XName moveToRangeStart = w + "moveToRangeStart";
        public static readonly XName multiLevelType = w + "multiLevelType";
        public static readonly XName multiLine = w + "multiLine";
        public static readonly XName mwSmallCaps = w + "mwSmallCaps";
        public static readonly XName name = w + "name";
        public static readonly XName namespaceuri = w + "namespaceuri";
        public static readonly XName next = w + "next";
        public static readonly XName nlCheck = w + "nlCheck";
        public static readonly XName noBorder = w + "noBorder";
        public static readonly XName noBreakHyphen = w + "noBreakHyphen";
        public static readonly XName noColumnBalance = w + "noColumnBalance";
        public static readonly XName noEndnote = w + "noEndnote";
        public static readonly XName noExtraLineSpacing = w + "noExtraLineSpacing";
        public static readonly XName noHBand = w + "noHBand";
        public static readonly XName noLeading = w + "noLeading";
        public static readonly XName noLineBreaksAfter = w + "noLineBreaksAfter";
        public static readonly XName noLineBreaksBefore = w + "noLineBreaksBefore";
        public static readonly XName noProof = w + "noProof";
        public static readonly XName noPunctuationKerning = w + "noPunctuationKerning";
        public static readonly XName noResizeAllowed = w + "noResizeAllowed";
        public static readonly XName noSpaceRaiseLower = w + "noSpaceRaiseLower";
        public static readonly XName noTabHangInd = w + "noTabHangInd";
        public static readonly XName notTrueType = w + "notTrueType";
        public static readonly XName noVBand = w + "noVBand";
        public static readonly XName noWrap = w + "noWrap";
        public static readonly XName nsid = w + "nsid";
        public static readonly XName _null = w + "null";
        public static readonly XName num = w + "num";
        public static readonly XName numbering = w + "numbering";
        public static readonly XName numberingChange = w + "numberingChange";
        public static readonly XName numFmt = w + "numFmt";
        public static readonly XName numId = w + "numId";
        public static readonly XName numIdMacAtCleanup = w + "numIdMacAtCleanup";
        public static readonly XName numPicBullet = w + "numPicBullet";
        public static readonly XName numPicBulletId = w + "numPicBulletId";
        public static readonly XName numPr = w + "numPr";
        public static readonly XName numRestart = w + "numRestart";
        public static readonly XName numStart = w + "numStart";
        public static readonly XName numStyleLink = w + "numStyleLink";
        public static readonly XName _object = w + "object";
        public static readonly XName odso = w + "odso";
        public static readonly XName offsetFrom = w + "offsetFrom";
        public static readonly XName oMath = w + "oMath";
        public static readonly XName optimizeForBrowser = w + "optimizeForBrowser";
        public static readonly XName orient = w + "orient";
        public static readonly XName original = w + "original";
        public static readonly XName other = w + "other";
        public static readonly XName outline = w + "outline";
        public static readonly XName outlineLvl = w + "outlineLvl";
        public static readonly XName overflowPunct = w + "overflowPunct";
        public static readonly XName p = w + "p";
        public static readonly XName pageBreakBefore = w + "pageBreakBefore";
        public static readonly XName panose1 = w + "panose1";
        public static readonly XName paperSrc = w + "paperSrc";
        public static readonly XName pBdr = w + "pBdr";
        public static readonly XName percent = w + "percent";
        public static readonly XName permEnd = w + "permEnd";
        public static readonly XName permStart = w + "permStart";
        public static readonly XName personal = w + "personal";
        public static readonly XName personalCompose = w + "personalCompose";
        public static readonly XName personalReply = w + "personalReply";
        public static readonly XName pgBorders = w + "pgBorders";
        public static readonly XName pgMar = w + "pgMar";
        public static readonly XName pgNum = w + "pgNum";
        public static readonly XName pgNumType = w + "pgNumType";
        public static readonly XName pgSz = w + "pgSz";
        public static readonly XName pict = w + "pict";
        public static readonly XName picture = w + "picture";
        public static readonly XName pitch = w + "pitch";
        public static readonly XName pixelsPerInch = w + "pixelsPerInch";
        public static readonly XName placeholder = w + "placeholder";
        public static readonly XName pos = w + "pos";
        public static readonly XName position = w + "position";
        public static readonly XName pPr = w + "pPr";
        public static readonly XName pPrChange = w + "pPrChange";
        public static readonly XName pPrDefault = w + "pPrDefault";
        public static readonly XName prefixMappings = w + "prefixMappings";
        public static readonly XName printBodyTextBeforeHeader = w + "printBodyTextBeforeHeader";
        public static readonly XName printColBlack = w + "printColBlack";
        public static readonly XName printerSettings = w + "printerSettings";
        public static readonly XName printFormsData = w + "printFormsData";
        public static readonly XName printFractionalCharacterWidth = w + "printFractionalCharacterWidth";
        public static readonly XName printPostScriptOverText = w + "printPostScriptOverText";
        public static readonly XName printTwoOnOne = w + "printTwoOnOne";
        public static readonly XName proofErr = w + "proofErr";
        public static readonly XName proofState = w + "proofState";
        public static readonly XName pStyle = w + "pStyle";
        public static readonly XName ptab = w + "ptab";
        public static readonly XName qFormat = w + "qFormat";
        public static readonly XName query = w + "query";
        public static readonly XName r = w + "r";
        public static readonly XName readModeInkLockDown = w + "readModeInkLockDown";
        public static readonly XName recipientData = w + "recipientData";
        public static readonly XName recommended = w + "recommended";
        public static readonly XName relativeTo = w + "relativeTo";
        public static readonly XName relyOnVML = w + "relyOnVML";
        public static readonly XName removeDateAndTime = w + "removeDateAndTime";
        public static readonly XName removePersonalInformation = w + "removePersonalInformation";
        public static readonly XName restart = w + "restart";
        public static readonly XName result = w + "result";
        public static readonly XName revisionView = w + "revisionView";
        public static readonly XName rFonts = w + "rFonts";
        public static readonly XName richText = w + "richText";
        public static readonly XName right = w + "right";
        public static readonly XName rightChars = w + "rightChars";
        public static readonly XName rightFromText = w + "rightFromText";
        public static readonly XName rPr = w + "rPr";
        public static readonly XName rPrChange = w + "rPrChange";
        public static readonly XName rPrDefault = w + "rPrDefault";
        public static readonly XName rsid = w + "rsid";
        public static readonly XName rsidDel = w + "rsidDel";
        public static readonly XName rsidP = w + "rsidP";
        public static readonly XName rsidR = w + "rsidR";
        public static readonly XName rsidRDefault = w + "rsidRDefault";
        public static readonly XName rsidRoot = w + "rsidRoot";
        public static readonly XName rsidRPr = w + "rsidRPr";
        public static readonly XName rsids = w + "rsids";
        public static readonly XName rsidSect = w + "rsidSect";
        public static readonly XName rsidTr = w + "rsidTr";
        public static readonly XName rStyle = w + "rStyle";
        public static readonly XName rt = w + "rt";
        public static readonly XName rtl = w + "rtl";
        public static readonly XName rtlGutter = w + "rtlGutter";
        public static readonly XName ruby = w + "ruby";
        public static readonly XName rubyAlign = w + "rubyAlign";
        public static readonly XName rubyBase = w + "rubyBase";
        public static readonly XName rubyPr = w + "rubyPr";
        public static readonly XName salt = w + "salt";
        public static readonly XName saveFormsData = w + "saveFormsData";
        public static readonly XName saveInvalidXml = w + "saveInvalidXml";
        public static readonly XName savePreviewPicture = w + "savePreviewPicture";
        public static readonly XName saveSmartTagsAsXml = w + "saveSmartTagsAsXml";
        public static readonly XName saveSubsetFonts = w + "saveSubsetFonts";
        public static readonly XName saveThroughXslt = w + "saveThroughXslt";
        public static readonly XName saveXmlDataOnly = w + "saveXmlDataOnly";
        public static readonly XName scrollbar = w + "scrollbar";
        public static readonly XName sdt = w + "sdt";
        public static readonly XName sdtContent = w + "sdtContent";
        public static readonly XName sdtEndPr = w + "sdtEndPr";
        public static readonly XName sdtPr = w + "sdtPr";
        public static readonly XName sectPr = w + "sectPr";
        public static readonly XName sectPrChange = w + "sectPrChange";
        public static readonly XName selectFldWithFirstOrLastChar = w + "selectFldWithFirstOrLastChar";
        public static readonly XName semiHidden = w + "semiHidden";
        public static readonly XName sep = w + "sep";
        public static readonly XName separator = w + "separator";
        public static readonly XName settings = w + "settings";
        public static readonly XName shadow = w + "shadow";
        public static readonly XName shapeDefaults = w + "shapeDefaults";
        public static readonly XName shapeid = w + "shapeid";
        public static readonly XName shapeLayoutLikeWW8 = w + "shapeLayoutLikeWW8";
        public static readonly XName shd = w + "shd";
        public static readonly XName showBreaksInFrames = w + "showBreaksInFrames";
        public static readonly XName showEnvelope = w + "showEnvelope";
        public static readonly XName showingPlcHdr = w + "showingPlcHdr";
        public static readonly XName showXMLTags = w + "showXMLTags";
        public static readonly XName sig = w + "sig";
        public static readonly XName size = w + "size";
        public static readonly XName sizeAuto = w + "sizeAuto";
        public static readonly XName smallCaps = w + "smallCaps";
        public static readonly XName smartTag = w + "smartTag";
        public static readonly XName smartTagPr = w + "smartTagPr";
        public static readonly XName smartTagType = w + "smartTagType";
        public static readonly XName snapToGrid = w + "snapToGrid";
        public static readonly XName softHyphen = w + "softHyphen";
        public static readonly XName solutionID = w + "solutionID";
        public static readonly XName sourceFileName = w + "sourceFileName";
        public static readonly XName space = w + "space";
        public static readonly XName spaceForUL = w + "spaceForUL";
        public static readonly XName spacing = w + "spacing";
        public static readonly XName spacingInWholePoints = w + "spacingInWholePoints";
        public static readonly XName specVanish = w + "specVanish";
        public static readonly XName spelling = w + "spelling";
        public static readonly XName splitPgBreakAndParaMark = w + "splitPgBreakAndParaMark";
        public static readonly XName src = w + "src";
        public static readonly XName start = w + "start";
        public static readonly XName startOverride = w + "startOverride";
        public static readonly XName statusText = w + "statusText";
        public static readonly XName storeItemID = w + "storeItemID";
        public static readonly XName storeMappedDataAs = w + "storeMappedDataAs";
        public static readonly XName strictFirstAndLastChars = w + "strictFirstAndLastChars";
        public static readonly XName strike = w + "strike";
        public static readonly XName style = w + "style";
        public static readonly XName styleId = w + "styleId";
        public static readonly XName styleLink = w + "styleLink";
        public static readonly XName styleLockQFSet = w + "styleLockQFSet";
        public static readonly XName styleLockTheme = w + "styleLockTheme";
        public static readonly XName stylePaneFormatFilter = w + "stylePaneFormatFilter";
        public static readonly XName stylePaneSortMethod = w + "stylePaneSortMethod";
        public static readonly XName styles = w + "styles";
        public static readonly XName subDoc = w + "subDoc";
        public static readonly XName subFontBySize = w + "subFontBySize";
        public static readonly XName subsetted = w + "subsetted";
        public static readonly XName suff = w + "suff";
        public static readonly XName summaryLength = w + "summaryLength";
        public static readonly XName suppressAutoHyphens = w + "suppressAutoHyphens";
        public static readonly XName suppressBottomSpacing = w + "suppressBottomSpacing";
        public static readonly XName suppressLineNumbers = w + "suppressLineNumbers";
        public static readonly XName suppressOverlap = w + "suppressOverlap";
        public static readonly XName suppressSpacingAtTopOfPage = w + "suppressSpacingAtTopOfPage";
        public static readonly XName suppressSpBfAfterPgBrk = w + "suppressSpBfAfterPgBrk";
        public static readonly XName suppressTopSpacing = w + "suppressTopSpacing";
        public static readonly XName suppressTopSpacingWP = w + "suppressTopSpacingWP";
        public static readonly XName swapBordersFacingPages = w + "swapBordersFacingPages";
        public static readonly XName sym = w + "sym";
        public static readonly XName sz = w + "sz";
        public static readonly XName szCs = w + "szCs";
        public static readonly XName t = w + "t";
        public static readonly XName t1 = w + "t1";
        public static readonly XName t2 = w + "t2";
        public static readonly XName tab = w + "tab";
        public static readonly XName table = w + "table";
        public static readonly XName tabs = w + "tabs";
        public static readonly XName tag = w + "tag";
        public static readonly XName targetScreenSz = w + "targetScreenSz";
        public static readonly XName tbl = w + "tbl";
        public static readonly XName tblBorders = w + "tblBorders";
        public static readonly XName tblCellMar = w + "tblCellMar";
        public static readonly XName tblCellSpacing = w + "tblCellSpacing";
        public static readonly XName tblGrid = w + "tblGrid";
        public static readonly XName tblGridChange = w + "tblGridChange";
        public static readonly XName tblHeader = w + "tblHeader";
        public static readonly XName tblInd = w + "tblInd";
        public static readonly XName tblLayout = w + "tblLayout";
        public static readonly XName tblLook = w + "tblLook";
        public static readonly XName tblOverlap = w + "tblOverlap";
        public static readonly XName tblpPr = w + "tblpPr";
        public static readonly XName tblPr = w + "tblPr";
        public static readonly XName tblPrChange = w + "tblPrChange";
        public static readonly XName tblPrEx = w + "tblPrEx";
        public static readonly XName tblPrExChange = w + "tblPrExChange";
        public static readonly XName tblpX = w + "tblpX";
        public static readonly XName tblpXSpec = w + "tblpXSpec";
        public static readonly XName tblpY = w + "tblpY";
        public static readonly XName tblpYSpec = w + "tblpYSpec";
        public static readonly XName tblStyle = w + "tblStyle";
        public static readonly XName tblStyleColBandSize = w + "tblStyleColBandSize";
        public static readonly XName tblStylePr = w + "tblStylePr";
        public static readonly XName tblStyleRowBandSize = w + "tblStyleRowBandSize";
        public static readonly XName tblW = w + "tblW";
        public static readonly XName tc = w + "tc";
        public static readonly XName tcBorders = w + "tcBorders";
        public static readonly XName tcFitText = w + "tcFitText";
        public static readonly XName tcMar = w + "tcMar";
        public static readonly XName tcPr = w + "tcPr";
        public static readonly XName tcPrChange = w + "tcPrChange";
        public static readonly XName tcW = w + "tcW";
        public static readonly XName temporary = w + "temporary";
        public static readonly XName tentative = w + "tentative";
        public static readonly XName text = w + "text";
        public static readonly XName textAlignment = w + "textAlignment";
        public static readonly XName textboxTightWrap = w + "textboxTightWrap";
        public static readonly XName textDirection = w + "textDirection";
        public static readonly XName textInput = w + "textInput";
        public static readonly XName tgtFrame = w + "tgtFrame";
        public static readonly XName themeColor = w + "themeColor";
        public static readonly XName themeFill = w + "themeFill";
        public static readonly XName themeFillShade = w + "themeFillShade";
        public static readonly XName themeFillTint = w + "themeFillTint";
        public static readonly XName themeFontLang = w + "themeFontLang";
        public static readonly XName themeShade = w + "themeShade";
        public static readonly XName themeTint = w + "themeTint";
        public static readonly XName titlePg = w + "titlePg";
        public static readonly XName tl2br = w + "tl2br";
        public static readonly XName tmpl = w + "tmpl";
        public static readonly XName tooltip = w + "tooltip";
        public static readonly XName top = w + "top";
        public static readonly XName topFromText = w + "topFromText";
        public static readonly XName topLinePunct = w + "topLinePunct";
        public static readonly XName tplc = w + "tplc";
        public static readonly XName tr = w + "tr";
        public static readonly XName tr2bl = w + "tr2bl";
        public static readonly XName trackRevisions = w + "trackRevisions";
        public static readonly XName trHeight = w + "trHeight";
        public static readonly XName trPr = w + "trPr";
        public static readonly XName trPrChange = w + "trPrChange";
        public static readonly XName truncateFontHeightsLikeWP6 = w + "truncateFontHeightsLikeWP6";
        public static readonly XName txbxContent = w + "txbxContent";
        public static readonly XName type = w + "type";
        public static readonly XName types = w + "types";
        public static readonly XName u = w + "u";
        public static readonly XName udl = w + "udl";
        public static readonly XName uiCompat97To2003 = w + "uiCompat97To2003";
        public static readonly XName uiPriority = w + "uiPriority";
        public static readonly XName ulTrailSpace = w + "ulTrailSpace";
        public static readonly XName underlineTabInNumList = w + "underlineTabInNumList";
        public static readonly XName unhideWhenUsed = w + "unhideWhenUsed";
        public static readonly XName updateFields = w + "updateFields";
        public static readonly XName uri = w + "uri";
        public static readonly XName url = w + "url";
        public static readonly XName usb0 = w + "usb0";
        public static readonly XName usb1 = w + "usb1";
        public static readonly XName usb2 = w + "usb2";
        public static readonly XName usb3 = w + "usb3";
        public static readonly XName useAltKinsokuLineBreakRules = w + "useAltKinsokuLineBreakRules";
        public static readonly XName useAnsiKerningPairs = w + "useAnsiKerningPairs";
        public static readonly XName useFELayout = w + "useFELayout";
        public static readonly XName useNormalStyleForList = w + "useNormalStyleForList";
        public static readonly XName usePrinterMetrics = w + "usePrinterMetrics";
        public static readonly XName useSingleBorderforContiguousCells = w + "useSingleBorderforContiguousCells";
        public static readonly XName useWord2002TableStyleRules = w + "useWord2002TableStyleRules";
        public static readonly XName useWord97LineBreakRules = w + "useWord97LineBreakRules";
        public static readonly XName useXSLTWhenSaving = w + "useXSLTWhenSaving";
        public static readonly XName val = w + "val";
        public static readonly XName vAlign = w + "vAlign";
        public static readonly XName value = w + "value";
        public static readonly XName vAnchor = w + "vAnchor";
        public static readonly XName vanish = w + "vanish";
        public static readonly XName vendorID = w + "vendorID";
        public static readonly XName vert = w + "vert";
        public static readonly XName vertAlign = w + "vertAlign";
        public static readonly XName vertAnchor = w + "vertAnchor";
        public static readonly XName vertCompress = w + "vertCompress";
        public static readonly XName view = w + "view";
        public static readonly XName viewMergedData = w + "viewMergedData";
        public static readonly XName vMerge = w + "vMerge";
        public static readonly XName vMergeOrig = w + "vMergeOrig";
        public static readonly XName vSpace = w + "vSpace";
        public static readonly XName _w = w + "w";
        public static readonly XName wAfter = w + "wAfter";
        public static readonly XName wBefore = w + "wBefore";
        public static readonly XName webHidden = w + "webHidden";
        public static readonly XName webSettings = w + "webSettings";
        public static readonly XName widowControl = w + "widowControl";
        public static readonly XName wordWrap = w + "wordWrap";
        public static readonly XName wpJustification = w + "wpJustification";
        public static readonly XName wpSpaceWidth = w + "wpSpaceWidth";
        public static readonly XName wrap = w + "wrap";
        public static readonly XName wrapTrailSpaces = w + "wrapTrailSpaces";
        public static readonly XName writeProtection = w + "writeProtection";
        public static readonly XName x = w + "x";
        public static readonly XName xAlign = w + "xAlign";
        public static readonly XName xpath = w + "xpath";
        public static readonly XName y = w + "y";
        public static readonly XName yAlign = w + "yAlign";
        public static readonly XName yearLong = w + "yearLong";
        public static readonly XName yearShort = w + "yearShort";
        public static readonly XName zoom = w + "zoom";
        public static readonly XName zOrder = w + "zOrder";
        public static readonly XName tblCaption = w + "tblCaption";
        public static readonly XName tblDescription = w + "tblDescription";
        public static readonly XName startChars = w + "startChars";
        public static readonly XName end = w + "end";
        public static readonly XName endChars = w + "endChars";
        public static readonly XName evenHBand = w + "evenHBand";
        public static readonly XName evenVBand = w + "evenVBand";
        public static readonly XName firstRowFirstColumn = w + "firstRowFirstColumn";
        public static readonly XName firstRowLastColumn = w + "firstRowLastColumn";
        public static readonly XName lastRowFirstColumn = w + "lastRowFirstColumn";
        public static readonly XName lastRowLastColumn = w + "lastRowLastColumn";
        public static readonly XName oddHBand = w + "oddHBand";
        public static readonly XName oddVBand = w + "oddVBand";
        public static readonly XName headers = w + "headers";

        public static readonly XName[] BlockLevelContentContainers =
        {
            W.body,
            W.tc,
            W.txbxContent,
            W.hdr,
            W.ftr,
            W.endnote,
            W.footnote
        };

        public static readonly XName[] SubRunLevelContent =
        {
            W.br,
            W.cr,
            W.dayLong,
            W.dayShort,
            W.drawing,
            W.drawing,
            W.monthLong,
            W.monthShort,
            W.noBreakHyphen,
            W.ptab,
            W.pgNum,
            W.pict,
            W.softHyphen,
            W.sym,
            W.t,
            W.tab,
            W.yearLong,
            W.yearShort,
            MC.AlternateContent,
        };
    }

    public static class P
    {
        public static readonly XNamespace p =
            "http://schemas.openxmlformats.org/presentationml/2006/main";
        public static readonly XName anim = p + "anim";
        public static readonly XName animClr = p + "animClr";
        public static readonly XName animEffect = p + "animEffect";
        public static readonly XName animMotion = p + "animMotion";
        public static readonly XName animRot = p + "animRot";
        public static readonly XName animScale = p + "animScale";
        public static readonly XName attrName = p + "attrName";
        public static readonly XName attrNameLst = p + "attrNameLst";
        public static readonly XName audio = p + "audio";
        public static readonly XName bg = p + "bg";
        public static readonly XName bgPr = p + "bgPr";
        public static readonly XName bgRef = p + "bgRef";
        public static readonly XName bldAsOne = p + "bldAsOne";
        public static readonly XName bldDgm = p + "bldDgm";
        public static readonly XName bldGraphic = p + "bldGraphic";
        public static readonly XName bldLst = p + "bldLst";
        public static readonly XName bldOleChart = p + "bldOleChart";
        public static readonly XName bldP = p + "bldP";
        public static readonly XName bldSub = p + "bldSub";
        public static readonly XName blinds = p + "blinds";
        public static readonly XName blipFill = p + "blipFill";
        public static readonly XName bodyStyle = p + "bodyStyle";
        public static readonly XName bold = p + "bold";
        public static readonly XName boldItalic = p + "boldItalic";
        public static readonly XName boolVal = p + "boolVal";
        public static readonly XName by = p + "by";
        public static readonly XName cBhvr = p + "cBhvr";
        public static readonly XName charRg = p + "charRg";
        public static readonly XName checker = p + "checker";
        public static readonly XName childTnLst = p + "childTnLst";
        public static readonly XName circle = p + "circle";
        public static readonly XName clrMap = p + "clrMap";
        public static readonly XName clrMapOvr = p + "clrMapOvr";
        public static readonly XName clrVal = p + "clrVal";
        public static readonly XName cm = p + "cm";
        public static readonly XName cmAuthor = p + "cmAuthor";
        public static readonly XName cmAuthorLst = p + "cmAuthorLst";
        public static readonly XName cmd = p + "cmd";
        public static readonly XName cMediaNode = p + "cMediaNode";
        public static readonly XName cmLst = p + "cmLst";
        public static readonly XName cNvCxnSpPr = p + "cNvCxnSpPr";
        public static readonly XName cNvGraphicFramePr = p + "cNvGraphicFramePr";
        public static readonly XName cNvGrpSpPr = p + "cNvGrpSpPr";
        public static readonly XName cNvPicPr = p + "cNvPicPr";
        public static readonly XName cNvPr = p + "cNvPr";
        public static readonly XName cNvSpPr = p + "cNvSpPr";
        public static readonly XName comb = p + "comb";
        public static readonly XName cond = p + "cond";
        public static readonly XName contentPart = p + "contentPart";
        public static readonly XName control = p + "control";
        public static readonly XName controls = p + "controls";
        public static readonly XName cover = p + "cover";
        public static readonly XName cSld = p + "cSld";
        public static readonly XName cSldViewPr = p + "cSldViewPr";
        public static readonly XName cTn = p + "cTn";
        public static readonly XName custData = p + "custData";
        public static readonly XName custDataLst = p + "custDataLst";
        public static readonly XName custShow = p + "custShow";
        public static readonly XName custShowLst = p + "custShowLst";
        public static readonly XName cut = p + "cut";
        public static readonly XName cViewPr = p + "cViewPr";
        public static readonly XName cxnSp = p + "cxnSp";
        public static readonly XName defaultTextStyle = p + "defaultTextStyle";
        public static readonly XName diamond = p + "diamond";
        public static readonly XName dissolve = p + "dissolve";
        public static readonly XName embed = p + "embed";
        public static readonly XName embeddedFont = p + "embeddedFont";
        public static readonly XName embeddedFontLst = p + "embeddedFontLst";
        public static readonly XName endCondLst = p + "endCondLst";
        public static readonly XName endSnd = p + "endSnd";
        public static readonly XName endSync = p + "endSync";
        public static readonly XName ext = p + "ext";
        public static readonly XName externalData = p + "externalData";
        public static readonly XName extLst = p + "extLst";
        public static readonly XName fade = p + "fade";
        public static readonly XName fltVal = p + "fltVal";
        public static readonly XName font = p + "font";
        public static readonly XName from = p + "from";
        public static readonly XName graphicEl = p + "graphicEl";
        public static readonly XName graphicFrame = p + "graphicFrame";
        public static readonly XName gridSpacing = p + "gridSpacing";
        public static readonly XName grpSp = p + "grpSp";
        public static readonly XName grpSpPr = p + "grpSpPr";
        public static readonly XName guide = p + "guide";
        public static readonly XName guideLst = p + "guideLst";
        public static readonly XName handoutMaster = p + "handoutMaster";
        public static readonly XName handoutMasterId = p + "handoutMasterId";
        public static readonly XName handoutMasterIdLst = p + "handoutMasterIdLst";
        public static readonly XName hf = p + "hf";
        public static readonly XName hsl = p + "hsl";
        public static readonly XName inkTgt = p + "inkTgt";
        public static readonly XName italic = p + "italic";
        public static readonly XName iterate = p + "iterate";
        public static readonly XName kinsoku = p + "kinsoku";
        public static readonly XName link = p + "link";
        public static readonly XName modifyVerifier = p + "modifyVerifier";
        public static readonly XName newsflash = p + "newsflash";
        public static readonly XName nextCondLst = p + "nextCondLst";
        public static readonly XName normalViewPr = p + "normalViewPr";
        public static readonly XName notes = p + "notes";
        public static readonly XName notesMaster = p + "notesMaster";
        public static readonly XName notesMasterId = p + "notesMasterId";
        public static readonly XName notesMasterIdLst = p + "notesMasterIdLst";
        public static readonly XName notesStyle = p + "notesStyle";
        public static readonly XName notesSz = p + "notesSz";
        public static readonly XName notesTextViewPr = p + "notesTextViewPr";
        public static readonly XName notesViewPr = p + "notesViewPr";
        public static readonly XName nvCxnSpPr = p + "nvCxnSpPr";
        public static readonly XName nvGraphicFramePr = p + "nvGraphicFramePr";
        public static readonly XName nvGrpSpPr = p + "nvGrpSpPr";
        public static readonly XName nvPicPr = p + "nvPicPr";
        public static readonly XName nvPr = p + "nvPr";
        public static readonly XName nvSpPr = p + "nvSpPr";
        public static readonly XName oleChartEl = p + "oleChartEl";
        public static readonly XName oleObj = p + "oleObj";
        public static readonly XName origin = p + "origin";
        public static readonly XName otherStyle = p + "otherStyle";
        public static readonly XName outlineViewPr = p + "outlineViewPr";
        public static readonly XName par = p + "par";
        public static readonly XName ph = p + "ph";
        public static readonly XName photoAlbum = p + "photoAlbum";
        public static readonly XName pic = p + "pic";
        public static readonly XName plus = p + "plus";
        public static readonly XName pos = p + "pos";
        public static readonly XName presentation = p + "presentation";
        public static readonly XName prevCondLst = p + "prevCondLst";
        public static readonly XName pRg = p + "pRg";
        public static readonly XName pull = p + "pull";
        public static readonly XName push = p + "push";
        public static readonly XName random = p + "random";
        public static readonly XName randomBar = p + "randomBar";
        public static readonly XName rCtr = p + "rCtr";
        public static readonly XName regular = p + "regular";
        public static readonly XName restoredLeft = p + "restoredLeft";
        public static readonly XName restoredTop = p + "restoredTop";
        public static readonly XName rgb = p + "rgb";
        public static readonly XName rtn = p + "rtn";
        public static readonly XName scale = p + "scale";
        public static readonly XName seq = p + "seq";
        public static readonly XName set = p + "set";
        public static readonly XName sld = p + "sld";
        public static readonly XName sldId = p + "sldId";
        public static readonly XName sldIdLst = p + "sldIdLst";
        public static readonly XName sldLayout = p + "sldLayout";
        public static readonly XName sldLayoutId = p + "sldLayoutId";
        public static readonly XName sldLayoutIdLst = p + "sldLayoutIdLst";
        public static readonly XName sldLst = p + "sldLst";
        public static readonly XName sldMaster = p + "sldMaster";
        public static readonly XName sldMasterId = p + "sldMasterId";
        public static readonly XName sldMasterIdLst = p + "sldMasterIdLst";
        public static readonly XName sldSyncPr = p + "sldSyncPr";
        public static readonly XName sldSz = p + "sldSz";
        public static readonly XName sldTgt = p + "sldTgt";
        public static readonly XName slideViewPr = p + "slideViewPr";
        public static readonly XName snd = p + "snd";
        public static readonly XName sndAc = p + "sndAc";
        public static readonly XName sndTgt = p + "sndTgt";
        public static readonly XName sorterViewPr = p + "sorterViewPr";
        public static readonly XName sp = p + "sp";
        public static readonly XName split = p + "split";
        public static readonly XName spPr = p + "spPr";
        public static readonly XName spTgt = p + "spTgt";
        public static readonly XName spTree = p + "spTree";
        public static readonly XName stCondLst = p + "stCondLst";
        public static readonly XName strips = p + "strips";
        public static readonly XName strVal = p + "strVal";
        public static readonly XName stSnd = p + "stSnd";
        public static readonly XName style = p + "style";
        public static readonly XName subSp = p + "subSp";
        public static readonly XName subTnLst = p + "subTnLst";
        public static readonly XName tag = p + "tag";
        public static readonly XName tagLst = p + "tagLst";
        public static readonly XName tags = p + "tags";
        public static readonly XName tav = p + "tav";
        public static readonly XName tavLst = p + "tavLst";
        public static readonly XName text = p + "text";
        public static readonly XName tgtEl = p + "tgtEl";
        public static readonly XName timing = p + "timing";
        public static readonly XName titleStyle = p + "titleStyle";
        public static readonly XName tmAbs = p + "tmAbs";
        public static readonly XName tmPct = p + "tmPct";
        public static readonly XName tmpl = p + "tmpl";
        public static readonly XName tmplLst = p + "tmplLst";
        public static readonly XName tn = p + "tn";
        public static readonly XName tnLst = p + "tnLst";
        public static readonly XName to = p + "to";
        public static readonly XName transition = p + "transition";
        public static readonly XName txBody = p + "txBody";
        public static readonly XName txEl = p + "txEl";
        public static readonly XName txStyles = p + "txStyles";
        public static readonly XName val = p + "val";
        public static readonly XName video = p + "video";
        public static readonly XName viewPr = p + "viewPr";
        public static readonly XName wedge = p + "wedge";
        public static readonly XName wheel = p + "wheel";
        public static readonly XName wipe = p + "wipe";
        public static readonly XName xfrm = p + "xfrm";
        public static readonly XName zoom = p + "zoom";
    }

    public static class EP
    {
        public static readonly XNamespace ep =
            "http://schemas.openxmlformats.org/officeDocument/2006/extended-properties";
        public static readonly XName Application = ep + "Application";
        public static readonly XName AppVersion = ep + "AppVersion";
        public static readonly XName Characters = ep + "Characters";
        public static readonly XName CharactersWithSpaces = ep + "CharactersWithSpaces";
        public static readonly XName Company = ep + "Company";
        public static readonly XName DocSecurity = ep + "DocSecurity";
        public static readonly XName HeadingPairs = ep + "HeadingPairs";
        public static readonly XName HiddenSlides = ep + "HiddenSlides";
        public static readonly XName HLinks = ep + "HLinks";
        public static readonly XName HyperlinkBase = ep + "HyperlinkBase";
        public static readonly XName HyperlinksChanged = ep + "HyperlinksChanged";
        public static readonly XName Lines = ep + "Lines";
        public static readonly XName LinksUpToDate = ep + "LinksUpToDate";
        public static readonly XName Manager = ep + "Manager";
        public static readonly XName MMClips = ep + "MMClips";
        public static readonly XName Notes = ep + "Notes";
        public static readonly XName Pages = ep + "Pages";
        public static readonly XName Paragraphs = ep + "Paragraphs";
        public static readonly XName PresentationFormat = ep + "PresentationFormat";
        public static readonly XName Properties = ep + "Properties";
        public static readonly XName ScaleCrop = ep + "ScaleCrop";
        public static readonly XName SharedDoc = ep + "SharedDoc";
        public static readonly XName Slides = ep + "Slides";
        public static readonly XName Template = ep + "Template";
        public static readonly XName TitlesOfParts = ep + "TitlesOfParts";
        public static readonly XName TotalTime = ep + "TotalTime";
        public static readonly XName Words = ep + "Words";
    }

    public static class A
    {
        public static readonly XNamespace a =
            "http://schemas.openxmlformats.org/drawingml/2006/main";
        public static readonly XName accent1 = a + "accent1";
        public static readonly XName accent2 = a + "accent2";
        public static readonly XName accent3 = a + "accent3";
        public static readonly XName accent4 = a + "accent4";
        public static readonly XName accent5 = a + "accent5";
        public static readonly XName accent6 = a + "accent6";
        public static readonly XName ahLst = a + "ahLst";
        public static readonly XName ahPolar = a + "ahPolar";
        public static readonly XName ahXY = a + "ahXY";
        public static readonly XName alpha = a + "alpha";
        public static readonly XName alphaBiLevel = a + "alphaBiLevel";
        public static readonly XName alphaCeiling = a + "alphaCeiling";
        public static readonly XName alphaFloor = a + "alphaFloor";
        public static readonly XName alphaInv = a + "alphaInv";
        public static readonly XName alphaMod = a + "alphaMod";
        public static readonly XName alphaModFix = a + "alphaModFix";
        public static readonly XName alphaOff = a + "alphaOff";
        public static readonly XName alphaOutset = a + "alphaOutset";
        public static readonly XName alphaRepl = a + "alphaRepl";
        public static readonly XName anchor = a + "anchor";
        public static readonly XName arcTo = a + "arcTo";
        public static readonly XName audioCd = a + "audioCd";
        public static readonly XName audioFile = a + "audioFile";
        public static readonly XName avLst = a + "avLst";
        public static readonly XName backdrop = a + "backdrop";
        public static readonly XName band1H = a + "band1H";
        public static readonly XName band1V = a + "band1V";
        public static readonly XName band2H = a + "band2H";
        public static readonly XName band2V = a + "band2V";
        public static readonly XName bevel = a + "bevel";
        public static readonly XName bevelB = a + "bevelB";
        public static readonly XName bevelT = a + "bevelT";
        public static readonly XName bgClr = a + "bgClr";
        public static readonly XName bgFillStyleLst = a + "bgFillStyleLst";
        public static readonly XName biLevel = a + "biLevel";
        public static readonly XName bldChart = a + "bldChart";
        public static readonly XName bldDgm = a + "bldDgm";
        public static readonly XName blend = a + "blend";
        public static readonly XName blip = a + "blip";
        public static readonly XName blipFill = a + "blipFill";
        public static readonly XName blue = a + "blue";
        public static readonly XName blueMod = a + "blueMod";
        public static readonly XName blueOff = a + "blueOff";
        public static readonly XName blur = a + "blur";
        public static readonly XName bodyPr = a + "bodyPr";
        public static readonly XName bottom = a + "bottom";
        public static readonly XName br = a + "br";
        public static readonly XName buAutoNum = a + "buAutoNum";
        public static readonly XName buBlip = a + "buBlip";
        public static readonly XName buChar = a + "buChar";
        public static readonly XName buClr = a + "buClr";
        public static readonly XName buClrTx = a + "buClrTx";
        public static readonly XName buFont = a + "buFont";
        public static readonly XName buFontTx = a + "buFontTx";
        public static readonly XName buNone = a + "buNone";
        public static readonly XName buSzPct = a + "buSzPct";
        public static readonly XName buSzPts = a + "buSzPts";
        public static readonly XName buSzTx = a + "buSzTx";
        public static readonly XName camera = a + "camera";
        public static readonly XName cell3D = a + "cell3D";
        public static readonly XName chart = a + "chart";
        public static readonly XName chExt = a + "chExt";
        public static readonly XName chOff = a + "chOff";
        public static readonly XName close = a + "close";
        public static readonly XName clrChange = a + "clrChange";
        public static readonly XName clrFrom = a + "clrFrom";
        public static readonly XName clrMap = a + "clrMap";
        public static readonly XName clrRepl = a + "clrRepl";
        public static readonly XName clrScheme = a + "clrScheme";
        public static readonly XName clrTo = a + "clrTo";
        public static readonly XName cNvCxnSpPr = a + "cNvCxnSpPr";
        public static readonly XName cNvGraphicFramePr = a + "cNvGraphicFramePr";
        public static readonly XName cNvGrpSpPr = a + "cNvGrpSpPr";
        public static readonly XName cNvPicPr = a + "cNvPicPr";
        public static readonly XName cNvPr = a + "cNvPr";
        public static readonly XName cNvSpPr = a + "cNvSpPr";
        public static readonly XName comp = a + "comp";
        public static readonly XName cont = a + "cont";
        public static readonly XName contourClr = a + "contourClr";
        public static readonly XName cs = a + "cs";
        public static readonly XName cubicBezTo = a + "cubicBezTo";
        public static readonly XName custClr = a + "custClr";
        public static readonly XName custClrLst = a + "custClrLst";
        public static readonly XName custDash = a + "custDash";
        public static readonly XName custGeom = a + "custGeom";
        public static readonly XName cxn = a + "cxn";
        public static readonly XName cxnLst = a + "cxnLst";
        public static readonly XName cxnSp = a + "cxnSp";
        public static readonly XName cxnSpLocks = a + "cxnSpLocks";
        public static readonly XName defPPr = a + "defPPr";
        public static readonly XName defRPr = a + "defRPr";
        public static readonly XName dgm = a + "dgm";
        public static readonly XName dk1 = a + "dk1";
        public static readonly XName dk2 = a + "dk2";
        public static readonly XName ds = a + "ds";
        public static readonly XName duotone = a + "duotone";
        public static readonly XName ea = a + "ea";
        public static readonly XName effect = a + "effect";
        public static readonly XName effectDag = a + "effectDag";
        public static readonly XName effectLst = a + "effectLst";
        public static readonly XName effectRef = a + "effectRef";
        public static readonly XName effectStyle = a + "effectStyle";
        public static readonly XName effectStyleLst = a + "effectStyleLst";
        public static readonly XName end = a + "end";
        public static readonly XName endCxn = a + "endCxn";
        public static readonly XName endParaRPr = a + "endParaRPr";
        public static readonly XName ext = a + "ext";
        public static readonly XName extLst = a + "extLst";
        public static readonly XName extraClrScheme = a + "extraClrScheme";
        public static readonly XName extraClrSchemeLst = a + "extraClrSchemeLst";
        public static readonly XName extrusionClr = a + "extrusionClr";
        public static readonly XName fgClr = a + "fgClr";
        public static readonly XName fill = a + "fill";
        public static readonly XName fillOverlay = a + "fillOverlay";
        public static readonly XName fillRect = a + "fillRect";
        public static readonly XName fillRef = a + "fillRef";
        public static readonly XName fillStyleLst = a + "fillStyleLst";
        public static readonly XName fillToRect = a + "fillToRect";
        public static readonly XName firstCol = a + "firstCol";
        public static readonly XName firstRow = a + "firstRow";
        public static readonly XName flatTx = a + "flatTx";
        public static readonly XName fld = a + "fld";
        public static readonly XName fmtScheme = a + "fmtScheme";
        public static readonly XName folHlink = a + "folHlink";
        public static readonly XName font = a + "font";
        public static readonly XName fontRef = a + "fontRef";
        public static readonly XName fontScheme = a + "fontScheme";
        public static readonly XName gamma = a + "gamma";
        public static readonly XName gd = a + "gd";
        public static readonly XName gdLst = a + "gdLst";
        public static readonly XName glow = a + "glow";
        public static readonly XName gradFill = a + "gradFill";
        public static readonly XName graphic = a + "graphic";
        public static readonly XName graphicData = a + "graphicData";
        public static readonly XName graphicFrame = a + "graphicFrame";
        public static readonly XName graphicFrameLocks = a + "graphicFrameLocks";
        public static readonly XName gray = a + "gray";
        public static readonly XName grayscl = a + "grayscl";
        public static readonly XName green = a + "green";
        public static readonly XName greenMod = a + "greenMod";
        public static readonly XName greenOff = a + "greenOff";
        public static readonly XName gridCol = a + "gridCol";
        public static readonly XName grpFill = a + "grpFill";
        public static readonly XName grpSp = a + "grpSp";
        public static readonly XName grpSpLocks = a + "grpSpLocks";
        public static readonly XName grpSpPr = a + "grpSpPr";
        public static readonly XName gs = a + "gs";
        public static readonly XName gsLst = a + "gsLst";
        public static readonly XName headEnd = a + "headEnd";
        public static readonly XName highlight = a + "highlight";
        public static readonly XName hlink = a + "hlink";
        public static readonly XName hlinkClick = a + "hlinkClick";
        public static readonly XName hlinkHover = a + "hlinkHover";
        public static readonly XName hlinkMouseOver = a + "hlinkMouseOver";
        public static readonly XName hsl = a + "hsl";
        public static readonly XName hslClr = a + "hslClr";
        public static readonly XName hue = a + "hue";
        public static readonly XName hueMod = a + "hueMod";
        public static readonly XName hueOff = a + "hueOff";
        public static readonly XName innerShdw = a + "innerShdw";
        public static readonly XName insideH = a + "insideH";
        public static readonly XName insideV = a + "insideV";
        public static readonly XName inv = a + "inv";
        public static readonly XName invGamma = a + "invGamma";
        public static readonly XName lastCol = a + "lastCol";
        public static readonly XName lastRow = a + "lastRow";
        public static readonly XName latin = a + "latin";
        public static readonly XName left = a + "left";
        public static readonly XName lightRig = a + "lightRig";
        public static readonly XName lin = a + "lin";
        public static readonly XName ln = a + "ln";
        public static readonly XName lnB = a + "lnB";
        public static readonly XName lnBlToTr = a + "lnBlToTr";
        public static readonly XName lnDef = a + "lnDef";
        public static readonly XName lnL = a + "lnL";
        public static readonly XName lnR = a + "lnR";
        public static readonly XName lnRef = a + "lnRef";
        public static readonly XName lnSpc = a + "lnSpc";
        public static readonly XName lnStyleLst = a + "lnStyleLst";
        public static readonly XName lnT = a + "lnT";
        public static readonly XName lnTlToBr = a + "lnTlToBr";
        public static readonly XName lnTo = a + "lnTo";
        public static readonly XName lstStyle = a + "lstStyle";
        public static readonly XName lt1 = a + "lt1";
        public static readonly XName lt2 = a + "lt2";
        public static readonly XName lum = a + "lum";
        public static readonly XName lumMod = a + "lumMod";
        public static readonly XName lumOff = a + "lumOff";
        public static readonly XName lvl1pPr = a + "lvl1pPr";
        public static readonly XName lvl2pPr = a + "lvl2pPr";
        public static readonly XName lvl3pPr = a + "lvl3pPr";
        public static readonly XName lvl4pPr = a + "lvl4pPr";
        public static readonly XName lvl5pPr = a + "lvl5pPr";
        public static readonly XName lvl6pPr = a + "lvl6pPr";
        public static readonly XName lvl7pPr = a + "lvl7pPr";
        public static readonly XName lvl8pPr = a + "lvl8pPr";
        public static readonly XName lvl9pPr = a + "lvl9pPr";
        public static readonly XName majorFont = a + "majorFont";
        public static readonly XName masterClrMapping = a + "masterClrMapping";
        public static readonly XName minorFont = a + "minorFont";
        public static readonly XName miter = a + "miter";
        public static readonly XName moveTo = a + "moveTo";
        public static readonly XName neCell = a + "neCell";
        public static readonly XName noAutofit = a + "noAutofit";
        public static readonly XName noFill = a + "noFill";
        public static readonly XName norm = a + "norm";
        public static readonly XName normAutofit = a + "normAutofit";
        public static readonly XName nvCxnSpPr = a + "nvCxnSpPr";
        public static readonly XName nvGraphicFramePr = a + "nvGraphicFramePr";
        public static readonly XName nvGrpSpPr = a + "nvGrpSpPr";
        public static readonly XName nvPicPr = a + "nvPicPr";
        public static readonly XName nvSpPr = a + "nvSpPr";
        public static readonly XName nwCell = a + "nwCell";
        public static readonly XName objectDefaults = a + "objectDefaults";
        public static readonly XName off = a + "off";
        public static readonly XName outerShdw = a + "outerShdw";
        public static readonly XName overrideClrMapping = a + "overrideClrMapping";
        public static readonly XName p = a + "p";
        public static readonly XName path = a + "path";
        public static readonly XName pathLst = a + "pathLst";
        public static readonly XName pattFill = a + "pattFill";
        public static readonly XName pic = a + "pic";
        public static readonly XName picLocks = a + "picLocks";
        public static readonly XName pos = a + "pos";
        public static readonly XName pPr = a + "pPr";
        public static readonly XName prstClr = a + "prstClr";
        public static readonly XName prstDash = a + "prstDash";
        public static readonly XName prstGeom = a + "prstGeom";
        public static readonly XName prstShdw = a + "prstShdw";
        public static readonly XName prstTxWarp = a + "prstTxWarp";
        public static readonly XName pt = a + "pt";
        public static readonly XName quadBezTo = a + "quadBezTo";
        public static readonly XName quickTimeFile = a + "quickTimeFile";
        public static readonly XName r = a + "r";
        public static readonly XName rect = a + "rect";
        public static readonly XName red = a + "red";
        public static readonly XName redMod = a + "redMod";
        public static readonly XName redOff = a + "redOff";
        public static readonly XName reflection = a + "reflection";
        public static readonly XName relIds = a + "relIds";
        public static readonly XName relOff = a + "relOff";
        public static readonly XName right = a + "right";
        public static readonly XName rot = a + "rot";
        public static readonly XName round = a + "round";
        public static readonly XName rPr = a + "rPr";
        public static readonly XName sat = a + "sat";
        public static readonly XName satMod = a + "satMod";
        public static readonly XName satOff = a + "satOff";
        public static readonly XName scene3d = a + "scene3d";
        public static readonly XName schemeClr = a + "schemeClr";
        public static readonly XName scrgbClr = a + "scrgbClr";
        public static readonly XName seCell = a + "seCell";
        public static readonly XName shade = a + "shade";
        public static readonly XName snd = a + "snd";
        public static readonly XName softEdge = a + "softEdge";
        public static readonly XName solidFill = a + "solidFill";
        public static readonly XName sp = a + "sp";
        public static readonly XName sp3d = a + "sp3d";
        public static readonly XName spAutoFit = a + "spAutoFit";
        public static readonly XName spcAft = a + "spcAft";
        public static readonly XName spcBef = a + "spcBef";
        public static readonly XName spcPct = a + "spcPct";
        public static readonly XName spcPts = a + "spcPts";
        public static readonly XName spDef = a + "spDef";
        public static readonly XName spLocks = a + "spLocks";
        public static readonly XName spPr = a + "spPr";
        public static readonly XName srcRect = a + "srcRect";
        public static readonly XName srgbClr = a + "srgbClr";
        public static readonly XName st = a + "st";
        public static readonly XName stCxn = a + "stCxn";
        public static readonly XName stretch = a + "stretch";
        public static readonly XName style = a + "style";
        public static readonly XName swCell = a + "swCell";
        public static readonly XName sx = a + "sx";
        public static readonly XName sy = a + "sy";
        public static readonly XName sym = a + "sym";
        public static readonly XName sysClr = a + "sysClr";
        public static readonly XName t = a + "t";
        public static readonly XName tab = a + "tab";
        public static readonly XName tableStyle = a + "tableStyle";
        public static readonly XName tableStyleId = a + "tableStyleId";
        public static readonly XName tabLst = a + "tabLst";
        public static readonly XName tailEnd = a + "tailEnd";
        public static readonly XName tbl = a + "tbl";
        public static readonly XName tblBg = a + "tblBg";
        public static readonly XName tblGrid = a + "tblGrid";
        public static readonly XName tblPr = a + "tblPr";
        public static readonly XName tblStyle = a + "tblStyle";
        public static readonly XName tblStyleLst = a + "tblStyleLst";
        public static readonly XName tc = a + "tc";
        public static readonly XName tcBdr = a + "tcBdr";
        public static readonly XName tcPr = a + "tcPr";
        public static readonly XName tcStyle = a + "tcStyle";
        public static readonly XName tcTxStyle = a + "tcTxStyle";
        public static readonly XName theme = a + "theme";
        public static readonly XName themeElements = a + "themeElements";
        public static readonly XName themeOverride = a + "themeOverride";
        public static readonly XName tile = a + "tile";
        public static readonly XName tileRect = a + "tileRect";
        public static readonly XName tint = a + "tint";
        public static readonly XName top = a + "top";
        public static readonly XName tr = a + "tr";
        public static readonly XName txBody = a + "txBody";
        public static readonly XName txDef = a + "txDef";
        public static readonly XName txSp = a + "txSp";
        public static readonly XName uFill = a + "uFill";
        public static readonly XName uFillTx = a + "uFillTx";
        public static readonly XName uLn = a + "uLn";
        public static readonly XName uLnTx = a + "uLnTx";
        public static readonly XName up = a + "up";
        public static readonly XName useSpRect = a + "useSpRect";
        public static readonly XName videoFile = a + "videoFile";
        public static readonly XName wavAudioFile = a + "wavAudioFile";
        public static readonly XName wholeTbl = a + "wholeTbl";
        public static readonly XName xfrm = a + "xfrm";
    }
    public static class M
    {
        public static readonly XNamespace m =
            "http://schemas.openxmlformats.org/officeDocument/2006/math";
        public static readonly XName acc = m + "acc";
        public static readonly XName accPr = m + "accPr";
        public static readonly XName aln = m + "aln";
        public static readonly XName alnAt = m + "alnAt";
        public static readonly XName alnScr = m + "alnScr";
        public static readonly XName argPr = m + "argPr";
        public static readonly XName argSz = m + "argSz";
        public static readonly XName bar = m + "bar";
        public static readonly XName barPr = m + "barPr";
        public static readonly XName baseJc = m + "baseJc";
        public static readonly XName begChr = m + "begChr";
        public static readonly XName borderBox = m + "borderBox";
        public static readonly XName borderBoxPr = m + "borderBoxPr";
        public static readonly XName box = m + "box";
        public static readonly XName boxPr = m + "boxPr";
        public static readonly XName brk = m + "brk";
        public static readonly XName brkBin = m + "brkBin";
        public static readonly XName brkBinSub = m + "brkBinSub";
        public static readonly XName cGp = m + "cGp";
        public static readonly XName cGpRule = m + "cGpRule";
        public static readonly XName chr = m + "chr";
        public static readonly XName count = m + "count";
        public static readonly XName cSp = m + "cSp";
        public static readonly XName ctrlPr = m + "ctrlPr";
        public static readonly XName d = m + "d";
        public static readonly XName defJc = m + "defJc";
        public static readonly XName deg = m + "deg";
        public static readonly XName degHide = m + "degHide";
        public static readonly XName den = m + "den";
        public static readonly XName diff = m + "diff";
        public static readonly XName dispDef = m + "dispDef";
        public static readonly XName dPr = m + "dPr";
        public static readonly XName e = m + "e";
        public static readonly XName endChr = m + "endChr";
        public static readonly XName eqArr = m + "eqArr";
        public static readonly XName eqArrPr = m + "eqArrPr";
        public static readonly XName f = m + "f";
        public static readonly XName fName = m + "fName";
        public static readonly XName fPr = m + "fPr";
        public static readonly XName func = m + "func";
        public static readonly XName funcPr = m + "funcPr";
        public static readonly XName groupChr = m + "groupChr";
        public static readonly XName groupChrPr = m + "groupChrPr";
        public static readonly XName grow = m + "grow";
        public static readonly XName hideBot = m + "hideBot";
        public static readonly XName hideLeft = m + "hideLeft";
        public static readonly XName hideRight = m + "hideRight";
        public static readonly XName hideTop = m + "hideTop";
        public static readonly XName interSp = m + "interSp";
        public static readonly XName intLim = m + "intLim";
        public static readonly XName intraSp = m + "intraSp";
        public static readonly XName jc = m + "jc";
        public static readonly XName lim = m + "lim";
        public static readonly XName limLoc = m + "limLoc";
        public static readonly XName limLow = m + "limLow";
        public static readonly XName limLowPr = m + "limLowPr";
        public static readonly XName limUpp = m + "limUpp";
        public static readonly XName limUppPr = m + "limUppPr";
        public static readonly XName lit = m + "lit";
        public static readonly XName lMargin = m + "lMargin";
        public static readonly XName _m = m + "m";
        public static readonly XName mathFont = m + "mathFont";
        public static readonly XName mathPr = m + "mathPr";
        public static readonly XName maxDist = m + "maxDist";
        public static readonly XName mc = m + "mc";
        public static readonly XName mcJc = m + "mcJc";
        public static readonly XName mcPr = m + "mcPr";
        public static readonly XName mcs = m + "mcs";
        public static readonly XName mPr = m + "mPr";
        public static readonly XName mr = m + "mr";
        public static readonly XName nary = m + "nary";
        public static readonly XName naryLim = m + "naryLim";
        public static readonly XName naryPr = m + "naryPr";
        public static readonly XName noBreak = m + "noBreak";
        public static readonly XName nor = m + "nor";
        public static readonly XName num = m + "num";
        public static readonly XName objDist = m + "objDist";
        public static readonly XName oMath = m + "oMath";
        public static readonly XName oMathPara = m + "oMathPara";
        public static readonly XName oMathParaPr = m + "oMathParaPr";
        public static readonly XName opEmu = m + "opEmu";
        public static readonly XName phant = m + "phant";
        public static readonly XName phantPr = m + "phantPr";
        public static readonly XName plcHide = m + "plcHide";
        public static readonly XName pos = m + "pos";
        public static readonly XName postSp = m + "postSp";
        public static readonly XName preSp = m + "preSp";
        public static readonly XName r = m + "r";
        public static readonly XName rad = m + "rad";
        public static readonly XName radPr = m + "radPr";
        public static readonly XName rMargin = m + "rMargin";
        public static readonly XName rPr = m + "rPr";
        public static readonly XName rSp = m + "rSp";
        public static readonly XName rSpRule = m + "rSpRule";
        public static readonly XName scr = m + "scr";
        public static readonly XName sepChr = m + "sepChr";
        public static readonly XName show = m + "show";
        public static readonly XName shp = m + "shp";
        public static readonly XName smallFrac = m + "smallFrac";
        public static readonly XName sPre = m + "sPre";
        public static readonly XName sPrePr = m + "sPrePr";
        public static readonly XName sSub = m + "sSub";
        public static readonly XName sSubPr = m + "sSubPr";
        public static readonly XName sSubSup = m + "sSubSup";
        public static readonly XName sSubSupPr = m + "sSubSupPr";
        public static readonly XName sSup = m + "sSup";
        public static readonly XName sSupPr = m + "sSupPr";
        public static readonly XName strikeBLTR = m + "strikeBLTR";
        public static readonly XName strikeH = m + "strikeH";
        public static readonly XName strikeTLBR = m + "strikeTLBR";
        public static readonly XName strikeV = m + "strikeV";
        public static readonly XName sty = m + "sty";
        public static readonly XName sub = m + "sub";
        public static readonly XName subHide = m + "subHide";
        public static readonly XName sup = m + "sup";
        public static readonly XName supHide = m + "supHide";
        public static readonly XName t = m + "t";
        public static readonly XName transp = m + "transp";
        public static readonly XName type = m + "type";
        public static readonly XName val = m + "val";
        public static readonly XName vertJc = m + "vertJc";
        public static readonly XName wrapIndent = m + "wrapIndent";
        public static readonly XName wrapRight = m + "wrapRight";
        public static readonly XName zeroAsc = m + "zeroAsc";
        public static readonly XName zeroDesc = m + "zeroDesc";
        public static readonly XName zeroWid = m + "zeroWid";
    }

    public static class MC
    {
        public static readonly XNamespace mc =
            "http://schemas.openxmlformats.org/markup-compatibility/2006";
        public static readonly XName AlternateContent = mc + "AlternateContent";
        public static readonly XName Choice = mc + "Choice";
        public static readonly XName Fallback = mc + "Fallback";
        public static readonly XName Ignorable = mc + "Ignorable";
        public static readonly XName PreserveAttributes = mc + "PreserveAttributes";
    }

    public static class XmlUtil
    {
        public static XAttribute GetXmlSpaceAttribute(string value)
        {
            return (value.Length > 0) && ((value[0] == ' ') || (value[value.Length - 1] == ' '))
                ? new XAttribute(XNamespace.Xml + "space", "preserve")
                : null;
        }

        public static XAttribute GetXmlSpaceAttribute(char value)
        {
            return value == ' ' ? new XAttribute(XNamespace.Xml + "space", "preserve") : null;
        }
    }

    public static class PtOpenXml
    {
        public static XNamespace ptOpenXml = "http://powertools.codeplex.com/documentbuilder/2011/insert";
        public static XName Insert = ptOpenXml + "Insert";
        public static XName Id = "Id";

        public static XNamespace pt = "http://powertools.codeplex.com/2011";
        public static XName Uri = pt + "Uri";
        public static XName Unid = pt + "Unid";
        public static XName SHA1Hash = pt + "SHA1Hash";
        public static XName CorrelatedSHA1Hash = pt + "CorrelatedSHA1Hash";
        public static XName StructureSHA1Hash = pt + "StructureSHA1Hash";
        public static XName CorrelationSet = pt + "CorrelationSet";
        public static XName Status = pt + "Status";

        public static XName Level = pt + "Level";
        public static XName IndentLevel = pt + "IndentLevel";
        public static XName ContentType = pt + "ContentType";

        public static XName trPr = pt + "trPr";
        public static XName tcPr = pt + "tcPr";
        public static XName rPr = pt + "rPr";
        public static XName pPr = pt + "pPr";
        public static XName tblPr = pt + "tblPr";
        public static XName style = pt + "style";

        public static XName FontName = pt + "FontName";
        public static XName LanguageType = pt + "LanguageType";
        public static XName AbstractNumId = pt + "AbstractNumId";
        public static XName StyleName = pt + "StyleName";
        public static XName TabWidth = pt + "TabWidth";
        public static XName Leader = pt + "Leader";

        public static XName ListItemRun = pt + "ListItemRun";

        public static XName HtmlToWmlCssWidth = pt + "HtmlToWmlCssWidth";
    }

    public static class W14
    {
        public static readonly XNamespace w14 =
            "http://schemas.microsoft.com/office/word/2010/wordml";
        public static readonly XName algn = w14 + "algn";
        public static readonly XName alpha = w14 + "alpha";
        public static readonly XName ang = w14 + "ang";
        public static readonly XName b = w14 + "b";
        public static readonly XName bevel = w14 + "bevel";
        public static readonly XName bevelB = w14 + "bevelB";
        public static readonly XName bevelT = w14 + "bevelT";
        public static readonly XName blurRad = w14 + "blurRad";
        public static readonly XName camera = w14 + "camera";
        public static readonly XName cap = w14 + "cap";
        public static readonly XName checkbox = w14 + "checkbox";
        public static readonly XName _checked = w14 + "checked";
        public static readonly XName checkedState = w14 + "checkedState";
        public static readonly XName cmpd = w14 + "cmpd";
        public static readonly XName cntxtAlts = w14 + "cntxtAlts";
        public static readonly XName cNvContentPartPr = w14 + "cNvContentPartPr";
        public static readonly XName conflictMode = w14 + "conflictMode";
        public static readonly XName contentPart = w14 + "contentPart";
        public static readonly XName contourClr = w14 + "contourClr";
        public static readonly XName contourW = w14 + "contourW";
        public static readonly XName defaultImageDpi = w14 + "defaultImageDpi";
        public static readonly XName dir = w14 + "dir";
        public static readonly XName discardImageEditingData = w14 + "discardImageEditingData";
        public static readonly XName dist = w14 + "dist";
        public static readonly XName docId = w14 + "docId";
        public static readonly XName editId = w14 + "editId";
        public static readonly XName enableOpenTypeKerning = w14 + "enableOpenTypeKerning";
        public static readonly XName endA = w14 + "endA";
        public static readonly XName endPos = w14 + "endPos";
        public static readonly XName entityPicker = w14 + "entityPicker";
        public static readonly XName extrusionClr = w14 + "extrusionClr";
        public static readonly XName extrusionH = w14 + "extrusionH";
        public static readonly XName fadeDir = w14 + "fadeDir";
        public static readonly XName fillToRect = w14 + "fillToRect";
        public static readonly XName font = w14 + "font";
        public static readonly XName glow = w14 + "glow";
        public static readonly XName gradFill = w14 + "gradFill";
        public static readonly XName gs = w14 + "gs";
        public static readonly XName gsLst = w14 + "gsLst";
        public static readonly XName h = w14 + "h";
        public static readonly XName hueMod = w14 + "hueMod";
        public static readonly XName id = w14 + "id";
        public static readonly XName kx = w14 + "kx";
        public static readonly XName ky = w14 + "ky";
        public static readonly XName l = w14 + "l";
        public static readonly XName lat = w14 + "lat";
        public static readonly XName ligatures = w14 + "ligatures";
        public static readonly XName lightRig = w14 + "lightRig";
        public static readonly XName lim = w14 + "lim";
        public static readonly XName lin = w14 + "lin";
        public static readonly XName lon = w14 + "lon";
        public static readonly XName lumMod = w14 + "lumMod";
        public static readonly XName lumOff = w14 + "lumOff";
        public static readonly XName miter = w14 + "miter";
        public static readonly XName noFill = w14 + "noFill";
        public static readonly XName numForm = w14 + "numForm";
        public static readonly XName numSpacing = w14 + "numSpacing";
        public static readonly XName nvContentPartPr = w14 + "nvContentPartPr";
        public static readonly XName paraId = w14 + "paraId";
        public static readonly XName path = w14 + "path";
        public static readonly XName pos = w14 + "pos";
        public static readonly XName props3d = w14 + "props3d";
        public static readonly XName prst = w14 + "prst";
        public static readonly XName prstDash = w14 + "prstDash";
        public static readonly XName prstMaterial = w14 + "prstMaterial";
        public static readonly XName r = w14 + "r";
        public static readonly XName rad = w14 + "rad";
        public static readonly XName reflection = w14 + "reflection";
        public static readonly XName rev = w14 + "rev";
        public static readonly XName rig = w14 + "rig";
        public static readonly XName rot = w14 + "rot";
        public static readonly XName round = w14 + "round";
        public static readonly XName sat = w14 + "sat";
        public static readonly XName satMod = w14 + "satMod";
        public static readonly XName satOff = w14 + "satOff";
        public static readonly XName scaled = w14 + "scaled";
        public static readonly XName scene3d = w14 + "scene3d";
        public static readonly XName schemeClr = w14 + "schemeClr";
        public static readonly XName shade = w14 + "shade";
        public static readonly XName shadow = w14 + "shadow";
        public static readonly XName solidFill = w14 + "solidFill";
        public static readonly XName srgbClr = w14 + "srgbClr";
        public static readonly XName stA = w14 + "stA";
        public static readonly XName stPos = w14 + "stPos";
        public static readonly XName styleSet = w14 + "styleSet";
        public static readonly XName stylisticSets = w14 + "stylisticSets";
        public static readonly XName sx = w14 + "sx";
        public static readonly XName sy = w14 + "sy";
        public static readonly XName t = w14 + "t";
        public static readonly XName textFill = w14 + "textFill";
        public static readonly XName textId = w14 + "textId";
        public static readonly XName textOutline = w14 + "textOutline";
        public static readonly XName tint = w14 + "tint";
        public static readonly XName uncheckedState = w14 + "uncheckedState";
        public static readonly XName val = w14 + "val";
        public static readonly XName w = w14 + "w";
        public static readonly XName wProps3d = w14 + "wProps3d";
        public static readonly XName wScene3d = w14 + "wScene3d";
        public static readonly XName wShadow = w14 + "wShadow";
        public static readonly XName wTextFill = w14 + "wTextFill";
        public static readonly XName wTextOutline = w14 + "wTextOutline";
        public static readonly XName xfrm = w14 + "xfrm";
    }
}
