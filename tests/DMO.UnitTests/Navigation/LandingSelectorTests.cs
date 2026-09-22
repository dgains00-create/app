using DMO.Application.Access;
using DMO.Web.Frontend.Shell;
using DMO.Web.Navigation;

namespace DMO.UnitTests.Navigation;

/// <summary>
/// P1-T07 landing selector — pure selection over an already-projected, ordered destination
/// list. This file never collapses, orders, filters or fabricates destinations: those are
/// A2 projection responsibilities (verified by the A2 projection tests).
/// </summary>
public sealed class LandingSelectorTests
{
    [Fact]
    public void Select_ExplicitValidLanding_Wins_WhereverItSitsInTheOrder()
    {
        var controlo = Destination("controlo", "/implemented/controlo");
        var jobOn = Destination("job-on", "/implemented/job-on");

        // Explicit landing in first position.
        var first = LandingSelector.Select("controlo", [controlo, jobOn]);
        var firstResult = Assert.IsType<LandingSelection.ExplicitValid>(first);
        Assert.Same(controlo, firstResult.Destination);

        // Explicit landing in a later position — the selector does not reorder.
        var later = LandingSelector.Select("job-on", [controlo, jobOn]);
        var laterResult = Assert.IsType<LandingSelection.ExplicitValid>(later);
        Assert.Same(jobOn, laterResult.Destination);
    }

    [Fact]
    public void Select_NullExplicitLanding_ReturnsFirstDestinationInGivenOrder()
    {
        var controlo = Destination("controlo", "/implemented/controlo");
        var jobOn = Destination("job-on", "/implemented/job-on");

        var result = LandingSelector.Select(null, [controlo, jobOn]);

        var first = Assert.IsType<LandingSelection.FirstValid>(result);
        Assert.Same(controlo, first.Destination);
    }

    [Fact]
    public void Select_ZeroDestinations_ReturnsNoLanding()
    {
        var result = LandingSelector.Select(null, []);

        Assert.IsType<LandingSelection.NoLanding>(result);

        // Even an explicit landing cannot select anything from an empty list.
        var withExplicit = LandingSelector.Select("controlo", []);
        Assert.IsType<LandingSelection.NoLanding>(withExplicit);
    }

    [Fact]
    public void Select_ContextualOnlyComposition_ProjectsEmptyList_ReturnsNoLanding()
    {
        // A contextual-only composition is projected by the A2 service to zero live
        // destinations; at the selector surface that is exactly the empty case. The
        // contextual exclusion itself is A2 responsibility (proven by projection tests).
        var result = LandingSelector.Select(null, []);

        Assert.IsType<LandingSelection.NoLanding>(result);
    }

    [Fact]
    public void Select_InvalidExplicitLanding_IsReportedNeverChosen()
    {
        var controlo = Destination("controlo", "/implemented/controlo");

        // Explicit landing absent from the ordered list and a non-null list: the marker
        // state is reported; it is never silently replaced by the first destination.
        var result = LandingSelector.Select("nao-existe", [controlo]);

        Assert.IsType<LandingSelection.InvalidExplicitLanding>(result);
    }

    [Fact]
    public void Select_InvalidExplicitLanding_DistinctFromNullLandingBehavior()
    {
        var controlo = Destination("controlo", "/implemented/controlo");

        // The null case selects the first destination; the invalid-explicit case does NOT.
        var first = LandingSelector.Select(null, [controlo]);
        var invalid = LandingSelector.Select("nao-existe", [controlo]);

        Assert.IsType<LandingSelection.FirstValid>(first);
        Assert.IsType<LandingSelection.InvalidExplicitLanding>(invalid);
    }

    [Fact]
    public void Select_SharedDestination_CollapsedOnce_MatchesByDestinationId()
    {
        // The A2 projection collapses job-on-view + job-on-create into one job-on
        // destination (proven by projection tests); the selector consumes that collapsed
        // destination and matches the persisted landing id against it.
        var jobOn = new PrimaryDestinationPresentation(
            "job-on",
            "Job On",
            "/implemented/job-on",
            [ModuleCatalog.JobOnView, ModuleCatalog.JobOnCreate]);

        var result = LandingSelector.Select("job-on", [jobOn]);

        var valid = Assert.IsType<LandingSelection.ExplicitValid>(result);
        Assert.Same(jobOn, valid.Destination);
        Assert.Equal(2, valid.Destination.GrantedModuleIds.Count);
    }

    [Fact]
    public void Select_PreservesCallerOrder_FirstValidIsExactlyTheFirstElement()
    {
        var first = Destination("historia", "/implemented/historia");
        var second = Destination("tampoes", "/implemented/tampoes");
        var third = Destination("armazem", "/implemented/armazem");

        var result = LandingSelector.Select(null, [third, first, second]);

        var selected = Assert.IsType<LandingSelection.FirstValid>(result);
        Assert.Same(third, selected.Destination);

        // An explicit landing on a non-first element also returns that exact element.
        var explicitResult = LandingSelector.Select("tampoes", [third, first, second]);
        var explicitSelected = Assert.IsType<LandingSelection.ExplicitValid>(explicitResult);
        Assert.Same(second, explicitSelected.Destination);
    }

    private static PrimaryDestinationPresentation Destination(string id, string href) =>
        new(id, id, href, []);
}