using AwesomeAssertions;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Media;
using Nop.Data;
using Nop.Plugin.Misc.ServingSuggestions.Domain;
using Nop.Plugin.Misc.ServingSuggestions.Services;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Media;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.ServingSuggestions;

[TestFixture]
public class ServingSuggestionServiceTests : ServiceTest
{
    private IServingSuggestionService _servingSuggestionService;
    private ILanguageService _languageService;
    private ILocalizationService _localizationService;
    private IPictureService _pictureService;
    private IProductService _productService;
    private IRepository<Picture> _pictureRepository;

    [OneTimeSetUp]
    public void SetUp()
    {
        _servingSuggestionService = GetService<IServingSuggestionService>();
        _languageService = GetService<ILanguageService>();
        _localizationService = GetService<ILocalizationService>();
        _pictureService = GetService<IPictureService>();
        _productService = GetService<IProductService>();
        _pictureRepository = GetService<IRepository<Picture>>();
    }

    private async Task<Picture> CreatePictureAsync(string seoFilename)
    {
        return await _pictureService.InsertPictureAsync([1, 2, 3], "image/png", seoFilename, validateBinary: false);
    }

    private async Task<Product> CreateProductAsync(string name)
    {
        var product = new Product { Name = name, Published = true };
        await _productService.InsertProductAsync(product);

        return product;
    }

    private async Task<ServingSuggestion> CreateServingSuggestionAsync(int productId, string title = "Title", string description = "Description")
    {
        var picture = await CreatePictureAsync(title);

        var servingSuggestion = new ServingSuggestion
        {
            ProductId = productId,
            Title = title,
            Description = description,
            PictureId = picture.Id
        };
        await _servingSuggestionService.InsertServingSuggestionAsync(servingSuggestion);

        return servingSuggestion;
    }

    [Test]
    public async Task InsertServingSuggestionAsync_PersistsLocalizedValues_InTheSameWrite()
    {
        var language = new Language { Name = "Serving suggestion test language A", LanguageCulture = "xx-SA", UniqueSeoCode = "sa", Published = true };
        await _languageService.InsertLanguageAsync(language);

        var product = await CreateProductAsync("Localization write test product");
        var picture = await CreatePictureAsync("Localized picture");

        var servingSuggestion = new ServingSuggestion { ProductId = product.Id, Title = "Serve chilled", Description = "Best served cold", PictureId = picture.Id };
        await _servingSuggestionService.InsertServingSuggestionAsync(servingSuggestion, new List<ServingSuggestionLocalizedValue>
        {
            new(language.Id, "Servir frais", "Meilleur servi froid")
        });

        var localizedTitle = await _localizationService.GetLocalizedAsync(servingSuggestion, x => x.Title, language.Id, false, false);

        await _servingSuggestionService.DeleteServingSuggestionAsync(servingSuggestion);
        await _productService.DeleteProductAsync(product);
        await _languageService.DeleteLanguageAsync(language);

        localizedTitle.Should().Be("Servir frais");
    }

    [Test]
    public async Task GetLocalizedAsync_FallsBackToDefaultValue_WhenTranslationMissing()
    {
        //two published languages are required for GetLocalizedAsync to even attempt loading a
        //translation (ensureTwoPublishedLanguages); the default install seeds only one, so a second
        //is created here specifically so this proves the fallback, not a "localization skipped" no-op
        var language = new Language { Name = "Serving suggestion test language B", LanguageCulture = "xx-SB", UniqueSeoCode = "sb", Published = true };
        await _languageService.InsertLanguageAsync(language);

        var product = await CreateProductAsync("Localization fallback test product");
        var servingSuggestion = await CreateServingSuggestionAsync(product.Id, title: "Serve at room temperature");

        //no LocalizedProperty row exists for this language
        var localizedTitle = await _localizationService.GetLocalizedAsync(servingSuggestion, x => x.Title, language.Id);

        await _servingSuggestionService.DeleteServingSuggestionAsync(servingSuggestion);
        await _productService.DeleteProductAsync(product);
        await _languageService.DeleteLanguageAsync(language);

        localizedTitle.Should().Be("Serve at room temperature");
    }

    [Test]
    public async Task InsertServingSuggestionAsync_WithZeroSteps_Succeeds()
    {
        var product = await CreateProductAsync("Zero steps test product");
        var servingSuggestion = await CreateServingSuggestionAsync(product.Id);

        var steps = await _servingSuggestionService.GetServingSuggestionStepsAsync(servingSuggestion.Id);

        await _servingSuggestionService.DeleteServingSuggestionAsync(servingSuggestion);
        await _productService.DeleteProductAsync(product);

        steps.Should().BeEmpty();
    }

