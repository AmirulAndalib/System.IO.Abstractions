﻿namespace System.IO.Abstractions.TestingHelpers.Tests;

using Collections.Generic;

using NUnit.Framework;

using Text;

using XFS = MockUnixSupport;

using System.Threading.Tasks;
using System.Threading;

public class MockFileWriteAllTextTests
{
    [Test]
    public async Task MockFile_WriteAllText_ShouldWriteTextFileToMemoryFileSystem()
    {
        // Arrange
        string path = XFS.Path(@"c:\something\demo.txt");
        string fileContent = "Hello there!";
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(XFS.Path(@"c:\something"));

        // Act
        fileSystem.File.WriteAllText(path, fileContent);

        // Assert
        await That(fileSystem.GetFile(path).TextContents).IsEqualTo(fileContent);
    }

    [Test]
    public async Task MockFile_WriteAllText_ShouldOverwriteAnExistingFile()
    {
        // http://msdn.microsoft.com/en-us/library/ms143375.aspx

        // Arrange
        string path = XFS.Path(@"c:\something\demo.txt");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(XFS.Path(@"c:\something"));

        // Act
        fileSystem.File.WriteAllText(path, "foo");
        fileSystem.File.WriteAllText(path, "bar");

        // Assert
        await That(fileSystem.GetFile(path).TextContents).IsEqualTo("bar");
    }

