namespace Jokenizer.Net.Tests.Fixture {

    public static class Extensions {

        public static int Len(this Company company) {
            return company == null ? 0 : company.Name.Length;
        }
    }
}
