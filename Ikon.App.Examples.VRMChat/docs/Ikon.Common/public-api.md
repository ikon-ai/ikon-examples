# Ikon.Common Public API

namespace Ikon.Common
  enum FileScanner.FileEventArgs.ActionType
    Added
    Changed
    Deleted
  class AppProjectConfig.ActivationConfig : ITomlMetadataProvider
    ctor()
    bool StopSessions { get; set; }
  class AppBundleConfig.ActivationConfig
    ctor()
    bool StopSessions { get; set; }
  class AppBundleConfig
    ctor()
    AppBundleConfig.ActivationConfig Activation { get; set; }
    AppBundleConfig.AuthConfig Auth { get; set; }
    // Per-cell entry points discovered in the bundle. The cloud uses this list to: (a) recognise URLs of the form /api/{CellType}/{path} and route them to a cell-instance (provisioned the same way an app-instance is, just with AppInitializationArgs.RunTarget = "{CellType}" instead of null); (b) hash the request's identity query params against the cell's IdentityFields to find-or-create the right channel-instance for that cell identity. Empty for apps without cells, or for cells whose ProcessScope is AppProcess (in-process — no separate cell-instance needed). See docs/private/endpoint-and-cell-architecture.md.
    List<AppBundleConfig.CellEntry> Cells { get; set; }
    string ChannelId { get; set; }
    string CreatedAt { get; set; }
    List<AppBundleConfig.DatabaseEntry> Databases { get; set; }
    List<AppBundleConfig.EmailTemplate> EmailTemplates { get; set; }
    List<AppBundleConfig.EndpointDescriptor> Endpoints { get; set; }
    string Hash { get; set; }
    string Name { get; set; }
    string OrganisationId { get; set; }
    List<AppBundleConfig.Pipeline> Pipelines { get; set; }
    List<string> SessionIdentityKeys { get; set; }
    string SpaceId { get; set; }
    string Version { get; set; }
    static string ConfigFileName
  class AppBundleConfigLegacy
    ctor()
    string AppTypeName { get; set; }
    string CreatedAt { get; set; }
    string DllName { get; set; }
    string Hash { get; set; }
    string Version { get; set; }
    static string ConfigFileName
  class AppBundleRuntimeConfig
    ctor()
    string AppTypeName { get; set; }
    string DllName { get; set; }
    static string ConfigFileName
  sealed class AppProjectUtils.AppDiscoveryResult
    ctor()
    bool Found { get; init; }
    string? TypeName { get; init; }
  class AppProjectConfig : ITomlMetadataProvider
    ctor()
    AppProjectConfig.ActivationConfig Activation { get; set; }
    AppProjectConfig.AuthConfig Auth { get; set; }
    List<string> Databases { get; set; }
    AppProjectConfig.TargetConfig Target { get; set; }
    static AppProjectConfig FromToml(string tomlContent)
    static string GetConfigFileName(IkonBackend.EnvironmentType environment)
    static string GetConfigFileName(string targetName)
    static string ConfigFileName
  class AppProjectConfigLegacy : ITomlMetadataProvider
    ctor()
    AppProjectConfig.ActivationConfig Activation { get; set; }
    AppProjectConfig.AuthConfig Auth { get; set; }
    List<string> Databases { get; set; }
    Dictionary<string, AppProjectConfigLegacy.Target> Targets { get; set; }
  static class AppProjectUtils
    static IEnumerable<string> EnumerateCsprojFiles(string rootDirectory, int maxDepth = 3)
    static AppProjectUtils.AppDiscoveryResult FindAppTypeInAssembly(string dllPath)
    static string FindBestProjectFilePath(string targetDirectory)
    // Finds the platform-dotnet directory by searching for ikon-platform.slnx. At each ancestor of the start directory, checks two containment candidates: 1. current/ikon-platform.slnx (current is platform-dotnet itself) 2. current/platform-dotnet/ikon-platform.slnx (current is the repo root) With includeSibling = true, also checks a third candidate: 3. current/ikon-platform/platform-dotnet/ikon-platform.slnx (current is a parent of a sibling ikon-platform/ checkout) Sibling matching is opt-in because it answers a different question — "is there a platform-dotnet anywhere nearby?" rather than "is startDirectory inside platform-dotnet?". Callers that decide whether to mutate the platform repo (e.g. add a new app to ikon-platform.slnx) must keep it off; the codegen lookup for external apps with a sibling checkout opts in.
    static string? FindIkonPlatformDotnetRoot(string startDirectory, bool includeSibling = false)
    // Finds the ikon-platform repo root (the parent of platform-dotnet).
    static string? FindIkonPlatformRepoRoot(string startDirectory)
    // Finds the ikon-platform.slnx file path by searching in the start directory and parent directories.
    static string? FindIkonPlatformSlnx(string startDirectory)
    // Generates tsconfig.paths.json in the frontend-node directory with appropriate TypeScript paths. Auto-detects internal/external mode based on whether the directory is inside ikon-platform. Internal mode: generates paths pointing to monorepo source files for IDE support. External mode: generates empty paths (uses node_modules).
    static Task GenerateTsconfigPathsJsonAsync(string frontendNodeDirectory)
    static AppProjectVariables GetAppProjectVars(string? targetDirectory)
    static string GetAssemblyNameFromCsproj(string csprojPath)
    static Type[] GetTypesSafely(Assembly assembly)
    static bool HasIkonAppAttribute(Type type)
    // Returns true if the given directory is inside the ikon-platform monorepo.
    static bool IsInsideIkonPlatform(string directory)
    static bool IsLegacyConfig(string tomlContent)
    static AppProjectConfig MigrateFromLegacy(string tomlContent, IkonBackend.EnvironmentType environment)
    static int ScoreProjectFile(string csprojPath)
    static string StripIkonAppPrefix(string projectName)
  sealed class AppProjectVariables
    ctor()
    string ConfigFilePath { get; init; }
    string CsProjectFilePath { get; init; }
    string CsProjectName { get; init; }
    string FrontendFlutterDirectory { get; init; }
    string FrontendNodeDirectory { get; init; }
    string GitRootDirectory { get; init; }
    string ProjectDirectory { get; init; }
    string ProjectName { get; init; }
    string RelativeConfigFilePath { get; init; }
    string RelativeCsProjectFilePath { get; init; }
    string RelativeRootDirectory { get; init; }
    string RootDirectory { get; init; }
    string TargetDirectory { get; init; }
  class AsyncLocalInstances
    void Capture(object owner, bool allowOverride = false)
    void InitializeAll()
    void Remove(object owner)
    void Restore(object owner)
    bool TryRestore(object owner)
    static AsyncLocalInstances Instance
  class AppProjectConfig.AuthConfig : ITomlMetadataProvider
    ctor()
    List<string> DomainAllowlist { get; set; }
    bool Enabled { get; set; }
    List<string> Methods { get; set; }
  class AppBundleConfig.AuthConfig
    ctor()
    List<string> DomainAllowlist { get; set; }
    bool Enabled { get; set; }
    List<string> Methods { get; set; }
  class AppBundleConfig.CellEntry
    ctor()
    string DllName { get; set; }
    string FullTypeName { get; set; }
    List<AppBundleConfig.CellHttpEndpoint> HttpEndpoints { get; set; }
    List<AppBundleConfig.CellIdentityField> IdentityFields { get; set; }
    List<AppBundleConfig.CellMcpTool> McpTools { get; set; }
    string ProcessScope { get; set; }
    string TypeName { get; set; }
  class AppBundleConfig.CellHttpEndpoint
    ctor()
    string AuthCellName { get; set; }
    string HttpMethod { get; set; }
    string MethodName { get; set; }
    string Path { get; set; }
  class AppBundleConfig.CellIdentityField
    ctor()
    bool HasDefault { get; set; }
    string Name { get; set; }
    string TypeName { get; set; }
  class AppBundleConfig.CellMcpTool
    ctor()
    string Description { get; set; }
    string MethodName { get; set; }
    string ToolName { get; set; }
  struct CertificateStore.Certificate
    ctor(X509Certificate2 cert, X509Certificate2? rootCert, string certHash, string spkiHash, bool isDotnetDevCert)
    X509Certificate2 Cert { get; }
    string CertHash { get; }
    bool IsDotnetDevCert { get; }
    X509Certificate2? RootCert { get; }
    string SpkiHash { get; }
  static class CertificateStore
    static CertificateStore.Certificate GetCertificate(string host, X509Certificate2? rootCert = null, bool disableDotnetDevCerts = false)
  sealed class DatabaseConnectionInfo
    ctor()
    string ConnectionString { get; set; }
    string Name { get; set; }
    string Type { get; set; }
  class AppBundleConfig.DatabaseEntry
    ctor()
    string Name { get; set; }
    string Tier { get; set; }
    string Type { get; set; }
  class DescriptionAttribute : Attribute
    ctor(string description, object? example = null, RequiredStatus isRequired = Default, int minArrayItems = 0)
    string Description { get; }
    object? Example { get; }
    RequiredStatus IsRequired { get; }
    int MinArrayItems { get; }
  class AppBundleConfig.EmailTemplate
    ctor()
    string Name { get; set; }
    string Path { get; set; }
    string Subject { get; set; }
  class AppBundleConfig.EndpointDescriptor
    ctor()
    bool AutoRegistered { get; set; }
    bool IsMcpTool { get; set; }
    string Name { get; set; }
    string Owner { get; set; }
    string OwnerKind { get; set; }
    string? Path { get; set; }
  enum EndpointProtocol
    Tcp
    Tls
    Udp
  class ExponentialMovingAverage
    ctor(double smoothingFactor = 0.1)
    double CurrentValue { get; }
    double Update(double value)
    double UpdatePerElapsedTime(double value)
  class FileScanner.FileEventArgs : EventArgs
    ctor(string absolutePath, string relativePath, FileScanner.FileEventArgs.ActionType action)
    string AbsolutePath { get; }
    FileScanner.FileEventArgs.ActionType Action { get; }
    string RelativePath { get; }
    override string ToString()
  class FileScanner : IAsyncDisposable, IStorage
    ctor()
    int QueueSize { get; }
    Task DeleteAsync(AssetUri assetUri)
    ValueTask DisposeAsync()
    Task<bool> ExistsAsync(AssetUri assetUri)
    List<string> GetPaths(string link)
    Task<AssetContent<Stream>> GetReadStreamAsync(AssetUri assetUri)
    Task<Stream> GetWriteStreamAsync(AssetUri assetUri, AssetMetadata? metadata, CancellationToken cancellationToken)
    Task InitializeAsync(string name, string dataDirectory, bool scanZipFiles = true, bool start = true)
    Task<IReadOnlyList<AssetListingEntry>> ListAsync(AssetQuery query, CancellationToken cancellationToken)
    Stream OpenStream(string absoluteOrRelativePath)
    Task<string> ReadAllTextAsync(string absoluteOrRelativePath)
    Task StartAsync()
    Task StopAsync()
    Task<AssetMetadata?> TryGetMetadataAsync(AssetUri assetUri)
    Task WaitUntilQueueEmptyAsync()
    event Func<AssetEventArgs, Task> AssetEventAsync
    event Func<object, FileScanner.FileEventArgs, Task>? FileEvent
  class GenericListCache<T> : IGenericListCache<T>
    ctor()
    void Add(string hash, T item)
    void Clear()
    void Delete(string hash)
    List<string> DeleteOlderThan(TimeSpan timeSpan)
    List<T>? Get(string hash)
  static class HashUtils
    static byte[] ComputeMD5Bytes(byte[] value)
    static byte[] ComputeMD5Bytes(string value)
    static string ComputeMD5FromFile(string path)
    static string ComputeMD5FromStream(Stream stream)
    static string ComputeMD5String(byte[] value)
    static string ComputeMD5String(string value)
    static byte[] ComputeSHA256Bytes(byte[] value)
    static string ComputeSHA256FromFile(string path)
    static string ComputeSHA256FromStream(Stream stream)
    static string ComputeSHA256String(byte[] value)
    static string ComputeSHA256String(string value)
    static string ToHexString(byte[] data)
  interface IGenericListCache<T>
    abstract void Add(string hash, T item)
    abstract void Clear()
    abstract void Delete(string hash)
    abstract List<string> DeleteOlderThan(TimeSpan timeSpan)
    abstract List<T>? Get(string hash)
  sealed class IkonLoggerProvider : IDisposable, ILoggerProvider
    ctor()
    ILogger CreateLogger(string categoryName)
    void Dispose()
  class IkonProjectConfigLegacy
    ctor()
    string ChannelId { get; set; }
    string OrganisationId { get; set; }
    string ProjectId { get; set; }
    string SpaceId { get; set; }
    static string ConfigFileName
  class IkonProjectConfigLegacyPerEnv
    ctor()
    Dictionary<string, IkonProjectConfigLegacy> Environments { get; set; }
  static class IkonTaskExtensions
    // Intentionally does not await the task. Exceptions are observed and sent to onException .
    static void RunParallel(Task task, Action<Exception>? onException = null)
  sealed class InMemoryProtocolMessageChannel : IProtocolMessageChannel
    ctor()
    Context ClientContext { get; }
    static ValueTuple<InMemoryProtocolMessageChannel, InMemoryProtocolMessageChannel> CreateConnectedPair()
    IDisposable RegisterMessageHandler(Func<ProtocolMessage, ValueTask> handler, Opcode? opcodeGroupMask = null, Opcode[]? opcodes = null)
    ValueTask SendMessageAsync(ProtocolMessage message)
    ValueTask SendMessageAsync(IProtocolMessagePayload payload)
  sealed class AppProjectUtils.IsolatedLoadContext : AssemblyLoadContext, IDisposable
    ctor(string mainAssemblyPath)
    void Dispose()
  static class Markdown
    static string To<T>(T obj, bool includeFields = true, bool enumsAsNames = true, bool camelCase = false, bool includeNull = true, int maxDepth = 5)
  static class MimeTypes
    static void AddOrUpdate(string mime, string extension)
    static string GetExtensionFromMimeType(string mimeType)
    static string GetMimeTypeFromExtension(string extension)
    static string GetMimeTypeFromFilename(string fileName)
    static bool Is(string mimeType, string mimeTypeToCompare)
    static bool IsAudio(string mimeType)
    static bool IsBinary(string mimeType)
    static bool IsCsv(string mimeType)
    static bool IsImage(string mimeType)
    static bool IsJson(string mimeType)
    static bool IsMarkdown(string mimeType)
    static bool IsMicrosoftExcel(string mimeType)
    static bool IsMicrosoftPowerpoint(string mimeType)
    static bool IsMicrosoftWord(string mimeType)
    static bool IsNotes(string mimeType)
    static bool IsPdf(string mimeType)
    static bool IsText(string mimeType)
    static bool IsVideo(string mimeType)
    static bool IsXml(string mimeType)
    static bool IsZip(string mimeType)
    static bool TypeMatchesMimetype(string type, string mimeType)
    static string ApplicationAndrewInset
    static string ApplicationApplixware
    static string ApplicationAtomXml
    static string ApplicationAtomcatXml
    static string ApplicationAtomsvcXml
    static string ApplicationCcxmlXml
    static string ApplicationCdmiCapability
    static string ApplicationCdmiContainer
    static string ApplicationCdmiDomain
    static string ApplicationCdmiObject
    static string ApplicationCdmiQueue
    static string ApplicationCuSeeme
    static string ApplicationDavmountXml
    static string ApplicationDocbookXml
    static string ApplicationDsscDer
    static string ApplicationDsscXml
    static string ApplicationEcmascript
    static string ApplicationEmmaXml
    static string ApplicationEpubZip
    static string ApplicationExcel
    static string ApplicationExi
    static string ApplicationFontTdpfr
    static string ApplicationGmlXml
    static string ApplicationGpxXml
    static string ApplicationGxf
    static string ApplicationHyperstudio
    static string ApplicationInkmlXml
    static string ApplicationIpfix
    static string ApplicationJavaArchive
    static string ApplicationJavaSerializedObject
    static string ApplicationJavaVm
    static string ApplicationJavascript
    static string ApplicationJson
    static string ApplicationJsonmlJson
    static string ApplicationLostXml
    static string ApplicationMacBinhex40
    static string ApplicationMacCompactpro
    static string ApplicationMadsXml
    static string ApplicationMarc
    static string ApplicationMarcxmlXml
    static string ApplicationMathematica
    static string ApplicationMathmlXml
    static string ApplicationMbox
    static string ApplicationMediaservercontrolXml
    static string ApplicationMetalink4Xml
    static string ApplicationMetalinkXml
    static string ApplicationMetsXml
    static string ApplicationModsXml
    static string ApplicationMp21
    static string ApplicationMp4
    static string ApplicationMsword
    static string ApplicationMxf
    static string ApplicationOctetStream
    static string ApplicationOda
    static string ApplicationOebpsPackageXml
    static string ApplicationOgg
    static string ApplicationOmdocXml
    static string ApplicationOnenote
    static string ApplicationOxps
    static string ApplicationPatchOpsErrorXml
    static string ApplicationPdf
    static string ApplicationPgpEncrypted
    static string ApplicationPgpSignature
    static string ApplicationPicsRules
    static string ApplicationPkcs10
    static string ApplicationPkcs7Mime
    static string ApplicationPkcs7Signature
    static string ApplicationPkcs8
    static string ApplicationPkixAttrCert
    static string ApplicationPkixCert
    static string ApplicationPkixCrl
    static string ApplicationPkixPkipath
    static string ApplicationPkixcmp
    static string ApplicationPlsXml
    static string ApplicationPostscript
    static string ApplicationPrsCww
    static string ApplicationPskcXml
    static string ApplicationRdfXml
    static string ApplicationReginfoXml
    static string ApplicationRelaxNgCompactSyntax
    static string ApplicationResourceListsDiffXml
    static string ApplicationResourceListsXml
    static string ApplicationRlsServicesXml
    static string ApplicationRpkiGhostbusters
    static string ApplicationRpkiManifest
    static string ApplicationRpkiRoa
    static string ApplicationRsdXml
    static string ApplicationRssXml
    static string ApplicationRtf
    static string ApplicationSbmlXml
    static string ApplicationScvpCvRequest
    static string ApplicationScvpCvResponse
    static string ApplicationScvpVpRequest
    static string ApplicationScvpVpResponse
    static string ApplicationSdp
    static string ApplicationSetPaymentInitiation
    static string ApplicationSetRegistrationInitiation
    static string ApplicationShfXml
    static string ApplicationSmilXml
    static string ApplicationSparqlQuery
    static string ApplicationSparqlResultsXml
    static string ApplicationSql
    static string ApplicationSrgs
    static string ApplicationSrgsXml
    static string ApplicationSruXml
    static string ApplicationSsdlXml
    static string ApplicationSsmlXml
    static string ApplicationTeiXml
    static string ApplicationThraudXml
    static string ApplicationTimestampedData
    static string ApplicationVnd3gpp2Tcap
    static string ApplicationVnd3gppPicBwLarge
    static string ApplicationVnd3gppPicBwSmall
    static string ApplicationVnd3gppPicBwVar
    static string ApplicationVnd3mPostItNotes
    static string ApplicationVndAccpacSimplyAso
    static string ApplicationVndAccpacSimplyImp
    static string ApplicationVndAcucobol
    static string ApplicationVndAcucorp
    static string ApplicationVndAdobeAirApplicationInstallerPackageZip
    static string ApplicationVndAdobeFormscentralFcdt
    static string ApplicationVndAdobeFxp
    static string ApplicationVndAdobeXdpXml
    static string ApplicationVndAdobeXfdf
    static string ApplicationVndAheadSpace
    static string ApplicationVndAirzipFilesecureAzf
    static string ApplicationVndAirzipFilesecureAzs
    static string ApplicationVndAmazonEbook
    static string ApplicationVndAmericandynamicsAcc
    static string ApplicationVndAmigaAmi
    static string ApplicationVndAndroidPackageArchive
    static string ApplicationVndAnserWebCertificateIssueInitiation
    static string ApplicationVndAnserWebFundsTransferInitiation
    static string ApplicationVndAntixGameComponent
    static string ApplicationVndAppleInstallerXml
    static string ApplicationVndAppleMpegurl
    static string ApplicationVndAristanetworksSwi
    static string ApplicationVndAstraeaSoftwareIota
    static string ApplicationVndAudiograph
    static string ApplicationVndBlueiceMultipass
    static string ApplicationVndBmi
    static string ApplicationVndBusinessobjects
    static string ApplicationVndChemdrawXml
    static string ApplicationVndChipnutsKaraokeMmd
    static string ApplicationVndCinderella
    static string ApplicationVndClaymore
    static string ApplicationVndCloantoRp9
    static string ApplicationVndClonkC4group
    static string ApplicationVndCluetrustCartomobileConfig
    static string ApplicationVndCluetrustCartomobileConfigPkg
    static string ApplicationVndCommonspace
    static string ApplicationVndContactCmsg
    static string ApplicationVndCosmocaller
    static string ApplicationVndCrickClicker
    static string ApplicationVndCrickClickerKeyboard
    static string ApplicationVndCrickClickerPalette
    static string ApplicationVndCrickClickerTemplate
    static string ApplicationVndCrickClickerWordbank
    static string ApplicationVndCriticaltoolsWbsXml
    static string ApplicationVndCtcPosml
    static string ApplicationVndCupsPpd
    static string ApplicationVndCurlCar
    static string ApplicationVndCurlPcurl
    static string ApplicationVndDart
    static string ApplicationVndDataVisionRdz
    static string ApplicationVndDeceData
    static string ApplicationVndDeceTtmlXml
    static string ApplicationVndDeceUnspecified
    static string ApplicationVndDeceZip
    static string ApplicationVndDenovoFcselayoutLink
    static string ApplicationVndDna
    static string ApplicationVndDolbyMlp
    static string ApplicationVndDpgraph
    static string ApplicationVndDreamfactory
    static string ApplicationVndDsKeypoint
    static string ApplicationVndDvbAit
    static string ApplicationVndDvbService
    static string ApplicationVndDynageo
    static string ApplicationVndEcowinChart
    static string ApplicationVndEnliven
    static string ApplicationVndEpsonEsf
    static string ApplicationVndEpsonMsf
    static string ApplicationVndEpsonQuickanime
    static string ApplicationVndEpsonSalt
    static string ApplicationVndEpsonSsf
    static string ApplicationVndEszigno3Xml
    static string ApplicationVndEzpixAlbum
    static string ApplicationVndEzpixPackage
    static string ApplicationVndFdf
    static string ApplicationVndFdsnMseed
    static string ApplicationVndFdsnSeed
    static string ApplicationVndFlographit
    static string ApplicationVndFluxtimeClip
    static string ApplicationVndFramemaker
    static string ApplicationVndFrogansFnc
    static string ApplicationVndFrogansLtf
    static string ApplicationVndFscWeblaunch
    static string ApplicationVndFujitsuOasys
    static string ApplicationVndFujitsuOasys2
    static string ApplicationVndFujitsuOasys3
    static string ApplicationVndFujitsuOasysgp
    static string ApplicationVndFujitsuOasysprs
    static string ApplicationVndFujixeroxDdd
    static string ApplicationVndFujixeroxDocuworks
    static string ApplicationVndFujixeroxDocuworksBinder
    static string ApplicationVndFuzzysheet
    static string ApplicationVndGenomatixTuxedo
    static string ApplicationVndGeogebraFile
    static string ApplicationVndGeogebraTool
    static string ApplicationVndGeometryExplorer
    static string ApplicationVndGeonext
    static string ApplicationVndGeoplan
    static string ApplicationVndGeospace
    static string ApplicationVndGmx
    static string ApplicationVndGoogleEarthKmlXml
    static string ApplicationVndGoogleEarthKmz
    static string ApplicationVndGrafeq
    static string ApplicationVndGrooveAccount
    static string ApplicationVndGrooveHelp
    static string ApplicationVndGrooveIdentityMessage
    static string ApplicationVndGrooveInjector
    static string ApplicationVndGrooveToolMessage
    static string ApplicationVndGrooveToolTemplate
    static string ApplicationVndGrooveVcard
    static string ApplicationVndHalXml
    static string ApplicationVndHandheldEntertainmentXml
    static string ApplicationVndHbci
    static string ApplicationVndHheLessonPlayer
    static string ApplicationVndHpHpgl
    static string ApplicationVndHpHpid
    static string ApplicationVndHpHps
    static string ApplicationVndHpJlyt
    static string ApplicationVndHpPcl
    static string ApplicationVndHpPclxl
    static string ApplicationVndHydrostatixSofData
    static string ApplicationVndIbmMinipay
    static string ApplicationVndIbmModcap
    static string ApplicationVndIbmRightsManagement
    static string ApplicationVndIbmSecureContainer
    static string ApplicationVndIccprofile
    static string ApplicationVndIgloader
    static string ApplicationVndImmervisionIvp
    static string ApplicationVndImmervisionIvu
    static string ApplicationVndInsorsIgm
    static string ApplicationVndInterconFormnet
    static string ApplicationVndIntergeo
    static string ApplicationVndIntuQbo
    static string ApplicationVndIntuQfx
    static string ApplicationVndIpunpluggedRcprofile
    static string ApplicationVndIrepositoryPackageXml
    static string ApplicationVndIsXpr
    static string ApplicationVndIsacFcs
    static string ApplicationVndJam
    static string ApplicationVndJcpJavameMidletRms
    static string ApplicationVndJisp
    static string ApplicationVndJoostJodaArchive
    static string ApplicationVndKahootz
    static string ApplicationVndKdeKarbon
    static string ApplicationVndKdeKchart
    static string ApplicationVndKdeKformula
    static string ApplicationVndKdeKivio
    static string ApplicationVndKdeKontour
    static string ApplicationVndKdeKpresenter
    static string ApplicationVndKdeKspread
    static string ApplicationVndKdeKword
    static string ApplicationVndKenameaapp
    static string ApplicationVndKidspiration
    static string ApplicationVndKinar
    static string ApplicationVndKoan
    static string ApplicationVndKodakDescriptor
    static string ApplicationVndLasLasXml
    static string ApplicationVndLlamagraphicsLifeBalanceDesktop
    static string ApplicationVndLlamagraphicsLifeBalanceExchangeXml
    static string ApplicationVndLotus123
    static string ApplicationVndLotusApproach
    static string ApplicationVndLotusFreelance
    static string ApplicationVndLotusNotes
    static string ApplicationVndLotusOrganizer
    static string ApplicationVndLotusScreencam
    static string ApplicationVndLotusWordpro
    static string ApplicationVndMacportsPortpkg
    static string ApplicationVndMcd
    static string ApplicationVndMedcalcdata
    static string ApplicationVndMediastationCdkey
    static string ApplicationVndMfer
    static string ApplicationVndMfmp
    static string ApplicationVndMicrografxFlo
    static string ApplicationVndMicrografxIgx
    static string ApplicationVndMif
    static string ApplicationVndMobiusDaf
    static string ApplicationVndMobiusDis
    static string ApplicationVndMobiusMbk
    static string ApplicationVndMobiusMqy
    static string ApplicationVndMobiusMsl
    static string ApplicationVndMobiusPlc
    static string ApplicationVndMobiusTxf
    static string ApplicationVndMophunApplication
    static string ApplicationVndMophunCertificate
    static string ApplicationVndMozillaXulXml
    static string ApplicationVndMsArtgalry
    static string ApplicationVndMsCabCompressed
    static string ApplicationVndMsExcel
    static string ApplicationVndMsExcelAddinMacroenabled12
    static string ApplicationVndMsExcelSheetBinaryMacroenabled12
    static string ApplicationVndMsExcelSheetMacroenabled12
    static string ApplicationVndMsExcelTemplateMacroenabled12
    static string ApplicationVndMsFontobject
    static string ApplicationVndMsHtmlhelp
    static string ApplicationVndMsIms
    static string ApplicationVndMsLrm
    static string ApplicationVndMsOfficetheme
    static string ApplicationVndMsPkiSeccat
    static string ApplicationVndMsPkiStl
    static string ApplicationVndMsPowerpoint
    static string ApplicationVndMsPowerpointAddinMacroenabled12
    static string ApplicationVndMsPowerpointPresentationMacroenabled12
    static string ApplicationVndMsPowerpointSlideMacroenabled12
    static string ApplicationVndMsPowerpointSlideshowMacroenabled12
    static string ApplicationVndMsPowerpointTemplateMacroenabled12
    static string ApplicationVndMsProject
    static string ApplicationVndMsWordDocumentMacroenabled12
    static string ApplicationVndMsWordTemplateMacroenabled12
    static string ApplicationVndMsWorks
    static string ApplicationVndMsWpl
    static string ApplicationVndMsXpsdocument
    static string ApplicationVndMseq
    static string ApplicationVndMusician
    static string ApplicationVndMuveeStyle
    static string ApplicationVndMynfc
    static string ApplicationVndNeurolanguageNlu
    static string ApplicationVndNitf
    static string ApplicationVndNoblenetDirectory
    static string ApplicationVndNoblenetSealer
    static string ApplicationVndNoblenetWeb
    static string ApplicationVndNokiaNGageData
    static string ApplicationVndNokiaNGageSymbianInstall
    static string ApplicationVndNokiaRadioPreset
    static string ApplicationVndNokiaRadioPresets
    static string ApplicationVndNovadigmEdm
    static string ApplicationVndNovadigmEdx
    static string ApplicationVndNovadigmExt
    static string ApplicationVndOasisOpendocumentChart
    static string ApplicationVndOasisOpendocumentChartTemplate
    static string ApplicationVndOasisOpendocumentDatabase
    static string ApplicationVndOasisOpendocumentFormula
    static string ApplicationVndOasisOpendocumentFormulaTemplate
    static string ApplicationVndOasisOpendocumentGraphics
    static string ApplicationVndOasisOpendocumentGraphicsTemplate
    static string ApplicationVndOasisOpendocumentImage
    static string ApplicationVndOasisOpendocumentImageTemplate
    static string ApplicationVndOasisOpendocumentPresentation
    static string ApplicationVndOasisOpendocumentPresentationTemplate
    static string ApplicationVndOasisOpendocumentSpreadsheet
    static string ApplicationVndOasisOpendocumentSpreadsheetTemplate
    static string ApplicationVndOasisOpendocumentText
    static string ApplicationVndOasisOpendocumentTextMaster
    static string ApplicationVndOasisOpendocumentTextTemplate
    static string ApplicationVndOasisOpendocumentTextWeb
    static string ApplicationVndOlpcSugar
    static string ApplicationVndOmaDd2Xml
    static string ApplicationVndOpenofficeorgExtension
    static string ApplicationVndOpenxmlformatsOfficedocumentPresentationmlPresentation
    static string ApplicationVndOpenxmlformatsOfficedocumentPresentationmlSlide
    static string ApplicationVndOpenxmlformatsOfficedocumentPresentationmlSlideshow
    static string ApplicationVndOpenxmlformatsOfficedocumentPresentationmlTemplate
    static string ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlSheet
    static string ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlTemplate
    static string ApplicationVndOpenxmlformatsOfficedocumentWordprocessingmlDocument
    static string ApplicationVndOpenxmlformatsOfficedocumentWordprocessingmlTemplate
    static string ApplicationVndOsgeoMapguidePackage
    static string ApplicationVndOsgiDp
    static string ApplicationVndOsgiSubsystem
    static string ApplicationVndPalm
    static string ApplicationVndPawaafile
    static string ApplicationVndPgFormat
    static string ApplicationVndPgOsasli
    static string ApplicationVndPicsel
    static string ApplicationVndPmiWidget
    static string ApplicationVndPocketlearn
    static string ApplicationVndPowerbuilder6
    static string ApplicationVndPreviewsystemsBox
    static string ApplicationVndProteusMagazine
    static string ApplicationVndPublishareDeltaTree
    static string ApplicationVndPviPtid1
    static string ApplicationVndQuarkQuarkxpress
    static string ApplicationVndRealvncBed
    static string ApplicationVndRecordareMusicxml
    static string ApplicationVndRecordareMusicxmlXml
    static string ApplicationVndRigCryptonote
    static string ApplicationVndRimCod
    static string ApplicationVndRnRealmedia
    static string ApplicationVndRnRealmediaVbr
    static string ApplicationVndRoute66Link66Xml
    static string ApplicationVndSailingtrackerTrack
    static string ApplicationVndSeemail
    static string ApplicationVndSema
    static string ApplicationVndSemd
    static string ApplicationVndSemf
    static string ApplicationVndShanaInformedFormdata
    static string ApplicationVndShanaInformedFormtemplate
    static string ApplicationVndShanaInformedInterchange
    static string ApplicationVndShanaInformedPackage
    static string ApplicationVndSimtechMindmapper
    static string ApplicationVndSmaf
    static string ApplicationVndSmartTeacher
    static string ApplicationVndSolentSdkmXml
    static string ApplicationVndSpotfireDxp
    static string ApplicationVndSpotfireSfs
    static string ApplicationVndStardivisionCalc
    static string ApplicationVndStardivisionDraw
    static string ApplicationVndStardivisionImpress
    static string ApplicationVndStardivisionMath
    static string ApplicationVndStardivisionWriter
    static string ApplicationVndStardivisionWriterGlobal
    static string ApplicationVndStepmaniaPackage
    static string ApplicationVndStepmaniaStepchart
    static string ApplicationVndSunXmlCalc
    static string ApplicationVndSunXmlCalcTemplate
    static string ApplicationVndSunXmlDraw
    static string ApplicationVndSunXmlDrawTemplate
    static string ApplicationVndSunXmlImpress
    static string ApplicationVndSunXmlImpressTemplate
    static string ApplicationVndSunXmlMath
    static string ApplicationVndSunXmlWriter
    static string ApplicationVndSunXmlWriterGlobal
    static string ApplicationVndSunXmlWriterTemplate
    static string ApplicationVndSusCalendar
    static string ApplicationVndSvd
    static string ApplicationVndSymbianInstall
    static string ApplicationVndSyncmlDmWbxml
    static string ApplicationVndSyncmlDmXml
    static string ApplicationVndSyncmlXml
    static string ApplicationVndTaoIntentModuleArchive
    static string ApplicationVndTcpdumpPcap
    static string ApplicationVndTmobileLivetv
    static string ApplicationVndTridTpt
    static string ApplicationVndTriscapeMxs
    static string ApplicationVndTrueapp
    static string ApplicationVndUfdl
    static string ApplicationVndUiqTheme
    static string ApplicationVndUmajin
    static string ApplicationVndUnity
    static string ApplicationVndUomlXml
    static string ApplicationVndVcx
    static string ApplicationVndVisio
    static string ApplicationVndVisionary
    static string ApplicationVndVsf
    static string ApplicationVndWapWbxml
    static string ApplicationVndWapWmlc
    static string ApplicationVndWapWmlscriptc
    static string ApplicationVndWebturbo
    static string ApplicationVndWolframPlayer
    static string ApplicationVndWordperfect
    static string ApplicationVndWqd
    static string ApplicationVndWtStf
    static string ApplicationVndXara
    static string ApplicationVndXfdl
    static string ApplicationVndYamahaHvDic
    static string ApplicationVndYamahaHvScript
    static string ApplicationVndYamahaHvVoice
    static string ApplicationVndYamahaOpenscoreformat
    static string ApplicationVndYamahaOpenscoreformatOsfpvgXml
    static string ApplicationVndYamahaSmafAudio
    static string ApplicationVndYamahaSmafPhrase
    static string ApplicationVndYellowriverCustomMenu
    static string ApplicationVndZul
    static string ApplicationVndZzazzDeckXml
    static string ApplicationVoicexmlXml
    static string ApplicationWidget
    static string ApplicationWinhlp
    static string ApplicationWsdlXml
    static string ApplicationWspolicyXml
    static string ApplicationX7zCompressed
    static string ApplicationXAbiword
    static string ApplicationXAceCompressed
    static string ApplicationXAppleDiskimage
    static string ApplicationXAuthorwareBin
    static string ApplicationXAuthorwareMap
    static string ApplicationXAuthorwareSeg
    static string ApplicationXBcpio
    static string ApplicationXBittorrent
    static string ApplicationXBlorb
    static string ApplicationXBzip
    static string ApplicationXBzip2
    static string ApplicationXCbr
    static string ApplicationXCdlink
    static string ApplicationXCfsCompressed
    static string ApplicationXChat
    static string ApplicationXChessPgn
    static string ApplicationXConference
    static string ApplicationXCpio
    static string ApplicationXCsh
    static string ApplicationXDebianPackage
    static string ApplicationXDgcCompressed
    static string ApplicationXDirector
    static string ApplicationXDoom
    static string ApplicationXDtbncxXml
    static string ApplicationXDtbookXml
    static string ApplicationXDtbresourceXml
    static string ApplicationXDvi
    static string ApplicationXEnvoy
    static string ApplicationXEva
    static string ApplicationXFontBdf
    static string ApplicationXFontGhostscript
    static string ApplicationXFontLinuxPsf
    static string ApplicationXFontPcf
    static string ApplicationXFontSnf
    static string ApplicationXFontType1
    static string ApplicationXFreearc
    static string ApplicationXFuturesplash
    static string ApplicationXGcaCompressed
    static string ApplicationXGlulx
    static string ApplicationXGnumeric
    static string ApplicationXGrampsXml
    static string ApplicationXGtar
    static string ApplicationXHdf
    static string ApplicationXInstallInstructions
    static string ApplicationXIso9660Image
    static string ApplicationXJavaJnlpFile
    static string ApplicationXLatex
    static string ApplicationXLzhCompressed
    static string ApplicationXMie
    static string ApplicationXMobipocketEbook
    static string ApplicationXMsApplication
    static string ApplicationXMsShortcut
    static string ApplicationXMsWmd
    static string ApplicationXMsWmz
    static string ApplicationXMsXbap
    static string ApplicationXMsaccess
    static string ApplicationXMsbinder
    static string ApplicationXMscardfile
    static string ApplicationXMsclip
    static string ApplicationXMsdownload
    static string ApplicationXMsmediaview
    static string ApplicationXMsmetafile
    static string ApplicationXMsmoney
    static string ApplicationXMspublisher
    static string ApplicationXMsschedule
    static string ApplicationXMsterminal
    static string ApplicationXMswrite
    static string ApplicationXNetcdf
    static string ApplicationXNzb
    static string ApplicationXPkcs12
    static string ApplicationXPkcs7Certificates
    static string ApplicationXPkcs7Certreqresp
    static string ApplicationXRarCompressed
    static string ApplicationXResearchInfoSystems
    static string ApplicationXSh
    static string ApplicationXShar
    static string ApplicationXShockwaveFlash
    static string ApplicationXSilverlightApp
    static string ApplicationXSql
    static string ApplicationXStuffit
    static string ApplicationXStuffitx
    static string ApplicationXSubrip
    static string ApplicationXSv4cpio
    static string ApplicationXSv4crc
    static string ApplicationXT3vmImage
    static string ApplicationXTads
    static string ApplicationXTar
    static string ApplicationXTcl
    static string ApplicationXTex
    static string ApplicationXTexTfm
    static string ApplicationXTexinfo
    static string ApplicationXTgif
    static string ApplicationXUstar
    static string ApplicationXWaisSource
    static string ApplicationXX509CaCert
    static string ApplicationXXfig
    static string ApplicationXXliffXml
    static string ApplicationXXpinstall
    static string ApplicationXXz
    static string ApplicationXZmachine
    static string ApplicationXamlXml
    static string ApplicationXcapDiffXml
    static string ApplicationXencXml
    static string ApplicationXhtmlXml
    static string ApplicationXml
    static string ApplicationXmlDtd
    static string ApplicationXopXml
    static string ApplicationXprocXml
    static string ApplicationXsltXml
    static string ApplicationXspfXml
    static string ApplicationXvXml
    static string ApplicationYang
    static string ApplicationYinXml
    static string ApplicationZip
    static string AudioAdpcm
    static string AudioBasic
    static string AudioMidi
    static string AudioMp4
    static string AudioMpeg
    static string AudioOgg
    static string AudioS3m
    static string AudioSilk
    static string AudioVndDeceAudio
    static string AudioVndDigitalWinds
    static string AudioVndDra
    static string AudioVndDts
    static string AudioVndDtsHd
    static string AudioVndLucentVoice
    static string AudioVndMsPlayreadyMediaPya
    static string AudioVndNueraEcelp4800
    static string AudioVndNueraEcelp7470
    static string AudioVndNueraEcelp9600
    static string AudioVndRip
    static string AudioWebm
    static string AudioXAac
    static string AudioXAiff
    static string AudioXCaf
    static string AudioXFlac
    static string AudioXMatroska
    static string AudioXMpegurl
    static string AudioXMsWax
    static string AudioXMsWma
    static string AudioXPnRealaudio
    static string AudioXPnRealaudioPlugin
    static string AudioXWav
    static string AudioXm
    static string Binary
    static string ChemicalXCdx
    static string ChemicalXCif
    static string ChemicalXCmdf
    static string ChemicalXCml
    static string ChemicalXCsml
    static string ChemicalXXyz
    static string DefaultExtension
    static string DefaultMimeType
    static string FontCollection
    static string FontOtf
    static string FontTtf
    static string FontWoff
    static string FontWoff2
    static string ImageBmp
    static string ImageCgm
    static string ImageG3fax
    static string ImageGif
    static string ImageHeif
    static string ImageIef
    static string ImageJpeg
    static string ImageKtx
    static string ImagePng
    static string ImagePrsBtif
    static string ImageSgi
    static string ImageSvg
    static string ImageSvgXml
    static string ImageTiff
    static string ImageVndAdobePhotoshop
    static string ImageVndDeceGraphic
    static string ImageVndDjvu
    static string ImageVndDvbSubtitle
    static string ImageVndDwg
    static string ImageVndDxf
    static string ImageVndFastbidsheet
    static string ImageVndFpx
    static string ImageVndFst
    static string ImageVndFujixeroxEdmicsMmr
    static string ImageVndFujixeroxEdmicsRlc
    static string ImageVndMsModi
    static string ImageVndMsPhoto
    static string ImageVndNetFpx
    static string ImageVndWapWbmp
    static string ImageVndXiff
    static string ImageWebp
    static string ImageX3ds
    static string ImageXCmuRaster
    static string ImageXCmx
    static string ImageXFreehand
    static string ImageXIcon
    static string ImageXMrsidImage
    static string ImageXPcx
    static string ImageXPict
    static string ImageXPortableAnymap
    static string ImageXPortableBitmap
    static string ImageXPortableGraymap
    static string ImageXPortablePixmap
    static string ImageXRgb
    static string ImageXTga
    static string ImageXXbitmap
    static string ImageXXpixmap
    static string ImageXXwindowdump
    static string MessageRfc822
    static string ModelIges
    static string ModelMesh
    static string ModelVndColladaXml
    static string ModelVndDwf
    static string ModelVndGdl
    static string ModelVndGtw
    static string ModelVndMts
    static string ModelVndVtu
    static string ModelVrml
    static string ModelX3dBinary
    static string ModelX3dVrml
    static string ModelX3dXml
    static string TextCacheManifest
    static string TextCalendar
    static string TextCss
    static string TextCsv
    static string TextHtml
    static string TextJavascript
    static string TextMarkdown
    static string TextN3
    static string TextPlain
    static string TextPrsLinesTag
    static string TextRichtext
    static string TextSgml
    static string TextTabSeparatedValues
    static string TextTroff
    static string TextTurtle
    static string TextUriList
    static string TextVcard
    static string TextVndCurl
    static string TextVndCurlDcurl
    static string TextVndCurlMcurl
    static string TextVndCurlScurl
    static string TextVndFly
    static string TextVndFmiFlexstor
    static string TextVndGraphviz
    static string TextVndIn3d3dml
    static string TextVndIn3dSpot
    static string TextVndSunJ2meAppDescriptor
    static string TextVndWapWml
    static string TextVndWapWmlscript
    static string TextXAsm
    static string TextXC
    static string TextXFortran
    static string TextXJavaSource
    static string TextXNfo
    static string TextXOpml
    static string TextXPascal
    static string TextXSetext
    static string TextXSfv
    static string TextXUuencode
    static string TextXVcalendar
    static string TextXVcard
    static string TextXml
    static string Video3gpp
    static string Video3gpp2
    static string VideoH261
    static string VideoH263
    static string VideoH264
    static string VideoJpeg
    static string VideoJpm
    static string VideoMj2
    static string VideoMp4
    static string VideoMpeg
    static string VideoOgg
    static string VideoQuicktime
    static string VideoVndDeceHd
    static string VideoVndDeceMobile
    static string VideoVndDecePd
    static string VideoVndDeceSd
    static string VideoVndDeceVideo
    static string VideoVndDvbFile
    static string VideoVndFvt
    static string VideoVndMpegurl
    static string VideoVndMsPlayreadyMediaPyv
    static string VideoVndUvvuMp4
    static string VideoVndVivo
    static string VideoWebm
    static string VideoXF4v
    static string VideoXFli
    static string VideoXFlv
    static string VideoXM4v
    static string VideoXMatroska
    static string VideoXMng
    static string VideoXMsAsf
    static string VideoXMsVob
    static string VideoXMsWm
    static string VideoXMsWmv
    static string VideoXMsWmx
    static string VideoXMsWvx
    static string VideoXMsvideo
    static string VideoXSgiMovie
    static string VideoXSmv
    static string XConferenceXCooltalk
  class MovingAverage
    ctor(int size = 32)
    void AddValue(double value)
    double GetAverage()
  // Convention for converting a developer-declared [Rest] path into the absolute external path under the space domain ({space}.ikonai.app{path}). Developers can write either absolute paths ("/billing/stripe", "/labs/{workspace}/increment") or the legacy relative form ("bump", "value" — what the cell-host's AppEndpointHost served under /{CellType}/{Method} before the inbound-unification plan landed). Relative names are auto-derived so the same endpoint flows through the same gateway path under both forms: On the app class: "bump" → /bumpOn a [Cell] LabCell: "bump" → /lab-cell/bump Used by both App.BuildEndpointsAsync (runtime, so BuildEndpointUrl emits the right PublicUrl) and AppBundleHandler.DiscoverEndpoints (build-time, so the bundle manifest carries the derived path through to the gateway's routing trie). One source of truth for the derivation keeps the two from drifting.
  static class PathConvention
    // Derive the absolute external path for an HTTP endpoint declared on ownerTypeName . Absolute paths (starting with '/') are returned unchanged. Empty / relative paths are kebab-cased and assembled under the cell-type prefix (or directly on the app class).
    static string DeriveAbsolutePath(string declaredPath, string ownerTypeName, bool isAppClass, string fallbackMethodName)
    // Convert a PascalCase / camelCase identifier to lowercase-kebab. "LabCell" → "lab-cell", "GetOrders" → "get-orders", "ID" → "id". Non-identifier characters (already-hyphens, underscores, slashes) pass through unchanged so a developer who wrote "my-path" gets "my-path" back.
    static string ToKebabCase(string s)
  class AppBundleConfig.Pipeline
    ctor()
    string? Description { get; set; }
    string DllName { get; set; }
    string? Guid { get; set; }
    string Name { get; set; }
    string OpenApiSpecJson { get; set; }
    string TypeName { get; set; }
    int Version { get; set; }
    List<AppBundleConfig.Workflow> Workflows { get; set; }
  class PipelineBundleConfigLegacy
    ctor()
    string CreatedAt { get; set; }
    string DllName { get; set; }
    string Hash { get; set; }
    string PipelineTypeName { get; set; }
    string Version { get; set; }
    static string ConfigFileName
    static string SpecFileName
  enum PipelineExecutionMode
    None
    HttpsEndpoint
    Scheduled
  sealed class PipelineProtocolHandler
    ctor(IDuplexPipe transport, Func<ProtocolMessage, Task> onMessageReceived, int maxMessageSize = 104857600)
    bool IsConnected { get; }
    Task CloseConnectionAsync()
    Task SendMessageAsync(ProtocolMessage message)
    Task StartReceiveAsync()
    void Stop()
  // Captures whether the running ikon tool (or hosted ikon-server) has a platform-dotnet checkout it can build against, and exposes the flags every downstream build step needs: the -p:IkonRoot=... MSBuild arg for dotnet, and the VITE_IS_IKON_INTERNAL / VITE_IKON_PLATFORM_TYPESCRIPT_PATH env vars for vite.
  sealed class PlatformContext : IEquatable<PlatformContext>
    // Captures whether the running ikon tool (or hosted ikon-server) has a platform-dotnet checkout it can build against, and exposes the flags every downstream build step needs: the -p:IkonRoot=... MSBuild arg for dotnet, and the VITE_IS_IKON_INTERNAL / VITE_IKON_PLATFORM_TYPESCRIPT_PATH env vars for vite.
    ctor(string? DotnetRoot)
    // MSBuild argument to splice into a dotnet build/restore/run/publish command line. Returns -p:IkonRoot="..." when internal, empty string when external.
    string DotnetMSBuildArg { get; }
    string? DotnetRoot { get; init; }
    bool IsIkonInternal { get; }
    string? RepoRoot { get; }
    string? TypescriptRoot { get; }
    // Stamps VITE_IS_IKON_INTERNAL and, when internal, VITE_IKON_PLATFORM_TYPESCRIPT_PATH onto an env dictionary before invoking npm/vite. Mutates and returns env for fluent use.
    IDictionary<string, string?> ApplyViteEnv(IDictionary<string, string?> env)
    static PlatformContext External
  // Probe primitives for locating the platform-dotnet directory. Compose them into a PlatformContext ; each probe returns null when it doesn't match so they chain cleanly with the null-coalescing operator.
  static class PlatformDetect
    // Reads [assembly: AssemblyMetadata("IkonRoot", ...)] baked in at .NET build time via the -p:IkonRoot=... arg. Used by hosted code (e.g. ViteServerHandler) to recover the platform location after the app DLL has been copied out of the repo tree.
    static string? FromAssemblyMetadata(Assembly assembly)
    // Shortcut for FromDirectory(AppContext.BaseDirectory). Matches when the ikon tool binary itself lives inside the platform repo (typical for in-repo artifacts/bin/IkonTool/... builds).
    static string? FromBaseDirectory()
    // Walks upward from directory looking for ikon-platform.slnx. See FindIkonPlatformDotnetRoot for the includeSibling semantics.
    static string? FromDirectory(string? directory, bool includeSibling = false)
    // Resolves the --platform-dir argument. Returns null when input is null or blank. Throws UserException when the path is set but doesn't contain ikon-platform.slnx at either the root or under platform-dotnet/.
    static string? FromExplicit(string? explicitPlatformDir)
  // A combined polymorphic converter that supports both single instances of TBase and collections of TBase. When reading, it searches for the "Type" property (in any order) to determine the concrete type. When writing, it writes a dictionary that always includes "Type" (as the first entry).
  class PolymorphicConverter<TBase> : JsonConverter<object> where TBase : class
    ctor()
    override bool CanConvert(Type typeToConvert)
    override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
  class PriceInfo
    ctor()
    bool IsCertain { get; set; }
    List<PriceTierInfo> PriceTiers { get; set; }
    string? UncertaintyReason { get; set; }
    string UsageName { get; set; }
  class PriceTierInfo
    ctor()
    string Currency { get; set; }
    double Price { get; set; }
    double Quantity { get; set; }
    double Threshold { get; set; }
    string? ThresholdCorrelatedUsageType { get; set; }
    string Unit { get; set; }
  class PricingOutput
    ctor()
    string CreatedAt { get; set; }
    List<PriceInfo> PriceInfos { get; set; }
  // Minimal QR code encoder. Generates QR codes as SVG without external dependencies. Supports byte mode encoding up to version 10.
  static class QrEncoder
    static string GenerateSvg(string data, int size)
  class RateLimiter
    ctor(TimeSpan window, int rateLimit)
    int Rate { get; }
    bool Guard()
  // Client-side agent for the in-house relay server. Establishes a single WebSocket to the relay, allocates endpoints on demand, and forwards incoming relay traffic to the matching local port.
  sealed class RelayAgent : IAsyncDisposable
    // Creates a relay agent with explicit connection parameters. Used when the relay host/port/token are already known (e.g. IkonServer's --public-access path). When stableId is non-empty, the relay assigns a fixed port-range segment to this identity so the public ports stay stable across reconnects.
    ctor(string relayServerAddress, int relayServerPort, string relayAuthToken, string stableId = "")
    // Allocates an endpoint. localPort of 0 picks an available port from an internal pool. The returned RelayEndpoint is disposable; dispose it to release the endpoint. When stablePortName is non-empty (and this agent has a non-empty stableId), the relay assigns a deterministic public port for that name within this agent's segment, so the endpoint's public URL stays the same across reconnects and process restarts. Empty = ephemeral, as before.
    Task<RelayEndpoint> AddEndpointAsync(EndpointProtocol protocol, int localPort = 0, string stablePortName = "", CancellationToken cancellationToken = null)
    // Connects to the relay server and authenticates. Called implicitly by AddEndpointAsync on first use; calling it explicitly is optional.
    Task ConnectAsync(CancellationToken cancellationToken = null)
    // Creates a relay agent whose host/port/token are fetched from IkonBackend on first connect. Pass a non-empty stableId to opt into stable public-port assignments.
    static RelayAgent CreateFromIkonBackend(string stableId = "")
    ValueTask DisposeAsync()
  // A relay endpoint. Exposes the locally bound port and the publicly reachable host/port. Dispose to release the endpoint and its local port reservation.
  sealed class RelayEndpoint : IAsyncDisposable
    int LocalPort { get; }
    EndpointProtocol Protocol { get; }
    string PublicHost { get; }
    int PublicPort { get; }
    ValueTask DisposeAsync()
  enum RequiredStatus
    Default
    Required
    Optional
  class Resources : AsyncLocalInstance<Resources>
    ctor()
    Task<byte[]> ReadAsBytesAsync(string resourcePath)
    Task<Stream> ReadAsStreamAsync(string resourcePath)
    Task<string> ReadAsStringAsync(string resourcePath)
  static class Retrier
    static T Run<T>(List<Type>? retryableExceptions, int retries, Func<T> func, string callerMemberName = "", string callerFilePath = "")
    static T Run<T>(Func<T> func, List<Type>? retryableExceptions = null, int retries = 5, Action<Exception>? onRetry = null, Action<Exception>? onFailure = null, bool useExponentialBackoff = true, string? description = null, string callerMemberName = "", string callerFilePath = "")
    static void Run(List<Type>? retryableExceptions, int retries, Action func, string callerMemberName = "", string callerFilePath = "")
    static void Run(Action func, List<Type>? retryableExceptions = null, int retries = 5, Action<Exception>? onRetry = null, Action<Exception>? onFailure = null, bool useExponentialBackoff = true, string? description = null, string callerMemberName = "", string callerFilePath = "")
    static Task<T> RunAsync<T>(List<Type>? retryableExceptions, int retries, Func<Task<T>> func, string callerMemberName = "", string callerFilePath = "")
    static Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> func, CancellationToken cancellationToken, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, string? description = null, string callerMemberName = "", string callerFilePath = "")
    static Task<T> RunAsync<T>(Func<Task<T>> func, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, string? description = null, string callerMemberName = "", string callerFilePath = "")
    static Task RunAsync(List<Type>? retryableExceptions, int retries, Func<Task> func, string callerMemberName = "", string callerFilePath = "")
    static Task RunAsync(Func<Task> func, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, string? description = null, string callerMemberName = "", string callerFilePath = "")
  class AppProjectConfigLegacy.Target : ITomlMetadataProvider
    ctor()
    string ChannelId { get; set; }
    string OrganisationId { get; set; }
    string SpaceId { get; set; }
  class AppProjectConfig.TargetConfig : ITomlMetadataProvider
    ctor()
    string ChannelId { get; set; }
    string Name { get; set; }
    string OrganisationId { get; set; }
    string SpaceId { get; set; }
  class TempDirectory
    ctor(string rootDirName)
    string FullPath { get; }
    long Size { get; }
    void Delete()
    string GetDirPath(string subDirName)
    string GetFilePath(string fileName)
  class TimeAverage
    ctor()
    void AddValue(double value)
    double GetValue()
  static class TimeSpanExtensions
    static string ToHumanReadable(TimeSpan timeSpan, int precision = 2)
  class TimedQueue<T>
    ctor()
    int Count { get; }
    void Enqueue(T item, long durationInMicroseconds)
    Task UpdateAsync(float deltaTime, Func<T, Task> process)
  class Translator
    ctor(string spaceId)
    ctor(string spaceId, string locale)
    Task InitializeAsync()
    void SetLocale(string newLocale)
    Task<string> TranslateAsync(string text, string description = "")
  class UsageTracker
    ctor()
    bool HasUsages { get; }
    Dictionary<string, double> Snapshot { get; }
    void Clear()
    void OnLogEvent(object sender, LogEvent logEvent)
    string PrettyPrint()
    void Register(string name, double value)
  static class Utils
    static string GenerateRandomToken(int size = 32)
    static string GetCSharpTypeName(object? obj)
    static IPAddress GetFirstIPv4AddressOrLocalhost()
    static string ToUnescapedString(string input, bool unicodeOnly = false)
  class ValueStatistics
    ctor(double alpha = 0.9, double interval = 30)
    string ResultString { get; }
    bool TimerElapsed { get; }
    void AddSample(double sample)
    void Reset()
    double Average
    double Maximum
    double Minimum
  class AppBundleConfig.Workflow
    ctor()
    PipelineExecutionMode ExecutionMode { get; set; }
    string Name { get; set; }
    string? Schedule { get; set; }