    [Test]
    public async Task MockFile_WriteAllText_ShouldThrowAnUnauthorizedAccessExceptionIfFileIsHidden()
    {
        // Arrange
        string path = XFS.Path(@"c:\something\demo.txt");
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { path, new MockFileData("this is hidden") },
        });
        fileSystem.File.SetAttributes(path, FileAttributes.Hidden);

        // Act
        Action action = () => fileSystem.File.WriteAllText(path, "hello world");

        // Assert
        await That(action).Throws<UnauthorizedAccessException>()
            .Because($"Access to the path '{path}' is denied.");
    }

    [Test]
    public async Task MockFile_WriteAllText_ShouldThrowAnArgumentExceptionIfThePathIsEmpty()
    {
        // Arrange
        var fileSystem = new MockFileSystem();

        // Act
        Action action = () => fileSystem.File.WriteAllText(string.Empty, "hello world");

        // Assert
        await That(action).Throws<ArgumentException>();
    }

    [Test]
    public async Task MockFile_WriteAllText_ShouldNotThrowAnArgumentNullExceptionIfTheContentIsNull()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        string directoryPath = XFS.Path(@"c:\something");
        string filePath = XFS.Path(@"c:\something\demo.txt");
        fileSystem.AddDirectory(directoryPath);

        // Act
        fileSystem.File.WriteAllText(filePath, null);

        // Assert
        // no exception should be thrown, also the documentation says so
        var data = fileSystem.GetFile(filePath);
        await That(data.Contents).IsEmpty();
    }

    [Test]
    public async Task MockFile_WriteAllText_ShouldThrowAnUnauthorizedAccessExceptionIfTheFileIsReadOnly()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        string filePath = XFS.Path(@"c:\something\demo.txt");
        var mockFileData = new MockFileData(new byte[0]);
        mockFileData.Attributes = FileAttributes.ReadOnly;
        fileSystem.AddFile(filePath, mockFileData);

        // Act
        Action action = () => fileSystem.File.WriteAllText(filePath, null);

        // Assert
        await That(action).Throws<UnauthorizedAccessException>();
    }

    [Test]
    public async Task MockFile_WriteAllText_ShouldThrowAnUnauthorizedAccessExceptionIfThePathIsOneDirectory()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        string directoryPath = XFS.Path(@"c:\something");
        fileSystem.AddDirectory(directoryPath);

        // Act
        Action action = () => fileSystem.File.WriteAllText(directoryPath, null);

        // Assert
        await That(action).Throws<UnauthorizedAccessException>();
    }

    [Test]
    public async Task MockFile_WriteAllText_ShouldThrowDirectoryNotFoundExceptionIfPathDoesNotExists()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        string path = XFS.Path(@"c:\something\file.txt");

        // Act
        Action action = () => fileSystem.File.WriteAllText(path, string.Empty);

        // Assert
        await That(action).Throws<DirectoryNotFoundException>();
    }

    public static IEnumerable<KeyValuePair<Encoding, byte[]>> GetEncodingsWithExpectedBytes()
    {
        Encoding utf8WithoutBom = new UTF8Encoding(false, true);
        return new Dictionary<Encoding, byte[]>
        {
            // ASCII does not need a BOM
            { Encoding.ASCII, new byte[] { 72, 101, 108, 108, 111, 32, 116,
                104, 101, 114, 101, 33, 32, 68, 122, 105, 63, 107, 105, 46 } },

            // BigEndianUnicode needs a BOM, the BOM is the first two bytes
            { Encoding.BigEndianUnicode, new byte [] { 254, 255, 0, 72, 0, 101,
                0, 108, 0, 108, 0, 111, 0, 32, 0, 116, 0, 104, 0, 101, 0, 114,
                0, 101, 0, 33, 0, 32, 0, 68, 0, 122, 0, 105, 1, 25, 0, 107, 0, 105, 0, 46 } },

            // UTF-32 needs a BOM, the BOM is the first four bytes
            { Encoding.UTF32, new byte [] {255, 254, 0, 0, 72, 0, 0, 0, 101,
                0, 0, 0, 108, 0, 0, 0, 108, 0, 0, 0, 111, 0, 0, 0, 32, 0, 0,
                0, 116, 0, 0, 0, 104, 0, 0, 0, 101, 0, 0, 0, 114, 0, 0, 0,
                101, 0, 0, 0, 33, 0, 0, 0, 32, 0, 0, 0, 68, 0, 0, 0, 122, 0,
                0, 0, 105, 0, 0, 0, 25, 1, 0, 0, 107, 0, 0, 0, 105, 0, 0, 0, 46, 0, 0, 0 } },

#pragma warning disable SYSLIB0001
            // UTF-7 does not need a BOM
            { Encoding.UTF7, new byte [] {72, 101, 108, 108, 111, 32, 116,
                104, 101, 114, 101, 43, 65, 67, 69, 45, 32, 68, 122, 105,
                43, 65, 82, 107, 45, 107, 105, 46 } },
#pragma warning restore SYSLIB0001

            // The default encoding does not need a BOM
            { utf8WithoutBom, new byte [] { 72, 101, 108, 108, 111, 32, 116,
                104, 101, 114, 101, 33, 32, 68, 122, 105, 196, 153, 107, 105, 46 } },

            // Unicode needs a BOM, the BOM is the first two bytes
            { Encoding.Unicode, new byte [] { 255, 254, 72, 0, 101, 0, 108,
                0, 108, 0, 111, 0, 32, 0, 116, 0, 104, 0, 101, 0, 114, 0,
                101, 0, 33, 0, 32, 0, 68, 0, 122, 0, 105, 0, 25, 1, 107, 0,
                105, 0, 46, 0 } }
        };
    }

    [TestCaseSource(typeof(MockFileWriteAllTextTests), nameof(GetEncodingsWithExpectedBytes))]
    public async Task MockFile_WriteAllText_Encoding_ShouldWriteTextFileToMemoryFileSystem(KeyValuePair<Encoding, byte[]> encodingsWithContents)
    {
        // Arrange
        const string FileContent = "Hello there! Dzięki.";
        string path = XFS.Path(@"c:\something\demo.txt");
        byte[] expectedBytes = encodingsWithContents.Value;
        Encoding encoding = encodingsWithContents.Key;
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(XFS.Path(@"c:\something"));

        // Act
        fileSystem.File.WriteAllText(path, FileContent, encoding);

        // Assert
        var actualBytes = fileSystem.GetFile(path).Contents;
        await That(actualBytes).IsEqualTo(expectedBytes);
    }

    [Test]
    public async Task MockFile_WriteAllTextMultipleLines_ShouldWriteTextFileToMemoryFileSystem()
    {
        // Arrange
        string path = XFS.Path(@"c:\something\demo.txt");

        var fileContent = new List<string> { "Hello there!", "Second line!" };
        var expected = "Hello there!" + Environment.NewLine + "Second line!" + Environment.NewLine;

        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(XFS.Path(@"c:\something"));

        // Act
        fileSystem.File.WriteAllLines(path, fileContent);

        // Assert
        await That(fileSystem.GetFile(path).TextContents).IsEqualTo(expected);
    }

