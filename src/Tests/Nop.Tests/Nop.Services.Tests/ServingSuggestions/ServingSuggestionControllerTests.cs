using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.Mapper;
using Nop.Data;
using Nop.Plugin.Misc.ServingSuggestions.Admin.Controllers;
using Nop.Plugin.Misc.ServingSuggestions.Admin.Factories;
using Nop.Plugin.Misc.ServingSuggestions.Admin.Models;
using Nop.Plugin.Misc.ServingSuggestions.Domain;
using Nop.Plugin.Misc.ServingSuggestions.Services;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Web.Framework.Factories;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.ServingSuggestions;

/// <summary>
/// Exercises ServingSuggestionController.ServingSuggestionEditPopup's POST action directly - the real
/// enforcement point for the picture-replace-deletes-old-picture guard
/// (<c>prevPictureId > 0 &amp;&amp; prevPictureId != servingSuggestion.PictureId</c>, delete-after-write-
/// succeeds ordering). ServingSuggestionServiceTests only re-implements that same guard inline in a test,
/// which cannot catch a real bug in the controller's own conditional; this test instead constructs the
/// controller directly (same style as ServingSuggestionsPluginTests) with a minimal HttpContext so the
/// action's own View()/TempData plumbing does not throw, and calls the actual action method.
/// </summary>
[TestFixture]
public class ServingSuggestionControllerTests : ServiceTest
{
    //a genuine 1x1 PNG - IPictureService.InsertPictureAsync(IFormFile, ...) always validates the binary
    //(SkiaSharp decode), unlike the byte[] overload used elsewhere in this test suite, which exposes a
    //validateBinary: false escape hatch
    private static readonly byte[] _validPngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    private IServingSuggestionService _servingSuggestionService;
    private IPictureService _pictureService;
    private IProductService _productService;
    private IRepository<Picture> _pictureRepository;
    private ServingSuggestionController _controller;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _servingSuggestionService = GetService<IServingSuggestionService>();
        _pictureService = GetService<IPictureService>();
        _productService = GetService<IProductService>();
        _pictureRepository = GetService<IRepository<Picture>>();

