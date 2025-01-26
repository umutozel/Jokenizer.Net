namespace Jokenizer.Net.Tests.Fixture;

public static class Extensions {

    public static int Len(this Company company) {
        return company.Name?.Length ?? 0;
    }
}
