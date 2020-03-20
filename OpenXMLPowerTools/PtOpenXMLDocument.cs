﻿using DocumentFormat.OpenXml.Packaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;

namespace OpenXMLPowerTools
{

    public class PowerToolsDocumentException : Exception
    {
        public PowerToolsDocumentException(string message) : base(message) { }
    }
    public class PowerToolsInvalidDataException : Exception
    {
        public PowerToolsInvalidDataException(string message) : base(message) { }
    }

    public class OpenXmlPowerToolsDocument
    {
        public string FileName { get; set; }
        public byte[] DocumentByteArray { get; set; }

        //public static OpenXmlPowerToolsDocument FromFileName(string fileName)
        //{
        //    byte[] bytes = File.ReadAllBytes(fileName);
        //    Type type;
        //    try
        //    {
        //        type = GetDocumentType(bytes);
        //    }
        //    catch (FileFormatException)
        //    {
        //        throw new PowerToolsDocumentException("Not an Open XML document.");
        //    }
        //    if (type == typeof(WordprocessingDocument))
        //        return new WmlDocument(fileName, bytes);
        //    if (type == typeof(SpreadsheetDocument))
        //        return new SmlDocument(fileName, bytes);
        //    if (type == typeof(PresentationDocument))
        //        return new PmlDocument(fileName, bytes);
        //    if (type == typeof(Package))
        //    {
        //        OpenXmlPowerToolsDocument pkg = new OpenXmlPowerToolsDocument(bytes);
        //        pkg.FileName = fileName;
        //        return pkg;
        //    }
        //    throw new PowerToolsDocumentException("Not an Open XML document.");
        //}

        //public static OpenXmlPowerToolsDocument FromDocument(OpenXmlPowerToolsDocument doc)
        //{
        //    Type type = doc.GetDocumentType();
        //    if (type == typeof(WordprocessingDocument))
        //        return new WmlDocument(doc);
        //    if (type == typeof(SpreadsheetDocument))
        //        return new SmlDocument(doc);
        //    if (type == typeof(PresentationDocument))
        //        return new PmlDocument(doc);
        //    return null;    // This should not be possible from a valid OpenXmlPowerToolsDocument object
        //}

        public OpenXmlPowerToolsDocument(OpenXmlPowerToolsDocument original)
        {
            DocumentByteArray = new byte[original.DocumentByteArray.Length];
            Array.Copy(original.DocumentByteArray, DocumentByteArray, original.DocumentByteArray.Length);
            FileName = original.FileName;
        }

        public OpenXmlPowerToolsDocument(OpenXmlPowerToolsDocument original, bool convertToTransitional)
        {
            if (convertToTransitional)
            {
                ConvertToTransitional(original.FileName, original.DocumentByteArray);
            }
            else
            {
                DocumentByteArray = new byte[original.DocumentByteArray.Length];
                Array.Copy(original.DocumentByteArray, DocumentByteArray, original.DocumentByteArray.Length);
                FileName = original.FileName;
            }
        }

        public OpenXmlPowerToolsDocument(string fileName)
        {
            this.FileName = fileName;
            DocumentByteArray = File.ReadAllBytes(fileName);
        }

        public OpenXmlPowerToolsDocument(string fileName, bool convertToTransitional)
        {
            this.FileName = fileName;

            if (convertToTransitional)
            {
                var tempByteArray = File.ReadAllBytes(fileName);
                ConvertToTransitional(fileName, tempByteArray);
            }
            else
            {
                this.FileName = fileName;
                DocumentByteArray = File.ReadAllBytes(fileName);
            }
        }

