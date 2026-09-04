using AwesomeAssertions;
using Nop.Core;
using Nop.Plugin.Misc.Ingredients.Domain;
using Nop.Plugin.Misc.Ingredients.Services;
using NUnit.Framework;
using Npgsql;

namespace Nop.Tests.Nop.Services.Tests.Ingredients;

[TestFixture]
public class IngredientCompositionServiceTests : ServiceTest
{
    private IIngredientService _ingredientService;
    private IIngredientCompositionService _ingredientCompositionService;
    private IProductIngredientService _productIngredientService;

    [OneTimeSetUp]
    public void SetUp()
    {
        _ingredientService = GetService<IIngredientService>();
        _ingredientCompositionService = GetService<IIngredientCompositionService>();
        _productIngredientService = GetService<IProductIngredientService>();
    }

    private async Task<Ingredient> CreateAsync(string name)
    {
        var ingredient = new Ingredient { Name = name };
        await _ingredientService.InsertIngredientAsync(ingredient);

        return ingredient;
    }

    [Test]
    public async Task AddChildIngredientAsync_Throws_OnSelfLoop()
    {
        var ingredient = await CreateAsync("Self-loop test ingredient");

        try
        {
            Assert.ThrowsAsync<NopException>(async () =>
                await _ingredientCompositionService.AddChildIngredientAsync(ingredient.Id, ingredient.Id));
        }
        finally
        {
            //cleanup runs even if the assertion above fails (i.e. the code under test stopped throwing),
            //so a regression here can never leak a row into the shared test database
            await _ingredientService.DeleteIngredientAsync(ingredient);
        }
    }

    [Test]
    public async Task AddChildIngredientAsync_Throws_OnCycle()
    {
        var a = await CreateAsync("Cycle test A");
        var b = await CreateAsync("Cycle test B");

        await _ingredientCompositionService.AddChildIngredientAsync(a.Id, b.Id);

        try
        {
            //Concurrent cycle-creation (two transactions each adding one edge of the same would-be cycle)
            //is intentionally not exercised here: the SQLite test double behind ServiceTest has no true
            //serializable-isolation semantics, so a Task.WhenAll-based race against it would prove
            //something about SQLite's own locking, not about this business rule - and the full-closure-
            //rewrite-on-every-write design (see RecomputeClosureAsync) means any two concurrent composition
            //writes would conflict, not something specific to cycles. See "Cycle prevention" in
            //Docs/BusinessLogic/product-ingredients.md for the full reasoning. This test covers the
            //validation logic itself, sequentially, which is what's actually being asserted here.
            Assert.ThrowsAsync<NopException>(async () =>
                await _ingredientCompositionService.AddChildIngredientAsync(b.Id, a.Id));
        }
        finally
        {
            var edge = (await _ingredientCompositionService.GetChildCompositionsAsync(a.Id)).Single();
            await _ingredientCompositionService.RemoveChildIngredientAsync(edge);
            await _ingredientService.DeleteIngredientAsync(a);
            await _ingredientService.DeleteIngredientAsync(b);
        }
    }

    [Test]
    public async Task AddChildIngredientAsync_Allows_ExactlyTheMaximumDepth()
    {
        //level0 -> level1 -> level2 -> level3: 3 composition edges, exactly at the ceiling
        var level0 = await CreateAsync("Depth test L0");
        var level1 = await CreateAsync("Depth test L1");
        var level2 = await CreateAsync("Depth test L2");
        var level3 = await CreateAsync("Depth test L3");

        await _ingredientCompositionService.AddChildIngredientAsync(level0.Id, level1.Id);
        await _ingredientCompositionService.AddChildIngredientAsync(level1.Id, level2.Id);
        await _ingredientCompositionService.AddChildIngredientAsync(level2.Id, level3.Id);

        var reachable = await _productIngredientService.GetCompositionsReachableFromAsync(new List<int> { level0.Id });

        //cleanup
        await _ingredientCompositionService.RemoveChildIngredientAsync((await _ingredientCompositionService.GetChildCompositionsAsync(level2.Id)).Single());
        await _ingredientCompositionService.RemoveChildIngredientAsync((await _ingredientCompositionService.GetChildCompositionsAsync(level1.Id)).Single());
        await _ingredientCompositionService.RemoveChildIngredientAsync((await _ingredientCompositionService.GetChildCompositionsAsync(level0.Id)).Single());
        await _ingredientService.DeleteIngredientAsync(level3);
        await _ingredientService.DeleteIngredientAsync(level2);
        await _ingredientService.DeleteIngredientAsync(level1);
        await _ingredientService.DeleteIngredientAsync(level0);

        reachable.Select(edge => edge.ChildIngredientId).Should().Contain(level3.Id);
    }

