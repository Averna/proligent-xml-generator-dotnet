using System.Xml.Linq;
using Xunit;

namespace Proligent.XmlGenerator.Tests;

public class DocumentCopyTests
{
    [Fact]
    public void SaveXml_CopiesAndRenames_ProductUnitDocument()
    {
        string tempSrc = Path.Combine(
            Path.GetTempPath(),
            "ProligentTests",
            Guid.NewGuid().ToString("N")
        );
        string tempDest = Path.Combine(
            Path.GetTempPath(),
            "ProligentTestsDest",
            Guid.NewGuid().ToString("N")
        );

        Directory.CreateDirectory(tempSrc);
        Directory.CreateDirectory(tempDest);

        string originalFileName = "product_document.txt";
        string sourcePath = Path.Combine(tempSrc, originalFileName);
        File.WriteAllText(sourcePath, "product-content");

        try
        {
            var product = new ProductUnit(
                productUnitIdentifier: "Test",
                productFullName: "Product/Test",
                manufacturer: "Tester",
                documents: new[] { new Document(sourcePath) }
            );

            var warehouse = new DataWareHouse(productUnit: product);
            string savedXml = warehouse.SaveXml(destinationFolder: tempDest);

            Assert.True(File.Exists(savedXml), "Saved XML should exist.");

            var copiedFiles = Directory
                .GetFiles(tempDest)
                .Select(Path.GetFileName)
                .Where(n =>
                    !string.IsNullOrWhiteSpace(n)
                    && n.StartsWith("Document_", StringComparison.OrdinalIgnoreCase)
                    && n.EndsWith(originalFileName, StringComparison.OrdinalIgnoreCase)
                )
                .Select(n => n!)
                .ToList();

            Assert.Single(copiedFiles);

            string copiedFileName = copiedFiles.Single();
            string copiedPath = Path.Combine(tempDest, copiedFileName);
            Assert.True(
                File.Exists(copiedPath),
                "Copied document should exist in destination folder."
            );
            Assert.Equal("product-content", File.ReadAllText(copiedPath));

            var doc = XDocument.Load(savedXml);
            var fileAttr = doc.Descendants(XmlNamespaces.Dw + "Document")
                .First()
                .Attribute("FileName");
            Assert.NotNull(fileAttr);
            Assert.Equal(copiedFileName, fileAttr!.Value);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempSrc))
                    Directory.Delete(tempSrc, recursive: true);
            }
            catch { }
            try
            {
                if (Directory.Exists(tempDest))
                    Directory.Delete(tempDest, recursive: true);
            }
            catch { }
        }
    }

    [Fact]
    public void SaveXml_CopiesAndRenames_MultipleNestedDocuments()
    {
        string tempSrc = Path.Combine(
            Path.GetTempPath(),
            "ProligentTests",
            Guid.NewGuid().ToString("N")
        );
        string tempDest = Path.Combine(
            Path.GetTempPath(),
            "ProligentTestsDest",
            Guid.NewGuid().ToString("N")
        );

        Directory.CreateDirectory(tempSrc);
        Directory.CreateDirectory(tempDest);

        string stepFile = Path.Combine(tempSrc, "step_document.txt");
        string seqFile = Path.Combine(tempSrc, "sequence_document.txt");
        File.WriteAllText(stepFile, "step-content");
        File.WriteAllText(seqFile, "sequence-content");

        try
        {
            var step = new StepRun(name: "StepA", status: ExecutionStatusKind.PASS);
            step.AddDocument(new Document(stepFile));

            var sequence = new SequenceRun(name: "SeqA", status: ExecutionStatusKind.PASS);
            sequence.AddStepRun(step);
            sequence.AddDocument(new Document(seqFile));

            var operation = new OperationRun(
                station: "Station/Test",
                sequences: new[] { sequence },
                name: "OpA",
                status: ExecutionStatusKind.PASS
            );
            var process = new ProcessRun(
                operations: new[] { operation },
                name: "ProcA",
                status: ExecutionStatusKind.PASS
            );

            var warehouse = new DataWareHouse(topProcess: process);
            string savedXml = warehouse.SaveXml(destinationFolder: tempDest);

            Assert.True(File.Exists(savedXml), "Saved XML should exist.");

            var copiedFiles = Directory
                .GetFiles(tempDest)
                .Select(Path.GetFileName)
                .Where(n =>
                    !string.IsNullOrWhiteSpace(n)
                    && n.StartsWith("Document_", StringComparison.OrdinalIgnoreCase)
                )
                .Select(n => n!)
                .ToList();

            // Expect two copied documents
            Assert.Equal(2, copiedFiles.Count);

            var doc = XDocument.Load(savedXml);
            var fileAttrs = doc.Descendants(XmlNamespaces.Dw + "Document")
                .Select(e => e.Attribute("FileName")?.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();

            Assert.Equal(2, fileAttrs.Count);

            // Each attribute value must match one of the copied file names and the file content must match original
            foreach (var attr in fileAttrs)
            {
                Assert.Contains(attr, copiedFiles);
                if (attr != null)
                {
                    string fullCopied = Path.Combine(tempDest, attr);
                    Assert.True(File.Exists(fullCopied), $"Copied file {attr} should exist.");
                    if (attr.EndsWith("step_document.txt", StringComparison.OrdinalIgnoreCase))
                    {
                        Assert.Equal("step-content", File.ReadAllText(fullCopied));
                    }
                    else if (
                        attr.EndsWith("sequence_document.txt", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        Assert.Equal("sequence-content", File.ReadAllText(fullCopied));
                    }
                    else
                    {
                        Assert.Fail($"Unexpected copied file name: {attr}");
                    }
                }
            }
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempSrc))
                    Directory.Delete(tempSrc, recursive: true);
            }
            catch { }
            try
            {
                if (Directory.Exists(tempDest))
                    Directory.Delete(tempDest, recursive: true);
            }
            catch { }
        }
    }

    // Negative scenarios

    [Fact]
    public void SaveXml_MissingSourceFile_ThrowsFileNotFoundException()
    {
        string tempSrc = Path.Combine(
            Path.GetTempPath(),
            "ProligentTests",
            Guid.NewGuid().ToString("N")
        );
        string tempDest = Path.Combine(
            Path.GetTempPath(),
            "ProligentTestsDest",
            Guid.NewGuid().ToString("N")
        );

        Directory.CreateDirectory(tempSrc);
        Directory.CreateDirectory(tempDest);

        string originalFileName = "missing_document.txt";
        string sourcePath = Path.Combine(tempSrc, originalFileName);
        // do not create the source file

        try
        {
            var product = new ProductUnit(
                productUnitIdentifier: "TestMissing",
                productFullName: "Product/Test",
                manufacturer: "Tester",
                documents: new[] { new Document(sourcePath) }
            );

            var warehouse = new DataWareHouse(productUnit: product);

            Assert.Throws<FileNotFoundException>(() =>
                warehouse.SaveXml(destinationFolder: tempDest)
            );
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempSrc))
                    Directory.Delete(tempSrc, recursive: true);
            }
            catch { }
            try
            {
                if (Directory.Exists(tempDest))
                    Directory.Delete(tempDest, recursive: true);
            }
            catch { }
        }
    }

    [Fact]
    public void SaveXml_NoDocumentElements_Succeeds_NoCopiesMade()
    {
        string tempDest = Path.Combine(
            Path.GetTempPath(),
            "ProligentTestsDest",
            Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(tempDest);

        try
        {
            // Create a warehouse with no documents anywhere
            var warehouse = new DataWareHouse();
            string savedXml = warehouse.SaveXml(destinationFolder: tempDest);

            Assert.True(File.Exists(savedXml), "Saved XML should exist.");

            var copiedFiles = Directory
                .GetFiles(tempDest)
                .Select(Path.GetFileName)
                .Where(n =>
                    !string.IsNullOrWhiteSpace(n)
                    && n.StartsWith("Document_", StringComparison.OrdinalIgnoreCase)
                )
                .Select(n => n!)
                .ToList();

            Assert.Empty(copiedFiles);

            var doc = XDocument.Load(savedXml);
            var fileElems = doc.Descendants(XmlNamespaces.Dw + "Document").ToList();
            Assert.Empty(fileElems);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDest))
                    Directory.Delete(tempDest, recursive: true);
            }
            catch { }
        }
    }

    [Fact]
    public void SaveXml_DestinationFolderIsAFile_ThrowsIOException()
    {
        string tempParent = Path.Combine(
            Path.GetTempPath(),
            "ProligentTests",
            Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(tempParent);

        string fileAsFolder = Path.Combine(tempParent, "marker.txt");
        File.WriteAllText(fileAsFolder, "marker");

        try
        {
            var product = new ProductUnit(
                productUnitIdentifier: "Test",
                productFullName: "Product/Test",
                manufacturer: "Tester",
                documents: Array.Empty<Document>()
            );

            var warehouse = new DataWareHouse(productUnit: product);

            // If SaveXml attempts to create a directory with the same name as an existing file,
            // Directory.CreateDirectory will throw an IOException.
            Assert.ThrowsAny<IOException>(() => warehouse.SaveXml(destinationFolder: fileAsFolder));
        }
        finally
        {
            try
            {
                if (File.Exists(fileAsFolder))
                    File.Delete(fileAsFolder);
            }
            catch { }
            try
            {
                if (Directory.Exists(tempParent))
                    Directory.Delete(tempParent, recursive: true);
            }
            catch { }
        }
    }

    [Fact]
    public void SaveXml_MixedExistingAndMissingDocuments_ThrowsFileNotFoundException()
    {
        string tempSrc = Path.Combine(
            Path.GetTempPath(),
            "ProligentTests",
            Guid.NewGuid().ToString("N")
        );
        string tempDest = Path.Combine(
            Path.GetTempPath(),
            "ProligentTestsDest",
            Guid.NewGuid().ToString("N")
        );

        Directory.CreateDirectory(tempSrc);
        Directory.CreateDirectory(tempDest);

        string existingFile = Path.Combine(tempSrc, "exists.txt");
        string missingFile = Path.Combine(tempSrc, "does_not_exist.txt");
        File.WriteAllText(existingFile, "exists-content");
        // missingFile not created

        try
        {
            var step = new StepRun(name: "StepMixed", status: ExecutionStatusKind.PASS);
            step.AddDocument(new Document(existingFile));
            step.AddDocument(new Document(missingFile));

            var sequence = new SequenceRun(
                name: "SeqMixed",
                status: ExecutionStatusKind.PASS,
                steps: new[] { step }
            );
            var operation = new OperationRun(
                station: "Station/Mix",
                sequences: new[] { sequence },
                name: "OpMix",
                status: ExecutionStatusKind.PASS
            );
            var process = new ProcessRun(
                operations: new[] { operation },
                name: "ProcMix",
                status: ExecutionStatusKind.PASS
            );

            var warehouse = new DataWareHouse(topProcess: process);

            Assert.Throws<FileNotFoundException>(() =>
                warehouse.SaveXml(destinationFolder: tempDest)
            );
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempSrc))
                    Directory.Delete(tempSrc, recursive: true);
            }
            catch { }
            try
            {
                if (Directory.Exists(tempDest))
                    Directory.Delete(tempDest, recursive: true);
            }
            catch { }
        }
    }
}