        private void ConvertToTransitional(string fileName, byte[] tempByteArray)
        {
            Type type;
            try
            {
                type = GetDocumentType(tempByteArray);
            }
            catch (FileFormatException)
            {
                throw new PowerToolsDocumentException("Not an Open XML document.");
            }

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(tempByteArray, 0, tempByteArray.Length);
                if (type == typeof(WordprocessingDocument))
                {
                    using (WordprocessingDocument sDoc = WordprocessingDocument.Open(ms, true))
                    {
                        // following code forces the SDK to serialize
                        foreach (var part in sDoc.Parts)
                        {
                            try
                            {
                                var z = part.OpenXmlPart.RootElement;
                            }
                            catch (Exception)
                            {
                                continue;
                            }
                        }
                    }
                }
                else if (type == typeof(SpreadsheetDocument))
                {
                    using (SpreadsheetDocument sDoc = SpreadsheetDocument.Open(ms, true))
                    {
                        // following code forces the SDK to serialize
                        foreach (var part in sDoc.Parts)
                        {
                            try
                            {
                                var z = part.OpenXmlPart.RootElement;
                            }
                            catch (Exception)
                            {
                                continue;
                            }
                        }
                    }
                }
                else if (type == typeof(PresentationDocument))
                {
                    using (PresentationDocument sDoc = PresentationDocument.Open(ms, true))
                    {
                        // following code forces the SDK to serialize
                        foreach (var part in sDoc.Parts)
                        {
                            try
                            {
                                var z = part.OpenXmlPart.RootElement;
                            }
                            catch (Exception)
                            {
                                continue;
                            }
                        }
                    }
                }
                this.FileName = fileName;
                DocumentByteArray = ms.ToArray();
            }
        }

        public OpenXmlPowerToolsDocument(byte[] byteArray)
        {
            DocumentByteArray = new byte[byteArray.Length];
            Array.Copy(byteArray, DocumentByteArray, byteArray.Length);
            this.FileName = null;
        }

        public OpenXmlPowerToolsDocument(byte[] byteArray, bool convertToTransitional)
        {
            if (convertToTransitional)
            {
                ConvertToTransitional(null, byteArray);
            }
            else
            {
                DocumentByteArray = new byte[byteArray.Length];
                Array.Copy(byteArray, DocumentByteArray, byteArray.Length);
                this.FileName = null;
            }
        }

        public OpenXmlPowerToolsDocument(string fileName, MemoryStream memStream)
        {
            FileName = fileName;
            DocumentByteArray = new byte[memStream.Length];
            Array.Copy(memStream.GetBuffer(), DocumentByteArray, memStream.Length);
        }

        public OpenXmlPowerToolsDocument(string fileName, MemoryStream memStream, bool convertToTransitional)
        {
            if (convertToTransitional)
            {
                ConvertToTransitional(fileName, memStream.ToArray());
            }
            else
            {
                FileName = fileName;
                DocumentByteArray = new byte[memStream.Length];
                Array.Copy(memStream.GetBuffer(), DocumentByteArray, memStream.Length);
            }
        }

        public string GetName()
        {
            if (FileName == null)
                return "Unnamed Document";
            FileInfo file = new FileInfo(FileName);
            return file.Name;
        }

        public void SaveAs(string fileName)
        {
            File.WriteAllBytes(fileName, DocumentByteArray);
        }

        public void Save()
        {
            if (this.FileName == null)
                throw new InvalidOperationException("Attempting to Save a document that has no file name.  Use SaveAs instead.");
            File.WriteAllBytes(this.FileName, DocumentByteArray);
        }

        public void WriteByteArray(Stream stream)
        {
            stream.Write(DocumentByteArray, 0, DocumentByteArray.Length);
        }

        public Type GetDocumentType()
        {
            return GetDocumentType(DocumentByteArray);
        }

