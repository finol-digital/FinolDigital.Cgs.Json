namespace FinolDigital.Cgs.Json.Tests;

public class ResourceTests
{
    [Theory]
    [InlineData("https:es.json")]
    [InlineData("HTTPS:es.json")]
    [InlineData("https:/es.json")]
    [InlineData("https:?x")]
    [InlineData("https:#x")]
    [InlineData("http:es.json")]
    [InlineData("1a:b")]
    public void SchemeLikeRelativeReferencesAreRejectedWithoutThrowing(string reference)
    {
        var manifest = LocalizationJson.ReadManifest("{\"defaultLocale\":\"en\",\"locales\":{\"es\":{\"url\":\"" + reference + "\"}}}");
        Assert.False(manifest.IsValid);
        Assert.Contains(manifest.Diagnostics, d => d.Code == "manifest" && d.Path == "/locales/es/url");
        var (_, _, targets) = PresentationTests.Create("{\"defaultLocale\":\"es\",\"locales\":{\"es\":{\"inline\":{\"assets\":{\"rulesUrl\":\"" + reference + "\"}}}}}");
        foreach (var scheme in new[] { "http", "https" })
        {
            var origin = LocalizationOrigin.Remote(new Uri(scheme + "://cdn.example/g/cgs.json"));
            Assert.Null(LocalizationOrigin.Resolve(reference, origin));
            var context = new LocalizationContext(targets, "es", definitionOrigin: origin);
            Assert.Empty(context.AssetCandidates("rulesUrl"));
        }
    }

    [Theory]
    [InlineData("docs/a:b.html", "docs/a:b.html")]
    [InlineData("./a:b.html", "a:b.html")]
    [InlineData("?url=https:foo", "cgs.json?url=https:foo")]
    [InlineData("#section:one", "cgs.json#section:one")]
    public void ColonsAfterTheFirstRelativeSegmentRemainValid(string reference, string suffix)
    {
        foreach (var scheme in new[] { "http", "https" })
        {
            var prefix = scheme + "://cdn.example/g/";
            Assert.Equal(prefix + suffix, LocalizationOrigin.Resolve(reference,
                LocalizationOrigin.Remote(new Uri(prefix + "cgs.json")))!.AbsoluteUri);
        }
    }

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
    [InlineData("docs/rules.html#setup", "https://example.invalid/game/docs/rules.html#setup")]
    [InlineData("#setup", "https://example.invalid/game/cgs.json#setup")]
    [InlineData("rules.html?lang=es#setup", "https://example.invalid/game/rules.html?lang=es#setup")]
    public void RelativeFragmentsResolveAgainstTheirContainingDocument(string reference, string expected)
    {
        var origin = LocalizationOrigin.Remote(new Uri("https://example.invalid/game/cgs.json"));
        Assert.Equal(expected, LocalizationOrigin.Resolve(reference, origin)!.AbsoluteUri);
        Assert.NotNull(LocalizationJson.ReadManifest("{\"defaultLocale\":\"en\",\"locales\":{\"es\":{\"url\":\"" + reference + "\"}}}").Value);
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
    [InlineData("a\n")]
    [InlineData("http:///foo")]
    [InlineData("https://?x")]
    [InlineData("http://example.invalid:99999/x")]
    [InlineData("a#one#two")]
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
            var fragment = LocalizationOrigin.Resolve("../card.png#setup", origin)!;
            Assert.Equal(PhysicalPath(Path.Combine(root,"card.png")), fragment.LocalPath);
            Assert.Equal("#setup", fragment.Fragment);
            var sameDocument = LocalizationOrigin.Resolve("#setup", origin)!;
            Assert.Equal(PhysicalPath(Path.Combine(locales,"es.json")), sameDocument.LocalPath);
            Assert.Equal("#setup", sameDocument.Fragment);
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