        //MapperConfiguration.Mapper (Mapster, wrapped behind an AutoMapper-like facade) is process-wide
        //static state that nothing in this test harness initializes: BaseNopTest hand-registers services
        //directly rather than going through the real app's INopStartup/IOrderedMapperProfile discovery
        //(NopEngine.AddMapper()), so model.ToEntity()/ToModel() calls have never needed it in this suite
        //before. ServingSuggestionEditPopup's POST action genuinely calls model.ToEntity<T>()/
        //model.ToEntity(existing), so this mirrors NopEngine.AddMapper() exactly, scoped to this fixture.
        if (MapperConfiguration.Mapper == null)
        {
            var typeFinder = Singleton<ITypeFinder>.Instance;
            var mapperConfigurations = typeFinder.FindClassesOfType<IOrderedMapperProfile>()
                .Select(type => (IOrderedMapperProfile)Activator.CreateInstance(type))
                .Where(profile => profile != null)
                .OrderBy(profile => profile.Order);

            MapperConfiguration.Init(mapperConfigurations);
        }
    }

    [SetUp]
    public void SetUp()
    {
        //a fresh controller (and so a fresh, empty ModelState) per test - some tests below add a
        //ModelState error by hand to simulate what [AutoValidation] would have done before the action ran
        //(this harness calls the action directly, bypassing the real filter pipeline), and that must not
        //leak into other tests sharing the same controller instance
        var servingSuggestionAdminModelFactory = new ServingSuggestionAdminModelFactory(
            GetService<ILocalizationService>(),
            GetService<ILocalizedModelFactory>(),
            _pictureService,
            _servingSuggestionService);

        _controller = new ServingSuggestionController(
            GetService<ILocalizationService>(),
            GetService<INotificationService>(),
            _pictureService,
            _servingSuggestionService,
            servingSuggestionAdminModelFactory)
        {
            //View()/TempData resolve services off HttpContext.RequestServices when the action executes -
            //without this, calling the action directly (bypassing the real MVC request pipeline) throws
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = ServiceProvider }
            }
        };
    }

    private static IFormCollection FormWithFile(string fileName)
    {
        var stream = new MemoryStream(_validPngBytes);
        var file = new FormFile(stream, 0, stream.Length, "picturefile", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        return new FormCollection(new Dictionary<string, StringValues>(), new FormFileCollection { file });
    }

    private static IFormCollection FormWithNoFile()
    {
        return new FormCollection(new Dictionary<string, StringValues>());
    }

    [Test]
    public async Task ServingSuggestionEditPopup_ReplacingThePicture_DeletesTheOldPictureAndKeepsOnlyTheNew()
    {
        var product = new Product { Name = "Controller test - picture replace product", Published = true };
        await _productService.InsertProductAsync(product);

        var oldPicture = await _pictureService.InsertPictureAsync(_validPngBytes, "image/png", "controller-test-old", validateBinary: false);
        var servingSuggestion = new ServingSuggestion { ProductId = product.Id, Title = "Title", Description = "Description", PictureId = oldPicture.Id };
        await _servingSuggestionService.InsertServingSuggestionAsync(servingSuggestion);

        var model = new ServingSuggestionModel
        {
            Id = servingSuggestion.Id,
            ProductId = product.Id,
            Title = "Updated title",
            Description = "Updated description",
            Locales = new List<ServingSuggestionLocalizedModel>()
        };

        await _controller.ServingSuggestionEditPopup(model, FormWithFile("replacement.png"));

        var reloadedServingSuggestion = await _servingSuggestionService.GetServingSuggestionByIdAsync(servingSuggestion.Id);
        //queried through IRepository<Picture> directly, not IPictureService.GetPictureByIdAsync - see
        //ServingSuggestionServiceTests for why (this isolated test project doesn't reference the one
        //plugin, Nop.Plugin.Misc.AzureBlob, that happens to invalidate Picture's by-id cache)
        var reloadedOldPicture = await _pictureRepository.GetByIdAsync(oldPicture.Id);
        var reloadedNewPicture = await _pictureRepository.GetByIdAsync(reloadedServingSuggestion.PictureId);

        await _servingSuggestionService.DeleteServingSuggestionAsync(reloadedServingSuggestion);
        await _productService.DeleteProductAsync(product);

        reloadedServingSuggestion.PictureId.Should().NotBe(oldPicture.Id);
        reloadedOldPicture.Should().BeNull();
        reloadedNewPicture.Should().NotBeNull();
    }

    [Test]
    public async Task ServingSuggestionEditPopup_UpdatingWithNoNewFile_KeepsTheExistingPicture()
    {
        var product = new Product { Name = "Controller test - keep picture product", Published = true };
        await _productService.InsertProductAsync(product);

        var picture = await _pictureService.InsertPictureAsync(_validPngBytes, "image/png", "controller-test-keep", validateBinary: false);
        var servingSuggestion = new ServingSuggestion { ProductId = product.Id, Title = "Title", Description = "Description", PictureId = picture.Id };
        await _servingSuggestionService.InsertServingSuggestionAsync(servingSuggestion);

        var model = new ServingSuggestionModel
        {
            Id = servingSuggestion.Id,
            ProductId = product.Id,
            Title = "Updated title, no new picture",
            Description = "Updated description",
            Locales = new List<ServingSuggestionLocalizedModel>()
        };

        await _controller.ServingSuggestionEditPopup(model, FormWithNoFile());

        var reloadedServingSuggestion = await _servingSuggestionService.GetServingSuggestionByIdAsync(servingSuggestion.Id);
        var reloadedPicture = await _pictureRepository.GetByIdAsync(picture.Id);

        await _servingSuggestionService.DeleteServingSuggestionAsync(reloadedServingSuggestion);
        await _productService.DeleteProductAsync(product);

        reloadedServingSuggestion.PictureId.Should().Be(picture.Id);
        reloadedServingSuggestion.Title.Should().Be("Updated title, no new picture");
        reloadedPicture.Should().NotBeNull();
    }

    /// <summary>
    /// Regression test: InsertPictureAsync used to run unconditionally whenever a file was present, before
    /// the rest of ModelState was known-valid. If Title (or anything else) failed validation on the same
    /// submission, the newly-inserted Picture row was never linked to anything and never cleaned up - an
    /// orphan on every failed resubmission, since the file input can't be re-populated on re-render.
    /// </summary>
    [Test]
    public async Task ServingSuggestionEditPopup_WhenTitleIsInvalid_DoesNotOrphanTheUploadedPicture()
    {
        var product = new Product { Name = "Controller test - orphan picture product", Published = true };
        await _productService.InsertProductAsync(product);

        var beforePictureIds = (await _pictureRepository.Table.Select(picture => picture.Id).ToListAsync()).ToList();

        var model = new ServingSuggestionModel
        {
            ProductId = product.Id,
            Title = string.Empty,
            Description = "Description",
            Locales = new List<ServingSuggestionLocalizedModel>()
        };

        //simulates what [AutoValidation] would already have added to ModelState before the action ran
        //(this test bypasses the real filter pipeline - see the class doc comment)
        _controller.ModelState.AddModelError(nameof(model.Title), "Title required");

        await _controller.ServingSuggestionEditPopup(model, FormWithFile("orphan-attempt.png"));

        var afterPictureIds = (await _pictureRepository.Table.Select(picture => picture.Id).ToListAsync()).ToList();
        var servingSuggestion = await _servingSuggestionService.GetServingSuggestionByProductIdAsync(product.Id);

        await _productService.DeleteProductAsync(product);

        servingSuggestion.Should().BeNull();
        afterPictureIds.Should().BeEquivalentTo(beforePictureIds);
    }
}