#if FEATURE_ASYNC_FILE
    [Test]
    public async Task MockFile_WriteAllTextAsync_ShouldWriteTextFileToMemoryFileSystem()
    {
        // Arrange
        string path = XFS.Path(@"c:\something\demo.txt");
        string fileContent = "Hello there!";
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(XFS.Path(@"c:\something"));

        // Act
        await fileSystem.File.WriteAllTextAsync(path, fileContent);

        // Assert
        await That(fileSystem.GetFile(path).TextContents).IsEqualTo(fileContent);
    }

    [Test]
    public async Task MockFile_WriteAllTextAsync_ShouldThrowOperationCanceledExceptionIfCancelled()
    {
        // Arrange
        const string path = "test.txt";
        var fileSystem = new MockFileSystem();

        // Act
        async Task Act() =>
            await fileSystem.File.WriteAllTextAsync(
                path,
                "line",
                new CancellationToken(canceled: true));
        await That(Act).Throws<OperationCanceledException>();

        // Assert
        await That(fileSystem.File.Exists(path)).IsFalse();
    }

    [Test]
    public async Task MockFile_WriteAllTextAsync_ShouldOverriteAnExistingFile()
    {
        // http://msdn.microsoft.com/en-us/library/ms143375.aspx

        // Arrange
        string path = XFS.Path(@"c:\something\demo.txt");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(XFS.Path(@"c:\something"));

        // Act
        await fileSystem.File.WriteAllTextAsync(path, "foo");
        await fileSystem.File.WriteAllTextAsync(path, "bar");

        // Assert
        await That(fileSystem.GetFile(path).TextContents).IsEqualTo("bar");
    }

    [Test]
    public async Task MockFile_WriteAllTextAsync_ShouldThrowAnUnauthorizedAccessExceptionIfFileIsHidden()
    {
        // Arrange
        string path = XFS.Path(@"c:\something\demo.txt");
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { path, new MockFileData("this is hidden") },
        });
        fileSystem.File.SetAttributes(path, FileAttributes.Hidden);

        // Act
        Func<Task> action = () => fileSystem.File.WriteAllTextAsync(path, "hello world");

        // Assert
        await That(action).Throws<UnauthorizedAccessException>()
            .Because($"Access to the path '{path}' is denied.");
    }

    [Test]
    public async Task MockFile_WriteAllTextAsync_ShouldThrowAnArgumentExceptionIfThePathIsEmpty()
    {
        // Arrange
        var fileSystem = new MockFileSystem();

        // Act
        Func<Task> action = () => fileSystem.File.WriteAllTextAsync(string.Empty, "hello world");

        // Assert
        await That(action).Throws<ArgumentException>();
    }

    [Test]
    public async Task MockFile_WriteAllTextAsync_ShouldNotThrowAnArgumentNullExceptionIfTheContentIsNull()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        string directoryPath = XFS.Path(@"c:\something");
        string filePath = XFS.Path(@"c:\something\demo.txt");
        fileSystem.AddDirectory(directoryPath);

        // Act
        await fileSystem.File.WriteAllTextAsync(filePath, "");

        // Assert
        // no exception should be thrown, also the documentation says so
        var data = fileSystem.GetFile(filePath);
        await That(data.Contents).IsEmpty();
    }

    [Test]
    public async Task MockFile_WriteAllTextAsync_ShouldThrowAnUnauthorizedAccessExceptionIfTheFileIsReadOnly()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        string filePath = XFS.Path(@"c:\something\demo.txt");
        var mockFileData = new MockFileData(new byte[0]);
        mockFileData.Attributes = FileAttributes.ReadOnly;
        fileSystem.AddFile(filePath, mockFileData);

        // Act
        Func<Task> action = () => fileSystem.File.WriteAllTextAsync(filePath, "");

        // Assert
        await That(action).Throws<UnauthorizedAccessException>();
    }

    [Test]
    public async Task MockFile_WriteAllTextAsync_ShouldThrowAnUnauthorizedAccessExceptionIfThePathIsOneDirectory()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        string directoryPath = XFS.Path(@"c:\something");
        fileSystem.AddDirectory(directoryPath);

        // Act
        Func<Task> action = () => fileSystem.File.WriteAllTextAsync(directoryPath, "");

        // Assert
        await That(action).Throws<UnauthorizedAccessException>();
    }

    [Test]
    public async Task MockFile_WriteAllTextAsync_ShouldThrowDirectoryNotFoundExceptionIfPathDoesNotExists()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        string path = XFS.Path(@"c:\something\file.txt");

        // Act
        Func<Task> action = () => fileSystem.File.WriteAllTextAsync(path, string.Empty);

        // Assert
        await That(action).Throws<DirectoryNotFoundException>();
    }

    [TestCaseSource(typeof(MockFileWriteAllTextTests), nameof(GetEncodingsWithExpectedBytes))]
    public async Task MockFile_WriteAllTextAsync_Encoding_ShouldWriteTextFileToMemoryFileSystem(KeyValuePair<Encoding, byte[]> encodingsWithContents)
    {
        // Arrange
        const string FileContent = "Hello there! Dzięki.";
        string path = XFS.Path(@"c:\something\demo.txt");
        byte[] expectedBytes = encodingsWithContents.Value;
        Encoding encoding = encodingsWithContents.Key;
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(XFS.Path(@"c:\something"));

        // Act
        await fileSystem.File.WriteAllTextAsync(path, FileContent, encoding);

        // Assert
        var actualBytes = fileSystem.GetFile(path).Contents;
        await That(actualBytes).IsEqualTo(expectedBytes);
    }

    [Test]
    public async Task MockFile_WriteAllTextAsyncMultipleLines_ShouldWriteTextFileToMemoryFileSystem()
    {
        // Arrange
        string path = XFS.Path(@"c:\something\demo.txt");

        var fileContent = new List<string> { "Hello there!", "Second line!" };
        var expected = "Hello there!" + Environment.NewLine + "Second line!" + Environment.NewLine;

        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(XFS.Path(@"c:\something"));

        // Act
        await fileSystem.File.WriteAllLinesAsync(path, fileContent);

        // Assert
        await That(fileSystem.GetFile(path).TextContents).IsEqualTo(expected);
    }
#endif
}