    [Test]
    public async Task GetServingSuggestionStepsAsync_ReturnsStepsInDisplayOrder()
    {
        var product = await CreateProductAsync("Step order test product");
        var servingSuggestion = await CreateServingSuggestionAsync(product.Id);

        var stepC = new ServingSuggestionStep { ServingSuggestionId = servingSuggestion.Id, Text = "Garnish with parsley", DisplayOrder = 3 };
        var stepA = new ServingSuggestionStep { ServingSuggestionId = servingSuggestion.Id, Text = "Heat gently", DisplayOrder = 1 };
        var stepB = new ServingSuggestionStep { ServingSuggestionId = servingSuggestion.Id, Text = "Pour into a bowl", DisplayOrder = 2 };

        await _servingSuggestionService.InsertServingSuggestionStepAsync(stepC);
        await _servingSuggestionService.InsertServingSuggestionStepAsync(stepA);
        await _servingSuggestionService.InsertServingSuggestionStepAsync(stepB);

        var steps = await _servingSuggestionService.GetServingSuggestionStepsAsync(servingSuggestion.Id);

        await _servingSuggestionService.DeleteServingSuggestionAsync(servingSuggestion);
        await _productService.DeleteProductAsync(product);

        steps.Select(step => step.Text).Should().ContainInOrder("Heat gently", "Pour into a bowl", "Garnish with parsley");
    }

    [Test]
    public async Task DeleteServingSuggestionAsync_DeletesTheEntityStepsLocalizedPropertiesAndPicture()
    {
        var product = await CreateProductAsync("Delete cascade test product");
        var servingSuggestion = await CreateServingSuggestionAsync(product.Id);
        var pictureId = servingSuggestion.PictureId;

        var step = new ServingSuggestionStep { ServingSuggestionId = servingSuggestion.Id, Text = "Serve immediately" };
        await _servingSuggestionService.InsertServingSuggestionStepAsync(step);

        var language = new Language { Name = "Serving suggestion delete-cascade test language", LanguageCulture = "xx-SC", UniqueSeoCode = "sc", Published = true };
        await _languageService.InsertLanguageAsync(language);
        await _servingSuggestionService.UpdateServingSuggestionAsync(servingSuggestion, new List<ServingSuggestionLocalizedValue>
        {
            new(language.Id, "Localized title", "Localized description")
        });

        await _servingSuggestionService.DeleteServingSuggestionAsync(servingSuggestion);

        var reloadedServingSuggestion = await _servingSuggestionService.GetServingSuggestionByIdAsync(servingSuggestion.Id);
        var reloadedStep = await _servingSuggestionService.GetServingSuggestionStepByIdAsync(step.Id);
        //queried through IRepository<Picture> directly, bypassing IPictureService.GetPictureByIdAsync's
        //by-id static cache. Nop.Services.Media itself declares no CacheEventConsumer<Picture> - one does
        //exist (Nop.Plugin.Misc.AzureBlob's PictureCacheEventConsumer, whose own by-id/by-ids/all
        //invalidation runs regardless of whether Azure Blob storage is actually configured, since event
        //consumers are discovered by type, not gated on plugin activation) - but this isolated test
        //project does not reference that plugin, so the cached read would return the pre-delete object for
        //the rest of this process's lifetime once warmed (as it was, above, by DeleteServingSuggestionAsync's
        //own internal GetPictureByIdAsync call). Reading via the repository sidesteps that gap in this test
        //project rather than being a defect in the deletion this test is actually verifying.
        var reloadedPicture = await _pictureRepository.GetByIdAsync(pictureId);
        var localizedTitleAfterDelete = await _localizationService.GetLocalizedAsync<ServingSuggestion, string>(servingSuggestion, x => x.Title, language.Id, false, false);

        await _productService.DeleteProductAsync(product);
        await _languageService.DeleteLanguageAsync(language);

        reloadedServingSuggestion.Should().BeNull();
        reloadedStep.Should().BeNull();
        reloadedPicture.Should().BeNull();
        localizedTitleAfterDelete.Should().BeNullOrEmpty();
    }

    [Test]
    public async Task ReplacingAPicture_ThenDeletingTheOldOne_KeepsOnlyTheNewPicture()
    {
        //exercises the same sequence ServingSuggestionController.ServingSuggestionEditPopup performs on a
        //picture replace (CategoryController.cs:294-299 ordering): update the entity to the new PictureId
        //first, then delete the previous Picture last
        var product = await CreateProductAsync("Picture replace test product");
        var servingSuggestion = await CreateServingSuggestionAsync(product.Id);
        var oldPictureId = servingSuggestion.PictureId;

        var newPicture = await CreatePictureAsync("Replacement picture");
        servingSuggestion.PictureId = newPicture.Id;
        await _servingSuggestionService.UpdateServingSuggestionAsync(servingSuggestion);

        var oldPicture = await _pictureService.GetPictureByIdAsync(oldPictureId);
        if (oldPicture != null)
            await _pictureService.DeletePictureAsync(oldPicture);

        //queried through IRepository<Picture> directly - see the comment on the equivalent check in
        //DeleteServingSuggestionAsync_DeletesTheEntityStepsLocalizedPropertiesAndPicture for why
        //IPictureService.GetPictureByIdAsync's own cached read is not used here
        var reloadedOldPicture = await _pictureRepository.GetByIdAsync(oldPictureId);
        var reloadedNewPicture = await _pictureRepository.GetByIdAsync(newPicture.Id);

        await _servingSuggestionService.DeleteServingSuggestionAsync(servingSuggestion);
        await _productService.DeleteProductAsync(product);

        reloadedOldPicture.Should().BeNull();
        reloadedNewPicture.Should().NotBeNull();
    }
}
