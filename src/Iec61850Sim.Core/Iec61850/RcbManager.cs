using IEC61850.Server;
using Iec61850Sim.Core.Points;

namespace Iec61850Sim.Core.Iec61850;

/// <summary>
/// Task 1 — Discovers and caches handles for DataSet members and RCBs.
///
/// DataSet member handles (DataAttribute)
///   Resolved upfront via IedModel.GetModelNodeByShortObjectReference, the .NET equivalent
///   of the C API IedModel_getModelNodeByObjectReference.  Each DevicePoint.Reference is the
///   short object reference produced by DataAttribute.GetObjectReference() (dot notation,
///   no IED-name prefix) and is suitable as-is for GetModelNodeByShortObjectReference.
///   Results are stored in a dictionary keyed by reference string.
///
/// RCB handles (ReportControlBlock)
///   ReportControlBlock does not inherit ModelNode in this .NET binding and cannot be
///   returned by GetModelNodeByShortObjectReference.  Handles are therefore populated
///   lazily from the SetRCBEventHandler callback the first time each RCB is seen
///   (see EnsureCached).  The cache is still keyed by a derived reference string so that
///   lookup semantics are consistent with the DataSet member cache.
/// </summary>
internal sealed class RcbManager
{
    // Task 1: DataSet member DA handle cache.
    // Key = DevicePoint.Reference (short object ref, dot notation, e.g. "Measurement/I3pMMXU1.TotW.mag.f").
    // Value = the DataAttribute handle resolved via GetModelNodeByShortObjectReference.
    private readonly Dictionary<string, DataAttribute> _memberHandleByRef
        = new(StringComparer.Ordinal);

    // Task 1 (lazy): RCB handle cache.
    // Key = "{ldName}/{lnName}/{rcbName}" derived from rcb.Parent hierarchy.
    // ReportControlBlock has no GetObjectReference() and is not a ModelNode subclass,
    // so handles are collected from the event callback instead of model traversal.
    private readonly Dictionary<string, ReportControlBlock> _rcbByKey
        = new(StringComparer.Ordinal);

    // DataSet name (rcb.DataSet) → owning LD name, used to scope GI refreshes.
    private readonly Dictionary<string, string> _dataSetToLd
        = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, ReportControlBlock> RcbByKey => _rcbByKey;

    /// <summary>
    /// Resolves all DataSet member DataAttribute handles at startup and stores them
    /// by reference string.  Must be called before IedServer.Start().
    /// </summary>
    public void Initialize(IedModel model, PointRegistry registry)
    {
        int resolved = 0;

        foreach (var point in registry.All)
        {
            if (point.ValueAttribute == null)
                continue;

            // GetModelNodeByShortObjectReference: resolve handle from string reference.
            // Returns null when the reference does not exist in the model (safe to skip).
            if (model.GetModelNodeByShortObjectReference(point.Reference) is DataAttribute da)
            {
                _memberHandleByRef[point.Reference] = da;
                resolved++;
            }
        }

        Console.WriteLine($"[RcbManager] Startup: {resolved}/{registry.All.Count()} " +
                          $"DataSet member handles resolved.");
    }

    /// <summary>
    /// Lazily caches an RCB the first time it appears in a SetRCBEventHandler callback.
    /// Also records the DataSet → LD mapping for GI scope resolution.
    /// </summary>
    public void EnsureCached(ReportControlBlock rcb)
    {
        // rcb.Parent is LogicalNode; rcb.Parent.GetParent() is LogicalDevice (a ModelNode).
        var ldName = (rcb.Parent.GetParent() as LogicalDevice)?.GetName() ?? string.Empty;
        var key = $"{ldName}/{rcb.Parent.GetName()}/{rcb.Name}";

        if (_rcbByKey.ContainsKey(key))
            return;

        _rcbByKey[key] = rcb;

        if (!string.IsNullOrEmpty(rcb.DataSet))
            _dataSetToLd[rcb.DataSet] = ldName;

        Console.WriteLine(
            $"[RcbManager] Lazy-cached RCB '{rcb.Name}' (LD={ldName}) DataSet='{rcb.DataSet}'");
    }

    /// <summary>
    /// Looks up a previously resolved DataAttribute handle by short reference string.
    /// Returns the handle from the cache, or falls back to the DevicePoint's ValueAttribute.
    /// </summary>
    public DataAttribute? GetMemberHandle(string reference) =>
        _memberHandleByRef.TryGetValue(reference, out var da) ? da : null;

    /// <summary>
    /// Returns the LD name that owns <paramref name="rcb"/>'s DataSet.
    /// Returns <c>null</c> for cross-LD DataSets; the caller falls back to publishing all points.
    /// </summary>
    public string? GetDataSetLd(ReportControlBlock rcb) =>
        _dataSetToLd.TryGetValue(rcb.DataSet ?? string.Empty, out var ld) ? ld : null;
}