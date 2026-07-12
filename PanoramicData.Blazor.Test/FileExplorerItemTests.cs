using PanoramicData.Blazor.Models;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>Tests for the FileExplorerItem class.</summary>
public class FileExplorerItemTests
{
    /// <summary>Verifies that ParentPath returns the correct parent directory path for various path formats.</summary>
    [Theory]
    [InlineData("/", "")]
    [InlineData("/abc.txt", "/")]
    [InlineData("/folder1/abc.txt", "/folder1")]
    [InlineData("/folder1/folder2/abc.txt", "/folder1/folder2")]
    public void WhenGettingParentPathThenReturnsCorrectValue(string path, string expectedParent)
    {
        var item = new FileExplorerItem { Path = path };

        item.ParentPath.ShouldBe(expectedParent);
    }

    /// <summary>Verifies that Rename updates both the Path and Name properties of the item.</summary>
    [Fact]
    public void WhenRenamingThenPathAndNameAreUpdated()
    {
        var item = new FileExplorerItem { Path = "/folder1/old.txt", Name = "old.txt" };

        item.Rename("new.txt");

        item.Path.ShouldBe("/folder1/new.txt");
        item.Name.ShouldBe("new.txt");
    }

    /// <summary>Verifies that renaming the root item leaves the path unchanged.</summary>
    [Fact]
    public void WhenRenamingRootThenNothingChanges()
    {
        var item = new FileExplorerItem { Path = "/", Name = "" };

        item.Rename("anything");

        item.Path.ShouldBe("/");
    }

    /// <summary>Verifies that FileExtension returns the file extension without a leading dot.</summary>
    [Theory]
    [InlineData("/file.txt", "txt")]
    [InlineData("/file.tar.gz", "gz")]
    [InlineData("/noext", "")]
    [InlineData("/", "")]
    public void WhenGettingFileExtensionThenReturnsWithoutDot(string path, string expectedExt)
    {
        var item = new FileExplorerItem { Path = path };

        item.FileExtension.ShouldBe(expectedExt);
    }

    /// <summary>Verifies that a read-only item reports CanAddItems and CanRemoveItems as false.</summary>
    [Fact]
    public void WhenReadOnlyThenCanAddItemsIsFalse()
    {
        var item = new FileExplorerItem { IsReadOnly = true };

        item.CanAddItems.ShouldBeFalse();
        item.CanRemoveItems.ShouldBeFalse();
    }

    /// <summary>Verifies that a non-read-only item reports CanAddItems and CanRemoveItems as true.</summary>
    [Fact]
    public void WhenNotReadOnlyThenCanAddItemsIsTrue()
    {
        var item = new FileExplorerItem { IsReadOnly = false };

        item.CanAddItems.ShouldBeTrue();
        item.CanRemoveItems.ShouldBeTrue();
    }

    /// <summary>Verifies that cloning an item produces a copy with identical property values.</summary>
    [Fact]
    public void WhenClonedThenCopiedPropertiesMatch()
    {
        var original = new FileExplorerItem
        {
            Path = "/folder/file.txt",
            FileSize = 1024,
            EntryType = FileExplorerItemType.File,
            IsHidden = true,
            IsReadOnly = true,
            IsSystem = true,
        };

        var clone = original.Clone();

        clone.Path.ShouldBe(original.Path);
        clone.FileSize.ShouldBe(original.FileSize);
        clone.EntryType.ShouldBe(original.EntryType);
        clone.IsHidden.ShouldBe(original.IsHidden);
        clone.IsReadOnly.ShouldBe(original.IsReadOnly);
        clone.IsSystem.ShouldBe(original.IsSystem);
    }

    /// <summary>Verifies that CompareTo compares items alphabetically by their Name property.</summary>
    [Fact]
    public void WhenCompareToThenComparesByName()
    {
        var a = new FileExplorerItem { Name = "alpha.txt" };
        var b = new FileExplorerItem { Name = "beta.txt" };

        a.CompareTo(b).ShouldBeLessThan(0);
        b.CompareTo(a).ShouldBeGreaterThan(0);
    }

    /// <summary>Verifies that CompareTo throws an InvalidOperationException when comparing against null.</summary>
    [Fact]
    public void WhenCompareToNullThenThrows()
    {
        var item = new FileExplorerItem { Name = "file.txt" };

        Should.Throw<InvalidOperationException>(() => item.CompareTo(null));
    }

    /// <summary>Verifies that ToString returns the item's full path.</summary>
    [Fact]
    public void WhenToStringThenReturnsPath()
    {
        var item = new FileExplorerItem { Path = "/folder/file.txt" };

        item.ToString().ShouldBe("/folder/file.txt");
    }

    /// <summary>Verifies that IsNameMatch correctly matches or rejects file names against various glob and extension patterns.</summary>
    [Theory]
    [InlineData("/file.html", "*.html", true)]
    [InlineData("/file.htm", "*.htm", true)]
    [InlineData("/file.htm", "*.html;*.htm", true)]
    [InlineData("/file.html", "*.html;*.htm", true)]
    [InlineData("/file.HTML", "*.html", true)]             // case-insensitive
    [InlineData("/file.xlsx", "*.xlsx", true)]
    [InlineData("/folder/file.html", "*.html", true)]      // path with folder
    [InlineData("/file.html", "", true)]                   // empty pattern matches all
    [InlineData("/Web HTML Only.docx", "*.html", false)]   // name contains "html" but wrong extension
    [InlineData("/Web HTML Only.docx", "*.html;*.htm", false)]
    [InlineData("/file.html.bak", "*.html", false)]        // extension is .bak not .html
    [InlineData("/file.xlsx", "*.html;*.htm", false)]
    [InlineData("/file.ncalc", ".ncalc", true)]             // bare extension means *.ncalc
    [InlineData("/folder/file.NCALC", ".ncalc", true)]      // bare extension, case-insensitive, path with folder
    [InlineData("/file.txt", ".ncalc", false)]
    [InlineData("/myncalc.txt", ".ncalc", false)]           // name contains "ncalc" but wrong extension
    [InlineData("/file.docx", ".docx$", true)]              // legacy regex-style end anchor
    [InlineData("/file.rmscript", ".docx$;.rmscript$", true)]
    [InlineData("/file.txt", ".docx$;.rmscript$", false)]
    [InlineData("/Web HTML Only.docx", ".html$", false)]    // legacy anchor still must not match wrong extension
    [InlineData("/file.html", "*.html;", true)]             // trailing semicolon ignored
    [InlineData("/file.xlsx", "*.html;", false)]            // empty segment must not match all
    [InlineData("/file.html", "*.html; *.htm", true)]       // whitespace around segments ignored
    [InlineData("/readme.txt", "readme.txt", true)]         // exact filename
    [InlineData("/myreadme.txt", "readme.txt", false)]      // exact filename must not suffix-match
    [InlineData("/file1.txt", "file?.txt", true)]           // single-char wildcard
    [InlineData("/file12.txt", "file?.txt", false)]
    public void WhenCheckingIsNameMatchThenReturnsCorrectResult(string path, string pattern, bool expected)
    {
        var item = new FileExplorerItem { Path = path };

        item.IsNameMatch(pattern).ShouldBe(expected);
    }
}