    [Test]
    public async Task AddChildIngredientAsync_Throws_WhenTheEdgeWouldExceedTheMaximumDepth()
    {
        //level0 -> level1 -> level2 -> level3: already at the 3-edge ceiling
        var level0 = await CreateAsync("Over-depth test L0");
        var level1 = await CreateAsync("Over-depth test L1");
        var level2 = await CreateAsync("Over-depth test L2");
        var level3 = await CreateAsync("Over-depth test L3");
        var level4 = await CreateAsync("Over-depth test L4");

        await _ingredientCompositionService.AddChildIngredientAsync(level0.Id, level1.Id);
        await _ingredientCompositionService.AddChildIngredientAsync(level1.Id, level2.Id);
        await _ingredientCompositionService.AddChildIngredientAsync(level2.Id, level3.Id);

        try
        {
            //level0 -> ... -> level4 would be a 4-edge path, over the ceiling
            Assert.ThrowsAsync<NopException>(async () =>
                await _ingredientCompositionService.AddChildIngredientAsync(level3.Id, level4.Id));
        }
        finally
        {
            await _ingredientCompositionService.RemoveChildIngredientAsync((await _ingredientCompositionService.GetChildCompositionsAsync(level2.Id)).Single());
            await _ingredientCompositionService.RemoveChildIngredientAsync((await _ingredientCompositionService.GetChildCompositionsAsync(level1.Id)).Single());
            await _ingredientCompositionService.RemoveChildIngredientAsync((await _ingredientCompositionService.GetChildCompositionsAsync(level0.Id)).Single());
            await _ingredientService.DeleteIngredientAsync(level4);
            await _ingredientService.DeleteIngredientAsync(level3);
            await _ingredientService.DeleteIngredientAsync(level2);
            await _ingredientService.DeleteIngredientAsync(level1);
            await _ingredientService.DeleteIngredientAsync(level0);
        }
    }

    [Test]
    public async Task AddChildIngredientAsync_IsIdempotent_WhenTheSameEdgeIsAddedTwice()
    {
        var parent = await CreateAsync("Idempotent add test parent");
        var child = await CreateAsync("Idempotent add test child");

        await _ingredientCompositionService.AddChildIngredientAsync(parent.Id, child.Id);
        //check-then-insert at the service layer: adding the identical edge again must not create a
        //duplicate row (mirrors ProductController's RelatedProductAddPopup/FilterLevelValuesAddPopup
        //"does this mapping already exist" precedent, per the design's Invariants section)
        await _ingredientCompositionService.AddChildIngredientAsync(parent.Id, child.Id);

        var children = await _ingredientCompositionService.GetChildCompositionsAsync(parent.Id);

        //cleanup
        await _ingredientCompositionService.RemoveChildIngredientAsync(children.Single());
        await _ingredientService.DeleteIngredientAsync(child);
        await _ingredientService.DeleteIngredientAsync(parent);

        children.Should().ContainSingle();
    }

    [Test]
    public async Task RemoveChildIngredientAsync_RecomputesTheClosure()
    {
        var parent = await CreateAsync("Remove test parent");
        var child = await CreateAsync("Remove test child");

        await _ingredientCompositionService.AddChildIngredientAsync(parent.Id, child.Id);

        var edge = (await _ingredientCompositionService.GetChildCompositionsAsync(parent.Id)).Single();
        await _ingredientCompositionService.RemoveChildIngredientAsync(edge);

        var reachableAfterRemoval = await _productIngredientService.GetCompositionsReachableFromAsync(new List<int> { parent.Id });

        await _ingredientService.DeleteIngredientAsync(child);
        await _ingredientService.DeleteIngredientAsync(parent);

        reachableAfterRemoval.Should().BeEmpty();
    }

    [Test]
    public async Task GetCompositeIngredientIdsAsync_ReturnsOnlyIngredientsWithAtLeastOneDirectChild()
    {
        var parent = await CreateAsync("Composite check test parent");
        var child = await CreateAsync("Composite check test child");
        var leaf = await CreateAsync("Composite check test leaf");

        await _ingredientCompositionService.AddChildIngredientAsync(parent.Id, child.Id);

        var compositeIds = await _ingredientCompositionService.GetCompositeIngredientIdsAsync(
            new[] { parent.Id, child.Id, leaf.Id });

        //cleanup
        await _ingredientCompositionService.RemoveChildIngredientAsync(
            (await _ingredientCompositionService.GetChildCompositionsAsync(parent.Id)).Single());
        await _ingredientService.DeleteIngredientAsync(leaf);
        await _ingredientService.DeleteIngredientAsync(child);
        await _ingredientService.DeleteIngredientAsync(parent);

        compositeIds.Should().ContainSingle().Which.Should().Be(parent.Id);
    }

    [Test]
    public async Task GetCompositeIngredientIdsAsync_ReturnsEmpty_WhenGivenNoIngredientIds()
    {
        var compositeIds = await _ingredientCompositionService.GetCompositeIngredientIdsAsync(Array.Empty<int>());

        compositeIds.Should().BeEmpty();
    }

    [Test]
    public void IsSerializationFailure_ReturnsTrue_ForASerializationFailurePostgresException()
    {
        var exception = new PostgresException("could not serialize access due to concurrent update",
            "ERROR", "ERROR", PostgresErrorCodes.SerializationFailure);

        IngredientCompositionService.IsSerializationFailure(exception).Should().BeTrue();
    }

    [Test]
    public void IsSerializationFailure_ReturnsFalse_ForAPostgresExceptionWithADifferentSqlState()
    {
        var exception = new PostgresException("unique constraint violated", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation);

        IngredientCompositionService.IsSerializationFailure(exception).Should().BeFalse();
    }

    [Test]
    public void IsSerializationFailure_ReturnsFalse_ForAnUnrelatedException()
    {
        IngredientCompositionService.IsSerializationFailure(new NopException("unrelated failure")).Should().BeFalse();
    }

    [Test]
    public void IsSerializationFailure_ReturnsTrue_WhenTheSerializationFailureIsWrappedInAnInnerException()
    {
        var postgresException = new PostgresException("could not serialize access due to concurrent update",
            "ERROR", "ERROR", PostgresErrorCodes.SerializationFailure);
        var wrapper = new InvalidOperationException("wrapped", postgresException);

        IngredientCompositionService.IsSerializationFailure(wrapper).Should().BeTrue();
    }
}
