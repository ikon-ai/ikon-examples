// Written by the ikon tool for an app that references Dapper.
// 'ikon build' and 'ikon update' rewrite this file; delete this line and the one above to keep your own edits.
// Npgsql reads an array column (text[], double precision[], real[], ...) as System.Array, which Dapper
// cannot pass to a record constructor that takes double[]. These handlers give Dapper the array type.

#pragma warning disable CA2255 // Dapper's handlers have to be registered before the app's first query

internal static class IkonDapperArrayHandlers
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register()
    {
        Dapper.SqlMapper.AddTypeHandler(new ArrayHandler<string>());
        Dapper.SqlMapper.AddTypeHandler(new ArrayHandler<double>());
        Dapper.SqlMapper.AddTypeHandler(new ArrayHandler<float>());
        Dapper.SqlMapper.AddTypeHandler(new ArrayHandler<int>());
        Dapper.SqlMapper.AddTypeHandler(new ArrayHandler<long>());
        Dapper.SqlMapper.AddTypeHandler(new ArrayHandler<System.Guid>());
        Dapper.SqlMapper.AddTypeHandler(new ArrayHandler<bool>());
        Dapper.SqlMapper.AddTypeHandler(new ArrayHandler<decimal>());
    }

    private sealed class ArrayHandler<T> : Dapper.SqlMapper.TypeHandler<T[]>
    {
        public override void SetValue(System.Data.IDbDataParameter parameter, T[]? value) =>
            parameter.Value = value is null ? System.DBNull.Value : value;

        public override T[]? Parse(object value) => (T[])value;
    }
}
