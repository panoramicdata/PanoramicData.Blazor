using PanoramicData.Blazor.Models;
using Shouldly;

namespace PanoramicData.Blazor.Test;

public class FileExplorerItemTests
{
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

    [Fact]
    public void WhenRenamingThenPathAndNameAreUpdated()
    {
        var item = new FileExplorerItem { Path = "/folder1/old.txt", Name = "old.txt" };

        item.Rename("new.txt");

        item.Path.ShouldBe("/folder1/new.txt");
        item.Name.ShouldBe("new.txt");
    }

    [Fact]
    public void WhenRenamingRootThenNothingChanges()
    {
        var item = new FileExplorerItem { Path = "/", Name = "" };

        item.Rename("anything");

        item.Path.ShouldBe("/");
    }

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

    [Fact]
    public void WhenReadOnlyThenCanAddItemsIsFalse()
    {
        var item = new FileExplorerItem { IsReadOnly = true };

        item.CanAddItems.ShouldBeFalse();
        item.CanRemoveItems.ShouldBeFalse();
    }

    [Fact]
    public void WhenNotReadOnlyThenCanAddItemsIsTrue()
    {
        var item = new FileExplorerItem { IsReadOnly = false };

        item.CanAddItems.ShouldBeTrue();
        item.CanRemoveItems.ShouldBeTrue();
    }

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

    [Fact]
    public void WhenCompareToThenComparesByName()
    {
        var a = new FileExplorerItem { Name = "alpha.txt" };
        var b = new FileExplorerItem { Name = "beta.txt" };

        a.CompareTo(b).ShouldBeLessThan(0);
        b.CompareTo(a).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void WhenCompareToNullThenThrows()
    {
        var item = new FileExplorerItem { Name = "file.txt" };

        Should.Throw<InvalidOperationException>(() => item.CompareTo(null));
    }

    [Fact]
    public void WhenToStringThenReturnsPath()
    {
        var item = new FileExplorerItem { Path = "/folder/file.txt" };

        item.ToString().ShouldBe("/folder/file.txt");
    }
}
