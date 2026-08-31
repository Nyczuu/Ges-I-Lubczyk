using AwesomeAssertions;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.Ingredients.Domain;
using Nop.Plugin.Misc.Ingredients.Services;
using Nop.Services.Catalog;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.Ingredients;

[TestFixture]
public class ProductIngredientServiceTests : ServiceTest
{
    private IIngredientService _ingredientService;
    private IIngredientCompositionService _ingredientCompositionService;
    private IProductIngredientService _productIngredientService;
    private IProductService _productService;

    [OneTimeSetUp]
    public void SetUp()
    {
        _ingredientService = GetService<IIngredientService>();
        _ingredientCompositionService = GetService<IIngredientCompositionService>();
        _productIngredientService = GetService<IProductIngredientService>();
        _productService = GetService<IProductService>();
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
    public async Task InsertProductIngredientAsync_IsIdempotent_WhenTheSameMappingIsInsertedTwice()
    {
        var ingredient = await CreateIngredientAsync("Idempotent mapping test ingredient");
        var product = await CreateProductAsync("Idempotent mapping test product");

        await _productIngredientService.InsertProductIngredientAsync(new ProductIngredientMapping { ProductId = product.Id, IngredientId = ingredient.Id });
        //check-then-insert at the service layer: inserting the identical mapping again must not create a
        //duplicate row (mirrors ProductController's RelatedProductAddPopup/FilterLevelValuesAddPopup
        //"does this mapping already exist" precedent, per the design's Invariants section)
        await _productIngredientService.InsertProductIngredientAsync(new ProductIngredientMapping { ProductId = product.Id, IngredientId = ingredient.Id });

        var mappings = await _productIngredientService.GetProductIngredientsByProductIdAsync(product.Id);

        //cleanup
        foreach (var mapping in mappings)
            await _productIngredientService.DeleteProductIngredientAsync(mapping);
        await _ingredientService.DeleteIngredientAsync(ingredient);
        await _productService.DeleteProductAsync(product);

        mappings.Should().ContainSingle();
    }

    [Test]
    public async Task GetDirectIngredientsByProductIdAsync_ReturnsASimpleAttachedIngredient()
    {
        var ingredient = await CreateIngredientAsync("Simple ingredient render test");
        var product = await CreateProductAsync("Simple render test product");

        var mapping = new ProductIngredientMapping { ProductId = product.Id, IngredientId = ingredient.Id };
        await _productIngredientService.InsertProductIngredientAsync(mapping);

        var directIngredients = await _productIngredientService.GetDirectIngredientsByProductIdAsync(product.Id);

        //cleanup
        await _productIngredientService.DeleteProductIngredientAsync(mapping);
        await _ingredientService.DeleteIngredientAsync(ingredient);
        await _productService.DeleteProductAsync(product);

        directIngredients.Select(i => i.Id).Should().Contain(ingredient.Id);
    }

    [Test]
    public async Task GetCompositionsReachableFromAsync_RendersNestingUpToTheMaximumDepth()
    {
        //broth (attached to the product) -> bones -> marrow -> essence: 3 composition edges
        var broth = await CreateIngredientAsync("Nesting test broth");
        var bones = await CreateIngredientAsync("Nesting test bones");
        var marrow = await CreateIngredientAsync("Nesting test marrow");
        var essence = await CreateIngredientAsync("Nesting test essence");

        await _ingredientCompositionService.AddChildIngredientAsync(broth.Id, bones.Id);
        await _ingredientCompositionService.AddChildIngredientAsync(bones.Id, marrow.Id);
        await _ingredientCompositionService.AddChildIngredientAsync(marrow.Id, essence.Id);

        var product = await CreateProductAsync("Nesting test product");
        var mapping = new ProductIngredientMapping { ProductId = product.Id, IngredientId = broth.Id };
        await _productIngredientService.InsertProductIngredientAsync(mapping);

        var rootIds = (await _productIngredientService.GetDirectIngredientsByProductIdAsync(product.Id))
            .Select(i => i.Id).ToList();
        var reachableEdges = await _productIngredientService.GetCompositionsReachableFromAsync(rootIds);

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

        reachableEdges.Should().Contain(edge => edge.ParentIngredientId == broth.Id && edge.ChildIngredientId == bones.Id);
        reachableEdges.Should().Contain(edge => edge.ParentIngredientId == bones.Id && edge.ChildIngredientId == marrow.Id);
        reachableEdges.Should().Contain(edge => edge.ParentIngredientId == marrow.Id && edge.ChildIngredientId == essence.Id);
    }

    [Test]
    public async Task GetCompositionsReachableFromAsync_ReflectsAnEditMadeToANestedIngredient_ForTheProductThatContainsIt()
    {
        var broth = await CreateIngredientAsync("Reachability edit test broth");
        var bones = await CreateIngredientAsync("Reachability edit test bones");
        await _ingredientCompositionService.AddChildIngredientAsync(broth.Id, bones.Id);

        var product = await CreateProductAsync("Reachability edit test product");
        var mapping = new ProductIngredientMapping { ProductId = product.Id, IngredientId = broth.Id };
        await _productIngredientService.InsertProductIngredientAsync(mapping);

        //edit the nested ingredient's own composition - "bones" sits one level under the product's root
        var marrow = await CreateIngredientAsync("Reachability edit test marrow");
        await _ingredientCompositionService.AddChildIngredientAsync(bones.Id, marrow.Id);

        var rootIds = (await _productIngredientService.GetDirectIngredientsByProductIdAsync(product.Id))
            .Select(i => i.Id).ToList();
        var reachableEdges = await _productIngredientService.GetCompositionsReachableFromAsync(rootIds);

        //cleanup
        await _productIngredientService.DeleteProductIngredientAsync(mapping);
        await _productService.DeleteProductAsync(product);
        await _ingredientCompositionService.RemoveChildIngredientAsync((await _ingredientCompositionService.GetChildCompositionsAsync(bones.Id)).Single());
        await _ingredientCompositionService.RemoveChildIngredientAsync((await _ingredientCompositionService.GetChildCompositionsAsync(broth.Id)).Single());
        await _ingredientService.DeleteIngredientAsync(marrow);
        await _ingredientService.DeleteIngredientAsync(bones);
        await _ingredientService.DeleteIngredientAsync(broth);

        reachableEdges.Should().Contain(edge => edge.ParentIngredientId == bones.Id && edge.ChildIngredientId == marrow.Id);
    }
}
