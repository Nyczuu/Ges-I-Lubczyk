using AwesomeAssertions;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.Ingredients.Domain;
using Nop.Plugin.Misc.Ingredients.Public.Components;
using Nop.Plugin.Misc.Ingredients.Services;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.Ingredients;

/// <summary>
/// Exercises the storefront rendering path itself - spec section 11's first two required scenarios
/// ("simple ingredient attached to a product renders" and "composite ingredient renders its children;
/// nesting at the maximum depth (3 levels) renders correctly") - rather than only the data-service layer
/// underneath it. IngredientsViewComponent is not registered in the DI container the test harness builds
/// (view components aren't a service the rest of the app resolves by interface), so it is constructed
/// directly here; its constructor takes only already-registered service interfaces.
/// </summary>
[TestFixture]
public class IngredientsViewComponentTests : ServiceTest
{
    private IIngredientService _ingredientService;
    private IIngredientCompositionService _ingredientCompositionService;
    private IProductIngredientService _productIngredientService;
    private ILocalizationService _localizationService;
    private IProductService _productService;
    private IngredientsViewComponent _ingredientsViewComponent;

    [OneTimeSetUp]
    public void SetUp()
    {
        _ingredientService = GetService<IIngredientService>();
        _ingredientCompositionService = GetService<IIngredientCompositionService>();
        _productIngredientService = GetService<IProductIngredientService>();
        _localizationService = GetService<ILocalizationService>();
        _productService = GetService<IProductService>();

        _ingredientsViewComponent = new IngredientsViewComponent(_ingredientService, _localizationService, _productIngredientService);
    }

    private async Task<Ingredient> CreateIngredientAsync(string name)
    {
        var ingredient = new Ingredient { Name = name };
        await _ingredientService.InsertIngredientAsync(ingredient);

        return ingredient;
    }

    private async Task<Product> CreateProductAsync(string name)
    {
        var product = new Product { Name = name, Published = true };
        await _productService.InsertProductAsync(product);

        return product;
    }

    [Test]
    public async Task PrepareIngredientsModelAsync_RendersASimpleAttachedIngredient()
    {
        var ingredient = await CreateIngredientAsync("View component test - simple ingredient");
        var product = await CreateProductAsync("View component test - simple product");

        var mapping = new ProductIngredientMapping { ProductId = product.Id, IngredientId = ingredient.Id };
        await _productIngredientService.InsertProductIngredientAsync(mapping);

        var model = await _ingredientsViewComponent.PrepareIngredientsModelAsync(product.Id);

        //cleanup
        await _productIngredientService.DeleteProductIngredientAsync(mapping);
        await _ingredientService.DeleteIngredientAsync(ingredient);
        await _productService.DeleteProductAsync(product);

        model.Ingredients.Should().ContainSingle();
        model.Ingredients.Single().Name.Should().Be(ingredient.Name);
        model.Ingredients.Single().Children.Should().BeEmpty();
    }

    [Test]
    public async Task PrepareIngredientsModelAsync_RendersACompositeIngredientNestedToTheMaximumDepth()
    {
        //broth (attached to the product) -> bones -> marrow -> essence: 3 composition edges, exactly the ceiling
        var broth = await CreateIngredientAsync("View component test - broth");
        var bones = await CreateIngredientAsync("View component test - bones");
        var marrow = await CreateIngredientAsync("View component test - marrow");
        var essence = await CreateIngredientAsync("View component test - essence");

        await _ingredientCompositionService.AddChildIngredientAsync(broth.Id, bones.Id);
        await _ingredientCompositionService.AddChildIngredientAsync(bones.Id, marrow.Id);
        await _ingredientCompositionService.AddChildIngredientAsync(marrow.Id, essence.Id);

        var product = await CreateProductAsync("View component test - composite product");
        var mapping = new ProductIngredientMapping { ProductId = product.Id, IngredientId = broth.Id };
        await _productIngredientService.InsertProductIngredientAsync(mapping);

        var model = await _ingredientsViewComponent.PrepareIngredientsModelAsync(product.Id);

        //cleanup
        await _productIngredientService.DeleteProductIngredientAsync(mapping);
        await _productService.DeleteProductAsync(product);
        await _ingredientCompositionService.RemoveChildIngredientAsync((await _ingredientCompositionService.GetChildCompositionsAsync(marrow.Id)).Single());
        await _ingredientCompositionService.RemoveChildIngredientAsync((await _ingredientCompositionService.GetChildCompositionsAsync(bones.Id)).Single());
        await _ingredientCompositionService.RemoveChildIngredientAsync((await _ingredientCompositionService.GetChildCompositionsAsync(broth.Id)).Single());
        await _ingredientService.DeleteIngredientAsync(essence);
        await _ingredientService.DeleteIngredientAsync(marrow);
        await _ingredientService.DeleteIngredientAsync(bones);
        await _ingredientService.DeleteIngredientAsync(broth);

        model.Ingredients.Should().ContainSingle();
        var brothNode = model.Ingredients.Single();
        brothNode.Name.Should().Be(broth.Name);

        brothNode.Children.Should().ContainSingle();
        var bonesNode = brothNode.Children.Single();
        bonesNode.Name.Should().Be(bones.Name);

        bonesNode.Children.Should().ContainSingle();
        var marrowNode = bonesNode.Children.Single();
        marrowNode.Name.Should().Be(marrow.Name);

        marrowNode.Children.Should().ContainSingle();
        var essenceNode = marrowNode.Children.Single();
        essenceNode.Name.Should().Be(essence.Name);
        essenceNode.Children.Should().BeEmpty();
    }
}
