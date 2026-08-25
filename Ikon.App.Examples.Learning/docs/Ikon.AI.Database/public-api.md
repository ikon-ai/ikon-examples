# Ikon.AI.Database Public API

namespace Ikon.AI.Database
  // Reached via AI's DatabaseConnection.BigQuery(projectId, dataset) factory in normal use. Construction is cheap and side-effect-free, per the ADO.NET contract; it does no credential or network work. Authentication is ambient and resolved at Open: a Google access token is read from CredentialStorage (the IKON_GOOGLE_BIGQUERY_ACCESS_TOKEN environment variable, falling back to IKON_GOOGLE_ACCESS_TOKEN) — a missing token throws from Open. Parameterized queries are not supported; the reader materializes the full result set in memory.
  sealed class BigQueryDbConnection : DbConnection
    ctor(string projectId, string datasetId)
    // Accepted for DbConnection compatibility but INERT: the connection targets the constructor's projectId/datasetId and authenticates from an ambient Google access token, so setting this changes nothing. The getter returns an empty string (never null) before it is set, per the ADO.NET contract.
    override string ConnectionString { get; set; }
    override string DataSource { get; }
    override string Database { get; }
    // Returns the version of the Google Cloud BigQuery client library this connection uses. BigQuery is a managed, serverless service that exposes no queryable server version, so the client library version stands in for it honestly rather than a fixed product name. Throws InvalidOperationException when the connection is not open, per the DbConnection contract.
    override string ServerVersion { get; }
    override ConnectionState State { get; }
    override void ChangeDatabase(string databaseName)
    override void Close()
    // Returns BigQuery metadata as a DataTable. Supported collection names (case-insensitive): MetaDataCollections, Schemas/Datasets, Tables (restrictions: datasetId), Columns (restrictions: datasetId, tableId — both required). The no-arg overload returns Tables. Any other collection name throws ArgumentException; call this only while the connection is open.
    override DataTable GetSchema()
    override DataTable GetSchema(string collectionName)
    override DataTable GetSchema(string collectionName, string?[]? restrictionValues)
    override void Open()