        private static Type GetDocumentType(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                stream.Write(bytes, 0, bytes.Length);
                using (Package package = Package.Open(stream, FileMode.Open))
                {
                    PackageRelationship relationship = package.GetRelationshipsByType("http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument").FirstOrDefault();
                    if (relationship == null)
                        relationship = package.GetRelationshipsByType("http://purl.oclc.org/ooxml/officeDocument/relationships/officeDocument").FirstOrDefault();
                    if (relationship != null)
                    {
                        PackagePart part = package.GetPart(PackUriHelper.ResolvePartUri(relationship.SourceUri, relationship.TargetUri));
                        switch (part.ContentType)
                        {
                            case "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml":
                            case "application/vnd.ms-word.document.macroEnabled.main+xml":
                            case "application/vnd.ms-word.template.macroEnabledTemplate.main+xml":
                            case "application/vnd.openxmlformats-officedocument.wordprocessingml.template.main+xml":
                                return typeof(WordprocessingDocument);
                            case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml":
                            case "application/vnd.ms-excel.sheet.macroEnabled.main+xml":
                            case "application/vnd.ms-excel.template.macroEnabled.main+xml":
                            case "application/vnd.openxmlformats-officedocument.spreadsheetml.template.main+xml":
                                return typeof(SpreadsheetDocument);
                            case "application/vnd.openxmlformats-officedocument.presentationml.template.main+xml":
                            case "application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml":
                            case "application/vnd.ms-powerpoint.template.macroEnabled.main+xml":
                            case "application/vnd.ms-powerpoint.addin.macroEnabled.main+xml":
                            case "application/vnd.openxmlformats-officedocument.presentationml.slideshow.main+xml":
                            case "application/vnd.ms-powerpoint.presentation.macroEnabled.main+xml":
                                return typeof(PresentationDocument);
                        }
                        return typeof(Package);
                    }
                    return null;
                }
            }
        }

        public static void SavePartAs(OpenXmlPart part, string filePath)
        {
            Stream partStream = part.GetStream(FileMode.Open, FileAccess.Read);
            byte[] partContent = new byte[partStream.Length];
            partStream.Read(partContent, 0, (int)partStream.Length);

            File.WriteAllBytes(filePath, partContent);
        }
    }

    public partial class WmlDocument : OpenXmlPowerToolsDocument
    {
        public WmlDocument(OpenXmlPowerToolsDocument original)
            : base(original)
        {
            if (GetDocumentType() != typeof(WordprocessingDocument))
                throw new PowerToolsDocumentException("Not a Wordprocessing document.");
        }

        public WmlDocument(OpenXmlPowerToolsDocument original, bool convertToTransitional)
            : base(original, convertToTransitional)
        {
            if (GetDocumentType() != typeof(WordprocessingDocument))
                throw new PowerToolsDocumentException("Not a Wordprocessing document.");
        }

        public WmlDocument(string fileName)
            : base(fileName)
        {
            if (GetDocumentType() != typeof(WordprocessingDocument))
                throw new PowerToolsDocumentException("Not a Wordprocessing document.");
        }

        public WmlDocument(string fileName, bool convertToTransitional)
            : base(fileName, convertToTransitional)
        {
            if (GetDocumentType() != typeof(WordprocessingDocument))
                throw new PowerToolsDocumentException("Not a Wordprocessing document.");
        }

        public WmlDocument(string fileName, byte[] byteArray)
            : base(byteArray)
        {
            FileName = fileName;
            if (GetDocumentType() != typeof(WordprocessingDocument))
                throw new PowerToolsDocumentException("Not a Wordprocessing document.");
        }

        public WmlDocument(string fileName, byte[] byteArray, bool convertToTransitional)
            : base(byteArray, convertToTransitional)
        {
            FileName = fileName;
            if (GetDocumentType() != typeof(WordprocessingDocument))
                throw new PowerToolsDocumentException("Not a Wordprocessing document.");
        }

        public WmlDocument(string fileName, MemoryStream memStream)
            : base(fileName, memStream)
        {
        }

        public WmlDocument(string fileName, MemoryStream memStream, bool convertToTransitional)
            : base(fileName, memStream, convertToTransitional)
        {
        }
    }

}
