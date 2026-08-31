using AwesomeAssertions;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Localization;
using Nop.Data;
using Nop.Plugin.Misc.Ingredients.Domain;
using Nop.Plugin.Misc.Ingredients.Services;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.Ingredients;

[TestFixture]
public class IngredientServiceTests : ServiceTest
{
    private IIngredientService _ingredientService;
    private IIngredientCompositionService _ingredientCompositionService;
    private IProductIngredientService _productIngredientService;
    private ILanguageService _languageService;
    private ILocalizationService _localizationService;
    private IProductService _productService;

    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        _ingredientService = GetService<IIngredientService>();
        _ingredientCompositionService = GetService<IIngredientCompositionService>();
        _productIngredientService = GetService<IProductIngredientService>();
        _languageService = GetService<ILanguageService>();
        _localizationService = GetService<ILocalizationService>();
        _productService = GetService<IProductService>();

        //this test harness registers the plugin descriptor and runs its schema migration
        //directly (see ServiceTest.InitPlugins) rather than calling the real IngredientsPlugin.
        //InstallAsync (which would also touch shared WidgetSettings state for every other test in
        //the suite), so the two locale resources the delete-blocked-by-usage error messages format
        //against are seeded here, scoped to this fixture, kept in sync with IngredientsPlugin.InstallAsync
        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Misc.Ingredients.Errors.InUseByIngredients"] = "This ingredient cannot be deleted because it is still used in the composition of: {0}.",
            ["Plugins.Misc.Ingredients.Errors.InUseByProducts"] = "This ingredient cannot be deleted because the following products still use it: {0}."
        });
    }

    [OneTimeTearDown]
    public async Task TearDownAsync()
    {
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Misc.Ingredients.Errors");
    }

    private static Ingredient CreateEntity(string name)
    {
        return new Ingredient { Name = name };
    }

    private async Task<Ingredient> CreateAsync(string name)
    {
        var ingredient = CreateEntity(name);
        await _ingredientService.InsertIngredientAsync(ingredient);

        return ingredient;
    }

    [Test]
    public async Task InsertIngredientAsync_PersistsLocalizedValues_InTheSameWrite()
    {
        var language = new Language { Name = "Ingredient test language A", LanguageCulture = "xx-AA", UniqueSeoCode = "xa", Published = true };
        await _languageService.InsertLanguageAsync(language);

        var ingredient = CreateEntity("Salt");
        await _ingredientService.InsertIngredientAsync(ingredient, new List<IngredientLocalizedValue>
        {
            new(language.Id, "Sel", "French for salt")
        });

        var localizedName = await _localizationService.GetLocalizedAsync(ingredient, x => x.Name, language.Id, false, false);

        await _ingredientService.DeleteIngredientAsync(ingredient);
        await _languageService.DeleteLanguageAsync(language);

        localizedName.Should().Be("Sel");
    }

    [Test]
    public async Task GetLocalizedAsync_FallsBackToDefaultValue_WhenTranslationMissing()
    {
        //two published languages are required for GetLocalizedAsync to even attempt loading a
        //translation (ensureTwoPublishedLanguages); the default install seeds only one, so a second
        //is created here specifically so this proves the fallback, not a "localization skipped" no-op
        var language = new Language { Name = "Ingredient test language B", LanguageCulture = "xx-BB", UniqueSeoCode = "xb", Published = true };
        await _languageService.InsertLanguageAsync(language);

        var ingredient = await CreateAsync("Salt");

        //no LocalizedProperty row exists for this language
        var localizedName = await _localizationService.GetLocalizedAsync(ingredient, x => x.Name, language.Id);

        await _ingredientService.DeleteIngredientAsync(ingredient);
        await _languageService.DeleteLanguageAsync(language);

        localizedName.Should().Be("Salt");
    }

    [Test]
    public async Task DeleteIngredientAsync_DeletesTheIngredient_WhenNotInUse()
    {
        var ingredient = await CreateAsync("Water");

        await _ingredientService.DeleteIngredientAsync(ingredient);

        var reloaded = await _ingredientService.GetIngredientByIdAsync(ingredient.Id);

        reloaded.Should().BeNull();
    }

    [Test]
    public async Task DeleteIngredientAsync_Throws_AndNamesTheProduct_WhenStillAttachedToAProduct()
    {
        var ingredient = await CreateAsync("Bones");

        var product = new Product { Name = "Test product using an ingredient", Published = true };
        await _productService.InsertProductAsync(product);

        var mapping = new ProductIngredientMapping { ProductId = product.Id, IngredientId = ingredient.Id };
        await _productIngredientService.InsertProductIngredientAsync(mapping);

        NopException ex;

        try
        {
            ex = Assert.ThrowsAsync<NopException>(async () => await _ingredientService.DeleteIngredientAsync(ingredient));
        }
        finally
        {
            //cleanup runs even if the assertion above fails (i.e. the code under test stopped throwing),
            //so a regression here can never leak a row into the shared test database
            await _productIngredientService.DeleteProductIngredientAsync(mapping);
            await _ingredientService.DeleteIngredientAsync(ingredient);
            await _productService.DeleteProductAsync(product);
        }

        ex.Message.Should().Contain(product.Name);
    }

    [Test]
    public async Task DeleteIngredientAsync_Throws_AndNamesTheCompositeIngredient_WhenStillUsedAsAComponent()
    {
        var child = await CreateAsync("Celery");
        var parent = await CreateAsync("Beef broth");

        await _ingredientCompositionService.AddChildIngredientAsync(parent.Id, child.Id);

        NopException ex;

        try
        {
            ex = Assert.ThrowsAsync<NopException>(async () => await _ingredientService.DeleteIngredientAsync(child));
        }
        finally
        {
            var composition = (await _ingredientCompositionService.GetChildCompositionsAsync(parent.Id)).Single();
            await _ingredientCompositionService.RemoveChildIngredientAsync(composition);
            await _ingredientService.DeleteIngredientAsync(child);
            await _ingredientService.DeleteIngredientAsync(parent);
        }

        ex.Message.Should().Contain(parent.Name);
    }

    [Test]
    public async Task InsertIngredientAsync_SeedsAReflexiveClosureRow()
    {
        var ingredientClosureRepository = GetService<IRepository<IngredientClosure>>();

        var ingredient = await CreateAsync("Reflexive closure test ingredient");

        var reflexiveRow = await ingredientClosureRepository.Table
            .Where(closure => closure.AncestorIngredientId == ingredient.Id && closure.DescendantIngredientId == ingredient.Id)
            .SingleOrDefaultAsync();

        await _ingredientService.DeleteIngredientAsync(ingredient);

        reflexiveRow.Should().NotBeNull();
        reflexiveRow.Depth.Should().Be(0);
    }
}