namespace Ikon.Common.Assets
  static class StorageExtensions
    static Task AddCloudFilePublicStorageAsync(Asset asset)
    static Task AddCloudFileStorageAsync(Asset asset)
    static Task AddCloudJsonStorageAsync(Asset asset)
    static Task AddCloudProfileStorageAsync(Asset asset)
    static Task AddLocalFileStorageAsync(Asset asset, string root)

namespace Ikon.Common.Git
  // Git branch information.
  class GitBranch : IEquatable<GitBranch>
    // Git branch information.
    ctor(string Name, bool IsRemote, bool IsCurrent)
    bool IsCurrent { get; init; }
    bool IsRemote { get; init; }
    string Name { get; init; }
  // Git file change type.
  enum GitChangeType
    Added
    Modified
    Deleted
    Renamed
    Untracked
  // Options for cloning a repository.
  class GitCloneOptions : IEquatable<GitCloneOptions>
    // Options for cloning a repository.
    ctor(string? Branch = null, bool Shallow = false, GitCredentials? Credentials = null)
    string? Branch { get; init; }
    GitCredentials? Credentials { get; init; }
    bool Shallow { get; init; }
  // Git commit information.
  class GitCommit : IEquatable<GitCommit>
    // Git commit information.
    ctor(string Sha, string ShortSha, string Author, string AuthorEmail, DateTimeOffset Date, string Message)
    string Author { get; init; }
    string AuthorEmail { get; init; }
    DateTimeOffset Date { get; init; }
    string Message { get; init; }
    string Sha { get; init; }
    string ShortSha { get; init; }
  // Git credentials for authenticated operations.
  class GitCredentials : IEquatable<GitCredentials>
    // Git credentials for authenticated operations.
    ctor(string Username, string Password)
    string Password { get; init; }
    string Username { get; init; }
  // Git diff between two commits.
  class GitDiff : IEquatable<GitDiff>
    // Git diff between two commits.
    ctor(string? FromSha, string? ToSha, List<GitFileDiff> Files)
    List<GitFileDiff> Files { get; init; }
    string? FromSha { get; init; }
    string? ToSha { get; init; }
  // A changed file in git status or diff.
  class GitFileChange : IEquatable<GitFileChange>
    // A changed file in git status or diff.
    ctor(string Path, GitChangeType Type)
    string Path { get; init; }
    GitChangeType Type { get; init; }
  // File diff information.
  class GitFileDiff : IEquatable<GitFileDiff>
    // File diff information.
    ctor(string Path, GitChangeType Type, int LinesAdded, int LinesRemoved, string? Patch = null)
    int LinesAdded { get; init; }
    int LinesRemoved { get; init; }
    string? Patch { get; init; }
    string Path { get; init; }
    GitChangeType Type { get; init; }
  // Strongly-typed git repository operations.
  class GitRepository
    // Strongly-typed git repository operations.
    ctor(string workingDirectory, GitCredentials? credentials = null)
    GitCredentials? Credentials { get; }
    string WorkingDirectory { get; }
    // Abort all in-progress operations (merge, rebase, cherry-pick).
    Task AbortAllInProgressOperationsAsync(CancellationToken ct = null)
    // Abort an in-progress cherry-pick.
    Task<bool> AbortCherryPickAsync(CancellationToken ct = null)
    // Abort an in-progress merge.
    Task<bool> AbortMergeAsync(CancellationToken ct = null)
    // Abort an in-progress rebase.
    Task<bool> AbortRebaseAsync(CancellationToken ct = null)
    // Add a remote.
    Task AddRemoteAsync(string name, string url, CancellationToken ct = null)
    // Checkout an existing branch.
    Task CheckoutAsync(string branchOrRef, CancellationToken ct = null)
    // Checkout files from a specific ref without changing HEAD.
    Task CheckoutFilesFromRefAsync(string refName, string path = ".", CancellationToken ct = null)
    // Clone a repository to a target directory.
    static Task<GitRepository> CloneAsync(string url, string targetDir, GitCloneOptions? options = null, CancellationToken ct = null)
    // Clone a repository or sync if it already exists. Returns the repository instance with the current SHA.
    static Task<ValueTuple<GitRepository, string?, bool>> CloneOrSyncAsync(string url, string targetDir, GitCloneOptions? options = null, CancellationToken ct = null)
    // Commit staged changes.
    Task<GitCommit> CommitAsync(string message, CancellationToken ct = null)
    // Commit staged changes with custom author.
    Task<GitCommit> CommitAsync(string message, string authorName, string authorEmail, bool allowEmpty = false, CancellationToken ct = null)
    // Create and checkout a new branch.
    Task CreateBranchAsync(string name, string? startPoint = null, CancellationToken ct = null)
    // Create a tag.
    Task<GitTag> CreateTagAsync(string name, string? message = null, CancellationToken ct = null)
    // Delete a tag.
    Task DeleteTagAsync(string name, CancellationToken ct = null)
    // Discard all uncommitted changes.
    Task DiscardChangesAsync(CancellationToken ct = null)
    // Escape a commit message for shell.
    static string EscapeMessage(string message)
    // Fetch from remote.
    Task FetchAsync(bool includeTags = false, CancellationToken ct = null)
    // Construct an authenticated URL.
    static string GetAuthenticatedUrl(string url, GitCredentials credentials)
    // Get all branches.
    Task<List<GitBranch>> GetBranchesAsync(CancellationToken ct = null)
    // Get a local git config value.
    Task<string?> GetConfigAsync(string key, CancellationToken ct = null)
    // Get the current branch name.
    Task<string> GetCurrentBranchAsync(CancellationToken ct = null)
    // Get diff between HEAD and another target (or working directory if null).
    Task<GitDiff> GetDiffAsync(string? target = null, CancellationToken ct = null)
    // Get the HEAD commit.
    Task<GitCommit?> GetHeadCommitAsync(CancellationToken ct = null)
    // Get the HEAD SHA.
    Task<string?> GetHeadShaAsync(bool shortSha = false, CancellationToken ct = null)
    // Get commit history.
    Task<List<GitCommit>> GetHistoryAsync(int limit = 20, string? fromRef = null, CancellationToken ct = null)
    // Get remote URL (without credentials).
    Task<string?> GetRemoteUrlAsync(string name = "origin", CancellationToken ct = null)
    // Get the current repository status.
    Task<GitStatus> GetStatusAsync(CancellationToken ct = null)
    // Get all tags.
    Task<List<GitTag>> GetTagsAsync(CancellationToken ct = null)
    // Check if repository has any commits.
    Task<bool> HasCommitsAsync(CancellationToken ct = null)
    // Check if a remote exists.
    Task<bool> HasRemoteAsync(string name = "origin", CancellationToken ct = null)
    // Check if there are uncommitted changes.
    Task<bool> HasUncommittedChangesAsync(CancellationToken ct = null)
    // Check if there are uncommitted changes under a specific path.
    Task<bool> HasUncommittedChangesAsync(string path, CancellationToken ct = null)
    // Initialize a git repository and connect to a remote, preserving local files. Local files are kept as-is and NOT merged with remote content. Returns the repository instance ready for use.
    static Task<GitRepository> InitAndConnectAsync(string directory, string remoteUrl, GitCredentials? credentials = null, string? configKey = null, string? configValue = null, CancellationToken ct = null)
    // Initialize a new git repository.
    static Task<GitRepository> InitAsync(string directory, CancellationToken ct = null)
    // Check if the working directory is a git repository.
    Task<bool> IsGitRepositoryAsync(CancellationToken ct = null)
    // Check if a directory is a git repository.
    static Task<bool> IsGitRepositoryAsync(string directory, CancellationToken ct = null)
    // List all worktrees attached to this repository (including the primary one). Parses the output of `git worktree list --porcelain`.
    Task<List<GitWorktreeInfo>> ListWorktreesAsync(CancellationToken ct = null)
    // Push to remote.
    Task PushAsync(bool setUpstream = false, CancellationToken ct = null)
    // Check if a ref exists.
    Task<bool> RefExistsAsync(string refName, CancellationToken ct = null)
    // Rename current branch.
    Task RenameBranchAsync(string oldName, string newName, CancellationToken ct = null)
    // Reset HEAD to a specific ref (hard reset).
    Task ResetHardAsync(string target, CancellationToken ct = null)
    // Reset HEAD to a specific ref (soft reset - keeps changes staged).
    Task ResetSoftAsync(string target, CancellationToken ct = null)
    // Restore to a specific target (tag, sha, or branch).
    Task<GitSyncResult> RestoreAsync(string target, CancellationToken ct = null)
    // Run a git command (throws on failure).
    Task<string> RunAsync(string args, CancellationToken ct = null)
    // Save changes (stage, commit, push).
    Task<GitSyncResult> SaveAsync(string message, CancellationToken ct = null)
    // Set a local git config value.
    Task SetConfigAsync(string key, string value, CancellationToken ct = null)
    // Set remote URL.
    Task SetRemoteUrlAsync(string name, string url, CancellationToken ct = null)
    // Set up tracking for a branch.
    Task SetUpstreamAsync(string remoteBranch, CancellationToken ct = null)
    static string ShortCommitHash(string? hash)
    // Stage all changes.
    Task StageAllAsync(CancellationToken ct = null)
    // Stage a specific path (file or directory).
    Task StagePathAsync(string path, CancellationToken ct = null)
    // Stash all changes.
    Task<bool> StashAsync(string? message = null, CancellationToken ct = null)
    // Pop the latest stash.
    Task<bool> StashPopAsync(CancellationToken ct = null)
    // Strip credentials from a git URL for safe display/comparison.
    static string StripCredentialsFromUrl(string url)
    // Sync to latest remote (fetch + reset --hard).
    Task<GitSyncResult> SyncAsync(CancellationToken ct = null)
    // Try to open an existing git repository.
    static GitRepository? TryOpen(string directory)
    // Run a git command (doesn't throw on failure).
    Task<ValueTuple<bool, string, string>> TryRunAsync(string args, CancellationToken ct = null)
    // Compare two git URLs, ignoring credentials and trailing slashes.
    static bool UrlsMatch(string? url1, string? url2)
  // Git repository status.
  class GitStatus : IEquatable<GitStatus>
    // Git repository status.
    ctor(string Branch, string? HeadSha, bool HasUncommittedChanges, bool IsDetachedHead, int AheadBy, int BehindBy, List<GitFileChange> Changes)
    int AheadBy { get; init; }
    int BehindBy { get; init; }
    string Branch { get; init; }
    List<GitFileChange> Changes { get; init; }
    bool HasUncommittedChanges { get; init; }
    string? HeadSha { get; init; }
    bool IsDetachedHead { get; init; }
  // Result of a sync/restore/save operation.
  class GitSyncResult : IEquatable<GitSyncResult>
    // Result of a sync/restore/save operation.
    ctor(bool Success, string? PreviousSha, string? CurrentSha, string? Error = null)
    string? CurrentSha { get; init; }
    string? Error { get; init; }
    string? PreviousSha { get; init; }
    bool Success { get; init; }
  // Git tag information.
  class GitTag : IEquatable<GitTag>
    // Git tag information.
    ctor(string Name, string Sha, GitCommit? Commit = null)
    GitCommit? Commit { get; init; }
    string Name { get; init; }
    string Sha { get; init; }
  // Git worktree entry reported by `git worktree list`.
  class GitWorktreeInfo : IEquatable<GitWorktreeInfo>
    // Git worktree entry reported by `git worktree list`.
    ctor(string Path, string? Head, string? Branch)
    string? Branch { get; init; }
    string? Head { get; init; }
    string Path { get; init; }

