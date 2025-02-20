using System;

namespace Jokenizer.Net.Tests.Fixture;

public static class Extensions {

    public static int Len(this Company company) {
        return company.Name?.Length ?? 0;
    }

    public static int LenProc(this Company company, Func<string?, int> func) {
        return func(company.Name);
    }

    public static int IdLen(this IEntity<Guid> entity) {
        return entity.Id.ToString().Length;
    }

    public static int IdProc<T>(this IEntity<T> entity, Func<T, int> func) {
        return func(entity.Id);
    }

    public static int NameLen(this EntityBase<Guid> entity) {
        return entity.Name?.Length ?? 0;
    }

    public static int NameProc<T>(this EntityBase<T> entity, Func<string?, int> func) {
        return func(entity.Name);
    }
}
