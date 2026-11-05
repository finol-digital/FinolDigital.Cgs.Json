namespace FinolDigital.Cgs.Json.Tests;

public class ResourceTests
{
    [Fact]
    public void InlineAndExternalAssetsUseTheirOwnRecordedOrigin()
    {
        var (game,card,targets)=PresentationTests.Create("""
        {"defaultLocale":"ja","locales":{
        "es-MX":{"inline":{"assets":{"cardImages":{"001.SET":"regional.png"}}}},
        "es":{"url":"locales/es.json"},
        "ja":{"inline":{"assets":{"cardImages":{"001.SET":"https://example.invalid/ja.png"}}}}}}
        """);
        game.AutoUpdateUrl=new Uri("https://updates.invalid/different.json");
        var definition=LocalizationOrigin.Remote(new Uri("https://example.invalid/game/cgs.json"));
        Assert.Equal("https://example.invalid/game/locales/es.json",LocalizationOrigin.Resolve("locales/es.json",definition)!.AbsoluteUri);
        var external=LocalizationJson.ReadCatalog("""{"assets":{"cardImages":{"001.SET":"images/card.png"}}}""").Value!;
        var context=new LocalizationContext(targets,"es-MX",new Dictionary<string,LocalizationCatalogDocument> {
            ["ES"]=new(external,LocalizationOrigin.Remote(new Uri("https://cdn.invalid/final/es.json")))},definition);
        var original=card.ImageWebUrl;
        Assert.Equal(new[] {"https://example.invalid/game/regional.png","https://cdn.invalid/final/images/card.png","https://example.invalid/ja.png"},
            context.AssetCandidates("cardImages","001.SET").Select(c=>c.Uri.AbsoluteUri));
        Assert.Equal(original,card.ImageWebUrl);
        Assert.Null(LocalizationOrigin.Resolve("locales/es.json",null));
        Assert.NotNull(LocalizationOrigin.Resolve("https://example.invalid/absolute.png",null));
    }

    [Theory]
    [InlineData("locales/espa%C3%B1ol.json")]
    [InlineData("%E7%94%BB%E5%83%8F/001.png")]
    public void EncodedInternationalPathsResolveAgainstRecordedOrigin(string reference)
    {
        var origin = LocalizationOrigin.Remote(new Uri("https://example.invalid/game/cgs.json"));
        Assert.Equal("https://example.invalid/game/" + reference,
            LocalizationOrigin.Resolve(reference, origin)!.AbsoluteUri);
        Assert.NotNull(LocalizationJson.ReadManifest("{\"defaultLocale\":\"en\",\"locales\":{\"es\":{\"url\":\"" + reference + "\"}}}").Value);
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("/etc/passwd")]
    [InlineData("C:\\secrets.txt")]
    [InlineData("javascript:alert(1)")]
    [InlineData("//other.invalid/path")]
    [InlineData("bad path")]
    [InlineData("locales/español.json")]
    [InlineData("画像/001.png")]
    [InlineData("images/{cardName}.png")]
    public void InvalidReferencesAreUnavailable(string reference) =>
        Assert.Null(LocalizationOrigin.Resolve(reference,LocalizationOrigin.Remote(new Uri("https://example.invalid/cgs.json"))));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PackageResolutionConfinesDecodedPathsAndAncestorSymlinks(bool linkedTemp)
    {
        var container=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));
        var temp=Path.Combine(container,"physical");
        Directory.CreateDirectory(temp);
        if(linkedTemp)
        {
            var link=Path.Combine(container,"logical");
            Directory.CreateSymbolicLink(link,temp);
            temp=link;
        }
        var root=Path.Combine(temp,"game");
        var locales=Path.Combine(root,"locales");
        Directory.CreateDirectory(locales);
        File.WriteAllText(Path.Combine(locales,"es.json"),"{}");
        File.WriteAllText(Path.Combine(root,"card.png"),"image");
        var specialNames=new[] {"card with space.png", "hash#asset.png", "percent%2e.png", "imágenes.png"};
        foreach(var name in specialNames) File.WriteAllText(Path.Combine(root,name),"image");
        File.WriteAllText(Path.Combine(temp,"outside.png"),"outside");
        Directory.CreateSymbolicLink(Path.Combine(root,"escape"),temp);
        try
        {
            var origin=LocalizationOrigin.Bundled(root,Path.Combine(locales,"es.json"),PhysicalPath);
            Assert.Equal(PhysicalPath(Path.Combine(root,"card.png")),LocalizationOrigin.Resolve("../card.png",origin)!.LocalPath);
            foreach(var name in specialNames)
                Assert.Equal(PhysicalPath(Path.Combine(root,name)),LocalizationOrigin.Resolve("../"+Uri.EscapeDataString(name),origin)!.LocalPath);
            foreach(var path in new[] {"../../outside.png","%2e%2e/%2e%2e/outside.png","../escape/outside.png","%2fetc/passwd","..%5c..%5coutside.png","missing.png"})
                Assert.Null(LocalizationOrigin.Resolve(path,origin));
            var relocated=Path.Combine(temp,"moved");
            Directory.Move(root,relocated);
            var moved=LocalizationOrigin.Bundled(relocated,Path.Combine(relocated,"locales","es.json"),PhysicalPath);
            Assert.Equal(PhysicalPath(Path.Combine(relocated,"card.png")),LocalizationOrigin.Resolve("../card.png",moved)!.LocalPath);
        }
        finally { Directory.Delete(container,true); }
    }

    // Consumer adapter used only by tests; resolves every ancestor, not just the final file.
    private static string? PhysicalPath(string path)
    {
        var full=Path.GetFullPath(path);
        var current=Path.GetPathRoot(full)!;
        foreach(var segment in full[current.Length..].Split(Path.DirectorySeparatorChar,StringSplitOptions.RemoveEmptyEntries))
        {
            current=Path.Combine(current,segment);
            FileSystemInfo entry=Directory.Exists(current)?new DirectoryInfo(current):new FileInfo(current);
            if (!entry.Exists) return null;
            current=entry.ResolveLinkTarget(true)?.FullName ?? entry.FullName;
        }
        return current;
    }
}
