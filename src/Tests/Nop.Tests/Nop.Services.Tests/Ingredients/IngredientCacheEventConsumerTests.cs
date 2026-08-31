using AwesomeAssertions;
using Nop.Plugin.Misc.Ingredients.Domain;
using Nop.Plugin.Misc.Ingredients.Services;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.Ingredients;

[TestFixture]
public class IngredientCacheEventConsumerTests : ServiceTest
{
    private IIngredientService _ingredientService;

    [OneTimeSetUp]
    public void SetUp()
    {
        _ingredientService = GetService<IIngredientService>();
    }

    [Test]
    public async Task UpdatingAnIngredient_InvalidatesItsByIdCacheEntry()
    {
        var ingredient = new Ingredient { Name = "Cache test - original name" };
        await _ingredientService.InsertIngredientAsync(ingredient);

        //warm the by-id cache
        await _ingredientService.GetIngredientByIdAsync(ingredient.Id);

        //UpdateIngredientAsync publishes EntityUpdatedEvent<Ingredient> by default, which
        //IngredientCacheEventConsumer (CacheEventConsumer<Ingredient>) should react to by
        //invalidating the cached entry
        ingredient.Name = "Cache test - updated name";
        await _ingredientService.UpdateIngredientAsync(ingredient);

        var reloaded = await _ingredientService.GetIngredientByIdAsync(ingredient.Id);

        await _ingredientService.DeleteIngredientAsync(ingredient);

        reloaded.Name.Should().Be("Cache test - updated name");
    }
}