namespace Ikon.Common.Maths
  // Determines the axis convention used.
  enum AxisConvention
    RightHanded_X_Up
    RightHanded_Y_Up
    RightHanded_Z_Up
    LeftHanded_X_Up
    LeftHanded_Y_Up
    LeftHanded_Z_Up
  // Axis-aligned bounding box value type in three dimensional space.
  struct BoundingBox : IEquatable<BoundingBox>
    // Constructs a new minimal bounding box fully enclosing a given bounding sphere.
    ctor(BoundingSphere sphere)
    // Creates a new bounding box from a given set of values in 3D space.
    ctor(Vector3[] points)
    // Constructs a new bounding box struct with given corner values.
    ctor(Vector3 a, Vector3 b)
    // Gets or sets the center point of the bounding box.
    Vector3 Center { get; set; }
    // Gets or sets the diagonal vector of the bounding box, which is a vector between the minimum and maximum corners.
    Vector3 Diagonal { get; set; }
    // Gets or sets half of the diagonal vector of the bounding box.
    Vector3 HalfDiagonal { get; set; }
    // Returns a cubic bounding box centered at the origin which spans from -1 to 1 in all axis.
    static BoundingBox UnitCube { get; }
    // Returns an array with all the eight corners comprising the bounding box.
    Vector3[] GetCorners()
    // Returns a new bounding box which fully encloses given bounding boxes. Remember this is in axis-aligned space.
    static BoundingBox Merge(BoundingBox value1, BoundingBox value2)
    // The maximum corner value of the bounding box.
    Vector3 Maximum
    // The minimum corner value of the bounding box.
    Vector3 Minimum
  class BoundingFrustum : IEquatable<BoundingFrustum>
    ctor(Matrix4x4 value)
    ctor(Matrix4x4 value, Vector2 offset, Vector2 size)
    Plane Bottom { get; }
    Plane Far { get; }
    Plane Left { get; }
    Matrix4x4 Matrix4x4 { get; set; }
    Plane Near { get; }
    Plane Right { get; }
    Plane Top { get; }
    ContainmentType Contains(BoundingBox box)
    void Contains(ref BoundingBox box, out ContainmentType result)
    ContainmentType Contains(BoundingFrustum frustum)
    ContainmentType Contains(BoundingSphere sphere)
    void Contains(ref BoundingSphere sphere, out ContainmentType result)
    ContainmentType Contains(Vector3 point)
    void Contains(ref Vector3 point, out ContainmentType result)
    Vector3[] GetCorners()
    bool Intersects(BoundingBox box)
    void Intersects(ref BoundingBox box, out bool result)
    bool Intersects(BoundingFrustum frustum)
    bool Intersects(BoundingSphere sphere)
    void Intersects(ref BoundingSphere sphere, out bool result)
    PlaneIntersectionType Intersects(Plane plane)
    void Intersects(ref Plane plane, out PlaneIntersectionType result)
    float? Intersects(Ray ray)
    void Intersects(ref Ray ray, out float? result)
  // Bounding sphere value type in three dimensional space.
  struct BoundingSphere : IEquatable<BoundingSphere>
    // Constructs a new minimal bounding sphere fully enclosing a given bounding box.
    ctor(BoundingBox value)
    // Creates a new bounding sphere from a given set of values in 3D space.
    ctor(Vector3[] points)
    // Constructs a new bounding sphere from given center and radius values.
    ctor(Vector3 center, float radius)
    // Returns a sphere centered at the origin with radius 1.
    static BoundingSphere UnitSphere { get; }
    // Constructs a new bounding sphere which fully encloses given bounding spheres.
    static BoundingSphere Merge(BoundingSphere value1, BoundingSphere value2)
    // The center of the bounding sphere.
    Vector3 Center
    // The radius of the bounding sphere.
    float Radius
  // Contains static methods to help in determining intersections, containment, etc.
  static class Collision
    // Determines whether a BoundingBox contains a BoundingBox .
    static ContainmentType BoxContainsBox(ref BoundingBox box1, ref BoundingBox box2)
    // Determines whether a BoundingBox contains a point.
    static ContainmentType BoxContainsPoint(ref BoundingBox box, ref Vector3 point)
    // Determines whether a BoundingBox contains a BoundingSphere .
    static ContainmentType BoxContainsSphere(ref BoundingBox box, ref BoundingSphere sphere)
    // Determines whether there is an intersection between a BoundingBox and a BoundingBox .
    static bool BoxIntersectsBox(ref BoundingBox box1, ref BoundingBox box2)
    // Determines whether there is an intersection between a BoundingBox and a BoundingSphere .
    static bool BoxIntersectsSphere(ref BoundingBox box, ref BoundingSphere sphere)
    // Determines the closest point between a BoundingBox and a point.
    static void ClosestPointBoxPoint(ref BoundingBox box, ref Vector3 point, out Vector3 result)
    // Determines the closest point between a Plane and a point.
    static void ClosestPointPlanePoint(ref Plane plane, ref Vector3 point, out Vector3 result)
    // Determines the closest point between a point and a triangle.
    static void ClosestPointPointTriangle(ref Vector3 point, ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3, out Vector3 result)
    // Determines the closest point between a BoundingSphere and a point.
    static void ClosestPointSpherePoint(ref BoundingSphere sphere, ref Vector3 point, out Vector3 result)
    // Determines the closest point between a BoundingSphere and a BoundingSphere .
    static void ClosestPointSphereSphere(ref BoundingSphere sphere1, ref BoundingSphere sphere2, out Vector3 result)
    // Determines the distance between a BoundingBox and a BoundingBox .
    static float DistanceBoxBox(ref BoundingBox box1, ref BoundingBox box2)
    // Determines the distance between a BoundingBox and a point.
    static float DistanceBoxPoint(ref BoundingBox box, ref Vector3 point)
    // Determines the distance between a Plane and a point.
    static float DistancePlanePoint(ref Plane plane, ref Vector3 point)
    // Determines the distance between a BoundingSphere and a point.
    static float DistanceSpherePoint(ref BoundingSphere sphere, ref Vector3 point)
    // Determines the distance between a BoundingSphere and a BoundingSphere .
    static float DistanceSphereSphere(ref BoundingSphere sphere1, ref BoundingSphere sphere2)
    // Determines whether there is an intersection between a Plane and a BoundingBox .
    static PlaneIntersectionType PlaneIntersectsBox(ref Plane plane, ref BoundingBox box)
    // Determines whether there is an intersection between a Plane and a Plane .
    static bool PlaneIntersectsPlane(ref Plane plane1, ref Plane plane2)
    // Determines whether there is an intersection between a Plane and a Plane .
    static bool PlaneIntersectsPlane(ref Plane plane1, ref Plane plane2, out Ray line)
    // Determines whether there is an intersection between a Plane and a point.
    static PlaneIntersectionType PlaneIntersectsPoint(ref Plane plane, ref Vector3 point)
    // Determines whether there is an intersection between a Plane and a BoundingSphere .
    static PlaneIntersectionType PlaneIntersectsSphere(ref Plane plane, ref BoundingSphere sphere)
    // Determines whether there is an intersection between a Plane and a triangle.
    static PlaneIntersectionType PlaneIntersectsTriangle(ref Plane plane, ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3)
    // Determines whether there is an intersection between a Ray and a BoundingBox .
    static bool RayIntersectsBox(ref Ray ray, ref BoundingBox box, out float distance)
    // Determines whether there is an intersection between a Ray and a Plane .
    static bool RayIntersectsBox(ref Ray ray, ref BoundingBox box, out Vector3 point)
    // Determines whether there is an intersection between a Ray and a Plane .
    static bool RayIntersectsPlane(ref Ray ray, ref Plane plane, out float distance)
    // Determines whether there is an intersection between a Ray and a Plane .
    static bool RayIntersectsPlane(ref Ray ray, ref Plane plane, out Vector3 point)
    // Determines whether there is an intersection between a Ray and a point.
    static bool RayIntersectsPoint(ref Ray ray, ref Vector3 point)
    // Determines whether there is an intersection between a Ray and a Ray .
    static bool RayIntersectsRay(ref Ray ray1, ref Ray ray2, out Vector3 point)
    // Determines whether there is an intersection between a Ray and a BoundingSphere .
    static bool RayIntersectsSphere(ref Ray ray, ref BoundingSphere sphere, out float distance)
    // Determines whether there is an intersection between a Ray and a BoundingSphere .
    static bool RayIntersectsSphere(ref Ray ray, ref BoundingSphere sphere, out Vector3 point)
    // Determines whether there is an intersection between a Ray and a triangle.
    static bool RayIntersectsTriangle(ref Ray ray, ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3, out float distance)
    // Determines whether there is an intersection between a Ray and a triangle.
    static bool RayIntersectsTriangle(ref Ray ray, ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3, out Vector3 point)
    // Determines whether a BoundingSphere contains a BoundingBox .
    static ContainmentType SphereContainsBox(ref BoundingSphere sphere, ref BoundingBox box)
    // Determines whether a BoundingSphere contains a point.
    static ContainmentType SphereContainsPoint(ref BoundingSphere sphere, ref Vector3 point)
    // Determines whether a BoundingSphere contains a BoundingSphere .
    static ContainmentType SphereContainsSphere(ref BoundingSphere sphere1, ref BoundingSphere sphere2)
    // Determines whether a BoundingSphere contains a triangle.
    static ContainmentType SphereContainsTriangle(ref BoundingSphere sphere, ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3)
    // Determines whether there is an intersection between a BoundingSphere and a BoundingSphere .
    static bool SphereIntersectsSphere(ref BoundingSphere sphere1, ref BoundingSphere sphere2)
    // Determines whether there is an intersection between a BoundingSphere and a triangle.
    static bool SphereIntersectsTriangle(ref BoundingSphere sphere, ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3)
  // Describes how one bounding volume contains another.
  enum ContainmentType
    Outside
    Inside
    Intersects
  // Keyframe that can be used by the interpolations.
  struct KeyFrame<T> where T : struct
    float Time
    T Value
  // Contains static functions to be used with the common mathematical value types: float, Vector2, Vector3, Vector4 and Matrix4x4.
  class Math
    ctor()
    // Returns the absolute value of a given value.
    static Vector4 Abs(Vector4 a)
    // Returns the absolute value of a given value.
    static Vector3 Abs(Vector3 a)
    // Returns the absolute value of a given value.
    static Vector2 Abs(Vector2 a)
    // Returns the absolute value of a given value.
    static float Abs(float a)
    // Returns the absolute value of a given value.
    static int Abs(int a)
    // Returns the angle whose cosine is the given value.
    static Vector4 Acos(Vector4 a)
    // Returns the angle whose cosine is the given value.
    static Vector3 Acos(Vector3 a)
    // Returns the angle whose cosine is the given value.
    static Vector2 Acos(Vector2 a)
    // Returns the angle whose cosine is the given value.
    static float Acos(float a)
    // Adds matrices together.
    static Matrix4x4 Add(Matrix4x4 a, Matrix4x4 b)
    // Determines the Euclidean angle between two given values.
    static float Angle(Vector4 a, Vector4 b)
    // Determines the Euclidean angle between two given values.
    static float Angle(Vector3 a, Vector3 b)
    // Determines the Euclidean angle between two given values.
    static float Angle(Vector2 a, Vector2 b)
    // Determines the Euclidean angle between two given values.
    static float Angle(float a, float b)
    // Determines whether all the values in a given array are all zero.
    static bool AreZeroes(float[] values)
    // Determines whether all the values in a given array are all zero.
    static bool AreZeroes(Vector2[] values)
    // Determines whether all the values in a given array are all zero.
    static bool AreZeroes(Vector3[] values)
    // Determines whether all the values in a given array are all zero.
    static bool AreZeroes(Vector4[] values)
    // Determines whether all the values in a given array are all zero.
    static bool AreZeroes(Quaternion[] values)
    // Returns the angle whose sine is the given value.
    static Vector4 Asin(Vector4 a)
    // Returns the angle whose sine is the given value.
    static Vector3 Asin(Vector3 a)
    // Returns the angle whose sine is the given value.
    static Vector2 Asin(Vector2 a)
    // Returns the angle whose sine is the given value.
    static float Asin(float a)
    // Returns the angle whose tangent is the given value.
    static Vector4 Atan(Vector4 a)
    // Returns the angle whose tangent is the given value.
    static Vector3 Atan(Vector3 a)
    // Returns the angle whose tangent is the given value.
    static Vector2 Atan(Vector2 a)
    // Returns the angle whose tangent is the given value.
    static float Atan(float a)
    // Returns the angle whose tangent is the quotient of two specified value.
    static Vector4 Atan2(Vector4 a, Vector4 b)
    // Returns the angle whose tangent is the quotient of two specified value.
    static Vector3 Atan2(Vector3 a, Vector3 b)
    // Returns the angle whose tangent is the quotient of two specified value.
    static Vector2 Atan2(Vector2 a, Vector2 b)
    // Returns the angle whose tangent is the quotient of two specified value.
    static float Atan2(float a, float b)
    static Vector3 CalculateCentroid(Vector3[] positions, double[]? weights = null)
    static float CalculateCentroidRootMeanSquareDistance(Vector3[] positions, Vector3 centroid, double[]? weights = null)
    static float CalculatePositionWiseRootMeanSquareDistance(Vector3[] positions)
    static float CalculateRootMeanSquareDistance(Vector3[] a, Vector3[] b)
    // Interpolates between given values using Catmull-Rom interpolation.
    static Vector4 CatmullRom(Vector4 value1, Vector4 value2, Vector4 value3, Vector4 value4, float weight)
    // Interpolates between given values using Catmull-Rom interpolation.
    static Vector3 CatmullRom(Vector3 value1, Vector3 value2, Vector3 value3, Vector3 value4, float weight)
    // Interpolates between given values using Catmull-Rom interpolation.
    static Vector2 CatmullRom(Vector2 value1, Vector2 value2, Vector2 value3, Vector2 value4, float weight)
    // Interpolates between given values using Catmull-Rom interpolation.
    static float CatmullRom(float value1, float value2, float value3, float value4, float weight)
    // Returns a value in a given time Position by interpolating the array of given keyframes.
    static Vector4 CatmullRom(KeyFrame<Vector4>[] values, float time)
    // Returns the smallest integer value that is greater than or equal to the specified value.
    static Vector4 Ceil(Vector4 a)
    // Returns the smallest integer value that is greater than or equal to the specified value.
    static Vector3 Ceil(Vector3 a)
    // Returns the smallest integer value that is greater than or equal to the specified value.
    static Vector2 Ceil(Vector2 a)
    // Returns the smallest integer value that is greater than or equal to the specified value.
    static float Ceil(float a)
    // Clamps the specified value to the specified range.
    static Vector4 Clamp(Vector4 a, Vector4 c1, Vector4 c2)
    // Clamps the specified value to the specified range.
    static Vector3 Clamp(Vector3 a, Vector3 c1, Vector3 c2)
    // Clamps the specified value to the specified range.
    static Vector2 Clamp(Vector2 a, Vector2 c1, Vector2 c2)
    // Clamps the specified value to the specified range.
    static Vector4 Clamp(Vector4 a, float c1, float c2)
    // Clamps the specified value to the specified range.
    static Vector3 Clamp(Vector3 a, float c1, float c2)
    // Clamps the specified value to the specified range.
    static Vector2 Clamp(Vector2 a, float c1, float c2)
    // Clamps the specified value to the specified range.
    static float Clamp(float a, float c1, float c2)
    // Clamps the specified value to the specified range.
    static int Clamp(int a, int c1, int c2)
    // Converts a given (3D) value to use another axis convention.
    static Vector3 ConvertAxis(Vector3 value, AxisConvention currentAxis, AxisConvention destinationAxis)
    // Converts a given (3D) value to use another axis convention.
    static Vector4 ConvertAxis(Vector4 value, AxisConvention currentAxis, AxisConvention destinationAxis)
    // Converts a given (3D) value to use another axis convention.
    static Matrix4x4 ConvertAxis(Matrix4x4 value, AxisConvention currentAxis, AxisConvention destinationAxis)
    // Returns the cosine of a given value.
    static Vector4 Cos(Vector4 a)
    // Returns the cosine of a given value.
    static Vector3 Cos(Vector3 a)
    // Returns the cosine of a given value.
    static Vector2 Cos(Vector2 a)
    // Returns the cosine of a given value.
    static float Cos(float a)
    // Returns the hyperbolic cosine of a given value.
    static Vector4 Cosh(Vector4 a)
    // Returns the hyperbolic cosine of a given value.
    static Vector3 Cosh(Vector3 a)
    // Returns the hyperbolic cosine of a given value.
    static Vector2 Cosh(Vector2 a)
    // Returns the hyperbolic cosine of a given value.
    static float Cosh(float a)
    // Returns the cross product of two vectors.
    static Vector3 Cross(Vector3 a, Vector3 b)
    // Converts a given radian value into degrees.
    static float Degrees(float radians)
    // Dehomogenizes a given vector by dividing all the other components with the last one.
    static Vector4 Dehomogenize(Vector4 a)
    // Returns the shortest Euclidean distance between two values.
    static float Distance(Vector4 a, Vector4 b)
    // Returns the shortest Euclidean distance between two values.
    static float Distance(Vector3 a, Vector3 b)
    // Returns the shortest Euclidean distance between two values.
    static float Distance(Vector2 a, Vector2 b)
    // Returns the shortest Euclidean distance between two values.
    static float Distance(float a, float b)
    // Returns the the shortest Euclidean distance between two values squared.
    static float DistanceSquared(Vector4 a, Vector4 b)
    // Returns the the shortest Euclidean distance between two values squared.
    static float DistanceSquared(Vector3 a, Vector3 b)
    // Returns the the shortest Euclidean distance between two values squared.
    static float DistanceSquared(Vector2 a, Vector2 b)
    // Returns the the shortest Euclidean distance between two values squared.
    static float DistanceSquared(float a, float b)
    // Returns the dot-product of two values.
    static float Dot(Plane a, Plane b)
    // Returns the dot-product of two values.
    static float Dot(Quaternion a, Quaternion b)
    // Returns the dot-product of two values.
    static float Dot(Vector4 a, Vector4 b)
    // Returns the dot-product of two values.
    static float Dot(Vector3 a, Vector3 b)
    // Returns the dot-product of two values.
    static float Dot(Vector2 a, Vector2 b)
    // Returns the dot-product of two values.
    static float Dot(float a, float b)
    // Returns the largest integer that is less than or equal to the specified value.
    static Vector4 Floor(Vector4 a)
    // Returns the largest integer that is less than or equal to the specified value.
    static Vector3 Floor(Vector3 a)
    // Returns the largest integer that is less than or equal to the specified value.
    static Vector2 Floor(Vector2 a)
    // Returns the largest integer that is less than or equal to the specified value.
    static float Floor(float a)
    // Calculates the point on a line with the shortest distance from a given point.
    static Vector2 GetClosestPoint(Vector2 start, Vector2 end, Vector2 position)
    // Calculates the point on an infinite line with the shortest distance from a given point.
    static Vector2 GetClosestPoint(Vector2 start, Vector2 end, Vector2 position, out float distance, out float u)
    // Calculates the point on a line with the shortest distance from a given point.
    static Vector2 GetClosestPoint(Vector2 start, Vector2 end, Vector2 position, out float distance, out float u, out float x)
    // Calculates the point on a segment with the shortest distance from a given point. This means that segment is not infinite line.
    static Vector2 GetClosestPointInClosedSegment(Vector2 start, Vector2 end, Vector2 position, out float distance, out float u)
    // Interpolates between given values using Hermite interpolation.
    static Vector4 Hermite(Vector4 value1, Vector4 tangent1, Vector4 value2, Vector4 tangent2, float weight)
    // Interpolates between given values using Hermite interpolation.
    static Vector3 Hermite(Vector3 value1, Vector3 tangent1, Vector3 value2, Vector3 tangent2, float weight)
    // Interpolates between given values using Hermite interpolation.
    static Vector2 Hermite(Vector2 value1, Vector2 tangent1, Vector2 value2, Vector2 tangent2, float weight)
    // Interpolates between given values using Hermite interpolation.
    static float Hermite(float value1, float tangent1, float value2, float tangent2, float weight)
    // Calculates the inverse for a given matrix.
    static Matrix4x4 Invert(Matrix4x4 value)
    // Calculates the inverse for a given quaternion.
    static Quaternion Invert(Quaternion value)
    // Returns the reciprocal or multiplicative inverse for a given value.
    static Vector4 Invert(Vector4 a)
    // Returns the reciprocal or multiplicative inverse for a given value.
    static Vector3 Invert(Vector3 a)
    // Returns the reciprocal or multiplicative inverse for a given value.
    static Vector2 Invert(Vector2 a)
    // Returns the reciprocal or multiplicative inverse for a given value.
    static float Invert(float a)
    // Determines whether a given value is a real number, and not -inf, +inf, or NaN.
    static bool IsNumber(float value)
    // Determines whether a given value is a real number, and not -inf, +inf, or NaN.
    static bool IsNumber(Vector2 value)
    // Determines whether a given value is a real number, and not -inf, +inf, or NaN.
    static bool IsNumber(Vector3 value)
    // Determines whether a given value is a real number, and not -inf, +inf, or NaN.
    static bool IsNumber(Vector4 value)
    // Determines whether a given value is a real number, and not -inf, +inf, or NaN.
    static bool IsNumber(Quaternion value)
    // Determines whether a value is power-of-two.
    static bool IsPower2(int v)
    // Returns the Euclidean length of a given value.
    static float Length(Plane a)
    // Returns the Euclidean length of a given value.
    static float Length(Quaternion a)
    // Returns the Euclidean length of a given value.
    static float Length(Vector4 a)
    // Returns the Euclidean length of a given value.
    static float Length(Vector3 a)
    // Returns the Euclidean length of a given value.
    static float Length(Vector2 a)
    // Returns the Euclidean length of a given value.
    static float Length(float a)
    // Returns the Euclidean length of a given value squared.
    static float LengthSquared(Quaternion a)
    // Returns the Euclidean length of a given value squared.
    static float LengthSquared(Vector4 a)
    // Returns the Euclidean length of a given value squared.
    static float LengthSquared(Vector3 a)
    // Returns the Euclidean length of a given value squared.
    static float LengthSquared(Vector2 a)
    // Returns the Euclidean length of a given value squared.
    static float LengthSquared(float a)
    // Interpolates between given values using linear interpolation.
    static Quaternion Lerp(Quaternion value1, Quaternion value2, float weight)
    // Interpolates between given values using linear interpolation.
    static float[] Lerp(float[] value1, float[] value2, float weight)
    // Interpolates between given values using linear interpolation.
    static Vector4 Lerp(Vector4 value1, Vector4 value2, float weight)
    // Interpolates between given values using linear interpolation.
    static Vector3 Lerp(Vector3 value1, Vector3 value2, float weight)
    // Interpolates between given values using linear interpolation.
    static Vector2 Lerp(Vector2 value1, Vector2 value2, float weight)
    // Interpolates between given values using linear interpolation.
    static float Lerp(float value1, float value2, float weight)
    // Selects the larger values of a and b.
    static Vector4 Maximum(Vector4 a, Vector4 b)
    // Selects the larger values of a and b.
    static Vector3 Maximum(Vector3 a, Vector3 b)
    // Selects the larger values of a and b.
    static Vector2 Maximum(Vector2 a, Vector2 b)
    // Selects the larger values of a and b.
    static float Maximum(float a, float b)
    // Selects the larger values of a and b.
    static int Maximum(int a, int b)
    // Selects the smaller values of a and b.
    static Vector4 Minimum(Vector4 a, Vector4 b)
    // Selects the smaller values of a and b.
    static Vector3 Minimum(Vector3 a, Vector3 b)
    // Selects the smaller values of a and b.
    static Vector2 Minimum(Vector2 a, Vector2 b)
    // Selects the smaller values of a and b.
    static float Minimum(float a, float b)
    // Selects the smaller values of a and b.
    static int Minimum(int a, int b)
    // Computes the nearest power-of-two value that is greater or equal to a given value.
    static int NextPower2(int v)
    // Normalizes a given value to be unit length.
    static Plane Normalize(Plane a)
    // Normalizes a given value to be unit length.
    static Quaternion Normalize(Quaternion a)
    // Normalizes a given value to be unit length.
    static Vector4 Normalize(Vector4 a)
    // Normalizes a given value to be unit length.
    static Vector3 Normalize(Vector3 a)
    // Normalizes a given value to be unit length.
    static Vector2 Normalize(Vector2 a)
    // Normalizes a given value to be unit length.
    static float Normalize(float a)
    // The outer product of two coordinate vectors is a matrix
    static Matrix4x4 Outer(Vector3 a, Vector3 b)
    // Returns a specified value raised to the specified power.
    static Vector4 Pow(Vector4 a, float power)
    // Returns a specified value raised to the specified power.
    static Vector3 Pow(Vector3 a, float power)
    // Returns a specified value raised to the specified power.
    static Vector2 Pow(Vector2 a, float power)
    // Returns a specified value raised to the specified power.
    static float Pow(float a, float power)
    // Converts a given degree value into radians.
    static float Radians(float degrees)
    // Returns a new random number between the closed range [0,1].
    static float Random()
    // Returns a new random number between a closed range [0, maximum].
    static float Random(float maximum)
    // Returns a new random number between a closed range [0, maximum].
    static Vector2 Random(Vector2 maximum)
    // Returns a new random number between a closed range [0, maximum].
    static Vector3 Random(Vector3 maximum)
    // Returns a new random number between a closed range [0, maximum].
    static Vector4 Random(Vector4 maximum)
    // Rounds a given value to the nearest integer.
    static Vector4 Round(Vector4 a)
    // Rounds a given value to the nearest integer.
    static Vector3 Round(Vector3 a)
    // Rounds a given value to the nearest integer.
    static Vector2 Round(Vector2 a)
    // Rounds a given value to the nearest integer.
    static float Round(float value)
    // Rounds a given value to the nearest interval point (Round(a / interval) * interval).
    static Vector4 Round(Vector4 a, float interval)
    // Rounds a given value to the nearest interval point (Round(a / interval) * interval).
    static Vector3 Round(Vector3 a, float interval)
    // Rounds a given value to the nearest interval point (Round(a / interval) * interval).
    static Vector2 Round(Vector2 a, float interval)
    // Rounds a given value to the nearest interval point (Round(a / interval) * interval).
    static float Round(float a, float interval)
    // Saturates a given value to be between closed interval [0,1].
    static Vector4 Saturate(Vector4 a)
    // Saturates a given value to be between closed interval [0,1].
    static Vector3 Saturate(Vector3 a)
    // Saturates a given value to be between closed interval [0,1].
    static Vector2 Saturate(Vector2 a)
    // Saturates a given value to be between closed interval [0,1].
    static float Saturate(float a)
    // Returns the sine of a given value.
    static Vector4 Sin(Vector4 a)
    // Returns the sine of a given value.
    static Vector3 Sin(Vector3 a)
    // Returns the sine of a given value.
    static Vector2 Sin(Vector2 a)
    // Returns the sine of a given value.
    static float Sin(float a)
    // Returns the hyperbolic sine of a given value.
    static Vector4 Sinh(Vector4 a)
    // Returns the hyperbolic sine of a given value.
    static Vector3 Sinh(Vector3 a)
    // Returns the hyperbolic sine of a given value.
    static Vector2 Sinh(Vector2 a)
    // Returns the hyperbolic sine of a given value.
    static float Sinh(float a)
    // Interpolates between given values using spherical interpolation.
    static Quaternion Slerp(Quaternion value1, Quaternion value2, float weight)
    // Interpolates between given values using Smoothstep interpolation.
    static Vector4 SmoothStep(Vector4 value1, Vector4 value2, float weight)
    // Interpolates between given values using Smoothstep interpolation.
    static Vector3 SmoothStep(Vector3 value1, Vector3 value2, float weight)
    // Interpolates between given values using Smoothstep interpolation.
    static Vector2 SmoothStep(Vector2 value1, Vector2 value2, float weight)
    // Interpolates between given values using Smoothstep interpolation.
    static float SmoothStep(float value1, float value2, float weight)
    // Returns the square root of a given value.
    static Vector4 Sqrt(Vector4 a)
    // Returns the square root of a given value.
    static Vector3 Sqrt(Vector3 a)
    // Returns the square root of a given value.
    static Vector2 Sqrt(Vector2 a)
    // Returns the square root of a given value.
    static float Sqrt(float a)
    // Returns the sum of the components in a given value.
    static float Sum(Plane a)
    // Returns the sum of the components in a given value.
    static float Sum(Quaternion a)
    // Returns the sum of the components in a given value.
    static float Sum(Vector4 a)
    // Returns the sum of the components in a given value.
    static float Sum(Vector3 a)
    // Returns the sum of the components in a given value.
    static float Sum(Vector2 a)
    // Returns the sum of the components in a given value.
    static float Sum(float a)
    // Returns the tangent of a specified value, measured in radians.
    static Vector4 Tan(Vector4 a)
    // Returns the tangent of a specified value, measured in radians.
    static Vector3 Tan(Vector3 a)
    // Returns the tangent of a specified value, measured in radians.
    static Vector2 Tan(Vector2 a)
    // Returns the tangent of a specified value, measured in radians.
    static float Tan(float a)
    // Calculates the integral part of a given value.
    static Vector4 Trunc(Vector4 a)
    // Calculates the integral part of a given value.
    static Vector3 Trunc(Vector3 a)
    // Calculates the integral part of a given value.
    static Vector2 Trunc(Vector2 a)
    // Calculates the integral part of a given value.
    static float Trunc(float a)
    // Represents a mathematical constant whose value is half the PI.
    static float HalfPI
    // Represents a mathematical constant whose value is the ratio of a circle's circumference to its diameter in Euclidean space.
    static float PI
    // Represents a mathematical constant whose value is quarter the PI.
    static float QuarterPI
    // Represents a value for which all smaller absolute values are considered equal to zero.
    static float ZeroTolerance
  static class MathExtensions
    static Quaternion Divide(Quaternion value, float scale)
    static Plane Divide(Plane value1, Plane value2)
    static Plane Divide(Plane value, float scale)
    static Quaternion Multiply(Quaternion value, float scale)
    static Plane Multiply(Plane value1, Plane value2)
    static Plane Multiply(Plane value, float scale)
    // 4x4 Matrix class for 3D linear algebra. All: Col1 Col2 Col3 Col4 [M11 M12 M13 M14] Row1 [M21 M22 M23 M24] Row2 [M31 M32 M33 M34] Row3 [M41 M42 M43 M44] Row4 Rotation: [M11 M12 M13 - ] Right/X [M21 M22 M23 - ] Up/Y [M31 M32 M33 - ] Forward/Z [ - - - - ] Scale: [M11 - - - ] Scale Right/X [ - M22 - - ] Scale Up/Y [ - - M33 - ] Scale Forward/Z [ - - - - ] Translation: [ - - - - ] [ - - - - ] [ - - - - ] [M41 M42 M43 - ] Translation Right/X Up/Y Forward/Z
    static void Set(ref Matrix4x4 m, Vector3 x, Vector3 y, Vector3 z, Vector3 p)
    static Vector2 XY(Vector4 value)
    static Vector2 XY(Vector3 value)
    static Vector3 XYZ(Vector4 value)
    static Vector2 XZ(Vector3 value)
    static Vector2 YX(Vector4 value)
    static Vector2 YX(Vector3 value)
    static Vector2 YZ(Vector3 value)
    static Vector2 ZX(Vector3 value)
    static Vector3 ZXY(Vector4 value)
    static Vector2 ZY(Vector3 value)
  class OneEuroFilter
    ctor(float freq = 10, float mincutoff = 1, float beta = 0, float dcutoff = 1)
    float PrevValue { get; }
    float Value { get; }
    float Filter(float value, float timestamp = -1)
    void UpdateParams(float freq, float mincutoff = 1, float beta = 0, float dcutoff = 1)
  // Describes the result of an intersection with a plane in three dimensions.
  enum PlaneIntersectionType
    Back
    Front
    Intersecting
  // Represents a three dimensional line based on a point in space and a Direction.
  struct Ray : IEquatable<Ray>
    // Initializes a new ray with given Position and Direction values.
    ctor(Vector3 position, Vector3 direction)
    // The normalized Direction in which the ray points.
    Vector3 Direction
    // The Position in three dimensional space where the ray starts.
    Vector3 Position
